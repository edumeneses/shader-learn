# checks/13-noise

**Unit 13.** Value and gradient noise. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `13-noise` | `library/shaders/13/noise.fs` | both kinds, split, with the lattice drawn and the interpolation curve on a control |

## Re-verify when

- **The shader is edited.** The unit quotes the shared skeleton and all three
  interpolation curves, and its steps depend on value noise being on the left.

## Design notes

**The lattice overlay is the teaching device.** Without it, the difference
between the two kinds is a matter of taste; with it, the reader can see that
value noise puts its extremes *on* the lattice and gradient noise puts them
*between*, and that gradient noise is exactly mid grey at every lattice point.
That last fact is what lets someone identify gradient noise from a screenshot.

Step 4 says smoothstep and quintic look nearly identical here, which is honest:
the difference is in the second derivative and it shows up when octaves are
summed in Unit 14, not in a single octave.

## Corrections and open questions

- The unit says simplex noise was "patent-encumbered in three dimensions and
  above until 2022". Perlin's patent US6867776 was filed in 2001 and expired in
  January 2022. It covered simplex noise in three or more dimensions. Stated
  from that filing; **worth a second source** before this unit is final.
