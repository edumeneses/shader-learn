# checks/28-the-pipeline

**Unit 28.** The graphics pipeline. No *ossia score* required.

## Shaders

None of its own. It sends the reader to the players of other units and to
`VERTEX_PREAMBLE` in `scripts/isf.py`.

## Re-verify when

- **`scripts/isf.py`'s vertex preamble changes.** Step 3 tells the reader to
  read it and says it is nine lines.
- **Unit 31 exists in another form.** Step 4 sends the reader there to see the
  roles inverted.

## Corrections and open questions

- The unit says a fragment shader runs "about two million" times at 1920x1080.
  Exactly 2,073,600, plus the helper invocations in partially covered 2x2 quads,
  which the unit mentions separately.
