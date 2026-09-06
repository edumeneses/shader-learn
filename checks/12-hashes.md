# checks/12-hashes

**Unit 12.** Hash functions. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `12-hashes` | `library/shaders/12/hashes.fs` | four hashes, three views including a correlation scatter |

## Attribution

The sine hash is folklore with no traceable origin. The fract-multiply hash is
Dave Hoskins's. The integer bit mix is the `lowbias32` finalizer, from Chris
Wellons's search for good 32-bit mixers, in the form Jarzynski and Olano
recommend for GPU work. All three are credited in the shader.

## Re-verify when

- **The shader is edited.** The unit quotes `hash21` verbatim and tells the
  reader to read `hashUint` and count its steps: three xor-shifts and two
  multiplies.

## Design notes

**The correlation plot loops over the sample set per pixel.** A fragment shader
cannot scatter, only gather, so each pixel asks "does any sample land on me"
rather than owning one sample. The first attempt gave each column its own
sample and lit almost nothing, because a column's sample lands at its *value*,
not at its column. The loop bound is a compile-time constant for the same
portability reason as Unit 11's supersampler.

## Corrections and open questions

- **The plot does not convict the sine hash on this hardware.** With 32-bit
  floats it scatters perfectly well, and its real failure is device dependent.
  The unit says so in step 5 rather than implying a demonstration it cannot
  give, and turns it into the argument for the integer hash: it is the only one
  whose behaviour can be predicted without testing on the target device.
- The unit describes blue noise and does not implement it. It needs a texture,
  which no shader in this course currently has. Revisit in Module F.
