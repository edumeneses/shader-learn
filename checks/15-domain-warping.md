# checks/15-domain-warping

**Unit 15.** Domain warping. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `15-warp` | `library/shaders/15/warp.fs` | zero, one, and two levels, with the warp field and displacement magnitude as diagnostic views |

## Attribution

The technique, the two-level form, and the offset constants (5.2, 1.3) and
(8.3, 2.8) are Inigo Quilez's, credited in the shader and in the unit.

## Re-verify when

- **The shader is edited.** The unit quotes the three-line form verbatim,
  including the offsets, and its steps depend on the three views.

## Design notes

**The shading gradient is scaled by a constant, not divided by `fwidth`.** The
first version divided, which amplifies the finest octave, the one closest to a
pixel in size, and the picture picked up a stipple that was aliasing rather than
detail. The unit records this in Common mistakes, because a reader warping their
own field will hit it.

The two diagnostic views exist because the technique reads as magic otherwise.
Seeing the warp vector field and the displacement magnitude beside the result
turns "why does that look like marble" into "because those points looked up
values from far apart".

## Corrections and open questions

Nothing yet.
