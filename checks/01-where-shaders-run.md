# checks/01-where-shaders-run

**Unit 01.** Setup and orientation. Pinned to *ossia score* 3.8.2.

## Figures

**None yet, and one is wanted.** A figure showing an ISF file loaded in *score*
with its inlets built from the JSON header would carry step 5 better than the
prose does. It is deferred because it needs *score* driven under synthetic
input, which `scripts/capture.py` can do but which has not been set up in this
repository yet. Recorded in `checks/FIGURES-PENDING.md`.

## Shaders

None of its own. It sends the reader back to `00-hello-field`.

## Re-verify when

- **The pinned *score* version changes.** Step 3 names the version, the download
  page layout may move, and the user library's ISF collection has grown between
  releases.
- **The ossia download page is restructured.** The link in step 3 and in Going
  further points at `getting-started/install.html`.
- **WebGL 2 support changes.** Unlikely, but the unit asserts "every current
  browser" and links to caniuse for the exception.

## Claims that were checked

- *score* ships a Vidvox ISF collection in its user library: stated in the
  ossia reference page for the shader process, which says the shaders are
  "already provided as part of the user library, courtesy of Vidvox".
- {% raw %}`Ctrl+Enter`{% endraw %} recompiles a shader during playback: stated in the same
  reference page under Editing shaders.
- ISF recommends against `gl_FragCoord` because *score*'s pipeline can run on
  OpenGL, Vulkan, Metal or Direct3D with differing Y directions: stated
  explicitly in that reference page.

## Corrections and open questions

- **Not verified in the running application yet.** Everything above is from the
  ossia documentation rather than from the build. Before this unit is treated as
  final, load a course shader into 3.8.2 and confirm the inlet count matches the
  header, which is the unit's own success criterion.
