/*
 * shader-player.js - a live ISF player for the lesson pages.
 *
 * Every shader in this course appears twice: once as a clip rendered offline
 * on the author's GPU, and once here, running in the reader's browser with
 * every parameter on a control. The two agree because they run the same
 * string: scripts/isf.py translates the ISF source to GLSL ES 3.00 once, and
 * both the offline renderer and this player consume that translation. This
 * file contains no ISF parser for exactly that reason.
 *
 * Parameters are the point. A shader in this course never hard-codes a number
 * that a reader might want to move, and it never names a control after the
 * device that happens to drive it: an input is `focus`, not `mouse`, so the
 * same shader reads a pointer here, an OSC message in ossia score, and an
 * automation curve in a score document, with nothing renamed in between.
 *
 * Usage, from _includes/shader.html:
 *
 *   <shader-player manifest="/assets/shaders/03-warp.json"
 *                  height="360" pointer="focus" caption="..."></shader-player>
 *
 * Attributes
 *   manifest   required, URL of a manifest from scripts/build_shaders.py
 *   height     canvas height in CSS pixels, default 360
 *   pointer    name of a point2D input the canvas pointer drives; "none" to
 *              disable. Defaults to the first point2D input.
 *   controls   full (default) | compact | none
 *   autoplay   start on its own once scrolled into view, unless the reader
 *              has asked for reduced motion
 *   loop       seconds after which TIME wraps, for a clip that must repeat
 *   poster     an image shown before the first frame
 */

(function () {
  "use strict";

  const REDUCED_MOTION = window.matchMedia("(prefers-reduced-motion: reduce)");
  const TAU = 6.283185307179586;

  // ---------------------------------------------------------------------
  // A deterministic audio signal, identical to synthetic_audio() in
  // scripts/render.py. A shader driven by sound has to look the same in the
  // page as it does in the figure above it, and it cannot do that if one of
  // them is fed noise.
  // ---------------------------------------------------------------------
  function syntheticAudio(t, bins) {
    const wave = new Float32Array(bins * 4);
    const spec = new Float32Array(bins * 4);
    const beat = Math.exp(-6.0 * ((t * 2.0) % 1.0));
    const sweep = 0.15 + 0.35 * (0.5 + 0.5 * Math.sin(t * 0.7));
    for (let i = 0; i < bins; i++) {
      const idx = i / bins;
      const body = Math.exp(-Math.pow(idx - sweep, 2) / 0.004);
      const low = Math.exp(-idx / 0.03) * beat;
      const s = Math.min(1, Math.max(0, 0.85 * low + 0.6 * body * beat));
      const w = 0.5 + 0.45 * Math.sin(TAU * (idx * 24.0 + t * 3.0)) * beat;
      for (let c = 0; c < 4; c++) {
        spec[i * 4 + c] = s;
        wave[i * 4 + c] = w;
      }
    }
    return { wave, spec };
  }

  // ---------------------------------------------------------------------
  // GL helpers
  // ---------------------------------------------------------------------

  function compile(gl, type, source, label) {
    const sh = gl.createShader(type);
    gl.shaderSource(sh, source);
    gl.compileShader(sh);
    if (!gl.getShaderParameter(sh, gl.COMPILE_STATUS)) {
      const log = gl.getShaderInfoLog(sh);
      gl.deleteShader(sh);
      throw new Error(label + " did not compile:\n" + log);
    }
    return sh;
  }

  function link(gl, vsSource, fsSource) {
    const vs = compile(gl, gl.VERTEX_SHADER, vsSource, "vertex shader");
    const fs = compile(gl, gl.FRAGMENT_SHADER, fsSource, "fragment shader");
    const prog = gl.createProgram();
    gl.attachShader(prog, vs);
    gl.attachShader(prog, fs);
    gl.linkProgram(prog);
    gl.deleteShader(vs);
    gl.deleteShader(fs);
    if (!gl.getProgramParameter(prog, gl.LINK_STATUS)) {
      const log = gl.getProgramInfoLog(prog);
      gl.deleteProgram(prog);
      throw new Error("program did not link:\n" + log);
    }
    return prog;
  }

  function makeTarget(gl, w, h, floatBuffer) {
    const tex = gl.createTexture();
    gl.bindTexture(gl.TEXTURE_2D, tex);
    const internal = floatBuffer ? gl.RGBA16F : gl.RGBA8;
    const type = floatBuffer ? gl.HALF_FLOAT : gl.UNSIGNED_BYTE;
    gl.texImage2D(gl.TEXTURE_2D, 0, internal, w, h, 0, gl.RGBA, type, null);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
    const fbo = gl.createFramebuffer();
    gl.bindFramebuffer(gl.FRAMEBUFFER, fbo);
    gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.TEXTURE_2D, tex, 0);
    gl.bindFramebuffer(gl.FRAMEBUFFER, null);
    return { tex, fbo, width: w, height: h };
  }

  // Size expressions in a pass, the same $WIDTH / $HEIGHT grammar the Python
  // side resolves. Kept to arithmetic and a short function list on purpose:
  // a manifest is generated, but it is also hand-edited during a lesson, and
  // an eval of arbitrary text in a reader's browser is not a thing to ship.
  const SAFE_EXPR = /^[0-9+\-*/%().,\s]|floor|ceil|min|max|abs|sqrt|round/;
  function evalSize(expr, w, h, buffers) {
    if (expr === undefined || expr === null) return w;
    let text = String(expr).replace(/\$(WIDTH|HEIGHT)(?:_([A-Za-z_][A-Za-z0-9_]*))?/g,
      function (_, axis, target) {
        if (target) {
          const b = buffers[target];
          if (!b) throw new Error("size expression names unknown target " + target);
          return String(axis === "WIDTH" ? b.width : b.height);
        }
        return String(axis === "WIDTH" ? w : h);
      });
    if (!SAFE_EXPR.test(text) || /[A-Za-z_]/.test(text.replace(/floor|ceil|min|max|abs|sqrt|round/g, ""))) {
      throw new Error("unsafe size expression: " + expr);
    }
    /* eslint-disable no-new-func */
    const fn = new Function("floor", "ceil", "min", "max", "abs", "sqrt", "round",
      "return (" + text + ");");
    /* eslint-enable no-new-func */
    return Math.max(1, Math.round(fn(Math.floor, Math.ceil, Math.min, Math.max,
      Math.abs, Math.sqrt, Math.round)));
  }

  // ---------------------------------------------------------------------
  // Live contexts, and the cap on them.
  //
  // A browser allows a small number of simultaneous WebGL contexts, commonly
  // sixteen, and silently drops the oldest when the limit is passed. The
  // library page shows every shader in the course on one page, so the limit is
  // reachable by scrolling. Players therefore create their context on first
  // sight rather than on load, and the least recently seen one is released when
  // the cap is exceeded. A released player keeps its manifest and its parameter
  // values, so coming back to it costs a recompile and nothing else.
  // ---------------------------------------------------------------------

  const MAX_CONTEXTS = 8;
  const live = [];

  function claimContext(player) {
    const i = live.indexOf(player);
    if (i >= 0) live.splice(i, 1);
    live.push(player);
    while (live.length > MAX_CONTEXTS) {
      const victim = live.shift();
      if (victim !== player) victim._release();
    }
  }

  function dropContext(player) {
    const i = live.indexOf(player);
    if (i >= 0) live.splice(i, 1);
  }

  // ---------------------------------------------------------------------
  // The element
  // ---------------------------------------------------------------------

  class ShaderPlayer extends HTMLElement {
    connectedCallback() {
      if (this._built) return;
      this._built = true;
      this.classList.add("shader-player");
      this._values = Object.create(null);
      this._time = 0;
      this._frame = 0;
      this._running = false;
      this._lastStamp = 0;
      this._speed = 1;
      this._fps = 0;
      this._audioMode = "synthetic";
      this._render();
      this._load();
    }

    disconnectedCallback() {
      this._release();
      if (this._observer) this._observer.disconnect();
      if (this._resizeBound) window.removeEventListener("resize", this._resizeBound);
    }

    // -- markup ---------------------------------------------------------

    _render() {
      const height = parseInt(this.getAttribute("height") || "360", 10);
      const mode = this.getAttribute("controls") || "full";

      this.innerHTML = "";
      const stage = document.createElement("div");
      stage.className = "sp-stage";
      const canvas = document.createElement("canvas");
      canvas.className = "sp-canvas";
      canvas.style.height = height + "px";
      canvas.setAttribute("role", "img");
      stage.appendChild(canvas);

      const overlay = document.createElement("div");
      overlay.className = "sp-overlay";
      overlay.innerHTML = '<button class="sp-bigplay" type="button" aria-label="Run this shader">&#9654;</button>';
      stage.appendChild(overlay);

      const status = document.createElement("p");
      status.className = "sp-status";
      stage.appendChild(status);

      const bar = document.createElement("div");
      bar.className = "sp-bar";

      const panel = document.createElement("div");
      panel.className = "sp-panel";
      if (mode === "none") panel.hidden = true;

      const source = document.createElement("div");
      source.className = "sp-source";
      source.hidden = true;

      this.appendChild(stage);
      this.appendChild(bar);
      this.appendChild(panel);
      this.appendChild(source);

      const caption = this.getAttribute("caption");
      if (caption) {
        const cap = document.createElement("p");
        cap.className = "sp-caption";
        cap.textContent = caption;
        this.appendChild(cap);
      }

      this._canvas = canvas;
      this._overlay = overlay;
      this._statusEl = status;
      this._bar = bar;
      this._panel = panel;
      this._sourceEl = source;

      overlay.querySelector(".sp-bigplay").addEventListener("click", () => {
        if (!this._gl && !this._wake()) return;
        this._start();
      });
    }

    _status(text, isError) {
      this._statusEl.textContent = text || "";
      this._statusEl.classList.toggle("sp-error", !!isError);
      this._statusEl.hidden = !text;
    }

    // -- loading --------------------------------------------------------

    async _load() {
      const url = this.getAttribute("manifest");
      if (!url) {
        this._status("shader-player has no manifest attribute", true);
        return;
      }
      try {
        const res = await fetch(url);
        if (!res.ok) throw new Error(res.status + " " + res.statusText);
        this._manifest = await res.json();
      } catch (err) {
        this._status("could not load " + url + ": " + err.message, true);
        return;
      }

      if (this._manifest.mode === "compute") {
        this._status(
          "This is a compute shader. WebGL 2 has no compute stage, so it runs " +
          "in ossia score and in the rendered clip, not here.");
        this._buildSourceView();
        return;
      }

      this._buildSourceView();

      const wantsAuto = this.hasAttribute("autoplay") && !REDUCED_MOTION.matches;
      this._observer = new IntersectionObserver((entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            if (!this._gl && !this._wake()) return;
            if (wantsAuto && !this._everStarted) this._start();
          } else if (this._running) {
            this._pause();          // never burn a GPU on an off-screen canvas
          }
        }
      }, { threshold: 0.15, rootMargin: "200px" });
      this._observer.observe(this);
    }

    /** Create the context and everything that depends on it. */
    _wake() {
      if (this._gl) return true;
      claimContext(this);
      try {
        this._initGL();
      } catch (err) {
        dropContext(this);
        this._status(err.message, true);
        return false;
      }
      this._buildControls();
      this._buildBar();
      this._resize();
      if (!this._resizeBound) {
        this._resizeBound = () => this._resize();
        window.addEventListener("resize", this._resizeBound);
      }
      // One frame immediately, so a resting player is a picture rather than a
      // black rectangle. A reader who has asked for reduced motion sees this
      // and nothing else until they press play.
      this._drawOnce();
      return true;
    }

    /**
     * Give the context back. The manifest and the parameter values survive, so
     * this is a recompile rather than a reload when the player is seen again.
     */
    _release() {
      if (!this._gl) return;
      this._pause();
      const lose = this._gl.getExtension("WEBGL_lose_context");
      if (lose) lose.loseContext();
      this._gl = null;
      this._program = null;
      this._targets = Object.create(null);
      this._audioTextures = Object.create(null);
      this._imageTextures = Object.create(null);
      this._controls = Object.create(null);
      this._bar.innerHTML = "";
      this._panel.innerHTML = "";
      this._overlay.hidden = false;
      dropContext(this);
    }

    _initGL() {
      const gl = this._canvas.getContext("webgl2", {
        alpha: false,
        antialias: false,
        preserveDrawingBuffer: true,   // needed for the still-frame download
        powerPreference: "high-performance",
      });
      if (!gl) throw new Error("this browser has no WebGL 2, which the player needs");
      this._gl = gl;

      this._floatLinear = gl.getExtension("OES_texture_float_linear");
      gl.getExtension("EXT_color_buffer_float");

      const m = this._manifest;
      this._program = link(gl, m.vertex, m.fragment);

      const vao = gl.createVertexArray();
      gl.bindVertexArray(vao);
      const buf = gl.createBuffer();
      gl.bindBuffer(gl.ARRAY_BUFFER, buf);
      gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 3, -1, -1, 3]), gl.STATIC_DRAW);
      const loc = gl.getAttribLocation(this._program, "isf_position");
      gl.enableVertexAttribArray(loc);
      gl.vertexAttribPointer(loc, 2, gl.FLOAT, false, 0, 0);
      gl.bindVertexArray(null);
      this._vao = vao;

      this._uniforms = Object.create(null);
      const count = gl.getProgramParameter(this._program, gl.ACTIVE_UNIFORMS);
      for (let i = 0; i < count; i++) {
        const info = gl.getActiveUniform(this._program, i);
        const name = info.name.replace(/\[0\]$/, "");
        this._uniforms[name] = {
          location: gl.getUniformLocation(this._program, name),
          type: info.type,
        };
      }

      this._targets = Object.create(null);
      this._audioTextures = Object.create(null);
      this._imageTextures = Object.create(null);

      for (const input of m.inputs) {
        this._values[input.name] = defaultFor(input);
      }

      const pointerAttr = this.getAttribute("pointer");
      if (pointerAttr === "none") {
        this._pointerInput = null;
      } else if (pointerAttr) {
        this._pointerInput = pointerAttr;
      } else {
        const first = m.inputs.find((i) => i.type === "point2D");
        this._pointerInput = first ? first.name : null;
      }
      if (this._pointerInput) this._wirePointer();
    }

    _wirePointer() {
      const canvas = this._canvas;
      const set = (ev) => {
        const rect = canvas.getBoundingClientRect();
        const x = (ev.clientX - rect.left) / rect.width;
        // The canvas y axis runs down; a point2D input runs up, like the
        // normalised coordinate the shader reads. Flipping here rather than
        // in the shader is what lets the same input take an OSC message or an
        // automation curve without a sign flip somewhere in the patch.
        const y = 1 - (ev.clientY - rect.top) / rect.height;
        this._setValue(this._pointerInput, [clamp01(x), clamp01(y)]);
        this._syncControl(this._pointerInput);
        if (!this._running) this._drawOnce();
      };
      canvas.addEventListener("pointerdown", (ev) => {
        canvas.setPointerCapture(ev.pointerId);
        this._pointerDown = true;
        set(ev);
        ev.preventDefault();
      });
      canvas.addEventListener("pointermove", (ev) => {
        if (this._pointerDown) set(ev);
      });
      const release = (ev) => {
        this._pointerDown = false;
        if (canvas.hasPointerCapture && ev.pointerId !== undefined &&
            canvas.hasPointerCapture(ev.pointerId)) {
          canvas.releasePointerCapture(ev.pointerId);
        }
      };
      canvas.addEventListener("pointerup", release);
      canvas.addEventListener("pointercancel", release);
      canvas.style.touchAction = "none";
      canvas.title = "Drag to drive " + this._pointerInput;
    }

    // -- controls -------------------------------------------------------

    _buildBar() {
      const bar = this._bar;
      bar.innerHTML = "";
      const button = (label, title, handler, className) => {
        const b = document.createElement("button");
        b.type = "button";
        b.className = "sp-btn " + (className || "");
        b.textContent = label;
        b.title = title;
        b.setAttribute("aria-label", title);
        b.addEventListener("click", handler);
        bar.appendChild(b);
        return b;
      };

      this._playBtn = button("▶", "Play", () => this._toggle(), "sp-play");
      button("↺", "Restart from zero", () => {
        this._time = 0;
        this._frame = 0;
        this._clearTargets();
        this._drawOnce();
      });

      const speed = document.createElement("select");
      speed.className = "sp-speed";
      speed.title = "Playback rate";
      for (const rate of [0.1, 0.25, 0.5, 1, 2, 4]) {
        const opt = document.createElement("option");
        opt.value = String(rate);
        opt.textContent = rate + "×";
        if (rate === 1) opt.selected = true;
        speed.appendChild(opt);
      }
      speed.addEventListener("change", () => {
        this._speed = parseFloat(speed.value);
      });
      bar.appendChild(speed);

      const clock = document.createElement("span");
      clock.className = "sp-clock";
      bar.appendChild(clock);
      this._clock = clock;

      const spacer = document.createElement("span");
      spacer.className = "sp-spacer";
      bar.appendChild(spacer);

      if (this._manifest.inputs.some((i) => i.type === "audio" || i.type === "audioFFT")) {
        const audio = document.createElement("select");
        audio.className = "sp-audio";
        audio.title = "Audio source";
        for (const [value, label] of [["synthetic", "test signal"], ["mic", "microphone"]]) {
          const opt = document.createElement("option");
          opt.value = value;
          opt.textContent = label;
          audio.appendChild(opt);
        }
        audio.addEventListener("change", () => this._setAudioMode(audio.value));
        bar.appendChild(audio);
      }

      button("⤓ PNG", "Download the current frame", () => this._downloadFrame());
      this._recBtn = button("● REC", "Record a WebM clip of this canvas",
        () => this._toggleRecording(), "sp-rec");
      button("‹›", "Show the shader source", () => {
        this._sourceEl.hidden = !this._sourceEl.hidden;
      });
      button("⛶", "Fullscreen", () => {
        if (document.fullscreenElement) document.exitFullscreen();
        else this._canvas.requestFullscreen();
      });
    }

    _buildControls() {
      const panel = this._panel;
      panel.innerHTML = "";
      const inputs = this._manifest.inputs.filter((i) => i.type !== "image");
      if (!inputs.length) {
        panel.hidden = true;
        return;
      }

      const head = document.createElement("div");
      head.className = "sp-panel-head";
      head.innerHTML = "<span>Parameters</span>";
      const reset = document.createElement("button");
      reset.type = "button";
      reset.className = "sp-btn sp-reset";
      reset.textContent = "reset";
      reset.title = "Return every parameter to the value the shader declares";
      reset.addEventListener("click", () => {
        for (const input of this._manifest.inputs) {
          this._setValue(input.name, defaultFor(input));
          this._syncControl(input.name);
        }
        if (!this._running) this._drawOnce();
      });
      head.appendChild(reset);
      panel.appendChild(head);

      this._controls = Object.create(null);
      for (const input of inputs) {
        panel.appendChild(this._control(input));
      }
    }

    _control(input) {
      const row = document.createElement("div");
      row.className = "sp-row sp-row-" + input.type;
      const id = "sp-" + Math.random().toString(36).slice(2, 9);

      const label = document.createElement("label");
      label.className = "sp-label";
      label.htmlFor = id;
      label.textContent = input.label || input.name;
      if (input.label && input.label !== input.name) {
        const code = document.createElement("code");
        code.textContent = input.name;
        label.appendChild(document.createTextNode(" "));
        label.appendChild(code);
      }
      row.appendChild(label);

      const holder = document.createElement("div");
      holder.className = "sp-input";
      row.appendChild(holder);

      const readout = document.createElement("output");
      readout.className = "sp-readout";
      row.appendChild(readout);

      const commit = () => {
        this._syncReadout(input.name);
        if (!this._running) this._drawOnce();
      };

      if (input.type === "float" || input.type === "long") {
        const min = input.min !== undefined ? Number(input.min) : 0;
        const max = input.max !== undefined ? Number(input.max) : 1;
        if (input.type === "long" && input.values && input.values.length) {
          const sel = document.createElement("select");
          sel.id = id;
          input.values.forEach((value, i) => {
            const opt = document.createElement("option");
            opt.value = String(value);
            opt.textContent = (input.labels && input.labels[i]) || String(value);
            sel.appendChild(opt);
          });
          sel.addEventListener("input", () => {
            this._setValue(input.name, parseInt(sel.value, 10));
            commit();
          });
          holder.appendChild(sel);
          this._controls[input.name] = { kind: "select", el: sel, readout, input };
        } else {
          const slider = document.createElement("input");
          slider.type = "range";
          slider.id = id;
          slider.min = String(min);
          slider.max = String(max);
          slider.step = input.type === "long" ? "1" : String((max - min) / 500 || 0.001);
          slider.addEventListener("input", () => {
            const v = input.type === "long"
              ? parseInt(slider.value, 10) : parseFloat(slider.value);
            this._setValue(input.name, v);
            commit();
          });
          holder.appendChild(slider);
          this._controls[input.name] = { kind: "range", el: slider, readout, input };
        }
      } else if (input.type === "bool" || input.type === "event") {
        const box = document.createElement("input");
        box.type = "checkbox";
        box.id = id;
        box.addEventListener("input", () => {
          this._setValue(input.name, box.checked);
          commit();
        });
        holder.appendChild(box);
        this._controls[input.name] = { kind: "check", el: box, readout, input };
      } else if (input.type === "color") {
        const picker = document.createElement("input");
        picker.type = "color";
        picker.id = id;
        picker.addEventListener("input", () => {
          this._setValue(input.name, hexToRgba(picker.value,
            (this._values[input.name] || [0, 0, 0, 1])[3]));
          commit();
        });
        holder.appendChild(picker);
        const alpha = document.createElement("input");
        alpha.type = "range";
        alpha.min = "0"; alpha.max = "1"; alpha.step = "0.01";
        alpha.className = "sp-alpha";
        alpha.title = "Alpha";
        alpha.addEventListener("input", () => {
          const rgba = (this._values[input.name] || [1, 1, 1, 1]).slice();
          rgba[3] = parseFloat(alpha.value);
          this._setValue(input.name, rgba);
          commit();
        });
        holder.appendChild(alpha);
        this._controls[input.name] = { kind: "color", el: picker, alpha, readout, input };
      } else if (input.type === "point2D") {
        const pad = document.createElement("div");
        pad.className = "sp-pad";
        pad.tabIndex = 0;
        pad.id = id;
        pad.setAttribute("role", "application");
        pad.setAttribute("aria-label", (input.label || input.name) +
          ", a two-dimensional value; drag, or use the arrow keys");
        const dot = document.createElement("span");
        dot.className = "sp-dot";
        pad.appendChild(dot);

        const range = padRange(input);
        const fromEvent = (ev) => {
          const rect = pad.getBoundingClientRect();
          const x = clamp01((ev.clientX - rect.left) / rect.width);
          const y = 1 - clamp01((ev.clientY - rect.top) / rect.height);
          this._setValue(input.name, [
            range.minX + x * (range.maxX - range.minX),
            range.minY + y * (range.maxY - range.minY),
          ]);
          this._syncControl(input.name);
          commit();
        };
        pad.addEventListener("pointerdown", (ev) => {
          pad.setPointerCapture(ev.pointerId);
          pad._down = true;
          fromEvent(ev);
          ev.preventDefault();
        });
        pad.addEventListener("pointermove", (ev) => { if (pad._down) fromEvent(ev); });
        pad.addEventListener("pointerup", () => { pad._down = false; });
        pad.addEventListener("keydown", (ev) => {
          const step = ev.shiftKey ? 0.1 : 0.02;
          const v = (this._values[input.name] || [0, 0]).slice();
          if (ev.key === "ArrowLeft") v[0] -= step * (range.maxX - range.minX);
          else if (ev.key === "ArrowRight") v[0] += step * (range.maxX - range.minX);
          else if (ev.key === "ArrowDown") v[1] -= step * (range.maxY - range.minY);
          else if (ev.key === "ArrowUp") v[1] += step * (range.maxY - range.minY);
          else return;
          ev.preventDefault();
          this._setValue(input.name, [
            Math.min(range.maxX, Math.max(range.minX, v[0])),
            Math.min(range.maxY, Math.max(range.minY, v[1])),
          ]);
          this._syncControl(input.name);
          commit();
        });
        pad.style.touchAction = "none";
        holder.appendChild(pad);
        this._controls[input.name] = { kind: "pad", el: pad, dot, range, readout, input };
      }

      this._syncControl(input.name);
      return row;
    }

    _syncControl(name) {
      const control = this._controls && this._controls[name];
      if (!control) return;
      const value = this._values[name];
      if (control.kind === "range") control.el.value = String(value);
      else if (control.kind === "select") control.el.value = String(value);
      else if (control.kind === "check") control.el.checked = !!value;
      else if (control.kind === "color") {
        control.el.value = rgbaToHex(value);
        control.alpha.value = String(value[3]);
      } else if (control.kind === "pad") {
        const r = control.range;
        const x = (value[0] - r.minX) / (r.maxX - r.minX || 1);
        const y = (value[1] - r.minY) / (r.maxY - r.minY || 1);
        control.dot.style.left = (clamp01(x) * 100) + "%";
        control.dot.style.top = ((1 - clamp01(y)) * 100) + "%";
      }
      this._syncReadout(name);
    }

    _syncReadout(name) {
      const control = this._controls && this._controls[name];
      if (!control) return;
      const value = this._values[name];
      control.readout.textContent = formatValue(value, control.input);
    }

    _setValue(name, value) {
      this._values[name] = value;
    }

    // -- source view ----------------------------------------------------

    _buildSourceView() {
      const m = this._manifest;
      const el = this._sourceEl;
      el.innerHTML = "";

      const tabs = document.createElement("div");
      tabs.className = "sp-tabs";
      const panes = document.createElement("div");
      panes.className = "sp-panes";

      const add = (label, text, note) => {
        const tab = document.createElement("button");
        tab.type = "button";
        tab.className = "sp-tab";
        tab.textContent = label;
        const pane = document.createElement("div");
        pane.className = "sp-pane";
        pane.hidden = true;
        if (note) {
          const p = document.createElement("p");
          p.className = "sp-note";
          p.textContent = note;
          pane.appendChild(p);
        }
        const copy = document.createElement("button");
        copy.type = "button";
        copy.className = "sp-btn sp-copy";
        copy.textContent = "copy";
        copy.addEventListener("click", async () => {
          try {
            await navigator.clipboard.writeText(text);
            copy.textContent = "copied";
            setTimeout(() => { copy.textContent = "copy"; }, 1200);
          } catch (e) {
            copy.textContent = "select and copy";
          }
        });
        pane.appendChild(copy);
        const pre = document.createElement("pre");
        const code = document.createElement("code");
        code.textContent = text;
        pre.appendChild(code);
        pane.appendChild(pre);
        tab.addEventListener("click", () => {
          for (const p of panes.children) p.hidden = true;
          for (const t of tabs.children) t.classList.remove("is-active");
          pane.hidden = false;
          tab.classList.add("is-active");
        });
        tabs.appendChild(tab);
        panes.appendChild(pane);
        return { tab, pane };
      };

      const first = add("ISF source", m.source,
        "This is the file. Save it with a .fs extension, drop it into ossia score's " +
        "library, and the controls above appear as inlets on the process.");
      add("Compiled GLSL ES 3.00", m.fragment,
        "What the browser and the offline renderer actually run, after the ISF " +
        "header became uniforms and the IMG_ accessors became texture reads.");

      first.tab.classList.add("is-active");
      first.pane.hidden = false;

      el.appendChild(tabs);
      el.appendChild(panes);
    }

    // -- sizing and drawing ---------------------------------------------

    _resize() {
      const canvas = this._canvas;
      const dpr = Math.min(window.devicePixelRatio || 1, 2);
      const cssWidth = canvas.clientWidth || this.clientWidth || 640;
      const cssHeight = parseInt(this.getAttribute("height") || "360", 10);
      const w = Math.max(1, Math.round(cssWidth * dpr));
      const h = Math.max(1, Math.round(cssHeight * dpr));
      if (canvas.width === w && canvas.height === h) return;
      canvas.width = w;
      canvas.height = h;
      this._buildTargets();
      if (!this._running) this._drawOnce();
    }

    _buildTargets() {
      const gl = this._gl;
      const w = this._canvas.width;
      const h = this._canvas.height;
      for (const key of Object.keys(this._targets)) {
        const t = this._targets[key];
        gl.deleteTexture(t.front.tex);
        gl.deleteFramebuffer(t.front.fbo);
        if (t.back) {
          gl.deleteTexture(t.back.tex);
          gl.deleteFramebuffer(t.back.fbo);
        }
      }
      this._targets = Object.create(null);
      const sizes = Object.create(null);
      for (const pass of this._manifest.passes) {
        if (!pass.target) continue;
        const pw = evalSize(pass.width, w, h, sizes);
        const ph = evalSize(pass.height, w, h, sizes);
        sizes[pass.target] = { width: pw, height: ph };
        const floatBuffer = !!pass.float;
        const entry = {
          front: makeTarget(gl, pw, ph, floatBuffer),
          back: pass.persistent ? makeTarget(gl, pw, ph, floatBuffer) : null,
          persistent: !!pass.persistent,
        };
        this._targets[pass.target] = entry;
      }
    }

    _clearTargets() {
      const gl = this._gl;
      for (const key of Object.keys(this._targets)) {
        const t = this._targets[key];
        for (const buf of [t.front, t.back]) {
          if (!buf) continue;
          gl.bindFramebuffer(gl.FRAMEBUFFER, buf.fbo);
          gl.clearColor(0, 0, 0, 0);
          gl.clear(gl.COLOR_BUFFER_BIT);
        }
      }
      gl.bindFramebuffer(gl.FRAMEBUFFER, null);
    }

    _setUniform(name, value) {
      const gl = this._gl;
      const u = this._uniforms[name];
      if (!u) return;                 // the compiler dropped an unused uniform
      switch (u.type) {
        case gl.FLOAT: gl.uniform1f(u.location, value); break;
        case gl.FLOAT_VEC2: gl.uniform2f(u.location, value[0], value[1]); break;
        case gl.FLOAT_VEC3: gl.uniform3f(u.location, value[0], value[1], value[2]); break;
        case gl.FLOAT_VEC4:
          gl.uniform4f(u.location, value[0], value[1], value[2], value[3]); break;
        case gl.INT: gl.uniform1i(u.location, value | 0); break;
        case gl.BOOL: gl.uniform1i(u.location, value ? 1 : 0); break;
        case gl.SAMPLER_2D: gl.uniform1i(u.location, value | 0); break;
        default: break;
      }
    }

    _draw(dt) {
      const gl = this._gl;
      const m = this._manifest;
      const canvas = this._canvas;

      gl.useProgram(this._program);
      gl.bindVertexArray(this._vao);

      this._updateAudioTextures();

      for (const input of m.inputs) {
        if (input.type === "image" || input.type === "audio" || input.type === "audioFFT") continue;
        this._setUniform(input.name, this._values[input.name]);
      }
      this._setUniform("TIME", this._time);
      this._setUniform("TIMEDELTA", dt);
      this._setUniform("FRAMEINDEX", this._frame);
      const now = new Date();
      this._setUniform("DATE", [now.getFullYear(), now.getMonth() + 1, now.getDate(),
        now.getHours() * 3600 + now.getMinutes() * 60 + now.getSeconds()]);

      for (let index = 0; index < m.passes.length; index++) {
        const pass = m.passes[index];
        this._setUniform("PASSINDEX", index);

        let unit = 0;
        const bind = (name, tex, width, height) => {
          if (!this._uniforms["_" + name]) return;
          gl.activeTexture(gl.TEXTURE0 + unit);
          gl.bindTexture(gl.TEXTURE_2D, tex);
          this._setUniform("_" + name, unit);
          this._setUniform("_" + name + "_imgSize", [width, height]);
          this._setUniform("_" + name + "_flip", false);
          unit++;
        };

        for (const key of Object.keys(this._targets)) {
          const t = this._targets[key];
          const readable = t.persistent ? t.back : t.front;
          bind(key, readable.tex, readable.width, readable.height);
        }
        for (const key of Object.keys(this._audioTextures)) {
          const a = this._audioTextures[key];
          bind(key, a.tex, a.bins, 1);
        }
        for (const key of Object.keys(this._imageTextures)) {
          const im = this._imageTextures[key];
          bind(key, im.tex, im.width, im.height);
        }

        let width, height;
        if (pass.target) {
          const t = this._targets[pass.target];
          gl.bindFramebuffer(gl.FRAMEBUFFER, t.front.fbo);
          width = t.front.width;
          height = t.front.height;
        } else {
          gl.bindFramebuffer(gl.FRAMEBUFFER, null);
          width = canvas.width;
          height = canvas.height;
        }
        gl.viewport(0, 0, width, height);
        this._setUniform("RENDERSIZE", [width, height]);
        gl.clearColor(0, 0, 0, 1);
        gl.clear(gl.COLOR_BUFFER_BIT);
        gl.drawArrays(gl.TRIANGLES, 0, 3);
      }

      for (const key of Object.keys(this._targets)) {
        const t = this._targets[key];
        if (t.persistent) {
          const tmp = t.front;
          t.front = t.back;
          t.back = tmp;
        }
      }

      gl.bindFramebuffer(gl.FRAMEBUFFER, null);
      gl.bindVertexArray(null);
      this._frame++;
    }

    _drawOnce() {
      if (!this._gl || !this._program) return;
      try {
        this._draw(1 / 60);
        this._updateClock();
      } catch (err) {
        this._status(err.message, true);
      }
    }

    // -- audio ----------------------------------------------------------

    _setAudioMode(mode) {
      this._audioMode = mode;
      if (mode === "mic" && !this._analyser) this._openMicrophone();
    }

    async _openMicrophone() {
      try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        const ctx = new (window.AudioContext || window.webkitAudioContext)();
        const src = ctx.createMediaStreamSource(stream);
        const analyser = ctx.createAnalyser();
        analyser.fftSize = 1024;
        analyser.smoothingTimeConstant = 0.6;
        src.connect(analyser);
        this._analyser = analyser;
        this._freqData = new Uint8Array(analyser.frequencyBinCount);
        this._timeData = new Uint8Array(analyser.fftSize);
      } catch (err) {
        this._status("microphone not available: " + err.message, true);
        this._audioMode = "synthetic";
      }
    }

    _updateAudioTextures() {
      const gl = this._gl;
      const bins = 256;
      const needed = this._manifest.inputs.filter(
        (i) => i.type === "audio" || i.type === "audioFFT");
      if (!needed.length) return;

      let wave, spec;
      if (this._audioMode === "mic" && this._analyser) {
        this._analyser.getByteFrequencyData(this._freqData);
        this._analyser.getByteTimeDomainData(this._timeData);
        wave = new Float32Array(bins * 4);
        spec = new Float32Array(bins * 4);
        for (let i = 0; i < bins; i++) {
          const s = this._freqData[Math.min(i, this._freqData.length - 1)] / 255;
          const w = this._timeData[Math.min(i, this._timeData.length - 1)] / 255;
          for (let c = 0; c < 4; c++) {
            spec[i * 4 + c] = s;
            wave[i * 4 + c] = w;
          }
        }
      } else {
        const generated = syntheticAudio(this._time, bins);
        wave = generated.wave;
        spec = generated.spec;
      }

      for (const input of needed) {
        let entry = this._audioTextures[input.name];
        if (!entry) {
          const tex = gl.createTexture();
          gl.bindTexture(gl.TEXTURE_2D, tex);
          gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
          gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
          gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
          gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
          entry = { tex, bins };
          this._audioTextures[input.name] = entry;
        }
        gl.bindTexture(gl.TEXTURE_2D, entry.tex);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA16F, bins, 1, 0, gl.RGBA, gl.FLOAT,
          input.type === "audio" ? wave : spec);
      }
    }

    // -- transport ------------------------------------------------------

    _start() {
      if (this._running) return;
      if (!this._gl && !this._wake()) return;
      this._everStarted = true;
      this._running = true;
      this._overlay.hidden = true;
      if (this._playBtn) {
        this._playBtn.textContent = "⏸";
        this._playBtn.title = "Pause";
      }
      this._lastStamp = performance.now();
      const loop = (stamp) => {
        if (!this._running) return;
        const raw = (stamp - this._lastStamp) / 1000;
        this._lastStamp = stamp;
        const dt = Math.min(raw, 0.1) * this._speed;   // a backgrounded tab
        this._time += dt;                              // must not jump an hour
        const loopAt = parseFloat(this.getAttribute("loop") || "0");
        if (loopAt > 0 && this._time > loopAt) this._time -= loopAt;
        this._fps = this._fps * 0.9 + (raw > 0 ? 0.1 / raw : 0);
        try {
          this._draw(dt);
        } catch (err) {
          this._status(err.message, true);
          this._pause();
          return;
        }
        this._updateClock();
        this._raf = requestAnimationFrame(loop);
      };
      this._raf = requestAnimationFrame(loop);
    }

    _pause() {
      this._running = false;
      if (this._raf) cancelAnimationFrame(this._raf);
      if (this._playBtn) {
        this._playBtn.textContent = "▶";
        this._playBtn.title = "Play";
      }
    }

    _stop() { this._pause(); }

    _toggle() {
      if (this._running) this._pause();
      else this._start();
    }

    _updateClock() {
      if (!this._clock) return;
      this._clock.textContent =
        this._time.toFixed(2) + " s" +
        (this._running ? "  ·  " + Math.round(this._fps) + " fps" : "");
    }

    // -- export ---------------------------------------------------------

    _downloadFrame() {
      this._canvas.toBlob((blob) => {
        const a = document.createElement("a");
        a.href = URL.createObjectURL(blob);
        a.download = this._manifest.id + "-" + this._time.toFixed(2) + "s.png";
        a.click();
        setTimeout(() => URL.revokeObjectURL(a.href), 5000);
      });
    }

    _toggleRecording() {
      if (this._recorder) {
        this._recorder.stop();
        return;
      }
      if (typeof MediaRecorder === "undefined" || !this._canvas.captureStream) {
        this._status("this browser cannot record a canvas", true);
        return;
      }
      const stream = this._canvas.captureStream(30);
      const chunks = [];
      const recorder = new MediaRecorder(stream, { mimeType: "video/webm" });
      recorder.ondataavailable = (e) => { if (e.data.size) chunks.push(e.data); };
      recorder.onstop = () => {
        this._recorder = null;
        this._recBtn.classList.remove("is-recording");
        this._recBtn.textContent = "● REC";
        const blob = new Blob(chunks, { type: "video/webm" });
        const a = document.createElement("a");
        a.href = URL.createObjectURL(blob);
        a.download = this._manifest.id + ".webm";
        a.click();
        setTimeout(() => URL.revokeObjectURL(a.href), 5000);
      };
      recorder.start();
      this._recorder = recorder;
      this._recBtn.classList.add("is-recording");
      this._recBtn.textContent = "■ STOP";
      if (!this._running) this._start();
    }
  }

  // ---------------------------------------------------------------------
  // Small helpers
  // ---------------------------------------------------------------------

  function clamp01(v) { return Math.min(1, Math.max(0, v)); }

  function padRange(input) {
    const min = input.min || [0, 0];
    const max = input.max || [1, 1];
    return { minX: Number(min[0]), minY: Number(min[1]),
             maxX: Number(max[0]), maxY: Number(max[1]) };
  }

  function defaultFor(input) {
    if (input.default !== undefined && input.default !== null) {
      return Array.isArray(input.default) ? input.default.slice() : input.default;
    }
    switch (input.type) {
      case "point2D": return [0.5, 0.5];
      case "color": return [1, 1, 1, 1];
      case "bool":
      case "event": return false;
      case "long": return input.values && input.values.length ? Number(input.values[0]) : 0;
      default: {
        const min = input.min !== undefined ? Number(input.min) : 0;
        const max = input.max !== undefined ? Number(input.max) : 1;
        return (min + max) / 2;
      }
    }
  }

  function formatValue(value, input) {
    if (Array.isArray(value)) {
      if (input.type === "color") {
        return value.map((v) => v.toFixed(2)).join(", ");
      }
      return value.map((v) => v.toFixed(3)).join(", ");
    }
    if (typeof value === "boolean") return value ? "on" : "off";
    if (input.type === "long") {
      const i = (input.values || []).indexOf(value);
      return i >= 0 && input.labels && input.labels[i] ? input.labels[i] : String(value);
    }
    return Number(value).toFixed(3);
  }

  function hexToRgba(hex, alpha) {
    const h = hex.replace("#", "");
    return [
      parseInt(h.slice(0, 2), 16) / 255,
      parseInt(h.slice(2, 4), 16) / 255,
      parseInt(h.slice(4, 6), 16) / 255,
      alpha === undefined ? 1 : alpha,
    ];
  }

  function rgbaToHex(rgba) {
    const to = (v) => Math.round(clamp01(v) * 255).toString(16).padStart(2, "0");
    return "#" + to(rgba[0]) + to(rgba[1]) + to(rgba[2]);
  }

  if (!window.customElements.get("shader-player")) {
    window.customElements.define("shader-player", ShaderPlayer);
  }
})();
