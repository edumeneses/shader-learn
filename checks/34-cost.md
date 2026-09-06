# checks/34-cost

**Unit 34.** Performance. No *ossia score* required.

## Shaders

None of its own. The unit works on the reader's own shader, deliberately: a
performance unit with a prepared example teaches the example rather than the
method.

## Re-verify when

- **`scripts/render.py`'s interface changes.** The unit gives a literal `time`
  command line.

## Design notes

The unit is the thirty-fifth by position on purpose and says so in its first
line. Optimising before you can write a shader produces work that is fast and
ugly.

**The exercise requires a reversal.** "If nothing was reversed, you were not
guessing hard enough to be learning anything" is the point of the exercise: the
intuition comes from the changes that did not work.

## Claims that were checked

- 1920x1080 is 2,073,600 pixels.
- Warp and wavefront sizes of 32 and 64 are NVIDIA's and AMD's respectively.

## Corrections and open questions

- The ordering of the expensive built-ins is the conventional one and is **not**
  measured here. It varies by hardware and it would be better as a measurement
  than as a list. A benchmark shader would be a good addition to Module I.
- "mediump is often twice as fast as highp on a phone" is the standard figure
  and is untested on this project.
