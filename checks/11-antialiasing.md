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
- **Corrected 2026-09-24: Fade thin rays faded to the background, not to
  grey.** Found while rendering figure 11-01. Step 4 said the centre goes grey;
  it went dark, because the fade multiplied coverage by the sub-pixel ray width
  and so drove it to zero, the background colour. It also ran on the zone plate
  alone, darkening its centre. The fade now mixes the fan's coverage towards
  its mean, `1 - thin`, which is what supersample 16 converges on, and touches
  only the fan. It also now measures the fan's *period*, a ray and its gap,
  rather than the dark ray alone, and fades as that period falls from four
  pixels to two: fading on the ray width left a ring of moiré where the period
  was already under the sampling limit and the ray was still wider than a
  pixel. The render now shows a flat grey disc matching supersample 16. The
  unit's Exercise had the same error in its requirements ("fade the arm
  towards the background") while its success criterion named it as the
  mistake; the requirement now says to fade towards the average.
- **Corrected 2026-09-24: the zone plate never aliased at the sizes this course
  uses.** Steps 1, 5, and 6 say its outer rings boil and that raising Detail
  moves the failure inward. The ring phase was `r² × detail` in frame heights,
  whose rate at the frame corner at Detail's old maximum of 260 was about 0.74
  radians per pixel at 720 lines and 1.26 at the player's 420, well under the
  limit of π; the rings rendered as clean stripes. The phase is now measured in
  pixels, `r² × RENDERSIZE.y × detail`, so the rings reach the limit at a
  radius of `π / (2 × detail)` frame heights at every resolution. Detail's
  range is now 0.5 to 16 with a default of 6, where the limit falls about a
  quarter of the way out, supersample 16 pushes the ghost rings towards the
  corners, and raising Detail pulls them back in. Rendered at 747 by 420 with
  one sample and with sixteen, both match steps 5 and 6.
