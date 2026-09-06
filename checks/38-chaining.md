# checks/38-chaining

**Unit 38.** Chaining processes. Pinned to *ossia score* 3.8.2.

## Figures

None yet. A patch with a four-link chain visible is wanted.

## Claims that were checked

- The eight-channel video mixer with per-channel opacity and blend mode, and the
  four-point video mapping object, are both in the user library: video mixing
  page.
- Each process writes to its own render target: graphics pipeline page.

## Design notes

The memory-traffic argument is the unit's real content, and it is a general
property of GPUs rather than of *score*: six full-resolution round trips at
1920x1080 is roughly 100 MB per frame at 8 MB per RGBA8 buffer read and written.

## Corrections and open questions

- **The frame-rate claims in step 5 have not been measured** in *score*. The
  exercise asks the reader to measure; this project has not.
