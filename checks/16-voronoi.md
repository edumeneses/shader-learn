# checks/16-voronoi

**Unit 16.** Voronoi and cellular noise. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `16-voronoi` | `library/shaders/16/voronoi.fs` | F1, cell id, F2-F1, true edge distance, and the combination |

## Attribution

The true-edge second pass is Inigo Quilez's, credited in the shader.

## Re-verify when

- **The shader is edited.** The unit refers to views by name from the `LABELS`
  array and its step 3 and 4 depend on F2-F1 and the true edge being adjacent
  choices so a reader can flip between them.

## Design notes

**Both border methods are implemented, on purpose.** The unit's argument is that
F2-F1 thickens at corners, and that is only convincing if the reader can switch
between the two with the same edge width and watch the junctions change.

**Jitter is capped at half a cell.** That is the invariant the 3x3 search
depends on: a site that could leave its cell would be missed. The unit says so
and step 5 gives the sanity check.

Squared distances are compared inside the loop and the root taken once, which
the unit lists as a common mistake because it is a real cost for nothing.

## Corrections and open questions

- The edge-distance pass searches a 5x5 neighbourhood around the winning cell
  rather than 3x3. That is more conservative than Quilez's version needs and has
  not been reduced; it costs twenty-five site evaluations per pixel in that view.
  Worth measuring before this shader is used in anything performance-sensitive.
