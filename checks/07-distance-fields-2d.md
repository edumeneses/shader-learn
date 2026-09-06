# checks/07-distance-fields-2d

**Unit 07.** Signed distance fields in 2D. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `07-sdf-2d` | `library/shaders/07/sdf-2d.fs` | six exact 2D SDFs, field or shape view |

## Attribution

Every distance function here is Inigo Quilez's, from his 2D distance functions
article, and so is the field visualisation: warm outside, cool inside, faded by
`1 - exp(-7|d|)`, isolines from `cos(n*d)`, white on the zero crossing. The
shader credits him in its `CREDIT` field and the unit says so in prose. This is
not incidental; it is the reference the whole module is built on.

## Re-verify when

- **The shader is edited.** The unit quotes `sdCircle`, `sdBox`, and
  `sdSegment` verbatim and walks the reader through `sdBox`'s two terms.
- **A shape is added or reordered.** Steps 3 to 7 refer to shapes by their
  position in the `LABELS` array.

## Design notes

The field view is the default, deliberately. A reader who only ever sees the
shape view has no reason to believe the extra information exists.

The isoline control goes up to 300 so that a reader can make the field alias on
purpose, which previews Unit 11 from inside a unit about something else.

## Corrections and open questions

- The unit asserts that `min` of two exact fields stays a valid bound but stops
  being exact between the shapes. True, and it is stated again in Unit 08 where
  it is the subject. If either statement is ever corrected, correct both.
