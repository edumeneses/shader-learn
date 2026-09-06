# checks/03-coordinates

**Unit 03.** Coordinate spaces. No *ossia score* required.

## Figures

None yet. A rendered clip sweeping the aspect ratio of the frame would show the
difference between the three spaces better than resizing a browser window does,
and it is the one figure in Module B worth making. Recorded in
`checks/FIGURES-PENDING.md`.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `03-aspect` | `library/shaders/03/aspect.fs` | three coordinate spaces behind a `long` input, with a grid drawn in the same space |

## Re-verify when

- **The shader is edited.** The unit quotes the centred, height-divided line
  verbatim and refers to spaces by number, 0, 1, and 2, matching the `VALUES`
  array.

## Design notes

The grid is the point of the figure, not decoration. A distorted circle is
arguable; a rectangular grid cell is not. The grid is drawn with `fwidth` so it
is one pixel wide at any resolution, which quietly previews Unit 11.

Dividing by height rather than width is stated as a convention rather than a
rule, and the reason given is that it is what Shadertoy examples use. That is an
observation about the corpus, not a citation; if a reader disputes it, the honest
form is "this course uses height, and mixing the two in one project is the
actual mistake".

## Corrections and open questions

Nothing yet.
