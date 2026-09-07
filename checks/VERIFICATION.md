# Verification against ossia score 3.8.2

What has been checked in, or against, a real *ossia score* 3.8.2, and what has
not. Written after the first verification pass, 2026-09-06.

The method that worked was not the one that was planned. Driving the interface
turned out to be blocked by the compositor, and reading the documents *score*
ships turned out to be **better evidence** for the claims that mattered: a
`.score` file is JSON, so the inlets *score* builds from a shader's JSON header
can be read directly, in bulk, across every example ossia publishes.

## Verified, from documents ossia ships

**Every ISF input becomes an inlet, in declaration order.** Checked across
**44 ISF processes** in 20 shipped `.score` documents. Every one matches, once
image inputs are accounted for by the rule below.

**An `image` input becomes a texture inlet; every other type becomes a value
inlet.** In `reference/processes/point-cloud.score` the header declares
`inputImage` then `blurAmount`, and the process has inlet 0 with no value, no
domain and no exposed name, then inlet 1 carrying all three. Confirms Unit 29's
table and explains the seven apparent mismatches in the bulk check, all of which
were image inputs.

**The type mapping is exactly as Unit 29 states.** From
`common-practices/led-design/led-with-shaders.score`:

| header | inlet value | inlet uuid prefix |
|:-------|:------------|:------------------|
| `bool` | `{"Bool": ...}` | `fb27e4cb` |
| `float` | `{"Float": ...}` | `af2b4fc3` |
| `point2D` | `{"Vec2f": [...]}` | `0adbbdda` |
| `color` | `{"Vec4f": [...]}` | `8f38638e` |
| `image` | none; texture inlet | `5ac86198` |

**`DEFAULT` becomes the inlet's `Init`, and `MIN`/`MAX` become its `Domain`.**
Same document: `iZoom` declared `DEFAULT 1.0, MIN 0.1, MAX 2.0` produces
`Init {"Float": 1.0}` and `Domain {"Float": {"Min": 0.1, "Max": 2.0}}`. This is
the mechanism Unit 37 calls a contract, and it is now a reading rather than an
assertion.

**A float with no `MIN`/`MAX` gets a domain of 0 to 0.** `blurAmount` in the
same shipped document has no range in its header and an inlet domain of
`{"Min": 0.0, "Max": 0.0}`. An automation curve on that inlet can only produce
zero. Added to Units 29 and 37 as a named mistake, with the citation.

**Inlets are exposed for OSC under a lower-cased name.** `blurAmount` is exposed
as `bluramount`. Added to Unit 37.

**A texture reaches a window at the address `Window:/`.** Present in
`reference/processes/vertex-shader-art.score`. Confirms Unit 35 and the score
course's own finding.

**The VSA header keys are `MODE: VERTEX_SHADER_ART`, `POINT_COUNT`,
`PRIMITIVE_MODE`, `LINE_SIZE`, `BACKGROUND_COLOR`.** Read from
`reference/processes/vertex-shader-art.score`. Exactly what `scripts/isf.py`
implements.

**VSA uniform names are vertexshaderart.com's.** The shipped shader uses
`vertexId`, `vertexCount`, `time`, `resolution`, `v_color`, `gl_PointSize`, and
`gl_Position`, and **none** of ISF's `TIME` or `RENDERSIZE`.

## Corrected as a result

- **Unit 31 overclaimed.** It said ossia supplies both the vertexshaderart and
  the ISF uniform names. Only the first is evidenced. The unit now says this
  course's toolchain supplies both and tells the reader not to rely on it in
  *score*.
- **Units 29 and 37** gained the verified inlet rule, the lower-cased OSC name,
  and the 0-to-0 domain trap.
- **Unit 30** gained a real citation for its central argument: the shipped
  `led-with-shaders.score` is a faithful Shadertoy port that kept `iMouse`,
  `iZoom`, `iSteps`, `iColor`, and `iMouse`'s range of 0 to 640 by 480 in
  pixels. The unit's warning is about a habit that is in ossia's own examples.
- **Unit 35** now says the address is `Window:/` and that a reader can confirm
  it by grepping a document.

## Blocked: driving the interface

*score* 3.8.2 launches and its window can be found and captured. Two things do
not work under this machine's compositor, and they are recorded so nobody
repeats the hour:

- **XTEST pointer motion works.** Asking for (900, 600) moves the pointer to
  (900, 600); `query_pointer` confirms it.
- **Focus can be taken, and it does not help.** An earlier version of this note
  said no X window ever holds keyboard focus. That was wrong and is corrected
  here. `capture.py`'s `activate()` sends `_NET_ACTIVE_WINDOW` with source 2 and
  a `CurrentTime` timestamp, which mutter refuses under focus-stealing
  prevention. Sending it with **source 1 and a real server timestamp**, obtained
  by appending zero bytes to a root property and reading the `PropertyNotify`,
  is accepted: `_NET_ACTIVE_WINDOW` then reports score's own window. A direct
  `XSetInputFocus` is accepted too.
- **Keys still do not arrive.** With the compositor's focus demonstrably on
  score's main window, `Ctrl+N` produces no document and typed characters do not
  appear in a focused text field. Xwayland here runs with `-enable-ei-portal`,
  which routes synthetic input through the RemoteDesktop portal; XTEST **pointer
  motion works** and XTEST **keyboard does not reach the client**. That, rather
  than focus, is the blocker. Keyboard injection was stopped rather than retried
  because keys that go nowhere visible might not be going nowhere.

**`:1` is not a separate X server.** The score course used `DISPLAY=:1` and this
note first assumed that meant a dedicated server. It does not: the single
Xwayland process is started with two listen descriptors and serves `:0` and `:1`
alike, and enumerating both gives an identical window list. The score course's
own figure instructions say the work "needs an unlocked session", which is the
real explanation: a human was at the machine with score genuinely focused and
receiving input, and the tooling never had to inject keys into an unfocused
application.

`Xvfb` and `Xephyr` are not installed.

**To unblock**, any one of:

1. `sudo apt install xserver-xephyr`, then run score inside a nested X server
   where XTEST owns the focus. This is the closest to the score course's setup.
2. Run the keyboard steps by hand, with score focused, and capture around them.
3. Log into an X11 session rather than Wayland for the figure session.

## A bug found while verifying

An ISF `float` input with no `MIN`/`MAX` sometimes gets an inlet domain of 0 to
0, which makes the control inert and any automation curve on it a constant zero.
The same header gives 0 to 1 in other shipped documents. Written up with
reproduction steps and an honest caveat in `checks/SCORE-BUG-DRAFT.md`, for Edu
to file or discard.

## Still unverified, in priority order

1. **Unit 36, step 5**: that a compile error leaves the last working shader
   rendering rather than blacking the output. The unit tells a reader they can
   rely on this in a performance. Needs the application.
2. **Unit 40**: whether ossia's FFT texture is linear in frequency across the
   audible range, which the shader's band boundaries assume.
3. **Unit 35's walkthrough** as a sequence: adding a window device, dropping a
   shader, addressing the outlet, live editing.
4. **Figure 37-01**, a shader's inlets beside the JSON that produced them.
5. **Unit 38's frame-rate claims** about chain length and memory traffic.

## Incidentally

The course's 36 shaders were copied to `~/Documents/ossia/score/shader-learn/`,
which is under the library root recorded in `~/.config/ossia/score.conf`, so
they appear in *score*'s own library panel. That is where a reader following
Unit 01 step 4 would put them.
