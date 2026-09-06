# checks/33-beyond-glsl

**Unit 33.** Shader languages. No *ossia score* required.

## Shaders

None. Reading unit.

## Re-verify when

- **WebGPU ships in the browsers this course targets.** The unit says Unit 32's
  shader will "eventually" run in a browser and does not today; that sentence
  has a shelf life.
- **ossia score changes its rendering backend.** The unit describes Qt's
  rendering hardware interface as how *score* reaches four APIs.

## Claims that were checked

- The five portability failures the unit cites are this project's own, all
  recorded in git: `flat` and `sample` (Module B), `round` (Module C),
  `reflect` (Module G), `layout` (Module H). All five compiled on the NVIDIA
  driver here and all five are reserved or built-in in the specification.
- `glslangValidator` is the reference compiler and is what CI runs.

## Corrections and open questions

- HLSL's row-major default, MSL being C++14, and Slang's feature list are stated
  from documentation rather than from use. **Nobody on this project has written
  MSL or Slang.** If a reader who has says something here is wrong, believe them.
