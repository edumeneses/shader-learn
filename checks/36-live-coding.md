# checks/36-live-coding

**Unit 36.** Live coding in score. Pinned to *ossia score* 3.8.2.

## Figures

None yet. A capture of the script editor open over a playing score is the
obvious one.

## Claims that were checked

- The script editor opens from a small window button, the second, on the node
  header: stated in the live coding page.
- Ctrl+Enter recompiles during execution: stated in the shader process page.
- Both fragment and vertex shaders are editable: same page.
- Processes, sounds, and shaders can be added, removed, and altered during
  playback, and a device cannot: live coding page.
- Using triggers to keep parts running forever, so an interval behaves like a
  Max or Pure Data patch, is ossia's own suggestion for live coding: same page.

## Corrections and open questions

- **Unverified in the application.** Step 5 asserts that a compile error leaves
  the last working shader rendering. That is the sensible behaviour and it is an
  assumption; **test it before relying on it in a show**, because the unit tells
  a reader they can.
- The claim that an edited shader's source lives in the `.score` document and
  can be recovered by reading the file as text follows from documents being text
  and has not been demonstrated here.
