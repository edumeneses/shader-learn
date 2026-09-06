# checks/14-fbm

**Unit 14.** Fractal Brownian motion. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `14-fbm` | `library/shaders/14/fbm.fs` | the sum, octaves side by side, and a running sum; ridged and turbulent switches |

## Re-verify when

- **The shader is edited.** The unit quotes the loop verbatim and its steps
  depend on the three views and on the Ridged and Turbulent switches.

## Design notes

**The octave view is why this unit works.** Lacunarity and gain are otherwise
two sliders that change how rough it looks; seen as columns at their real scale
and loudness, they mean something. The running-sum view carries the unit's most
useful practical fact, that the first octave settles the composition and later
ones only add texture.

The shader returns noise centred on zero, and the unit says why: summing
positive values drifts the field upward with every octave.

Step 7 asks the reader to find a faint grid at exactly 2.0 lacunarity and lose
it at 2.02. **This has not been verified to be visible at every setting**; it is
most visible at high octave counts and low base scale, which is what the step
says to use.

## Corrections and open questions

- The erosion trick in Going further is described, not implemented; the exercise
  asks the reader to implement it. If a reference solution is ever added, it
  belongs in `library/shaders/14/` beside the main shader.
