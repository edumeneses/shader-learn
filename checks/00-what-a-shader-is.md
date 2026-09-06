# checks/00-what-a-shader-is

**Unit 00.** Conceptual; no installation, no exercise code, no *ossia score*.

## Figures

None. The unit's illustration is the live player embedded from
`library/shaders/00/hello-field.fs`, which is also the site's front-page shader.
It is deliberately the only shader in Module A that uses techniques the reader
has not met: it is there to be looked at, not understood.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `00-hello-field` | `library/shaders/00/hello-field.fs` | SDF circle, cosine palette, `fwidth` antialiasing, eight named inputs |

## Re-verify when

- **The shader is edited.** The unit's walkthrough quotes five things by name:
  `void main()`, `isf_FragNormCoord`, `gl_FragColor`, the line
  `float d = length(uv - centre) - radius;`, and the inputs `radius`,
  `softness`, `hue`. Renaming any of them breaks the prose silently, because
  nothing in the build checks that a quoted line still exists.
- **A Shadertoy link rots.** Three are cited: `Ms2SD1`, `XsXXDn`, `4ttSWf`.
  html-proofer runs with `--disable-external`, so CI will not catch a dead one.

## Corrections and open questions

Nothing yet.
