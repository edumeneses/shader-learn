#!/usr/bin/env python3
"""Render an ISF shader on the local GPU, headless, to stills and clips.

Shader art is motion, so most figures in this course are clips rather than
screenshots. This renders them on the machine's own GPU with no window, no X
server, and no compositor in the way, which is what makes a figure
reproducible: the same command a year from now produces the same pixels.

How it gets to the GPU: an EGL context with no surface, created through
moderngl's egl backend. That path talks to libEGL_nvidia directly, so it works
over SSH and inside a service. Root-window screen capture, which is how the
score course takes its figures, cannot be used here at all; a GPU surface reads
back black under this compositor, which the score course records as a
hard-won fact and which is the reason this renderer exists.

Why the shaders are GLSL ES 3.00: the browser player on the lesson pages is
WebGL 2, which accepts nothing else, and desktop NVIDIA accepts ES shaders
through ARB_ES3_compatibility. One source string, two runtimes, no drift.

Encoding is NVENC where the format allows it. Raw RGBA frames are piped to
ffmpeg rather than written out as PNGs, because a six-second 1080p clip is
about 1.5 GB of intermediate files otherwise.

Examples
--------
A still, supersampled:

    python3 scripts/render.py library/shaders/03-circle.fs \\
        --out docs/learn/assets/03/03-01 --formats png \\
        --size 1600x900 --supersample 2 --time 2.0

A looping clip with one parameter swept across it:

    python3 scripts/render.py library/shaders/07-warp.fs \\
        --out docs/learn/assets/07/07-01 --formats mp4,gif,png \\
        --size 1280x720 --duration 8 --fps 30 \\
        --set softness=0.15 --sweep warp=0.0:1.0

A whole figure spec, which is what make_figures.py drives:

    python3 scripts/render.py --spec figures/07.json
"""

from __future__ import annotations

import argparse
import json
import math
import os
import shutil
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Iterable

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(Path(__file__).resolve().parent))

import isf  # noqa: E402

try:
    import moderngl
    import numpy as np
except ImportError as exc:  # pragma: no cover - the message is the point
    print(
        f"missing dependency: {exc}\n"
        f"activate the project venv first:  source {ROOT}/.venv/bin/activate",
        file=sys.stderr,
    )
    raise SystemExit(2)


FFMPEG = shutil.which("ffmpeg") or "ffmpeg"

# The full-render-size triangle. A triangle rather than two triangles: it has
# no shared edge for the rasteriser to sample twice, which matters for the
# derivative-based antialiasing several lessons use.
FULLSCREEN_TRIANGLE = np.array([-1.0, -1.0, 3.0, -1.0, -1.0, 3.0], dtype="f4")


# ---------------------------------------------------------------------------
# Parameter values and their automation
# ---------------------------------------------------------------------------

Keyframes = list[tuple[float, Any]]


def _lerp(a: Any, b: Any, t: float) -> Any:
    if isinstance(a, (list, tuple)):
        return [(_lerp(x, y, t)) for x, y in zip(a, b)]
    if isinstance(a, bool) or isinstance(b, bool):
        return b if t >= 0.5 else a
    return a + (b - a) * t


@dataclass
class Automation:
    """A parameter's value as a function of time, from linear keyframes."""

    keys: Keyframes

    def at(self, t: float) -> Any:
        keys = self.keys
        if not keys:
            return None
        if t <= keys[0][0]:
            return keys[0][1]
        if t >= keys[-1][0]:
            return keys[-1][1]
        for (t0, v0), (t1, v1) in zip(keys, keys[1:]):
            if t0 <= t <= t1:
                span = t1 - t0
                return v0 if span <= 0 else _lerp(v0, v1, (t - t0) / span)
        return keys[-1][1]


def parse_value(text: str) -> Any:
    """Read a --set value: a number, a bool, or a comma-separated vector."""
    text = text.strip()
    low = text.lower()
    if low in {"true", "false"}:
        return low == "true"
    if "," in text:
        return [float(p) for p in text.split(",")]
    if text.startswith("#"):
        return _hex_colour(text)
    return float(text)


def _hex_colour(text: str) -> list[float]:
    h = text.lstrip("#")
    if len(h) == 6:
        h += "ff"
    if len(h) != 8:
        raise ValueError(f"colour {text!r} is not #rrggbb or #rrggbbaa")
    return [int(h[i : i + 2], 16) / 255.0 for i in (0, 2, 4, 6)]


# ---------------------------------------------------------------------------
# Audio, for the audio-reactive lessons
# ---------------------------------------------------------------------------


def audio_textures(path: Path | None, duration: float, fps: int,
                   bins: int = 256) -> tuple[np.ndarray, np.ndarray] | None:
    """Waveform and spectrum rows, one per frame, for ISF audio inputs.

    Returns (waveform, spectrum), each shaped (frames, bins), values in 0..1.
    With no file, returns None and the caller feeds a synthetic signal, so a
    lesson figure can demonstrate an audio-reactive shader without shipping a
    sound file for every one of them.
    """
    if path is None:
        return None
    rate = 48000
    raw = subprocess.run(
        [FFMPEG, "-v", "error", "-i", str(path), "-f", "f32le",
         "-ac", "1", "-ar", str(rate), "-"],
        check=True, stdout=subprocess.PIPE,
    ).stdout
    samples = np.frombuffer(raw, dtype="<f4")
    frames = int(round(duration * fps))
    window = 2 * bins
    wave = np.zeros((frames, bins), dtype="f4")
    spec = np.zeros((frames, bins), dtype="f4")
    hann = np.hanning(window).astype("f4")
    for i in range(frames):
        start = int(i / fps * rate)
        chunk = samples[start : start + window]
        if chunk.size < window:
            chunk = np.pad(chunk, (0, window - chunk.size))
        wave[i] = chunk[:bins] * 0.5 + 0.5
        mags = np.abs(np.fft.rfft(chunk * hann))[:bins]
        # dB, then squashed into 0..1. Linear magnitude makes every figure look
        # like a flat line with one spike at the fundamental.
        db = 20.0 * np.log10(mags + 1e-6)
        spec[i] = np.clip((db + 60.0) / 60.0, 0.0, 1.0)
    return wave, spec


def synthetic_audio(duration: float, fps: int, bins: int = 256
                    ) -> tuple[np.ndarray, np.ndarray]:
    """A four-on-the-floor envelope with a swept tone, for figures with no file.

    Deterministic on purpose: an audio-reactive figure has to be reproducible
    like every other figure, and a random signal would make it a lottery.
    """
    frames = int(round(duration * fps))
    t = np.arange(frames) / fps
    beat = np.exp(-6.0 * ((t * 2.0) % 1.0))            # 120 bpm decay envelope
    sweep = 0.15 + 0.35 * (0.5 + 0.5 * np.sin(t * 0.7))
    idx = np.arange(bins) / bins
    spec = np.zeros((frames, bins), dtype="f4")
    wave = np.zeros((frames, bins), dtype="f4")
    for i in range(frames):
        centre = sweep[i]
        body = np.exp(-((idx - centre) ** 2) / 0.004)
        low = np.exp(-idx / 0.03) * beat[i]
        spec[i] = np.clip(0.85 * low + 0.6 * body * beat[i], 0.0, 1.0)
        phase = 2.0 * math.pi * (idx * 24.0 + t[i] * 3.0)
        wave[i] = 0.5 + 0.45 * np.sin(phase) * beat[i]
    return wave, spec


# ---------------------------------------------------------------------------
# The renderer
# ---------------------------------------------------------------------------


@dataclass
class Target:
    """A pass's render target, double-buffered when it is PERSISTENT."""

    name: str
    width: int
    height: int
    float_buffer: bool
    persistent: bool
    ctx: Any
    front: Any = None
    back: Any = None
    fbo_front: Any = None
    fbo_back: Any = None

    def build(self) -> None:
        dtype = "f4" if self.float_buffer else "f1"
        self.front = self.ctx.texture((self.width, self.height), 4, dtype=dtype)
        self.front.filter = (moderngl.LINEAR, moderngl.LINEAR)
        self.front.repeat_x = self.front.repeat_y = False
        self.fbo_front = self.ctx.framebuffer(color_attachments=[self.front])
        self.fbo_front.clear(0.0, 0.0, 0.0, 0.0)
        if self.persistent:
            self.back = self.ctx.texture((self.width, self.height), 4, dtype=dtype)
            self.back.filter = (moderngl.LINEAR, moderngl.LINEAR)
            self.back.repeat_x = self.back.repeat_y = False
            self.fbo_back = self.ctx.framebuffer(color_attachments=[self.back])
            self.fbo_back.clear(0.0, 0.0, 0.0, 0.0)

    def swap(self) -> None:
        if self.persistent:
            self.front, self.back = self.back, self.front
            self.fbo_front, self.fbo_back = self.fbo_back, self.fbo_front

    @property
    def read_texture(self) -> Any:
        # A persistent target reads what it wrote last frame, which is the
        # whole point of the flag; a plain target reads what it holds now.
        return self.back if self.persistent else self.front


class Renderer:
    def __init__(self, shader_path: Path, width: int, height: int,
                 supersample: int = 1) -> None:
        self.shader_path = shader_path
        self.shader = isf.load(shader_path)
        if self.shader.is_compute:
            raise SystemExit(
                f"{shader_path} is a compute shader. Render it with "
                f"scripts/render_compute.py, which dispatches instead of drawing."
            )
        self.compiled = isf.compile_shader(self.shader, name=shader_path.stem)
        self.width = width * supersample
        self.height = height * supersample
        self.out_width = width
        self.out_height = height
        self.supersample = supersample

        self.ctx = moderngl.create_context(standalone=True, backend="egl", require=460)
        self.renderer_name = self.ctx.info.get("GL_RENDERER", "unknown")

        try:
            self.program = self.ctx.program(
                vertex_shader=self.compiled.vertex,
                fragment_shader=self.compiled.fragment,
            )
        except Exception as exc:
            raise SystemExit(_shader_error(shader_path, self.compiled, exc)) from exc

        vbo = self.ctx.buffer(FULLSCREEN_TRIANGLE.tobytes())
        self.vao = self.ctx.vertex_array(self.program, [(vbo, "2f", "isf_position")])

        self.targets: dict[str, Target] = {}
        sizes: dict[str, tuple[int, int]] = {}
        for p in self.shader.passes:
            if not p.target or p.target in self.targets:
                # Several passes may name the same target, which is how a
                # simulation steps more than once per frame. One buffer.
                continue
            w = isf.eval_size(p.width, self.width, self.height, sizes)
            h = isf.eval_size(p.height, self.width, self.height, sizes)
            sizes[p.target] = (w, h)
            target = Target(p.target, w, h, p.float_buffer, p.persistent, self.ctx)
            target.build()
            self.targets[p.target] = target

        self.screen = self.ctx.texture((self.width, self.height), 4, dtype="f1")
        self.screen_fbo = self.ctx.framebuffer(color_attachments=[self.screen])

        # True when any pass writes a target it will read again next frame.
        # The caller needs this: such a shader has to be stepped from frame
        # zero rather than sampled at an instant.
        self.has_state = any(p.persistent for p in self.shader.passes)

        self.image_units: dict[str, Any] = {}
        self._black = self.ctx.texture((1, 1), 4, data=b"\x00\x00\x00\xff")
        self.audio_tex: dict[str, Any] = {}

    # -- uniforms ----------------------------------------------------------

    def _set(self, name: str, value: Any) -> None:
        """Write a uniform, coercing to what the program actually declared.

        The coercion is not a convenience. A command line says `--set space=0`
        and cannot know whether `space` is a float or an int, and moderngl
        raises rather than converting; without this, an ISF `long` input is
        unsettable from the shell and the figure silently renders its default.
        """
        member = self.program.get(name, None)
        if member is None:
            return  # the compiler dropped an unused uniform; not an error
        fmt = getattr(member, "fmt", "")
        try:
            if fmt.endswith("i") or fmt.endswith("I"):
                if isinstance(value, (list, tuple)):
                    member.value = tuple(int(round(float(v))) for v in value)
                else:
                    member.value = int(round(float(value)))
            elif fmt.endswith("f"):
                count = int(fmt[0]) if fmt and fmt[0].isdigit() else 1
                if isinstance(value, (list, tuple)):
                    member.value = tuple(float(v) for v in value[:count])
                elif count == 1:
                    member.value = float(value)
                else:
                    member.value = tuple([float(value)] * count)
            elif isinstance(value, (list, tuple)):
                member.value = tuple(value)
            else:
                member.value = value
        except Exception as exc:
            raise SystemExit(
                f"cannot set uniform {name!r} (declared as {fmt!r}) to {value!r}: {exc}"
            )

    def _bind_images(self) -> None:
        unit = 0
        for name, tex in self.image_units.items():
            member = self.program.get(f"_{name}", None)
            if member is None:
                continue
            tex.use(unit)
            member.value = unit
            self._set(f"_{name}_imgSize", [float(tex.width), float(tex.height)])
            self._set(f"_{name}_flip", False)
            unit += 1

    # -- the frame loop ----------------------------------------------------

    def render_frame(self, time_s: float, dt: float, frame_index: int,
                     values: dict[str, Any]) -> bytes:
        for name, value in values.items():
            self._set(name, value)

        self._set("RENDERSIZE", [float(self.width), float(self.height)])
        self._set("TIME", float(time_s))
        self._set("TIMEDELTA", float(dt))
        self._set("FRAMEINDEX", int(frame_index))
        self._set("DATE", [2026.0, 1.0, 1.0, float(time_s)])

        passes = self.shader.passes
        for index, p in enumerate(passes):
            self._set("PASSINDEX", index)

            # Every image the shader can read: declared inputs, imports, and
            # the targets of earlier passes.
            self.image_units = dict(self.audio_tex)
            for name, target in self.targets.items():
                self.image_units[name] = target.read_texture
            self._bind_images()

            if p.target:
                target = self.targets[p.target]
                fbo = target.fbo_front
                self._set("RENDERSIZE", [float(target.width), float(target.height)])
            else:
                fbo = self.screen_fbo
                self._set("RENDERSIZE", [float(self.width), float(self.height)])
            fbo.use()
            self.ctx.clear(0.0, 0.0, 0.0, 0.0)
            self.vao.render(moderngl.TRIANGLES)

            # A persistent target swaps immediately after the pass that wrote
            # it, not at the end of the frame. Two consequences, both wanted:
            # a later pass in the same frame reads what was just written rather
            # than last frame's copy, and several passes naming the same target
            # perform several real steps instead of overwriting each other.
            # A simulation that needs more than one step per displayed frame,
            # which Gray-Scott does, is impossible without this.
            if p.target:
                self.targets[p.target].swap()

        # A shader whose last pass wrote to a target rather than to the screen
        # is still expected to show something: ISF's convention is that the
        # final pass draws to the output, so this reads the screen buffer, and
        # a shader that leaves it empty is a bug in the shader, not here.
        return self._read_downscaled()

    def _read_downscaled(self) -> bytes:
        data = self.screen_fbo.read(components=4, alignment=1)
        if self.supersample == 1:
            return _flip_rows(data, self.width, self.height)
        arr = np.frombuffer(data, dtype=np.uint8).reshape(
            self.height, self.width, 4
        )
        s = self.supersample
        arr = arr.reshape(
            self.out_height, s, self.out_width, s, 4
        ).mean(axis=(1, 3)).astype(np.uint8)
        return np.flipud(arr).tobytes()

    def attach_audio(self, wave: np.ndarray, spec: np.ndarray) -> None:
        """Upload one frame's audio rows into the shader's audio inputs."""
        for inp in self.shader.inputs:
            if inp.type == "audio":
                self._upload_audio(inp.name, wave)
            elif inp.type == "audioFFT":
                self._upload_audio(inp.name, spec)

    def _upload_audio(self, name: str, row: np.ndarray) -> None:
        bins = row.shape[0]
        rgba = np.repeat(row.astype("f4")[:, None], 4, axis=1)
        tex = self.audio_tex.get(name)
        if tex is None or tex.width != bins:
            tex = self.ctx.texture((bins, 1), 4, dtype="f4")
            tex.filter = (moderngl.LINEAR, moderngl.LINEAR)
            self.audio_tex[name] = tex
        tex.write(rgba.tobytes())


def _flip_rows(data: bytes, width: int, height: int) -> bytes:
    """OpenGL reads bottom-up; every consumer downstream wants top-down."""
    arr = np.frombuffer(data, dtype=np.uint8).reshape(height, width, 4)
    return np.flipud(arr).tobytes()


def _shader_error(path: Path, compiled: isf.CompiledShader, exc: Exception) -> str:
    """Report a compile failure against numbered lines of the generated source.

    The line numbers the driver reports are in the translated shader, not in
    the ISF file, and they are useless without it. Printing the numbered source
    turns a two-minute hunt into a glance.
    """
    text = str(exc)
    lines = compiled.fragment.splitlines()
    numbered = "\n".join(f"{i + 1:4d} | {line}" for i, line in enumerate(lines))
    return f"{path}: shader did not compile\n\n{text}\n\n--- generated fragment ---\n{numbered}"


# ---------------------------------------------------------------------------
# Encoding
# ---------------------------------------------------------------------------


def encode_mp4(frames: Iterable[bytes], out: Path, width: int, height: int,
               fps: int, quality: int = 20, nvenc: bool = True) -> None:
    """H.264 through NVENC, faststart, yuv420p so every browser plays it."""
    codec = (
        ["-c:v", "h264_nvenc", "-preset", "p6", "-tune", "hq",
         "-rc", "vbr", "-cq", str(quality), "-b:v", "0"]
        if nvenc
        else ["-c:v", "libx264", "-preset", "slow", "-crf", str(quality)]
    )
    cmd = [
        FFMPEG, "-v", "error", "-y",
        "-f", "rawvideo", "-pix_fmt", "rgba",
        "-s", f"{width}x{height}", "-r", str(fps), "-i", "-",
        *codec,
        "-pix_fmt", "yuv420p",
        "-movflags", "+faststart",
        str(out),
    ]
    _pipe(cmd, frames, out)


def encode_gif(source_mp4: Path, out: Path, fps: int, width: int) -> None:
    """Two-pass palette GIF.

    One pass with a shared 256-colour palette is what separates a readable
    gradient from the banded mess a default GIF encode produces, and gradients
    are most of what these figures are.
    """
    filters = (
        f"fps={fps},scale={width}:-1:flags=lanczos,"
        f"split[a][b];[a]palettegen=stats_mode=diff[p];"
        f"[b][p]paletteuse=dither=bayer:bayer_scale=4:diff_mode=rectangle"
    )
    subprocess.run(
        [FFMPEG, "-v", "error", "-y", "-i", str(source_mp4),
         "-filter_complex", filters, "-loop", "0", str(out)],
        check=True,
    )


def encode_webm(source_mp4: Path, out: Path, quality: int = 32) -> None:
    """VP9, for the browsers that will not play an H.264 in a <video> tag."""
    subprocess.run(
        [FFMPEG, "-v", "error", "-y", "-i", str(source_mp4),
         "-c:v", "libvpx-vp9", "-crf", str(quality), "-b:v", "0",
         "-row-mt", "1", "-pix_fmt", "yuv420p", str(out)],
        check=True,
    )


def _pipe(cmd: list[str], frames: Iterable[bytes], out: Path) -> None:
    out.parent.mkdir(parents=True, exist_ok=True)
    proc = subprocess.Popen(cmd, stdin=subprocess.PIPE)
    assert proc.stdin is not None
    try:
        for frame in frames:
            proc.stdin.write(frame)
    finally:
        proc.stdin.close()
        code = proc.wait()
    if code:
        raise SystemExit(f"ffmpeg failed with exit code {code} writing {out}")


def write_png(frame: bytes, out: Path, width: int, height: int,
              max_width: int | None = None) -> None:
    """Write a frame as a PNG, optionally downscaled.

    A poster is only ever seen before a clip plays, so a clip's poster is
    written at `max_width` rather than at full size. It matters: a PNG of a
    noise field does not compress, and a 1280-wide poster for an eight-second
    figure was larger than the H.264 clip it was a poster for.
    """
    from PIL import Image

    out.parent.mkdir(parents=True, exist_ok=True)
    image = Image.frombytes("RGBA", (width, height), frame).convert("RGB")
    if max_width and width > max_width:
        height = max(1, round(height * max_width / width))
        image = image.resize((max_width, height), Image.LANCZOS)
    image.save(out, optimize=True)


# ---------------------------------------------------------------------------
# Driving it
# ---------------------------------------------------------------------------


@dataclass
class Job:
    shader: Path
    out: Path
    size: tuple[int, int] = (1280, 720)
    duration: float = 6.0
    fps: int = 30
    formats: list[str] = field(default_factory=lambda: ["mp4", "gif", "png"])
    supersample: int = 1
    poster_time: float | None = None
    values: dict[str, Any] = field(default_factory=dict)
    automation: dict[str, Automation] = field(default_factory=dict)
    audio: Path | None = None
    synth_audio: bool = False
    gif_fps: int = 15
    gif_width: int = 640
    quality: int = 20
    nvenc: bool = True
    # Posters for clips are downscaled; a still figure is written full size.
    poster_width: int | None = 960
    # Frames to run before the first recorded one, for a shader whose picture
    # takes a moment to build. Only meaningful with a persistent pass.
    settle: int = 0


def defaults_for(shader: isf.ISFShader) -> dict[str, Any]:
    values: dict[str, Any] = {}
    for inp in shader.inputs:
        if inp.is_image:
            continue
        if inp.default is not None:
            values[inp.name] = inp.default
        elif inp.type == "point2D":
            values[inp.name] = [0.5, 0.5]
        elif inp.type == "color":
            values[inp.name] = [1.0, 1.0, 1.0, 1.0]
        elif inp.type in {"bool", "event"}:
            values[inp.name] = False
        elif inp.type == "long":
            values[inp.name] = int(inp.values[0]) if inp.values else 0
        else:
            lo = float(inp.minimum) if inp.minimum is not None else 0.0
            hi = float(inp.maximum) if inp.maximum is not None else 1.0
            values[inp.name] = (lo + hi) * 0.5
    return values


def run(job: Job) -> list[Path]:
    renderer = Renderer(job.shader, job.size[0], job.size[1], job.supersample)
    print(f"GPU: {renderer.renderer_name}")

    values = defaults_for(renderer.shader)
    values.update(job.values)

    frames = max(1, int(round(job.duration * job.fps)))
    dt = 1.0 / job.fps

    audio = None
    if job.audio is not None:
        audio = audio_textures(job.audio, job.duration, job.fps)
    elif job.synth_audio or any(
        i.type in {"audio", "audioFFT"} for i in renderer.shader.inputs
    ):
        audio = synthetic_audio(job.duration, job.fps)

    written: list[Path] = []
    want_clip = any(f in job.formats for f in ("mp4", "gif", "webm"))
    poster_frame: bytes | None = None
    poster_index = (
        int(round((job.poster_time if job.poster_time is not None else job.duration * 0.5)
                  * job.fps))
    )
    poster_index = min(max(poster_index, 0), frames - 1)

    def frame_stream() -> Iterable[bytes]:
        nonlocal poster_frame
        for i in range(-job.settle, 0):
            t = i / job.fps
            frame_values = dict(values)
            for name, auto in job.automation.items():
                frame_values[name] = auto.at(0.0)
            renderer.render_frame(t, dt, i, frame_values)
        for i in range(frames):
            t = i / job.fps
            frame_values = dict(values)
            for name, auto in job.automation.items():
                frame_values[name] = auto.at(t)
            if audio is not None:
                renderer.attach_audio(audio[0][i], audio[1][i])
            data = renderer.render_frame(t, dt, i, frame_values)
            if i == poster_index:
                poster_frame = data
            yield data

    if want_clip:
        mp4 = job.out.with_suffix(".mp4")
        encode_mp4(frame_stream(), mp4, job.size[0], job.size[1],
                   job.fps, job.quality, job.nvenc)
        if "mp4" in job.formats:
            written.append(mp4)
        if "gif" in job.formats:
            gif = job.out.with_suffix(".gif")
            encode_gif(mp4, gif, job.gif_fps, job.gif_width)
            written.append(gif)
        if "webm" in job.formats:
            webm = job.out.with_suffix(".webm")
            encode_webm(mp4, webm)
            written.append(webm)
        if "mp4" not in job.formats:
            mp4.unlink(missing_ok=True)
    elif "png" in job.formats:
        target_time = job.poster_time if job.poster_time is not None else 0.0
        target = int(round(target_time * job.fps))

        # A shader with a persistent pass cannot be sampled at an instant. Its
        # frame depends on every frame before it, so a still of a feedback
        # shader has to be *stepped* to, not jumped to. Rendering one frame of
        # such a shader gives an almost empty buffer, which looks like a broken
        # shader rather than like a missing simulation.
        start = 0 if renderer.has_state else target
        for i in range(start, target + 1):
            t = i / job.fps
            if audio is not None:
                renderer.attach_audio(audio[0][min(i, frames - 1)],
                                      audio[1][min(i, frames - 1)])
            frame_values = dict(values)
            for name, auto in job.automation.items():
                frame_values[name] = auto.at(t)
            poster_frame = renderer.render_frame(t, dt, i, frame_values)

    if "png" in job.formats and poster_frame is not None:
        png = job.out.with_suffix(".png")
        write_png(poster_frame, png, job.size[0], job.size[1],
                  job.poster_width if want_clip else None)
        written.append(png)

    return written


def job_from_spec(spec: dict[str, Any], base: Path) -> Job:
    shader = Path(spec["shader"])
    if not shader.is_absolute():
        shader = (base / shader).resolve()
    out = Path(spec["out"])
    if not out.is_absolute():
        out = (ROOT / out).resolve()
    size = spec.get("size", [1280, 720])

    automation = {
        name: Automation([(float(k[0]), k[1]) for k in keys])
        for name, keys in (spec.get("automation") or {}).items()
    }
    audio = spec.get("audio")
    return Job(
        shader=shader,
        out=out,
        size=(int(size[0]), int(size[1])),
        duration=float(spec.get("duration", 6.0)),
        fps=int(spec.get("fps", 30)),
        formats=list(spec.get("formats", ["mp4", "gif", "png"])),
        supersample=int(spec.get("supersample", 1)),
        poster_time=spec.get("poster_time"),
        values=dict(spec.get("set") or {}),
        automation=automation,
        audio=(ROOT / audio) if audio else None,
        synth_audio=bool(spec.get("synth_audio", False)),
        gif_fps=int(spec.get("gif_fps", 15)),
        gif_width=int(spec.get("gif_width", 640)),
        quality=int(spec.get("quality", 20)),
        nvenc=not bool(spec.get("no_nvenc", False)),
        poster_width=spec.get("poster_width", 960),
        settle=int(spec.get("settle", 0)),
    )


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("shader", nargs="?", type=Path, help="an .fs ISF shader")
    ap.add_argument("--spec", type=Path, help="a figure spec, one or many jobs")
    ap.add_argument("--out", type=Path, help="output path without an extension")
    ap.add_argument("--size", default="1280x720")
    ap.add_argument("--duration", type=float, default=6.0)
    ap.add_argument("--fps", type=int, default=30)
    ap.add_argument("--formats", default="mp4,gif,png")
    ap.add_argument("--supersample", type=int, default=1,
                    help="render at N times the size and average down")
    ap.add_argument("--time", type=float, dest="poster_time",
                    help="the instant a still is taken from")
    ap.add_argument("--set", action="append", default=[], metavar="NAME=VALUE")
    ap.add_argument("--sweep", action="append", default=[], metavar="NAME=A:B",
                    help="linear ramp across the whole clip")
    ap.add_argument("--audio", type=Path, help="drive audio inputs from a file")
    ap.add_argument("--synth-audio", action="store_true")
    ap.add_argument("--gif-fps", type=int, default=15)
    ap.add_argument("--gif-width", type=int, default=640)
    ap.add_argument("--quality", type=int, default=20,
                    help="NVENC constant quality; higher is smaller")
    ap.add_argument("--poster-width", type=int, default=960,
                    help="downscale a clip's poster to this width")
    ap.add_argument("--settle", type=int, default=0,
                    help="frames to run before recording, for a stateful shader")
    ap.add_argument("--no-nvenc", action="store_true",
                    help="encode on the CPU; use when NVENC sessions are exhausted")
    args = ap.parse_args(argv)

    jobs: list[Job] = []
    if args.spec:
        spec = json.loads(args.spec.read_text(encoding="utf8"))
        entries = spec if isinstance(spec, list) else spec.get("figures", [spec])
        for entry in entries:
            jobs.append(job_from_spec(entry, args.spec.parent))
    else:
        if not args.shader or not args.out:
            ap.error("give a shader and --out, or a --spec")
        w, _, h = args.size.partition("x")
        values: dict[str, Any] = {}
        for item in args.set:
            name, _, raw = item.partition("=")
            values[name] = parse_value(raw)
        automation: dict[str, Automation] = {}
        for item in args.sweep:
            name, _, raw = item.partition("=")
            start, _, end = raw.partition(":")
            automation[name] = Automation(
                [(0.0, parse_value(start)), (args.duration, parse_value(end))]
            )
        jobs.append(Job(
            shader=args.shader,
            out=args.out,
            size=(int(w), int(h)),
            duration=args.duration,
            fps=args.fps,
            formats=[f.strip() for f in args.formats.split(",") if f.strip()],
            supersample=args.supersample,
            poster_time=args.poster_time,
            values=values,
            automation=automation,
            audio=args.audio,
            synth_audio=args.synth_audio,
            gif_fps=args.gif_fps,
            gif_width=args.gif_width,
            quality=args.quality,
            nvenc=not args.no_nvenc,
            poster_width=args.poster_width,
            settle=args.settle,
        ))

    for job in jobs:
        written = run(job)
        for path in written:
            size_kb = path.stat().st_size / 1024
            print(f"  {path.relative_to(ROOT) if ROOT in path.parents else path}"
                  f"  {size_kb:.0f} kB")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
