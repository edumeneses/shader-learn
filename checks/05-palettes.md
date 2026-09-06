# checks/05-palettes

**Unit 05.** Cosine palettes. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `05-palette` | `library/shaders/05/palette.fs` | four constants on colour pickers, five presets, three views, channel plot |

## Re-verify when

- **The shader is edited.** The unit names the four constants as **a bias**,
  **b amplitude**, **c frequency**, and **d phase**, and steps 1 to 8 depend on
  the preset behaviour: switching to `custom` must leave the pickers untouched.

## Design notes

The channel plot under the strip is the part that does the teaching. Without it,
the four constants are four sliders that change the colours somehow; with it,
the reader can watch one cosine slide when they drag one component of `d`.

`preset` does not write into the pickers. Writing back would be friendlier and
would destroy the ability to A/B a preset against your own tuning, which is what
the control is for.

ISF has no vec3 input type, so the four constants are `color` inputs and the
shader takes `.rgb`. The alpha component of each picker is therefore unused and
the panel still shows an alpha slider for it. Worth a note in the unit if a
reader is confused by it; not worth adding four more float inputs to avoid.

## Corrections and open questions

- The presets are hand-tuned by eye and are not derived from anything. If a
  reader wants perceptually uniform presets, that is OKLab, which Unit 04 points
  at and neither unit implements.
