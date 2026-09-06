# checks/04-colour

**Unit 04.** Colour spaces. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `04-mixing` | `library/shaders/04/mixing.fs` | split-screen naive against linear blend, plus quantise and ordered dither |

## Re-verify when

- **The shader is edited.** The unit quotes `srgbToLinear` and `linearToSrgb`
  verbatim in the prose and refers to the controls **Quantise**, **Dither**, and
  **Show midpoint** by name. It also tells the reader to read the `bayer`
  function, so that name is load-bearing.

## Claims that were checked

- The sRGB midpoint is about 21 percent of maximum light: `srgbToLinear(0.5)`
  is 0.2140 under the piecewise definition used in the shader.
- The `pow(x, 2.2)` approximation diverges below about 0.04: that is where the
  standard's linear segment ends, at 0.04045.
- Rec. 709 luminance weights are 0.2126, 0.7152, 0.0722.

## Design notes

The dither is a 4x4 ordered Bayer matrix as a `float[16]` constant rather than a
texture, so the shader has no external dependency and can be pasted anywhere.
Blue noise would be better and needs a texture; the unit says so rather than
pretending Bayer is the state of the art.

The unit deliberately tells the reader **not** to convert palettes to linear.
That is a real position, not an omission, and it is repeated in Unit 05.

## Corrections and open questions

- **OKLab is described and not implemented.** If a later unit needs a
  perceptually uniform blend, add it there rather than retrofitting it here; the
  reason given for leaving it out, that thirty lines of matrices would be in the
  way, stops being true once a unit's subject is colour itself.
