# checks/02-first-fragment-shader

**Unit 02.** First code. No *ossia score* required.

## Figures

None. The live player is the figure.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `02-first-steps` | `library/shaders/02/first-steps.fs` | four stages behind a `long` input with `VALUES` and `LABELS` |

## Re-verify when

- **The shader is edited.** The walkthrough quotes `colour = vec3(uv, 0.0)`,
  `colour = vec3(uv.x)`, and `fract(uv.x + TIME * rate)` verbatim, and names the
  inputs **Flat colour**, **Rate**, and **Gamma**.
- **The ISF preamble changes.** The unit shows a minimal ISF file and asserts
  that a declared input needs no further declaration in the code. That is true
  because `scripts/isf.py` emits the uniform; if the translation changes, the
  claim has to be rechecked.

## Design notes

The `long` input exists so that four shaders can be one file. A reader copying
the source gets all four stages, which is deliberate: the exercise asks them to
rebuild stage 2 from memory and having the others present is a fair reference.

## Corrections and open questions

- The unit says GLSL ES is strict about `uv * 2` versus `uv * 2.0`. Confirmed
  by the spec; **not** confirmed against every driver, some of which are lenient.
  If a reader reports that it compiles, the sentence should say "the spec
  requires" rather than "the compiler rejects".
