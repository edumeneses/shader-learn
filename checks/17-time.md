# checks/17-time

**Unit 17.** Time, phase, and easing. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `17-time` | `library/shaders/17/time.fs` | seven easing curves plotted above the motion, with a phase cascade |

## Re-verify when

- **The shader is edited.** The unit quotes the phase idiom and the ping-pong
  fold verbatim, tells the reader to read `phaseAt`, and its steps walk the
  `LABELS` array in order.

## Design notes

**The plot and the motion share a screen on purpose.** An easing curve on its
own is a graph, and motion on its own is a feel; the marker riding the curve at
the current phase is what connects them, and it is the reason this figure works
where a catalogue of curves does not.

The `back` and `elastic` curves leave 0..1, and the plot draws a red band at 1
so that is visible rather than surprising. The unit makes a point of it: an
easing curve is not required to stay in its range, and the ones that leave are
the ones that feel physical.

The trail behind each dot is drawn by sampling the easing function at earlier
phases, **not** by feedback. That is deliberate: Unit 18 is next and the
distinction between "ask the same function about the past" and "remember the
past" is the thing it turns on.

## Corrections and open questions

Nothing yet.
