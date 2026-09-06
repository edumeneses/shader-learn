#!/usr/bin/env python3
"""Type into a widget that already has keyboard focus, without re-activating.

`capture.py key` and `capture.py type` call `require_focus`, which activates the
main window before sending. That is right for shortcuts and for the panels, and
wrong for anything typed into a sub-widget: activation resets the keyboard focus
to the window's default widget, so the keystrokes land in the score instead of in
the editor you clicked into. The consequence is not merely that nothing is typed.
`ctrl+a` followed by `delete`, meant to replace a script, is Select All and Delete
in the main window.

This sends a click to put focus where it belongs and then types, using XTEST and
nothing else, so the focus that the click established survives.

    python3 scripts/typeinto.py 2000 800 --file processor.dsp
    python3 scripts/typeinto.py 2000 800 --select-all --text 'process = _ : _;'

Newlines in the text are sent as Return. `--select-all` sends ctrl+a first, which
is safe here precisely because focus is not reset.
"""

from __future__ import annotations

import argparse
import sys
import time

from Xlib import X, XK, display
from Xlib.ext import xtest

def keymap(d: display.Display) -> dict[int, tuple[int, int]]:
    """keysym -> (keycode, shift level), read from the server's own keymap.

    Assuming a US layout is what breaks here. This machine's keyboard is a
    multilingual one on which keycode 48 carries `dead_acute`, `dead_diaeresis`,
    `apostrophe`, and `quotedbl` at levels 0 to 3, so a quote needs AltGr and shift
    together and `keysym_to_keycode` plus a guessed shift silently produces
    nothing. A missing quote is a compile error in a Faust or ISF script rather
    than a typo you can see in the figure, so resolve it properly: ask the server
    which keycode and which level carries the character, and press the modifiers
    that level requires.

    Levels are the usual four: plain, shift, AltGr, AltGr with shift.
    """
    # One request, rather than a call per keycode: python-xlib's per-keycode path
    # pumps the event queue. That matters because a RandR event sitting in this
    # connection's queue makes python-xlib raise
    # `AttributeError: 'BadRRModeError' object has no attribute 'sequence_number'`
    # from inside its own error handler, which no caller can usefully catch by
    # type. A connection opened fresh has no such queue, so read the map on one.
    fresh = display.Display()
    try:
        lo = fresh.display.info.min_keycode
        hi = fresh.display.info.max_keycode
        rows = fresh.get_keyboard_mapping(lo, hi - lo + 1)
    finally:
        pass

    out: dict[int, tuple[int, int]] = {}
    for index, syms in enumerate(rows):
        code = lo + index
        for level, sym in enumerate(syms[:4]):
            if sym and sym not in out:
                out[sym] = (code, level)
    return out


# python-xlib's XK does not carry the ISO keysym names, and `string_to_keysym`
# returns 0 for them rather than failing. Silently getting no AltGr is how the
# quotes came out as diaereses.
EXTRA_KEYSYMS = {"ISO_Level3_Shift": 0xFE03, "ISO_Level5_Shift": 0xFE11}


def code_for(table: dict[int, tuple[int, int]], name: str) -> int:
    """A named key's keycode, from the table rather than from the connection.

    Everything is resolved through the table on purpose: `keysym_to_keycode`
    round-trips, and a round-trip is what trips the BadRRModeError above.
    """
    sym = EXTRA_KEYSYMS.get(name) or XK.string_to_keysym(name)
    if sym == 0 and len(name) == 1:
        sym = ord(name)
    entry = table.get(sym)
    return entry[0] if entry else 0


def tap(d: display.Display, code: int, level: int = 0,
        shift: int = 0, altgr: int = 0) -> None:
    """Press one key at a shift level, holding whatever modifiers it needs."""
    held = []
    if level in (1, 3) and shift:
        held.append(shift)
    if level in (2, 3) and altgr:
        held.append(altgr)
    for mod in held:
        xtest.fake_input(d, X.KeyPress, mod)
    xtest.fake_input(d, X.KeyPress, code)
    d.sync()
    time.sleep(0.012)
    xtest.fake_input(d, X.KeyRelease, code)
    for mod in reversed(held):
        xtest.fake_input(d, X.KeyRelease, mod)
    d.sync()
    time.sleep(0.012)


def main() -> int:
    p = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    p.add_argument("x", type=int, help="where to click to give the widget focus")
    p.add_argument("y", type=int)
    p.add_argument("--text", default=None)
    p.add_argument("--file", default=None, help="read the text from here")
    p.add_argument("--select-all", action="store_true",
                   help="ctrl+a before typing, to replace existing content")
    p.add_argument("--settle", type=float, default=0.6)
    args = p.parse_args()

    if args.file:
        text = open(args.file, encoding="utf8").read()
    elif args.text is not None:
        text = args.text
    else:
        text = sys.stdin.read()

    d = display.Display()
    table = keymap(d)
    shift = code_for(table, "Shift_L")
    altgr = code_for(table, "ISO_Level3_Shift")
    ctrl = code_for(table, "Control_L")

    xtest.fake_input(d, X.MotionNotify, x=args.x, y=args.y)
    d.sync()
    time.sleep(0.2)
    xtest.fake_input(d, X.ButtonPress, 1)
    d.sync()
    time.sleep(0.05)
    xtest.fake_input(d, X.ButtonRelease, 1)
    d.sync()
    time.sleep(args.settle)

    if args.select_all:
        xtest.fake_input(d, X.KeyPress, ctrl)
        tap(d, code_for(table, "a"))
        xtest.fake_input(d, X.KeyRelease, ctrl)
        d.sync()
        time.sleep(0.2)

    missing = []
    for char in text:
        if char == "\n":
            tap(d, code_for(table, "Return"))
            continue
        if char == "\t":
            tap(d, code_for(table, "Tab"))
            continue
        entry = table.get(ord(char))
        if entry is None:
            missing.append(char)
            continue
        tap(d, entry[0], entry[1], shift=shift, altgr=altgr)

    if missing:
        # Loud, because a silently dropped character is a compile error later.
        print(f"UNTYPED, no key carries them: {''.join(sorted(set(missing)))!r}")
        return 1
    print(f"typed {len(text)} chars at {args.x},{args.y}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
