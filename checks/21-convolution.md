# checks/21-convolution

**Unit 21.** Convolution and separable kernels. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `21-convolution` | `library/shaders/21/convolution.fs` | six kernels, separable and naive Gaussian, compare wipe |

## Re-verify when

- **The shader is edited.** The unit names the kernels in `LABELS` order and
  points at three things in the source: the 12-tap cap on the naive branch, the
  0.5 bias on emboss, and the fixed loop bound with a `continue`.

## Design notes

**Both the separable and the naive Gaussian are implemented.** The unit's claim
is that they give identical results at very different costs, and that is only
convincing if a reader can switch between them and see the picture not change.

**The naive branch is capped at 12 taps.** Without it, the maximum radius would
be 2401 samples per pixel, which is a genuinely bad time on integrated graphics
and would have made the comparison a trap rather than a demonstration. The unit
says the cap is there.

The unit is honest that on a fast GPU the cost difference may not be visible at
all, and says why that matters: the reader's hardware is hiding a cost their
audience's will not.

## Corrections and open questions

- **The blur is done in code space, not in linear light.** That is the common
  shortcut and it is wrong by Unit 04's own rule, since a blur is an average.
  The unit lists it under Common mistakes and turns it into an experiment rather
  than quietly doing the right thing, because every published blur a reader
  meets will have the same shortcut. Reconsider if a later unit depends on a
  physically correct blur.
- The bilinear two-taps-for-one trick is described and **not** implemented.
