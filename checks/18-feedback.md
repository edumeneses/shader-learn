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
