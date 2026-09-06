# checks/20-sampling

**Unit 20.** Reading a texture. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `20-testcard` | `library/shaders/20/testcard.fs` | the source image for all of Module F |
| `20-sampling` | `library/shaders/20/sampling.fs` | five mappings, zoom, and a nearest-neighbour grid |

## The test card, and why it is a shader

Module F was blocked until the toolchain could supply an image to a shader.
Neither runtime bound anything to an ISF `image` input, so a whole module about
filters could not start.

The test card is written as an **ISF shader**, rendered once, and committed as
`docs/learn/assets/images/testcard.png`, with its spec at `figures/testcard.json`.
Both the offline renderer and the browser player load that PNG. Drawing the card
twice, once in PIL and once in canvas 2D, would have reintroduced exactly the
divergence the manifest design exists to prevent.

Its four regions each answer a question a filter can get wrong: colour bars for
colour-space mistakes, a greyscale ramp for banding and gamma, frequency wedges
for what a blur or a resample loses, and a continuous-tone region because a
filter tested only on flat colour and hard edges will look fine and fall apart on
gradients. The **red bracket is deliberately asymmetric**: a Y flip on a
symmetric test image is invisible, and a Y flip is the most common bug in this
module.

## Image support, as built

- `render.py` gained `load_image` and `--image name=path`, defaulting to the test
  card. It flips on load, because OpenGL's texture origin is bottom left and
  every image format's is top left.
- The player gained a per-input source picker: test card, a file the reader
  drops in, or the camera. It flips with `UNPACK_FLIP_Y_WEBGL` for the same
  reason.
- **A camera stream is stopped in `_release()`.** The player caps live WebGL
  contexts at eight, so without that a library page would leave cameras running
  after their players were released.

## Re-verify when

- **Either shader is edited.** The unit refers to the five mappings by their
  `LABELS` order and to the red bracket and the registration cross by name.

## Corrections and open questions

- The unit says a minified texture needs mipmaps and mentions `textureLod`. The
  offline renderer builds mipmaps on load; **the player does not**, so a heavily
  minified image aliases in the browser and not in a rendered figure. Fix in the
  player before any unit depends on minification.
