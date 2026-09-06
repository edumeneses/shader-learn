#!/usr/bin/env python3
"""Crop and annotate a raw capture into a course figure.

Figures carry numbered badges rather than sentences. The numbers match the
walkthrough steps of the lesson, so the wording lives in the page, where it can
be corrected, translated, and read aloud when the videos are recorded, instead
of being baked into a PNG.

The spec is JSON, so a figure is reproducible from a raw capture:

    {
      "source": "raw-00-01.png",
      "out": "docs/learn/assets/00/00-01-annotated-score.png",
      "crop": [720, 64, 3280, 1472],
      "badges": [
        {"n": 1, "at": [1500, 80], "to": [1500, 140]},
        {"n": 2, "at": [860, 880]}
      ]
    }

`at` is a point in the SOURCE image; `to` is an optional target the leader line
points at. Coordinates are source pixels, so a spec survives re-cropping.

Usage:
    python3 scripts/annotate.py figures/00.json
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent

BADGE_R = 26
BADGE_FILL = (255, 176, 0)
BADGE_TEXT = (18, 18, 18)
LEADER = (255, 176, 0)
LEADER_WIDTH = 4

FONT_CANDIDATES = (
    "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
    "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
)


def font(size: int) -> ImageFont.FreeTypeFont:
    for path in FONT_CANDIDATES:
        if Path(path).exists():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def annotate(spec: dict, spec_dir: Path) -> Path:
    source = Path(spec["source"])
    if not source.is_absolute():
        source = (spec_dir / source).resolve()
    image = Image.open(source).convert("RGB")

    crop = spec.get("crop")
    ox, oy = 0, 0
    if crop:
        x0, y0, x1, y1 = crop
        image = image.crop((x0, y0, x1, y1))
        ox, oy = x0, y0

    draw = ImageDraw.Draw(image)
    label_font = font(spec.get("font_size", 30))

    for badge in spec["badges"]:
        bx, by = badge["at"][0] - ox, badge["at"][1] - oy
        if "to" in badge:
            tx, ty = badge["to"][0] - ox, badge["to"][1] - oy
            draw.line((bx, by, tx, ty), fill=LEADER, width=LEADER_WIDTH)
            draw.ellipse(
                (tx - 6, ty - 6, tx + 6, ty + 6), fill=LEADER
            )
        draw.ellipse(
            (bx - BADGE_R, by - BADGE_R, bx + BADGE_R, by + BADGE_R),
            fill=BADGE_FILL,
            outline=(18, 18, 18),
            width=3,
        )
        text = str(badge["n"])
        tw = draw.textlength(text, font=label_font)
        draw.text(
            (bx - tw / 2, by - label_font.size * 0.62),
            text,
            fill=BADGE_TEXT,
            font=label_font,
        )

    out = Path(spec["out"])
    if not out.is_absolute():
        out = ROOT / out
    out.parent.mkdir(parents=True, exist_ok=True)
    image.save(out)
    return out


def main() -> int:
    if len(sys.argv) < 2:
        print(__doc__)
        return 1
    for arg in sys.argv[1:]:
        spec_path = Path(arg)
        spec = json.loads(spec_path.read_text(encoding="utf8"))
        out = annotate(spec, spec_path.resolve().parent)
        image = Image.open(out)
        print(f"wrote {out.relative_to(ROOT)} ({image.width}x{image.height})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
