# checks/08-combining-fields

**Unit 08.** Boolean and smooth operators. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `08-combine` | `library/shaders/08/combine.fs` | six operators on two circles, field or shape view |

## Attribution

The polynomial `smin` is Inigo Quilez's, credited in the shader and in the unit.

## Re-verify when

- **The shader is edited.** The unit quotes `smin` verbatim and explains `h` and
  the correction term line by line. It also states that `smax` is `smin` with
  signs flipped, which is how the shader implements it.
- **Operators are reordered.** Steps 1 to 6 walk the `LABELS` array in order.

## Design notes

Two circles rather than two interesting shapes: the operators are the subject,
and a more elaborate pair of primitives would compete with them.

The field view is the default here for the same reason as Unit 07, and more
strongly: what an operator does to the space between two shapes is invisible in
the shape view, and that space is where the whole difference between `min` and
`smin` lives.

## Corrections and open questions

- The unit says the exponential `smin` "never quite reaches either input, so a
  shape blended this way is always slightly larger than it should be". That is
  the standard characterisation and it has **not** been demonstrated in a figure
  here. Worth a figure if Module G needs the exponential form.
