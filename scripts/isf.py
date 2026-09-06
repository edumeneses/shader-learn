#!/usr/bin/env python3
"""Parse and compile Interactive Shader Format (ISF) shaders.

This module is the course's single source of truth for what an ISF shader
means. Everything downstream reads it: the headless GPU renderer that makes
the figures, the browser player that runs the same shader live on a lesson
page, and the structural checks that refuse to publish a shader which does
not compile. Writing the translation once is what keeps the three in
agreement; an earlier design parsed ISF again in JavaScript, and any
divergence between the two parsers would have shown up as a figure that does
not match the live player beside it.

The output is GLSL ES 3.00, for one reason: WebGL 2 accepts nothing else, and
desktop OpenGL on this machine accepts it through ARB_ES3_compatibility. One
source string therefore runs unmodified in the browser and in the offline
renderer, so a figure is a recording of the player rather than a lookalike.

ISF itself is specified at <https://isf.video/>. What is implemented here:

  * the JSON header in a leading `/*{ ... }*/` comment;
  * INPUTS of type bool, long, float, point2D, color, image, audio, audioFFT
    and event, with their DEFAULT, MIN, MAX, VALUES and LABELS;
  * PASSES, including TARGET, PERSISTENT, FLOAT, and WIDTH/HEIGHT expressions
    in terms of $WIDTH and $HEIGHT;
  * IMPORTED images;
  * the automatic uniforms RENDERSIZE, TIME, TIMEDELTA, DATE, FRAMEINDEX and
    PASSINDEX, plus isf_FragNormCoord;
  * the IMG_PIXEL, IMG_NORM_PIXEL, IMG_THIS_PIXEL, IMG_THIS_NORM_PIXEL and
    IMG_SIZE accessors;
  * MODE: COMPUTE_SHADER, recognised and reported, since ossia score's
    compute variant is part of the course. Compute shaders are not rendered
    by the WebGL player, which has no compute stage; they are rendered
    offline instead.

Usage:
    from isf import parse, compile_shader
    shader = parse(Path("library/shaders/01-gradient.fs").read_text())
    program = compile_shader(shader)   # .vertex, .fragment, .manifest
"""

from __future__ import annotations

import json
import math
import re
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

# ---------------------------------------------------------------------------
# Header extraction
# ---------------------------------------------------------------------------

# ISF puts its JSON in the first block comment. The comment is not necessarily
# the very first thing in the file: shaders exported from other tools often
# carry a licence banner above it.
HEADER_RE = re.compile(r"/\*\s*(\{.*?\})\s*\*/", re.S)

VALUE_TYPES = {"bool", "long", "float", "point2D", "color", "event"}

# Words GLSL ES 3.00 will not let an input be named. An ISF header is JSON, so
# nothing stops a shader declaring an input called `flat` or `sample`; the name
# then becomes a uniform declaration and the shader fails to compile with a
# syntax error pointing at the line after it, which reads as a missing
# semicolon. Worse, the NVIDIA driver accepts several of these, so a shader can
# render correctly on the machine the figures are made on and fail in every
# browser. Caught here, at parse time, with the offending name said out loud.
GLSL_RESERVED = frozenset("""
attribute varying flat smooth noperspective centroid sample patch subroutine
common partition active asm class union enum typedef template this packed
resource goto inline noinline public static extern external interface
long short double half fixed unsigned superp input output hvec2 hvec3 hvec4
fvec2 fvec3 fvec4 sampler3DRect filter image1D image2D image3D imageCube
iimage1D iimage2D iimage3D iimageCube uimage1D uimage2D uimage3D uimageCube
image1DArray image2DArray namespace using row_major
const uniform buffer shared coherent volatile restrict readonly writeonly
atomic_uint layout precise break continue do for while switch case default
if else in out inout float int void bool true false invariant discard return
mat2 mat3 mat4 vec2 vec3 vec4 ivec2 ivec3 ivec4 bvec2 bvec3 bvec4 uint uvec2
uvec3 uvec4 lowp mediump highp precision struct sampler2D sampler3D samplerCube
""".split())

# Built-in function names. Declaring a uniform called `round` or `mix` is a
# redefinition, not a shadowing, and glslang says so; the NVIDIA driver accepts
# several of them, which is how `round` reached CI in the first place. Kept in a
# separate set so the message can say "built-in" rather than "reserved word",
# which is the difference between a reader renaming the input and a reader
# wondering what is reserved about `round`.
GLSL_BUILTINS = frozenset("""
radians degrees sin cos tan asin acos atan sinh cosh tanh asinh acosh atanh
pow exp log exp2 log2 sqrt inversesqrt abs sign floor trunc round roundEven
ceil fract mod modf min max clamp mix step smoothstep isnan isinf
floatBitsToInt floatBitsToUint intBitsToFloat uintBitsToFloat
length distance dot cross normalize faceforward reflect refract
matrixCompMult outerProduct transpose determinant inverse
lessThan lessThanEqual greaterThan greaterThanEqual equal notEqual any all not
textureSize texture textureProj textureLod textureOffset texelFetch
texelFetchOffset textureProjOffset textureLodOffset textureProjLod
textureProjLodOffset textureGrad textureGradOffset textureProjGrad
textureProjGradOffset dFdx dFdy fwidth
packSnorm2x16 unpackSnorm2x16 packUnorm2x16 unpackUnorm2x16
packHalf2x16 unpackHalf2x16
""".split())

# Names the ISF preamble already declares. An input with one of these silently
# replaces the host's own uniform, which is worse than a compile error: the
# shader builds and TIME stops advancing.
ISF_SUPPLIED = frozenset(
    "RENDERSIZE TIME TIMEDELTA DATE FRAMEINDEX PASSINDEX "
    "isf_FragNormCoord isf_FragCoord isf_FragColor isf_position isf_texel".split()
)
IMAGE_TYPES = {"image", "audio", "audioFFT"}


class ISFError(ValueError):
    """A shader that is not valid ISF, reported with the file it came from."""


@dataclass
class ISFInput:
    name: str
    type: str
    label: str = ""
    default: Any = None
    minimum: Any = None
    maximum: Any = None
    values: list[Any] = field(default_factory=list)
    labels: list[str] = field(default_factory=list)
    identity: Any = None
    # Compute-only, from ossia score's RESOURCES block.
    image_format: str | None = None
    access: str | None = None
    width_expr: str | None = None
    height_expr: str | None = None

    @property
    def is_image(self) -> bool:
        return self.type in IMAGE_TYPES

    def as_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"name": self.name, "type": self.type}
        if self.label:
            out["label"] = self.label
        for key, value in (
            ("default", self.default),
            ("min", self.minimum),
            ("max", self.maximum),
            ("identity", self.identity),
        ):
            if value is not None:
                out[key] = value
        if self.values:
            out["values"] = self.values
        if self.labels:
            out["labels"] = self.labels
        return out


@dataclass
class ISFPass:
    target: str | None = None
    persistent: bool = False
    float_buffer: bool = False
    width: str | None = None
    height: str | None = None
    # Compute-shader passes carry these instead; see ossia score's CSF variant.
    local_size: list[int] | None = None
    execution_model: dict[str, Any] | None = None

    def as_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {}
        if self.target:
            out["target"] = self.target
        if self.persistent:
            out["persistent"] = True
        if self.float_buffer:
            out["float"] = True
        if self.width:
            out["width"] = self.width
        if self.height:
            out["height"] = self.height
        if self.local_size:
            out["localSize"] = self.local_size
        if self.execution_model:
            out["executionModel"] = self.execution_model
        return out


@dataclass
class ISFShader:
    header: dict[str, Any]
    body: str
    source: str
    path: Path | None = None
    inputs: list[ISFInput] = field(default_factory=list)
    passes: list[ISFPass] = field(default_factory=list)
    imported: dict[str, str] = field(default_factory=dict)
    vertex_body: str | None = None

    @property
    def mode(self) -> str:
        return str(self.header.get("MODE", "")).upper()

    @property
    def is_compute(self) -> bool:
        return self.mode == "COMPUTE_SHADER"

    @property
    def is_vsa(self) -> bool:
        """A Vertex Shader Art shader, in ossia score's spelling.

        VSA inverts ISF: the shader is a vertex shader that decides where each
        of many points lands and what colour it is, and the fragment stage is
        fixed. It is the only way a fragment-based pipeline can scatter, which
        is what Unit 19 said a particle system needs.
        """
        return self.mode in ("VERTEX_SHADER_ART", "VERTEX_SHADER")

    @property
    def point_count(self) -> int:
        return int(self.header.get("POINT_COUNT", 10000))

    @property
    def primitive(self) -> str:
        return str(self.header.get("PRIMITIVE_MODE", "POINTS")).upper()

    @property
    def background(self) -> list[float]:
        bg = self.header.get("BACKGROUND_COLOR") or [0.0, 0.0, 0.0, 1.0]
        return [float(v) for v in bg]

    @property
    def description(self) -> str:
        return str(self.header.get("DESCRIPTION", ""))

    @property
    def credit(self) -> str:
        return str(self.header.get("CREDIT", ""))

    @property
    def categories(self) -> list[str]:
        return list(self.header.get("CATEGORIES", []))


def _as_bool(value: Any) -> bool:
    if isinstance(value, str):
        return value.strip().lower() in {"true", "yes", "1"}
    return bool(value)


def parse(source: str, path: Path | None = None) -> ISFShader:
    """Read an ISF source string into a shader description.

    Raises ISFError with a readable message rather than a JSONDecodeError,
    because the usual mistake is a trailing comma in the header and the raw
    exception does not say which file it was in.
    """
    match = HEADER_RE.search(source)
    if not match:
        where = f" in {path}" if path else ""
        raise ISFError(f"no ISF JSON header found{where}")
    try:
        header = json.loads(match.group(1))
    except json.JSONDecodeError as exc:
        where = f" in {path}" if path else ""
        raise ISFError(f"ISF header is not valid JSON{where}: {exc}") from exc

    body = source[: match.start()] + source[match.end() :]

    shader = ISFShader(header=header, body=body, source=source, path=path)

    # ossia score's compute variant names the block RESOURCES; classic ISF
    # names it INPUTS. Both are read, so a compute shader's controls reach the
    # same parameter panel as a fragment shader's.
    declared = header.get("INPUTS") or header.get("RESOURCES") or []
    for entry in declared:
        if not isinstance(entry, dict) or "NAME" not in entry:
            raise ISFError(f"input without a NAME in {path or '<string>'}")
        name = str(entry["NAME"])
        if name in GLSL_RESERVED:
            raise ISFError(
                f"input {name!r} in {path or '<string>'} is a GLSL reserved word, "
                f"so the uniform it becomes will not compile. Rename it."
            )
        if name in GLSL_BUILTINS:
            raise ISFError(
                f"input {name!r} in {path or '<string>'} is a GLSL built-in "
                f"function, so declaring a uniform with that name is a "
                f"redefinition. Rename it."
            )
        if name in ISF_SUPPLIED:
            raise ISFError(
                f"input {name!r} in {path or '<string>'} is supplied by ISF "
                f"itself; declaring it would shadow the host's own uniform."
            )
        if not re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", name):
            raise ISFError(
                f"input {name!r} in {path or '<string>'} is not a valid GLSL identifier"
            )
        if name.startswith("gl_") or name.startswith("isf_"):
            raise ISFError(
                f"input {name!r} in {path or '<string>'} uses a reserved prefix"
            )
        shader.inputs.append(
            ISFInput(
                name=name,
                type=str(entry.get("TYPE", "float")),
                label=str(entry.get("LABEL", "")),
                default=entry.get("DEFAULT"),
                minimum=entry.get("MIN"),
                maximum=entry.get("MAX"),
                values=list(entry.get("VALUES", []) or []),
                labels=[str(x) for x in entry.get("LABELS", []) or []],
                identity=entry.get("IDENTITY"),
                image_format=entry.get("FORMAT"),
                access=entry.get("ACCESS"),
                width_expr=str(entry["WIDTH"]) if "WIDTH" in entry else None,
                height_expr=str(entry["HEIGHT"]) if "HEIGHT" in entry else None,
            )
        )

    for entry in header.get("PASSES", []) or []:
        if not isinstance(entry, dict):
            raise ISFError(f"malformed PASSES entry in {path or '<string>'}")
        shader.passes.append(
            ISFPass(
                target=entry.get("TARGET"),
                persistent=_as_bool(entry.get("PERSISTENT", False)),
                float_buffer=_as_bool(entry.get("FLOAT", False)),
                width=str(entry["WIDTH"]) if "WIDTH" in entry else None,
                height=str(entry["HEIGHT"]) if "HEIGHT" in entry else None,
                local_size=entry.get("LOCAL_SIZE"),
                execution_model=entry.get("EXECUTION_MODEL"),
            )
        )
    if not shader.passes:
        shader.passes.append(ISFPass())

    imported = header.get("IMPORTED") or {}
    if isinstance(imported, dict):
        for name, spec in imported.items():
            if isinstance(spec, dict):
                shader.imported[name] = str(spec.get("PATH", ""))
            else:
                shader.imported[name] = str(spec)

    return shader


# ---------------------------------------------------------------------------
# Size expressions
# ---------------------------------------------------------------------------

SIZE_TOKEN = re.compile(r"\$(WIDTH|HEIGHT)(?:_([A-Za-z_][A-Za-z0-9_]*))?")
SAFE_EXPR = re.compile(r"^[0-9golcinemaxfrpsqtb_+\-*/%().,\s]+$", re.I)

_EXPR_NAMES = {
    "floor": math.floor,
    "ceil": math.ceil,
    "min": min,
    "max": max,
    "abs": abs,
    "pow": pow,
    "sqrt": math.sqrt,
    "round": round,
    "int": int,
    "log": math.log,
    "exp": math.exp,
}


def eval_size(expr: str | None, width: int, height: int,
              buffers: dict[str, tuple[int, int]] | None = None) -> int:
    """Resolve an ISF WIDTH/HEIGHT expression to a pixel count.

    `$WIDTH` and `$HEIGHT` are the render size; `$WIDTH_name` is the size of
    another pass target, which ossia score's compute shaders use to declare an
    output image the same size as their input.
    """
    if expr is None:
        return width
    buffers = buffers or {}
    text = str(expr)

    def substitute(m: re.Match[str]) -> str:
        axis, target = m.group(1), m.group(2)
        if target:
            size = buffers.get(target)
            if size is None:
                raise ISFError(f"size expression refers to unknown target {target!r}")
            return str(size[0] if axis == "WIDTH" else size[1])
        return str(width if axis == "WIDTH" else height)

    text = SIZE_TOKEN.sub(substitute, text)
    if not SAFE_EXPR.match(text):
        raise ISFError(f"unsafe size expression {expr!r}")
    value = eval(text, {"__builtins__": {}}, dict(_EXPR_NAMES))  # noqa: S307
    return max(1, int(round(float(value))))


# ---------------------------------------------------------------------------
# Translation to GLSL ES 3.00
# ---------------------------------------------------------------------------

GLSL_TYPE = {
    "bool": "bool",
    "long": "int",
    "float": "float",
    "point2D": "vec2",
    "color": "vec4",
    "event": "bool",
}

# GLSL 1.x spellings that ISF shaders in the wild still use. ES 3.00 renamed
# them; rewriting is safe because none of the new names are legal identifiers
# in the old dialect, so a shader cannot have defined them itself.
LEGACY_SUBSTITUTIONS = (
    (re.compile(r"\btexture2DRect\s*\("), "texture("),
    (re.compile(r"\btexture2DLod\s*\("), "textureLod("),
    (re.compile(r"\btexture2D\s*\("), "texture("),
    (re.compile(r"\btextureCube\s*\("), "texture("),
)

VARYING_IN = re.compile(r"^(\s*)varying\b", re.M)
VARYING_OUT = re.compile(r"^(\s*)varying\b", re.M)
ATTRIBUTE = re.compile(r"^(\s*)attribute\b", re.M)

# The default ISF vertex shader. It draws the full-render-size quad and hands
# the fragment stage its normalised coordinate. A shader may override it with
# its own .vs file, which is what the vertex-shader lessons do.
DEFAULT_VERTEX_BODY = """
void main() {
    isf_vertShaderInit();
}
"""

# ossia score's compute variant. It is not ISF with a different stage: a
# compute shader has no predefined input or output at all, so the RESOURCES
# block declares every image, texture, and buffer the shader touches, and the
# PASSES block says how many invocations to launch. GLSL ES has no compute
# stage, so this one target is desktop GLSL 4.30 rather than ES 3.00; that is
# also why the browser player cannot run these and says so.
COMPUTE_PREAMBLE = """#version 430

layout(local_size_x = {lx}, local_size_y = {ly}, local_size_z = {lz}) in;
"""

IMAGE_FORMATS = {
    "RGBA8": "rgba8",
    "RGBA16F": "rgba16f",
    "RGBA32F": "rgba32f",
    "R32F": "r32f",
    "RG32F": "rg32f",
    "R8": "r8",
}


def compute_source(shader: "ISFShader", pass_index: int = 0) -> str:
    """Assemble the GLSL a compute pass actually compiles as."""
    p = shader.passes[pass_index]
    local = p.local_size or [16, 16, 1]
    lines = [COMPUTE_PREAMBLE.format(lx=local[0], ly=local[1], lz=local[2] if len(local) > 2 else 1)]

    binding = 0
    for inp in shader.inputs:
        # ossia score writes compute resource types in upper case, "IMAGE" and
        # "TEXTURE", while classic ISF input types are lower case. Both
        # spellings appear in real files, so neither is normalised at parse
        # time and the comparison is folded here instead.
        kind = inp.type.lower()
        if kind == "image":
            fmt = IMAGE_FORMATS.get(str(inp.image_format or "RGBA8").upper(), "rgba8")
            access = (inp.access or "readonly").lower()
            qualifier = {"read_only": "readonly", "write_only": "writeonly",
                         "read_write": "", "readonly": "readonly",
                         "writeonly": "writeonly"}.get(access, "")
            lines.append(
                f"layout(binding = {binding}, {fmt}) uniform {qualifier} image2D {inp.name};".replace("  ", " ")
            )
            binding += 1
        elif kind == "texture":
            lines.append(f"layout(binding = {binding}) uniform sampler2D {inp.name};")
            binding += 1
        else:
            glsl_type = GLSL_TYPE.get(inp.type) or GLSL_TYPE.get(kind)
            if glsl_type is None:
                raise ISFError(f"unsupported compute resource type {inp.type!r}")
            lines.append(f"uniform {glsl_type} {inp.name};")

    lines.append("")
    lines.append(_sanitise_body(shader.body))
    return "\n".join(lines)


# The Vertex Shader Art preamble. The names come from vertexshaderart.com and
# ossia score reproduces them, so a shader written for either runs here
# unmodified. `vertexId` is the only per-vertex input: a shader is handed a
# number and has to decide, from that number alone, where the point goes. There
# is no mesh and no buffer of positions.
VSA_VERTEX_PREAMBLE = """#version 300 es
precision highp float;
precision highp int;

in float vertexId;

uniform float vertexCount;
uniform float time;
uniform vec2 resolution;
uniform vec2 mouse;
uniform float volume;
uniform vec4 background;
uniform sampler2D sound;
uniform sampler2D floatSound;
uniform vec2 soundRes;

// ISF's own names, so a shader can use either vocabulary. ossia score supplies
// both and a reader porting from vertexshaderart.com should not have to choose.
uniform vec2 RENDERSIZE;
uniform float TIME;
uniform float TIMEDELTA;
uniform vec4 DATE;
uniform int FRAMEINDEX;
uniform int PASSINDEX;

out vec4 v_color;
"""

# The fragment stage a VSA shader does not write. It exists only to hand the
# interpolated colour through, which is why the format can call itself a vertex
# shader format at all.
VSA_FRAGMENT = """#version 300 es
precision highp float;

in vec4 v_color;
out vec4 isf_FragColor;

void main() {
    isf_FragColor = v_color;
}
"""

VERTEX_PREAMBLE = """#version 300 es
precision highp float;
precision highp int;

in vec2 isf_position;
out vec2 isf_FragNormCoord;
out vec2 isf_FragCoord;

uniform vec2 RENDERSIZE;
uniform float TIME;
uniform float TIMEDELTA;
uniform vec4 DATE;
uniform int FRAMEINDEX;
uniform int PASSINDEX;

void isf_vertShaderInit() {
    gl_Position = vec4(isf_position, 0.0, 1.0);
    isf_FragNormCoord = isf_position * 0.5 + 0.5;
    isf_FragCoord = isf_FragNormCoord * RENDERSIZE;
}
"""

FRAGMENT_PREAMBLE = """#version 300 es
precision highp float;
precision highp int;
precision highp sampler2D;

in vec2 isf_FragNormCoord;
in vec2 isf_FragCoord;
out vec4 isf_FragColor;

uniform vec2 RENDERSIZE;
uniform float TIME;
uniform float TIMEDELTA;
uniform vec4 DATE;
uniform int FRAMEINDEX;
uniform int PASSINDEX;

#define gl_FragColor isf_FragColor
"""

# The IMG_ accessors. ISF defines them against a rect-or-2D sampler and a flip
# flag, because the host may hand a shader a texture whose origin is at the
# top. Keeping the flip in the accessor rather than in the caller is what lets
# the same shader source run on a WebGL texture and on a desktop-GL one; the
# ossia score documentation warns about exactly this, and says never to reach
# for gl_FragCoord directly for that reason.
IMG_ACCESSORS = """
vec4 isf_texel(sampler2D tex, vec2 normCoord, bool flip) {
    vec2 c = flip ? vec2(normCoord.x, 1.0 - normCoord.y) : normCoord;
    return texture(tex, c);
}
"""


def _sanitise_body(body: str) -> str:
    text = body
    for pattern, replacement in LEGACY_SUBSTITUTIONS:
        text = pattern.sub(replacement, text)
    return text


def _fragment_legacy_fixups(body: str) -> str:
    """Rewrite the GLSL 1.x fragment spellings ISF shaders were written in."""
    text = _sanitise_body(body)
    text = VARYING_IN.sub(r"\1in", text)
    return text


def _vertex_legacy_fixups(body: str) -> str:
    text = _sanitise_body(body)
    text = ATTRIBUTE.sub(r"\1in", text)
    text = VARYING_OUT.sub(r"\1out", text)
    return text


def _uniform_declarations(shader: ISFShader) -> str:
    lines: list[str] = []
    for inp in shader.inputs:
        if inp.is_image:
            lines.append(f"uniform sampler2D _{inp.name};")
            lines.append(f"uniform vec2 _{inp.name}_imgSize;")
            lines.append(f"uniform bool _{inp.name}_flip;")
        else:
            glsl_type = GLSL_TYPE.get(inp.type)
            if glsl_type is None:
                raise ISFError(f"unsupported input type {inp.type!r} for {inp.name!r}")
            lines.append(f"uniform {glsl_type} {inp.name};")
    # Pass targets are readable by later passes, and a PERSISTENT target is
    # readable by the pass that writes it, which is how feedback is written.
    #
    # De-duplicated, because several passes may name the same target: that is
    # how a simulation takes several steps in one frame, and declaring its
    # sampler once per pass is a redefinition the driver rejects with an error
    # pointing at the generated source rather than at the shader.
    declared: set[str] = set()
    for p in shader.passes:
        if p.target and p.target not in declared:
            declared.add(p.target)
            lines.append(f"uniform sampler2D _{p.target};")
            lines.append(f"uniform vec2 _{p.target}_imgSize;")
            lines.append(f"uniform bool _{p.target}_flip;")
    for name in shader.imported:
        if name in declared:
            continue
        declared.add(name)
        lines.append(f"uniform sampler2D _{name};")
        lines.append(f"uniform vec2 _{name}_imgSize;")
        lines.append(f"uniform bool _{name}_flip;")
    return "\n".join(lines)


def _img_macros(shader: ISFShader) -> str:
    """Per-image macros for the IMG_ accessors.

    A macro rather than an overloaded function, because ISF passes the image
    by its bare name and GLSL has no way to bind a name to both a sampler and
    its size and flip flag.
    """
    names: list[str] = []
    names += [i.name for i in shader.inputs if i.is_image]
    names += [p.target for p in shader.passes if p.target]
    names += list(shader.imported)

    lines: list[str] = []
    for name in dict.fromkeys(names):  # de-duplicate, keep order
        lines.append(
            f"#define IMG_NORM_PIXEL_{name}(nc) isf_texel(_{name}, (nc), _{name}_flip)"
        )
    if names:
        lines.append("")
    return "\n".join(lines)


IMG_CALL = re.compile(
    r"\bIMG_(NORM_PIXEL|PIXEL|THIS_NORM_PIXEL|THIS_PIXEL|SIZE)\s*\("
)


def _expand_img_calls(body: str, image_names: set[str]) -> str:
    """Rewrite IMG_*(image, coord) into the per-image accessor.

    Done textually because the ISF accessors take an image as their first
    argument, and GLSL ES 3.00 forbids passing a sampler around except as a
    function parameter, which would need one overload per call shape. Matching
    the balanced parentheses by hand keeps nested calls such as
    IMG_NORM_PIXEL(tex, IMG_SIZE(other)) intact.
    """
    out: list[str] = []
    i = 0
    while True:
        match = IMG_CALL.search(body, i)
        if not match:
            out.append(body[i:])
            break
        out.append(body[i : match.start()])
        kind = match.group(1)
        # Walk to the matching close parenthesis.
        depth = 1
        j = match.end()
        while j < len(body) and depth:
            if body[j] == "(":
                depth += 1
            elif body[j] == ")":
                depth -= 1
            j += 1
        if depth:
            raise ISFError(f"unbalanced IMG_{kind}( call")
        inner = body[match.end() : j - 1]
        args = _split_args(inner)
        name = args[0].strip() if args else ""
        if name not in image_names:
            raise ISFError(
                f"IMG_{kind} refers to {name!r}, which is not a declared image, "
                f"pass target, or import"
            )
        rest = [a.strip() for a in args[1:]]

        if kind == "SIZE":
            out.append(f"_{name}_imgSize")
        elif kind == "NORM_PIXEL":
            out.append(f"isf_texel(_{name}, {rest[0]}, _{name}_flip)")
        elif kind == "PIXEL":
            out.append(
                f"isf_texel(_{name}, ({rest[0]}) / _{name}_imgSize, _{name}_flip)"
            )
        elif kind == "THIS_NORM_PIXEL":
            out.append(f"isf_texel(_{name}, isf_FragNormCoord, _{name}_flip)")
        elif kind == "THIS_PIXEL":
            out.append(f"isf_texel(_{name}, isf_FragNormCoord, _{name}_flip)")
        i = j
    return "".join(out)


def _split_args(text: str) -> list[str]:
    args: list[str] = []
    depth = 0
    current: list[str] = []
    for ch in text:
        if ch == "," and depth == 0:
            args.append("".join(current))
            current = []
            continue
        if ch in "([":
            depth += 1
        elif ch in ")]":
            depth -= 1
        current.append(ch)
    args.append("".join(current))
    return args


@dataclass
class CompiledShader:
    vertex: str
    fragment: str
    manifest: dict[str, Any]
    shader: ISFShader


def image_names(shader: ISFShader) -> set[str]:
    names = {i.name for i in shader.inputs if i.is_image}
    names |= {p.target for p in shader.passes if p.target}
    names |= set(shader.imported)
    return names


def compile_shader(shader: ISFShader, name: str = "") -> CompiledShader:
    """Translate a parsed shader into GLSL ES 3.00 plus a manifest.

    The manifest is what the browser player consumes: it never sees the ISF
    source, only this. That is deliberate, and it is the reason the live
    player and the recorded figure cannot drift apart.
    """
    if shader.is_vsa:
        imgs = image_names(shader)
        body = _vertex_legacy_fixups(shader.body)
        body = _expand_img_calls(body, imgs)
        vertex = "\n".join((
            VSA_VERTEX_PREAMBLE,
            _uniform_declarations(shader),
            IMG_ACCESSORS,
            _img_macros(shader),
            body,
        ))
        manifest = _manifest(shader, name, vertex=vertex, fragment=VSA_FRAGMENT,
                             compute=False)
        manifest["mode"] = "vertex"
        manifest["pointCount"] = shader.point_count
        manifest["primitive"] = shader.primitive
        manifest["background"] = shader.background
        return CompiledShader(vertex=vertex, fragment=VSA_FRAGMENT,
                              manifest=manifest, shader=shader)

    if shader.is_compute:
        # A compute shader has no fragment stage to translate. It is reported
        # so the caller can route it to the offline renderer and so the page
        # can say why there is no live player.
        manifest = _manifest(shader, name, vertex="", fragment="", compute=True)
        manifest["compute"] = compute_source(shader)
        manifest["localSize"] = shader.passes[0].local_size or [16, 16, 1]
        manifest["executionModel"] = shader.passes[0].execution_model or {}
        return CompiledShader(vertex="", fragment="", manifest=manifest, shader=shader)

    imgs = image_names(shader)

    frag_body = _fragment_legacy_fixups(shader.body)
    frag_body = _expand_img_calls(frag_body, imgs)

    fragment = "\n".join(
        part
        for part in (
            FRAGMENT_PREAMBLE,
            _uniform_declarations(shader),
            IMG_ACCESSORS,
            _img_macros(shader),
            frag_body,
        )
        if part is not None
    )

    vert_body = shader.vertex_body if shader.vertex_body is not None else DEFAULT_VERTEX_BODY
    vert_body = _vertex_legacy_fixups(vert_body)
    vert_body = _expand_img_calls(vert_body, imgs)
    vertex = "\n".join(
        (
            VERTEX_PREAMBLE,
            _uniform_declarations(shader).replace("uniform sampler2D", "uniform sampler2D"),
            IMG_ACCESSORS,
            _img_macros(shader),
            vert_body,
        )
    )

    manifest = _manifest(shader, name, vertex=vertex, fragment=fragment, compute=False)
    return CompiledShader(vertex=vertex, fragment=fragment, manifest=manifest, shader=shader)


def _manifest(shader: ISFShader, name: str, vertex: str, fragment: str,
              compute: bool) -> dict[str, Any]:
    return {
        "name": name or (shader.path.stem if shader.path else "shader"),
        "description": shader.description,
        "credit": shader.credit,
        "categories": shader.categories,
        "mode": "compute" if compute else "fragment",
        "inputs": [i.as_dict() for i in shader.inputs],
        "passes": [p.as_dict() for p in shader.passes],
        "imported": shader.imported,
        "vertex": vertex,
        "fragment": fragment,
    }


def load(path: Path) -> ISFShader:
    """Read a shader file, picking up a sibling .vs vertex shader if present."""
    shader = parse(path.read_text(encoding="utf8"), path=path)
    vs = path.with_suffix(".vs")
    if vs.exists():
        shader.vertex_body = vs.read_text(encoding="utf8")
    return shader


if __name__ == "__main__":  # pragma: no cover - a convenience for the shell
    import sys

    for arg in sys.argv[1:]:
        p = Path(arg)
        compiled = compile_shader(load(p), name=p.stem)
        print(f"--- {p} ---")
        print(json.dumps(
            {k: v for k, v in compiled.manifest.items()
             if k not in {"vertex", "fragment"}},
            indent=2,
        ))
