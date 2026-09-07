# checks/29-isf

**Unit 29.** The ISF format in full. Pinned to *ossia score* 3.8.2.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `29-every-input` | `library/shaders/29/every-input.fs` | every input type, three passes including a sized target and a persistent float buffer |

## The reserved-word guard earned its keep again

Two inputs in this shader had to be renamed before it would compile: `layout` is
a GLSL reserved word, and the pass target `half` was renamed to `quarter` for
readability. `layout` is the **fifth** name caught by the parse-time guard, after
`flat`, `sample`, `round`, and `reflect`. The unit lists all five by name in its
Common mistakes section, because a reader writing a header will hit them.

## Re-verify when

- **The shader is edited.** The unit's table of input types must match what the
  shader declares, and its steps refer to the four quadrants by position.
- **The ISF specification changes.** The unit documents the format, not this
  implementation, and the two are only as close as `scripts/isf.py` makes them.

## Claims that were checked

- *ossia score* recommends against `gl_FragCoord` because its pipeline can run
  on OpenGL, Vulkan, Metal or Direct3D with differing Y direction: stated
  explicitly in the ossia reference page for the shader process.

## Verified against ossia score 3.8.2

The INPUTS-to-inlets rule was checked across **44 ISF processes in 20 shipped
`.score` documents**, not by opening the application but by reading the
documents, which are JSON. Every one matches. See `checks/VERIFICATION.md`.

The unit gained three things from it: the exact rule including image inputs, the
lower-cased OSC name, and the 0-to-0 domain a float with no range receives,
which is in a shipped example.

## Corrections and open questions

- The unit lists `IMPORTED` and says this course avoids it. `scripts/isf.py`
  parses it and **no shader in the library uses it**, so that code path is
  untested. Test it before recommending it to anyone.
- The claim that "a trailing comma is the most common ISF error by a wide
  margin" is experience, not measurement.
