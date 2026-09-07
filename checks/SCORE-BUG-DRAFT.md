# Draft bug report for ossia score

**Not filed.** Written for Edu to send, edit, or discard. Everything below is
reproducible from files that ship with ossia's own documentation, so a
maintainer can check it without our repository.

---

## An ISF float input with no MIN/MAX sometimes gets an inlet domain of 0 to 0

**Version observed:** documents shipping with the current score-docs, opened as
JSON. Not yet confirmed against a live 3.8.2 session; see Caveat.

### What happens

When an ISF shader declares a `float` input without `MIN` and `MAX`, the inlet
*score* builds for it sometimes has a domain of `Min: 0.0, Max: 0.0`. A control
with that domain cannot be moved, and an automation curve addressed at it can
only ever produce zero.

It is inconsistent: **the same header produces a different domain in different
documents.**

### Evidence, from documents in score-docs

The header `{"NAME": "blurAmount", "TYPE": "float", "DEFAULT": 0.0}` appears in
three shipped documents. Two give it a usable domain and one does not:

| document | stored inlet domain |
|:---------|:--------------------|
| `reference/processes/3d.score` | `{"Min": 0.0, "Max": 1.0}` |
| `examples/3d/maxicube.score` | `{"Min": 0.0, "Max": 1.0}` |
| `reference/processes/point-cloud.score` | `{"Min": 0.0, "Max": 0.0}` |

And `{"NAME": "blurAmount", "TYPE": "float"}`, with no `DEFAULT` either, in
`common-practices/led-design/led-with-shaders.score`, gives
`{"Min": 0.0, "Max": 0.0}`.

Across every `.score` in `score-docs/assets/scores`, there are **135 float
inputs in ISF processes**. 123 declare `MIN` and `MAX` and are fine. Of the 12
that do not, **10 have a domain of 0 to 0** and 2 have 0 to 1. The ten include
seven inputs in `reference/processes/geometry-filter.score`, which is a
reference example, so a reader following it meets seven dead controls.

### Reproducing it from the JSON

```bash
cd score-docs/assets/scores
python3 - <<'PY'
import json, pathlib
def procs(o):
    if isinstance(o, dict):
        if "Fragment" in o and "Inlets" in o: yield o
        for v in o.values(): yield from procs(v)
    elif isinstance(o, list):
        for v in o: yield from procs(v)
for p in sorted(pathlib.Path('.').rglob('*.score')):
    try: d = json.load(open(p, encoding='utf8'))
    except Exception: continue
    for pr in procs(d):
        src = pr["Fragment"]; i, k = src.find("/*"), src.find("*/")
        if i < 0 or k < 0: continue
        try: hdr = json.loads(src[i+2:k])
        except Exception: continue
        by = {x.get("Custom"): x for x in pr["Inlets"] if x.get("Custom")}
        for inp in hdr.get("INPUTS", []):
            if inp.get("TYPE") != "float" or ("MIN" in inp and "MAX" in inp): continue
            il = by.get(inp["NAME"])
            if il: print(p, inp["NAME"], il.get("Domain"))
PY
```

### Expected

An ISF `float` input is allowed to omit `MIN` and `MAX`; the specification does
not require them. When it does, the inlet should get a usable default domain,
presumably 0 to 1, consistently. A degenerate 0-to-0 domain makes the control
inert and gives no indication why.

### Caveat, stated plainly

These are **saved documents**, so a stored domain is not proof of what score
does today at load time. Three explanations fit the evidence and we cannot tell
them apart from the outside:

1. a bug in deriving the domain when `MIN`/`MAX` are absent;
2. a change between score versions, with the 0-to-0 documents saved by an older
   one;
3. a user having edited the range in the interface before saving, though nobody
   would deliberately set 0 to 0.

We were unable to confirm against a running 3.8.2 because synthetic input cannot
reach score on the machine this was found on: it is a Wayland session and
Xwayland runs with `-enable-ei-portal`, so XTEST keyboard events are not
delivered even when the compositor has focused the window. See
`checks/VERIFICATION.md`.

**The check a maintainer can do in a minute:** load any ISF shader declaring a
float with no `MIN`/`MAX`, and look at the inlet's range in the inspector.

### Why it matters beyond the inspector

Ranges are the contract between a shader and everything that drives it. An
automation curve runs 0 to 1 in its own space and is mapped onto the inlet's
domain, so a domain of 0 to 0 silently turns every curve into a constant zero.
That failure has no error message and looks like a broken shader.

### Suggested wording for the docs either way

If the behaviour is intended, the ISF page could say that `MIN` and `MAX` are
effectively required for a `float` in score, which would be useful regardless.
