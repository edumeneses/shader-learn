# checks/23-compositing

**Unit 23.** Blend modes and alpha. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `23-compositing` | `library/shaders/23/compositing.fs` | nine separable blend modes, straight against premultiplied alpha, linear-light switch |

## Attribution

The blend formulas follow the PDF imaging model as restated in the CSS
compositing specification, linked from the unit.

## Re-verify when

- **The shader is edited.** The unit's table of formulas must match `blendFn`
  exactly, and its steps refer to modes in `LABELS` order.

## Design notes

**The alpha convention switch only shows a difference at a soft edge.** That is
the unit's central point and it is why step 8 tells the reader to raise Edge
softness first: with a hard edge the two conventions agree, which is precisely
why the bug survives in real work.

The linear-light switch exists because the honest answer is not uniform. Add and
screen model light arriving and belong in linear; multiply, overlay, and soft
light were defined on codes and tuned by eye there. The unit states that as a
position and lets the reader decide rather than asserting one is correct.

## Corrections and open questions

- Only the **separable** modes are implemented. The non-separable ones, hue,
  saturation, colour, and luminosity, need the whole colour rather than each
  channel and are a further page of code. Neither the shader nor the unit
  mentions them. Worth a sentence if a reader asks.
