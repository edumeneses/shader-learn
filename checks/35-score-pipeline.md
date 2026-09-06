# checks/35-score-pipeline

**Unit 35.** The score graphics pipeline. Pinned to *ossia score* 3.8.2.

## Figures

**None, and several are wanted.** Every step of the walkthrough is an action in
the application. Recorded in `checks/FIGURES-PENDING.md`.

## Claims that were checked, all from ossia's own documentation

- *score* uses Qt RHI and can drive OpenGL ES 2.0, Vulkan, Metal, and Direct3D
  11: stated in the graphics pipeline page.
- Video processes form a dynamic render graph processed in a separate thread,
  each process writing to a render target: same page.
- A new device cannot be added during playback: stated in the live coding page,
  as the one exception ossia has not lifted.
- The eight-channel video mixer is at Visuals > ISF Shader > Utility > Video
  Mixer, and a four-point video mapping object exists: video mixing page.
- A texture reaches a window by addressing an outlet at the window device rather
  than by drawing a cable: carried over from the score course's own findings.

## Corrections and open questions

- **Nothing in this unit has been verified in the running application by this
  project.** It is documentation plus the score course's notes. Before Phase 4
  is treated as final, walk the eight steps in 3.8.2 and correct what is wrong.
  That is the single largest outstanding item in this repository.
