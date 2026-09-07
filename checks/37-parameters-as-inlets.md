# checks/37-parameters-as-inlets

**Unit 37.** Parameters as inlets. Pinned to *ossia score* 3.8.2.

## Figures

None yet, and **the most valuable figure in Phase 4 would be here**: a shader's
inlets beside the JSON header that produced them, with an automation curve
addressed at one of them.

## Design notes

This is the unit the whole course is arranged around, and it says so. Every
`focus` instead of `mouse` and every `drive` instead of `audioLevel` in the
thirty-six preceding units exists for the mechanism described here.

The naming list is repeated in full even though it appears in `CLAUDE.md` and in
several earlier units. It is the course's central convention and this is the
page where its payoff is visible.

## Verified against ossia score 3.8.2

**`MIN` and `MAX` become the inlet's `Domain` and `DEFAULT` becomes its `Init`.**
Read from `common-practices/led-design/led-with-shaders.score`: `iZoom` declared
`DEFAULT 1.0, MIN 0.1, MAX 2.0` produces `Init {"Float": 1.0}` and
`Domain {"Float": {"Min": 0.1, "Max": 2.0}}`. The unit's "ranges are contracts"
is now a reading rather than an inference.

Also verified: inlets are exposed for OSC under a **lower-cased** name.

## Corrections and open questions

- **Unverified in the application.** The mapping from ISF input type to inlet
  type is stated from the format and from ossia's shader page. The specific
  behaviour of `MIN` and `MAX` as a scaling contract for an automation curve is
  an inference and should be confirmed in 3.8.2.
