# checks/p1-poster

**Milestone P1.** Assembly, no new technique. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `p1-poster` | `library/shaders/p1/poster.fs` | the reference solution: sun, slatted screen, ridge line |

## Re-verify when

- **The reference shader is edited.** The unit names four compositional
  decisions in it: the palette driven by the signed distance, the tapered slats,
  the five-box ridge jittered by a sine, and the three percent grain. All four
  are claims about the code.
- **`scripts/render.py`'s interface changes.** Step 8 gives a literal command
  line, including `--supersample 2` and the A2 size 4960x7016.

## Claims that were checked

- 4960 by 7016 is A2 at 300 dpi: A2 is 420 by 594 mm, which is 16.54 by 23.39
  inches, giving 4961 by 7016. The unit rounds down by one pixel on the short
  edge, which is what the render command uses.
- The render command runs and completes in a few seconds on the RTX 4080 Super.
  **Verify this again if the machine changes**, since the unit tells the reader
  it is fast.

## Design notes

The reference solution is deliberately modest. A spectacular one would have made
the milestone read as a target to copy rather than as a brief to answer, and the
unit says outright that the reader's answer should not look like it.

The success criteria are mostly not technical. That is the point of the
milestone, and it is stated in the first paragraph.

## Corrections and open questions

- The brief forbids noise, and the reference solution uses a one-line hash for
  its grain. That is a real inconsistency. The defensible reading is that grain
  is a display correction rather than a picture element, and the shader's
  comment says so, but a reader could fairly call it a violation. Consider
  rewording the brief to "no noise in the composition" rather than "no noise".
