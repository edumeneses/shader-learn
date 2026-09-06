# checks/31-vertex-shaders

**Unit 31.** Vertex shaders and VSA. Pinned to *ossia score* 3.8.2.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `31-lissajous` | `library/shaders/31/lissajous.fs` | 60,000 points, `MODE: VERTEX_SHADER_ART`, additive blending |

## This unit needed new machinery in both runtimes

Neither `scripts/render.py` nor `assets/js/shader-player.js` could draw anything
but a full-screen triangle. Both now support ossia score's VSA mode:

- `scripts/isf.py` recognises `"MODE": "VERTEX_SHADER_ART"`, emits the
  vertexshaderart.com preamble (`vertexId`, `vertexCount`, `time`, `resolution`,
  `mouse`, `volume`, `background`, `sound`) **alongside** the ISF names, and
  generates the fixed fragment stage that passes `v_color` through.
- Both runtimes build a buffer of one float per point holding its own index,
  draw with `POINT_COUNT` and `PRIMITIVE_MODE`, clear to `BACKGROUND_COLOR`, and
  enable additive blending.

Supplying both name sets is deliberate: ossia score does the same, so a shader
copied from vertexshaderart.com runs here unmodified.

## Re-verify when

- **The shader is edited.** The unit quotes the index-splitting idiom verbatim
  and its steps depend on Strands, Twist, Brightness, and Point size.
- **The VSA header keys change in ossia score.** `POINT_COUNT`,
  `PRIMITIVE_MODE`, and `BACKGROUND_COLOR` are read from ossia's own reference
  page for the VSA process.

## Corrections and open questions

- **`sound` and `floatSound` are declared and never fed.** A VSA shader that
  samples them gets an unbound sampler. Wire them to the same synthetic signal
  the audio inputs use before Unit 40.
- `LINE_SIZE` is in ossia's header spec and is not implemented here.
