# checks/19-state-in-a-texture

**Unit 19.** Simulation with state in a texture. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `19-reaction` | `library/shaders/19/reaction.fs` | Gray-Scott, four simulation passes plus a display pass, state at half resolution |

## This unit changed the toolchain

Writing it exposed three real bugs, all now fixed, all recorded because they
will be rediscovered otherwise.

1. **Iterating inside one pass diverges.** The first version looped the reaction
   several times per pass, but the Laplacian reads the *texture*, so every
   iteration used the same stale neighbourhood. It produced a checkerboard
   within a second. The unit's Common mistakes section says so, because it is
   the mistake a reader building their own will make.

2. **A persistent target used to swap at the end of the frame.** That made
   several passes naming the same target overwrite each other rather than step,
   and it also meant a display pass read the *previous* frame's state rather
   than what the simulation had just written. Both `scripts/render.py` and
   `assets/js/shader-player.js` now swap immediately after the pass that writes
   a target. **The two must stay in agreement** or a simulation runs at a
   different speed in the page than in the figure.

3. **A repeated `TARGET` declared its sampler once per pass**, which the driver
   rejects as a redefinition, with a line number in the generated source rather
   than in the shader. `scripts/isf.py` now de-duplicates, and `render.py` and
   the player build one buffer per target name.

## Re-verify when

- **The shader is edited.** The unit quotes the Laplacian weights and both
  Gray-Scott lines verbatim, names the recipes in `LABELS` order, and step 8
  tells the reader to count four passes in the `PASSES` block.

## Claims that were checked

- The recipes are Pearson's parameterisation. The five feed/kill pairs were
  chosen by running the shader, not from a table; **if a reader reports one does
  not match its name, believe them.**
- Step size above about 1.2 goes unstable: observed, at the default diffusion
  rates. It is not a derived bound and will move if `diffuseA` or `diffuseB` is
  changed.

## Corrections and open questions

- The unit says two GPUs running this shader diverge visibly within a minute
  because float arithmetic differs between drivers and the system is chaotic.
  That is the standard expectation and it has **not** been tested here; there is
  only one GPU on this machine. Before anyone relies on this shader for a
  multi-screen installation, test it.
