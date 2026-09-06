#!/usr/bin/env python3
"""Screen capture and input driver for authoring the course figures.

Every figure in the course is captured from one pinned ossia score build at a
fixed window size, so that the whole set stays visually consistent and can be
re-shot when the pinned version changes. This script is that harness. It talks
to X11 directly through python-xlib, so it needs no screenshot utility and no
root privileges, and it uses the XTEST extension for synthetic input.

Requires: python-xlib, Pillow. Needs a running X server (DISPLAY).

Examples
--------
    # start score, wait for its window, size it to the figure format
    python3 scripts/capture.py launch --geometry 1920x1080+0+0

    # capture the score window, or a region of it
    python3 scripts/capture.py shot docs/learn/assets/00/raw-01.png
    python3 scripts/capture.py shot out.png --region 0,0,960,540

    # drive the interface
    python3 scripts/capture.py click 400 300
    python3 scripts/capture.py drag 400 300 700 340
    python3 scripts/capture.py key ctrl+n
    python3 scripts/capture.py type "my automation"
    python3 scripts/capture.py windows          # list, to find the right one
"""

from __future__ import annotations

import argparse
import os
import shlex
import subprocess
import sys
import time

from PIL import Image
from Xlib import X, display, Xatom
from Xlib.ext import xtest

APPIMAGE = os.path.expanduser(
    "~/Applications/ossia.score-3.8.2-linux-x86_64.AppImage"
)
WINDOW_MATCH = "score"

KEYSYMS = {
    "ctrl": "Control_L",
    "control": "Control_L",
    "alt": "Alt_L",
    "shift": "Shift_L",
    "super": "Super_L",
    "esc": "Escape",
    "enter": "Return",
    "return": "Return",
    "tab": "Tab",
    "space": "space",
    "del": "Delete",
    "delete": "Delete",
    "backspace": "BackSpace",
    "up": "Up",
    "down": "Down",
    "left": "Left",
    "right": "Right",
    "plus": "plus",
    "minus": "minus",
}


def dpy() -> display.Display:
    return display.Display()


# ---------------------------------------------------------------- windows


def window_list(d: display.Display) -> list[tuple[int, str, tuple[int, int, int, int]]]:
    """Every viewable window with a name, as (id, name, (x, y, w, h))."""
    out: list[tuple[int, str, tuple[int, int, int, int]]] = []
    root = d.screen().root

    def visit(win) -> None:
        try:
            attrs = win.get_attributes()
        except Exception:
            return
        if attrs.map_state == X.IsViewable:
            name = ""
            try:
                net_name = win.get_full_property(
                    d.intern_atom("_NET_WM_NAME"), 0
                )
                if net_name:
                    name = net_name.value.decode("utf8", "replace")
                else:
                    name = win.get_wm_name() or ""
            except Exception:
                name = ""
            if name:
                geo = win.get_geometry()
                coords = root.translate_coords(win, 0, 0)
                out.append(
                    (win.id, name, (coords.x, coords.y, geo.width, geo.height))
                )
        try:
            for child in win.query_tree().children:
                visit(child)
        except Exception:
            return

    visit(root)
    return out


def _is_frame(d: display.Display, win_id: int) -> bool:
    """True if this is a window-manager frame rather than an application window.

    A reparenting window manager wraps the main window in a frame that carries
    the same name and the same geometry, so a match by name finds two identical
    candidates. Capturing the frame's drawable returns the application's content
    only sometimes, which made figure captures come back missing a dialog for no
    visible reason; preferring the application's own window makes them
    deterministic.
    """
    try:
        cls = d.create_resource_object("window", win_id).get_wm_class()
    except Exception:
        return False
    return bool(cls) and "mutter" in cls[0].lower()


def find_window(d: display.Display, match: str):
    """Largest viewable window whose name contains `match`, case-insensitively.

    Frames lose to the window they wrap, at equal size, per `_is_frame`.
    """
    hits = [w for w in window_list(d) if match.lower() in w[1].lower()]
    if not hits:
        return None
    hits.sort(key=lambda w: (_is_frame(d, w[0]), -(w[2][2] * w[2][3])))
    win_id, name, geo = hits[0]
    return d.create_resource_object("window", win_id), name, geo


def activate(d: display.Display, win) -> None:
    """Raise and focus a window through the window manager."""
    root = d.screen().root
    data = [2, X.CurrentTime, 0, 0, 0]  # source 2 = pager
    event = display.event.ClientMessage(
        window=win,
        client_type=d.intern_atom("_NET_ACTIVE_WINDOW"),
        data=(32, data),
    )
    root.send_event(event, event_mask=X.SubstructureRedirectMask | X.SubstructureNotifyMask)
    win.configure(stack_mode=X.Above)
    d.sync()


def set_geometry(d: display.Display, win, spec: str) -> None:
    """Apply a `WxH+X+Y` geometry, unmaximising first so the request is honoured."""
    size, _, offset = spec.partition("+")
    w, _, h = size.partition("x")
    x, _, y = offset.partition("+") if offset else ("0", "", "0")

    root = d.screen().root
    # _NET_WM_STATE_REMOVE on maximised states, or the resize is ignored
    for state in ("_NET_WM_STATE_MAXIMIZED_HORZ", "_NET_WM_STATE_MAXIMIZED_VERT"):
        event = display.event.ClientMessage(
            window=win,
            client_type=d.intern_atom("_NET_WM_STATE"),
            data=(32, [0, d.intern_atom(state), 0, 1, 0]),
        )
        root.send_event(
            event, event_mask=X.SubstructureRedirectMask | X.SubstructureNotifyMask
        )
    d.sync()
    time.sleep(0.3)
    win.configure(x=int(x or 0), y=int(y or 0), width=int(w), height=int(h))
    d.sync()
    time.sleep(0.5)


# ---------------------------------------------------------------- capture


def grab_window(win, box: tuple[int, int, int, int]) -> Image.Image:
    """Capture directly from a window's own drawable.

    This is the path the course uses. Capturing the root window returns black
    here, because the compositor keeps the window's contents out of the root
    drawable; asking the window itself returns real pixels, and it has the
    further advantage of being independent of stacking, of the pointer, and of
    whatever else is on screen.

    `box` is x, y, w, h relative to the window's top-left corner.
    """
    x, y, w, h = box
    raw = win.get_image(x, y, w, h, X.ZPixmap, 0xFFFFFFFF)
    return Image.frombytes("RGB", (w, h), raw.data, "raw", "BGRX")


def grab(d: display.Display, box: tuple[int, int, int, int]) -> Image.Image:
    """Capture a screen rectangle from the root window.

    Kept for whole-screen captures. Note that on a compositing window manager
    this can come back black for redirected windows; prefer grab_window.

    The rectangle is clamped to the screen, since a window placed partly
    off-screen would otherwise make XGetImage fail with BadMatch.
    """
    screen = d.screen()
    x, y, w, h = box
    x0, y0 = max(0, x), max(0, y)
    x1 = min(screen.width_in_pixels, x + w)
    y1 = min(screen.height_in_pixels, y + h)
    w, h = x1 - x0, y1 - y0
    if w <= 0 or h <= 0:
        raise SystemExit(f"nothing to capture: {box} is off-screen")
    if (x0, y0, w, h) != box:
        print(f"clamped {box} to {(x0, y0, w, h)}")
    raw = screen.root.get_image(x0, y0, w, h, X.ZPixmap, 0xFFFFFFFF)
    return Image.frombytes("RGB", (w, h), raw.data, "raw", "BGRX")


def _score_descendant(d: display.Display, win, depth: int = 3):
    """First descendant whose WM_CLASS is score's, or None."""
    if depth <= 0:
        return None
    try:
        kids = win.query_tree().children
    except Exception:
        return None
    for kid in kids:
        try:
            if kid.get_attributes().map_state != X.IsViewable:
                continue
            cls = kid.get_wm_class()
            if cls and any("score" in c.lower() or "ossia" in c.lower() for c in cls):
                return kid
            deeper = _score_descendant(d, kid, depth - 1)
            if deeper is not None:
                return deeper
        except Exception:
            continue
    return None


def popups(d: display.Display, exclude_id: int) -> list:
    """score's own menus, dialogs, and tooltips, bottom-to-top.

    Qt draws these in separate windows, so a capture of the main window's
    drawable does not contain them. Compositing them back in keeps menu and
    dialog figures in the same capture format as every other figure.

    Two kinds qualify, and nothing else: override-redirect windows, which is
    what menus, combo boxes, and tooltips are; and normal windows whose WM_CLASS
    is score's, which is what modal dialogs are. Anything else on the desktop,
    including other applications, is excluded -- an early version of this
    composited the terminal it was being run from into a figure.
    """
    out = []
    root = d.screen().root
    screen = d.screen()
    for child in root.query_tree().children:       # bottom-to-top stacking order
        try:
            if child.id == exclude_id:
                continue
            attrs = child.get_attributes()
            if attrs.map_state != X.IsViewable:
                continue

            mine = False
            target = child
            if attrs.override_redirect:
                mine = True                        # menu, combo, or tooltip
            else:
                cls = child.get_wm_class()
                if cls and any("score" in c.lower() or "ossia" in c.lower()
                               for c in cls):
                    mine = True                    # score's own dialog
                elif cls and "mutter" in cls[0].lower():
                    # A reparenting window manager wraps dialogs in a frame whose
                    # WM_CLASS is the manager's, not the application's. Descend to
                    # find the real window: score's add-device dialog is only
                    # reachable this way.
                    inner = _score_descendant(d, child)
                    if inner is not None:
                        mine, target = True, inner
            if not mine:
                continue
            child = target
            geo = child.get_geometry()
            if geo.width < 16 or geo.height < 16:
                continue
            if geo.width >= screen.width_in_pixels and \
               geo.height >= screen.height_in_pixels:
                continue                            # guard / desktop windows
            coords = root.translate_coords(child, 0, 0)
            out.append((child, coords.x, coords.y, geo.width, geo.height))
        except Exception:
            continue
    return out


def set_fullscreen(d: display.Display, win, on: bool = True) -> None:
    """Ask the window manager for fullscreen.

    This is how the figure format is made deterministic: fullscreen drops the
    decorations, so the window is exactly the screen size and every figure of
    the course shares one geometry.
    """
    root = d.screen().root
    event = display.event.ClientMessage(
        window=win,
        client_type=d.intern_atom("_NET_WM_STATE"),
        data=(32, [1 if on else 0, d.intern_atom("_NET_WM_STATE_FULLSCREEN"), 0, 1, 0]),
    )
    root.send_event(
        event, event_mask=X.SubstructureRedirectMask | X.SubstructureNotifyMask
    )
    d.sync()
    time.sleep(0.8)


def shot(args: argparse.Namespace) -> int:
    d = dpy()
    if args.full:
        screen = d.screen()
        box = (0, 0, screen.width_in_pixels, screen.height_in_pixels)
        image = grab(d, box)
    else:
        found = find_window(d, args.match)
        if not found:
            print(f"no window matching {args.match!r}; try `windows`")
            return 1
        win, name, geo = found
        if args.raise_first:
            activate(d, win)
            time.sleep(args.settle)
            found = find_window(d, args.match)
            win, name, geo = found
        print(f"window {name!r} at {geo}")

        box = (0, 0, geo[2], geo[3])
        if args.region:
            rx, ry, rw, rh = (int(v) for v in args.region.split(","))
            box = (rx, ry, min(rw, geo[2] - rx), min(rh, geo[3] - ry))
        image = grab_window(win, box)

        if args.popups:
            for child, px, py, pw, ph in popups(d, win.id):
                try:
                    layer = grab_window(child, (0, 0, pw, ph))
                except Exception:
                    continue
                # position relative to the captured region
                image.paste(layer, (px - geo[0] - box[0], py - geo[1] - box[1]))
                print(f"composited popup {pw}x{ph} at {px},{py}")
    if args.scale != 1.0:
        image = image.resize(
            (int(image.width * args.scale), int(image.height * args.scale)),
            Image.LANCZOS,
        )
    os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
    image.save(args.out)
    print(f"wrote {args.out} ({image.width}x{image.height})")
    return 0


# ---------------------------------------------------------------- input


def _normal_windows(d: display.Display) -> list:
    """Viewable, non-override, non-frame top-level windows, bottom-to-top."""
    out = []
    root = d.screen().root
    for child in root.query_tree().children:
        try:
            attrs = child.get_attributes()
            if attrs.map_state != X.IsViewable or attrs.override_redirect:
                continue
            geo = child.get_geometry()
            if geo.width < 200 or geo.height < 200:
                continue
            cls = child.get_wm_class()
            if cls and "mutter" in cls[0].lower():
                continue                          # window-manager frames
            coords = root.translate_coords(child, 0, 0)
            out.append((child.id, cls, coords.x, coords.y, geo.width, geo.height))
        except Exception:
            continue
    return out


def covered_at(d: display.Display, win_id: int, x: int, y: int):
    """What, if anything, sits above `win_id` at the point (x, y).

    Synthetic clicks go to whatever is under the pointer, not to the window we
    think we are driving: a fullscreen browser above score once turned a figure
    run into clicks landing in somebody's browser. Checking the specific point
    rather than the whole stack matters, because a window parked mostly
    off-screen is topmost and yet covers nothing we care about.
    """
    stack = _normal_windows(d)
    ids = [w[0] for w in stack]
    if win_id not in ids:
        return None
    above = stack[ids.index(win_id) + 1:]
    for _id, cls, wx, wy, ww, wh in above:
        if wx <= x < wx + ww and wy <= y < wy + wh:
            return cls or "an unnamed window"
    return None


def require_clear(d: display.Display, match: str, x: int, y: int) -> None:
    found = find_window(d, match)
    if not found:
        raise SystemExit(f"no window matching {match!r}")
    who = covered_at(d, found[0].id, x, y)
    if who:
        raise SystemExit(
            f"refusing to send input: {who} covers the target window at {x},{y}. "
            "Raise score, or move that window, and try again."
        )


def require_focus(d: display.Display, match: str) -> None:
    """For keystrokes, which go to the focused window rather than to a point."""
    found = find_window(d, match)
    if not found:
        raise SystemExit(f"no window matching {match!r}")
    activate(d, found[0])
    time.sleep(0.4)
    win = d.get_input_focus().focus
    for _ in range(8):                            # focus may be on a child
        try:
            if getattr(win, "id", None) == found[0].id:
                return
            win = win.query_tree().parent
        except Exception:
            break
    print("warning: score may not have keyboard focus; sending anyway")


def move(d: display.Display, x: int, y: int) -> None:
    xtest.fake_input(d, X.MotionNotify, x=x, y=y)
    d.sync()


def click(args: argparse.Namespace) -> int:
    d = dpy()
    require_clear(d, args.match, args.x, args.y)
    move(d, args.x, args.y)
    time.sleep(0.15)
    for _ in range(args.count):
        xtest.fake_input(d, X.ButtonPress, args.button)
        d.sync()
        time.sleep(0.05)
        xtest.fake_input(d, X.ButtonRelease, args.button)
        d.sync()
        time.sleep(0.12)
    print(f"clicked {args.x},{args.y} button={args.button} x{args.count}")
    return 0


def drag(args: argparse.Namespace) -> int:
    d = dpy()
    require_clear(d, args.match, args.x1, args.y1)
    move(d, args.x1, args.y1)
    time.sleep(0.2)
    xtest.fake_input(d, X.ButtonPress, args.button)
    d.sync()
    steps = max(args.steps, 1)
    for i in range(1, steps + 1):
        move(
            d,
            int(args.x1 + (args.x2 - args.x1) * i / steps),
            int(args.y1 + (args.y2 - args.y1) * i / steps),
        )
        time.sleep(0.02)
    time.sleep(0.15)
    xtest.fake_input(d, X.ButtonRelease, args.button)
    d.sync()
    print(f"dragged {args.x1},{args.y1} -> {args.x2},{args.y2}")
    return 0


def keycode(d: display.Display, name: str) -> int:
    from Xlib import XK

    sym = XK.string_to_keysym(KEYSYMS.get(name.lower(), name))
    if sym == 0 and len(name) == 1:
        sym = ord(name)
    return d.keysym_to_keycode(sym)


def key(args: argparse.Namespace) -> int:
    d = dpy()
    require_focus(d, args.match)
    parts = args.combo.split("+")
    mods, base = parts[:-1], parts[-1]
    codes = [keycode(d, m) for m in mods]
    for code in codes:
        xtest.fake_input(d, X.KeyPress, code)
    xtest.fake_input(d, X.KeyPress, keycode(d, base))
    d.sync()
    time.sleep(0.05)
    xtest.fake_input(d, X.KeyRelease, keycode(d, base))
    for code in reversed(codes):
        xtest.fake_input(d, X.KeyRelease, code)
    d.sync()
    print(f"key {args.combo}")
    return 0


def type_text(args: argparse.Namespace) -> int:
    d = dpy()
    require_focus(d, args.match)
    from Xlib import XK

    for char in args.text:
        sym = ord(char)
        code = d.keysym_to_keycode(sym)
        shift = char.isupper() or char in '!@#$%^&*()_+{}|:"<>?~'
        if shift:
            xtest.fake_input(d, X.KeyPress, keycode(d, "shift"))
        xtest.fake_input(d, X.KeyPress, code)
        d.sync()
        time.sleep(0.02)
        xtest.fake_input(d, X.KeyRelease, code)
        if shift:
            xtest.fake_input(d, X.KeyRelease, keycode(d, "shift"))
        d.sync()
        time.sleep(0.02)
    print(f"typed {len(args.text)} chars")
    return 0


# ---------------------------------------------------------------- launch


def waitshot(args: argparse.Namespace) -> int:
    """Wait for a dialog to appear, then capture the window with it composited.

    Some dialogs cannot be opened by synthetic input at all: score's add-device
    dialog registers the click on the menu row and simply does not appear. The
    remedy is to let a human open it while this watches, so nobody has to reach
    for a terminal while a modal dialog is in the way.
    """
    d = dpy()
    deadline = time.time() + args.timeout
    print(f"watching for a dialog at least {args.min_size} px wide, "
          f"{args.timeout:.0f}s to go; open it now")
    while time.time() < deadline:
        found = find_window(d, args.match)
        if found:
            big = []
            for cand in popups(d, found[0].id):
                if cand[3] < args.min_size or cand[4] < 120:
                    continue
                if args.title:
                    try:
                        nm = cand[0].get_wm_name() or ""
                    except Exception:
                        nm = ""
                    if args.title.lower() not in nm.lower():
                        print(f"  ignoring {cand[3]}x{cand[4]} titled {nm!r}")
                        continue
                big.append(cand)
            if big:
                for _child, px, py, pw, ph in big:
                    print(f"  dialog {pw}x{ph} at {px},{py}")
                time.sleep(args.settle)          # let it finish drawing
                win, _name, geo = find_window(d, args.match)
                image = grab_window(win, (0, 0, geo[2], geo[3]))
                for child, px, py, pw, ph in popups(d, win.id):
                    try:
                        layer = grab_window(child, (0, 0, pw, ph))
                    except Exception:
                        continue
                    image.paste(layer, (px - geo[0], py - geo[1]))
                os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
                image.save(args.out)
                print(f"wrote {args.out} ({image.width}x{image.height})")
                return 0
        time.sleep(1.0)
    print("timed out with no dialog")
    return 1


def menu(args: argparse.Namespace) -> int:
    """Open a menu, measure its rows, and click one by index.

    Clicking menu items by guessed coordinates is unreliable: the popup's
    position depends on where it was opened and its rows are not evenly spaced
    once separators are involved. This opens the menu, finds the popup window,
    scans its own drawable for rows containing text, and clicks the row asked
    for. `--pick 0` only reports the rows it found, which is how you discover
    the index you want.
    """
    d = dpy()
    found = find_window(d, args.match)
    if not found:
        print(f"no window matching {args.match!r}")
        return 1
    main = found[0]
    activate(d, main)                       # the menu will not open unfocused
    time.sleep(0.8)
    who = covered_at(d, main.id, args.x, args.y)
    if who:
        raise SystemExit(
            f"refusing to open a menu: {who} covers the target at {args.x},{args.y}."
        )

    move(d, args.x, args.y)
    time.sleep(0.2)
    xtest.fake_input(d, X.ButtonPress, args.button)
    d.sync()
    time.sleep(0.05)
    xtest.fake_input(d, X.ButtonRelease, args.button)
    d.sync()
    time.sleep(args.settle)

    pops = [p for p in popups(d, main.id) if p[3] > 80 and p[4] > 40]
    if not pops:
        print("no menu appeared")
        return 1
    child, px, py, pw, ph = pops[-1]           # topmost
    image = grab_window(child, (0, 0, pw, ph)).convert("L")

    rows = []
    for y in range(ph):
        n = sum(1 for x in range(4, pw - 4, 2) if image.getpixel((x, y)) > 150)
        rows.append(n >= 3)
    groups, run = [], None
    for y, hot in enumerate(rows):
        if hot and run is None:
            run = y
        elif not hot and run is not None:
            if y - run >= 4:
                groups.append((run + y) // 2)
            run = None
    print(f"menu at {px},{py} size {pw}x{ph}, {len(groups)} text rows")
    for i, cy in enumerate(groups, 1):
        print(f"  row {i}: y={py + cy}")

    if args.pick:
        if args.pick > len(groups):
            print(f"only {len(groups)} rows")
            return 1
        cx = px + min(60, pw // 3)
        cy = py + groups[args.pick - 1]
        move(d, cx, cy)
        time.sleep(0.25)
        xtest.fake_input(d, X.ButtonPress, 1)
        d.sync()
        time.sleep(0.05)
        xtest.fake_input(d, X.ButtonRelease, 1)
        d.sync()
        print(f"clicked row {args.pick} at {cx},{cy}")
    return 0


def launch(args: argparse.Namespace) -> int:
    d = dpy()
    existing = find_window(d, args.match)
    if existing and not args.force:
        print(f"already running: {existing[1]!r} at {existing[2]}")
    else:
        if not os.path.exists(args.appimage):
            print(f"missing {args.appimage}")
            return 1
        # shlex, not str.split: a path with a space in it was being split into
        # two nonexistent paths, and score then opened an empty window that looks
        # exactly like a document which failed to load for some interesting reason.
        cmd = [args.appimage] + (shlex.split(args.open) if args.open else [])
        env = dict(os.environ)
        if args.qt_scale:
            # Figures are specified at 2x device pixels. On a display with no
            # HiDPI scaling, forcing Qt's scale factor is what produces them:
            # a 3840x2160 window at scale 2 is a 1920x1080 logical layout.
            env["QT_SCALE_FACTOR"] = str(args.qt_scale)
        subprocess.Popen(
            cmd,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            start_new_session=True,
            env=env,
        )
        print(f"launched {os.path.basename(args.appimage)}")

    deadline = time.time() + args.timeout
    found = None
    while time.time() < deadline:
        found = find_window(d, args.match)
        if found and found[2][2] > 300:
            break
        time.sleep(0.5)
    if not found:
        print(f"no window matching {args.match!r} after {args.timeout}s")
        return 1

    win, name, geo = found
    activate(d, win)
    if args.fullscreen:
        set_fullscreen(d, win)
        found = find_window(d, args.match)
        geo = found[2]
    elif args.geometry:
        set_geometry(d, win, args.geometry)
        found = find_window(d, args.match)
        geo = found[2]
    print(f"window {name!r} at {geo}")
    return 0


def windows(args: argparse.Namespace) -> int:
    d = dpy()
    for win_id, name, geo in window_list(d):
        if args.match.lower() in name.lower() or args.match == "":
            print(f"0x{win_id:08x}  {geo[2]:>5}x{geo[3]:<5} +{geo[0]},{geo[1]}  {name}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--match", default=WINDOW_MATCH, help="window name substring")
    sub = parser.add_subparsers(dest="cmd", required=True)

    p = sub.add_parser("launch", help="start score and size its window")
    p.add_argument("--appimage", default=APPIMAGE)
    p.add_argument("--geometry", default=None, help="WxH+X+Y")
    p.add_argument(
        "--fullscreen",
        action="store_true",
        help="fullscreen instead of a geometry: no decorations, exactly the "
        "screen size, which is what keeps the figure format identical",
    )
    p.add_argument("--open", default=None, help="score file to open")
    p.add_argument("--timeout", type=float, default=90.0)
    p.add_argument("--force", action="store_true", help="launch even if running")
    p.add_argument(
        "--qt-scale",
        type=float,
        default=None,
        help="QT_SCALE_FACTOR for the child; use 2 with a 3840x2160 window "
        "to get 2x figures of a 1920x1080 layout",
    )
    p.set_defaults(func=launch)

    p = sub.add_parser("shot", help="capture the window or a region of it")
    p.add_argument("out")
    p.add_argument("--region", default=None, help="x,y,w,h inside the window")
    p.add_argument("--full", action="store_true", help="whole screen")
    p.add_argument("--scale", type=float, default=1.0)
    p.add_argument("--settle", type=float, default=0.6)
    p.add_argument(
        "--popups",
        action="store_true",
        help="composite open menus, dialogs, and tooltips into the capture; "
        "Qt draws them in separate windows that a window-drawable capture misses",
    )
    p.add_argument(
        "--raise-first",
        action="store_true",
        help="activate the window before capturing; unnecessary with "
        "window-level capture and it disturbs whatever the user is doing",
    )
    p.set_defaults(func=shot)

    p = sub.add_parser("click")
    p.add_argument("x", type=int)
    p.add_argument("y", type=int)
    p.add_argument("--button", type=int, default=1)
    p.add_argument("--count", type=int, default=1)
    p.set_defaults(func=click)

    p = sub.add_parser("drag")
    for name in ("x1", "y1", "x2", "y2"):
        p.add_argument(name, type=int)
    p.add_argument("--button", type=int, default=1)
    p.add_argument("--steps", type=int, default=25)
    p.set_defaults(func=drag)

    p = sub.add_parser("key")
    p.add_argument("combo", help="e.g. ctrl+n, alt+f, esc")
    p.set_defaults(func=key)

    p = sub.add_parser("type")
    p.add_argument("text")
    p.set_defaults(func=type_text)

    p = sub.add_parser("waitshot", help="wait for a dialog, then capture")
    p.add_argument("out")
    p.add_argument("--timeout", type=float, default=180.0)
    p.add_argument("--min-size", type=int, default=300, help="dialog width to accept")
    p.add_argument("--title", default=None,
                   help="only accept a dialog whose title contains this, which is "
                        "how the start screen stops being mistaken for a dialog")
    p.add_argument("--settle", type=float, default=1.2)
    p.set_defaults(func=waitshot)

    p = sub.add_parser("menu", help="open a menu and click one of its rows")
    p.add_argument("x", type=int, help="where to click to open the menu")
    p.add_argument("y", type=int)
    p.add_argument("--button", type=int, default=1, help="3 for a context menu")
    p.add_argument("--pick", type=int, default=0, help="row to click; 0 only lists")
    p.add_argument("--settle", type=float, default=1.5)
    p.set_defaults(func=menu)

    p = sub.add_parser("windows", help="list viewable windows")
    p.set_defaults(func=windows)

    args = parser.parse_args()
    if not os.environ.get("DISPLAY"):
        print("DISPLAY is unset; this harness needs a running X server")
        return 1
    return args.func(args)


if __name__ == "__main__":
    sys.exit(main())
