# Figures pending

The queue, grouped by what each figure needs. A unit with no figure is not
broken: the live players carry most of the load by design, and a figure is added
when it shows something a player cannot.

**All 47 units are written.** What follows is the gap between written and
verified.

## The largest outstanding item: Phase 4 is unverified in the application

Units 35 to 41, P4, and the capstone are written from *ossia score*'s own
documentation and from the score course's recorded findings. **None of it has
been walked through in a running 3.8.2.** Each unit's `checks/` note says so.

Before Phase 4 is treated as final, someone has to open *score* and do it. The
specific claims most worth testing, because a reader is told they can rely on
them:

- **Unit 36, step 5**: that a compile error leaves the last working shader
  rendering rather than blacking the output.
- **Unit 37**: that a `float` input's `MIN` and `MAX` act as the scaling
  contract an automation curve is mapped onto.
- **Unit 35, step 6**: addressing a texture outlet at the window device.
- **Unit 40**: whether ossia's FFT texture is linear in frequency across the
  audible range, which the shader's band boundaries assume.

## Needs *ossia score* driven under synthetic input

`scripts/capture.py` and `scripts/typeinto.py` are carried over from the score
course and work there. They have **not** been run in this repository.

X access from this session is possible: the user's Xwayland display is `:0`, and
`XAUTHORITY=/run/user/<uid>/.mutter-Xwaylandauth.*` reaches it. That puts score
windows on the user's live desktop, so it is worth asking before doing.

Wanted, in order of value:

- **37-01** a shader's inlets beside the JSON header that produced them, with an
  automation curve addressed at one. This is the most valuable figure in the
  course: it is the single picture that shows why every parameter in the library
  is named for its role.
- **36-01** the script editor open over a playing score, mid-recompile.
- **35-01** the smallest complete patch: shader, window device, output.
- **38-01** a four-link chain in a patch.
- **01-01** an ISF file loaded, inlets matched against its header. Supports Unit
  01's success criterion.
- **41-01** VSA, compute, and Model Display processes in one patch.

## Needs a renderer feature that does not exist yet

- **03-01** the same circle at several aspect ratios, side by side or swept.
  `render.py` renders one size per job, so this needs either a montage step or
  the ability to animate `RENDERSIZE`. The montage is cheaper.

## Ordinary work, just not done

- **Clips for Modules C to G**, one per unit, sweeping the parameter the unit is
  about. These are `render.py --sweep` jobs needing no new machinery. Only
  `p2-01` and `32-01` exist so far.
- **A reference `.score` document** for P4, once Phase 4 is verified.
- **A motion-detection shader** for Unit 39, which describes the technique and
  ships nothing.
