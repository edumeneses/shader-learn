# Figures pending

The queue, grouped by what each figure needs. A unit with no figure is not
broken; the live players carry most of the load, and a figure is added when it
shows something a player cannot.

## Needs *ossia score* driven under synthetic input

`scripts/capture.py` and `scripts/typeinto.py` are carried over from the score
course and work, but the setup has not been repeated in this repository: there
is no X display configured for it here, and the score course's notes say root
capture returns black under this compositor, so score must be captured through
its own window drawable.

- **01-01** an ISF file loaded in *score*, inlets visible, matched against the
  JSON header that produced them. Supports Unit 01 step 5 and its success
  criterion.
- **Phase 4 figures generally.** Units 35 to 41 and P4 will each need at least
  one, and none of them can be rendered offline because the subject is the
  application rather than the shader.

## Needs a renderer feature that does not exist yet

- **03-01** the same circle at several aspect ratios, side by side or swept.
  `render.py` renders one size per job, so this needs either a montage step or
  the ability to animate `RENDERSIZE`. The montage is the cheaper of the two.

## Ordinary work, just not done

- Clips for Modules C, D, E, F, and G, one per unit, sweeping the parameter the
  unit is about. These are `render.py --sweep` jobs and need no new machinery.
