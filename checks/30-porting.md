# checks/30-porting

**Unit 30.** Porting between shader formats. Pinned to *ossia score* 3.8.2.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `30-ported` | `library/shaders/30/ported.fs` | a Shadertoy-shaped body under a three-line shim |

## Design notes

**The body below the shim is written exactly as it would be on Shadertoy**,
including `mainImage`, its signature, and a pixel-space `fragCoord`. That is the
demonstration: the reader can see that nothing under the shim had to change.

**`iMouse` is built from an input named `pointer`, not the other way round.**
This is the unit's actual argument and the reason it is a whole unit rather than
a table. A faithful port produces a shader that does one thing; the rename is
what turns a fixed picture into an instrument.

The plasma is a common idiom rather than any one author's work, which is
deliberate: a port of a specific Shadertoy would have raised a licensing
question the course does not need to answer in a reference shader.

## Re-verify when

- **The shader is edited.** The unit quotes the shim and the `main` wrapper
  verbatim and tells the reader to read the file from the top.

## The unit's argument has a shipped example now

`common-practices/led-design/led-with-shaders.score`, which ships with ossia's
documentation, is a faithful Shadertoy port whose inputs are `iMouse`, `iZoom`,
`iSteps`, and `iColor`, with `iMouse` ranged 0 to 640 by 480 **in pixels**. It
is exactly the mistake the unit warns about, in ossia's own examples, and the
unit now cites it.

## Corrections and open questions

- The unit states Shadertoy's default licence as CC BY-NC-SA. That is the site's
  stated default; **many authors override it in a comment**, and the unit says
  so. Worth re-checking if the site's terms change.
