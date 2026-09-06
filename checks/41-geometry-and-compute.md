# checks/41-geometry-and-compute

**Unit 41.** Compute, vertex, and 3D in score. Pinned to *ossia score* 3.8.2.

## Figures

None yet.

## Design notes

The unit is a decision table plus its justification, which is the right shape
for the last technical unit: the reader can already do all three things and
needs to know when to reach for each.

**The three limits section** is the most reusable part: gather but not scatter,
per-element but not across elements, per-pixel but not per-frame. Each names the
unit where the course ran into it.

## Corrections and open questions

- **Unverified in the application.** The existence and names of the VSA Shader,
  Compute Shader, and Model Display processes come from ossia's reference pages,
  and the array-and-texture conversion utilities from its process list. Nothing
  here has been run.
- The claim that a mesh is "an order of magnitude" cheaper than raymarching a
  comparable scene is a rule of thumb, not a measurement.
