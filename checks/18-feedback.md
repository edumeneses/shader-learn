# checks/18-feedback

**Unit 18.** Persistent buffers and feedback. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `18-feedback` | `library/shaders/18/feedback.fs` | two passes, a persistent float target, zoom/twist/drag transform, hue drift by age |

## Re-verify when

- **The shader is edited.** The unit quotes the `PASSES` block and refers to
  `PASSINDEX == 0` and `PASSINDEX == 1` by name, and step 8 tells the reader
  that tone mapping is in the display pass.
- **The renderer's pass semantics change.** See the note in
  `checks/19-state-in-a-texture.md`: persistent targets now swap after the pass
  that writes them, and both `scripts/render.py` and
  `assets/js/shader-player.js` had to be changed together.

## Design notes

The buffer is `FLOAT` and the unit explains why at length: at 8 bits a value of
3 multiplied by 0.98 rounds back to 3 and never decays, so trails stick and
leave permanent smears. That is the most common feedback bug and it reads as a
shader fault rather than a format one.

Edge handling fades to nothing rather than reading the clamped edge texel. The
smear-from-one-edge artefact is listed under common mistakes because a reader
building their own will produce it.

## Corrections and open questions

- The unit says a persistent float target at 1080p is "16 megabytes, doubled for
  the swap". That is RGBA16F: 1920 x 1080 x 4 channels x 2 bytes = 16.6 MB, and
  the shader's targets are half-float in the browser and full float in the
  offline renderer. **The two runtimes therefore do not use the same precision**,
  which has not caused a visible difference and is worth knowing.
- **Corrected 2026-09-24: step 3 said Zoom at 1.03 makes "a tunnel" and that
  "the source stays put while everything else moves".** Rendering figure 18-01
  showed neither. With Twist at 0 the trail streams outward from the zoom
  centre and floods the frame; nothing reads as a tunnel. The source keeps
  orbiting, so it does not stay put; what distinguishes feedback from a moving
  camera is that the source keeps its size while the material it left grows.
  Step 3 now says both.
- **`render.py --settle` did nothing for this shader until 2026-09-24.** Settle
  frames were numbered with a negative `FRAMEINDEX` and recording restarted at
  zero, so the shader's `FRAMEINDEX < 2` initialisation reset the buffer for
  the whole settle and again on the first recorded frame. `FRAMEINDEX` now
  counts up from zero across settle and recording; `TIME` still starts at zero
  when recording starts.
