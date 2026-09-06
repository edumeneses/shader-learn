# checks/11-antialiasing

**Unit 11.** Antialiasing. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `11-antialias` | `library/shaders/11/antialias.fs` | zone plate and converging rays, four methods, analytic thin-line fade |

## Re-verify when

- **The shader is edited.** The unit quotes the three-line `fwidth` idiom and
  the thin-ray fade, and step 7 tells the reader to read the supersampling loop
  and notice its fixed bound.

## Design notes

**The scene is chosen to be hostile.** A zone plate is the standard sampling
torture test, and a fan converging to a point produces sub-pixel features by
construction. A friendlier scene would have made `fwidth` look like a complete
answer, which is the misconception the unit exists to correct.

**The supersampling loop iterates a fixed 4x4 and skips samples.** That is not
style: some WebGL drivers require a compile-time loop bound, and the unit says
so, because Module G's raymarchers have to be written the same way and it is
better to meet the constraint here than to debug it there.

The unit is explicit that the offline figures are supersampled and the players
are not, so a player can shimmer where a clip does not.

## Corrections and open questions

- The thin-ray fade computes the ray width in pixels as
  `length(p) * TAU / arms * thin / px`. That is the arc width, which slightly
  over-estimates for a straight ray. The error is under one percent for the
  ranges the controls allow and has not been corrected.
