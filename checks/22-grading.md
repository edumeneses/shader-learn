# checks/22-grading

**Unit 22.** Colour grading. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `22-grading` | `library/shaders/22/grading.fs` | exposure, white balance, contrast with pivot, lift/gamma/gain, saturation, three tone curves, histogram |

## Attribution

The filmic curve is Krzysztof Narkowicz's ACES approximation, credited in the
shader and linked from the unit.

## Re-verify when

- **The shader is edited.** The unit quotes the pivot value 0.18, describes the
  three-way pickers as offsets from mid grey, and its steps walk the tone-curve
  `LABELS` in order.

## Design notes

The whole grade runs in linear light and converts once at each end, which is
Unit 04's rule applied rather than restated.

**The three-way pickers default to mid grey and are read as offsets**, so an
untouched picker changes nothing. Without that, opening the panel would already
have graded the image.

## Corrections and open questions

- **The histogram is not a histogram.** A fragment shader cannot accumulate
  across pixels, so it samples along a scanline and calls it an approximation.
  The unit says so in step 8 and points at Unit 32. It is good enough to show a
  highlight pile-up, which is what steps 1 and 2 need it for, and it should not
  be trusted for anything else.
- White balance is a channel scale rather than a chromatic adaptation matrix.
  The unit says so.
- **No LUT.** The technique is described and not implemented, because a LUT
  needs an asset and the course's shaders are meant to travel as one file.
