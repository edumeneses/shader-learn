# checks/24-raymarching

**Unit 24.** The sphere-traced loop. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `24-raymarch` | `library/shaders/24/raymarch.fs` | render, step count, and a 2D cross-section that draws every step's safety circle |

## Design notes

**The cross-section is the whole reason this unit works.** The algorithm is
usually explained with a static diagram; here it is the same loop in two
dimensions, running, with the ray aimable by the reader. Each circle is one
step, its radius is what the field reported, and no circle ever overlaps a
surface. That last fact is the guarantee sphere tracing rests on, and it is
visible rather than asserted.

Step scale is a control specifically so a reader can push it above 1 and watch
the ray punch through a thin bar. The failure mode of an over-estimating field
is the single most confusing thing in Module G and it is worth causing on
purpose in the unit where the loop is introduced.

**Every loop bound is a compile-time constant with a `break`.** The unit says
why: some WebGL drivers reject a uniform bound, so a raymarcher written the
natural way compiles on a desktop and fails in a browser.

## Re-verify when

- **The shader is edited.** The unit quotes the loop verbatim and its steps
  depend on the three views and on Step scale being able to exceed 1.

## Corrections and open questions

- The unit says scaling epsilon with distance is "the standard refinement". The
  shader does **not** do it; epsilon is a flat control. Consistent with the
  unit, which describes it rather than claiming the shader uses it, but a reader
  reading the source for the technique will not find it.
