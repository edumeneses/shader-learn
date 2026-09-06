# checks/40-audio-reactive

**Unit 40.** Audio-reactive shaders. Pinned to *ossia score* 3.8.2.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `40-audio-reactive` | `library/shaders/40/audio-reactive.fs` | three bands with separate attack and release in a 4x1 persistent float buffer, an onset signal, and meters |

## Design notes

**The envelopes live in a persistent buffer**, which is the only memory a shader
has. A 4 by 1 target is the smallest in this repository and it demonstrates that
a pass target need not be image-sized.

**Attack and release are separate and both frame-rate independent**, using
`1 - exp(-dt/tau)`. The unit is emphatic that the fixed-fraction version is
wrong and that it only shows up on someone else's machine.

**Onset is derived from the rise of the low band**, not from a threshold on the
envelope, because a threshold fires late and double-triggers on a sustained note.

The unit's position is that analysis belongs in the patch rather than in the
shader for performance work, and that the shader does it internally only so the
technique is readable in one file. That is stated rather than implied.

## Claims that were checked

- ossia's own audio-reactive example page makes the same recommendations: smooth
  your signals, map ranges, use multiple bands, lag different parameters, and
  consider attack and release separately.

## Corrections and open questions

- The band boundaries, 0 to 0.06, 0.06 to 0.28, and 0.28 to 1.0 of the FFT
  texture, are **chosen by ear against the synthetic test signal** and are not
  derived from frequencies. They assume the host's FFT texture is linear in
  frequency across the audible range, which is usual and is not guaranteed.
  Check against ossia's analysis output before relying on them.
