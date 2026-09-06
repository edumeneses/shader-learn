#!/usr/bin/env python3
"""Structural checks for the Learn shader art course.

Run from the repository root:  python3 scripts/check_units.py

Enforces the rules the course commits to, so that a drifting unit fails the
build rather than reaching a reader:

  1. required front matter keys are present;
  2. the reading budget holds: body prose between MIN_WORDS and MAX_WORDS,
     which is 10 to 15 minutes at 180 to 200 words per minute. Code fences and
     link targets are stripped before counting, because a reader reads neither
     at prose speed;
  3. permalink matches the file name, since published permalinks are
     contractual once a unit is written;
  4. score_version matches the pinned site version on every unit that declares
     one, and every unit that _data/units.yml marks `score: true` declares one;
  5. a checks/ note exists for every unit, recording what to re-verify when the
     pinned version or the shader toolchain changes;
  6. every unit corresponds to an entry in _data/units.yml, is marked
     `written: true` there, declares that entry's number, orders itself by the
     entry's position in the file, and carries its read and practice budgets.
     Position rather than number, because a milestone is `P1`, which has no
     place in a numeric sort;
  7. every internal /learn/<slug>.html link points at a slug that exists in
     _data/units.yml, so a forward reference to a unit not yet written is
     allowed while a reference to one that will never exist is not;
  8. every `{% include shader.html id="..." %}` names a shader manifest that
     scripts/build_shaders.py has produced, so a page cannot embed a player
     that will 404 in the reader's browser;
  9. every `{% include figure.html %}` names files that exist, both the poster
     and, for a clip, the .mp4.

Runs on a bare Python 3 with no third-party imports, and needs no GPU, so it
is the first step of CI.

Exit code is non-zero if any check fails.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
UNITS_DIR = ROOT / "docs" / "learn"
ASSETS = UNITS_DIR / "assets"
MANIFESTS = ASSETS / "shaders"
CHECKS = ROOT / "checks"
UNITS = ROOT / "_data" / "units.yml"

MIN_WORDS = 1200
MAX_WORDS = 1800
PINNED_VERSION = "3.8.2"

REQUIRED_KEYS = (
    "layout",
    "title",
    "description",
    "parent",
    "nav_order",
    "unit",
    "permalink",
    "reading_time",
    "practice_time",
)

FRONT_MATTER = re.compile(r"\A---\n(.*?)\n---\n", re.S)
HTML_COMMENT = re.compile(r"<!--.*?-->", re.S)
LIQUID_COMMENT = re.compile(r"\{%-?\s*comment\s*-?%\}.*?\{%-?\s*endcomment\s*-?%\}", re.S)
LIQUID_TAG = re.compile(r"\{%.*?%\}", re.S)
LIQUID_VAR = re.compile(r"\{\{.*?\}\}")
MD_LINK = re.compile(r"\[([^\]]*)\]\([^)]*\)")
FENCE = re.compile(r"```.*?```", re.S)
UNIT_LINK = re.compile(r"/learn/([a-z0-9][a-z0-9-]*)\.html")

SHADER_INCLUDE = re.compile(r"\{%\s*include\s+shader\.html\s+([^%]*?)%\}")
FIGURE_INCLUDE = re.compile(r"\{%\s*include\s+figure\.html\s+([^%]*?)%\}")
ATTR = re.compile(r'(\w+)\s*=\s*"([^"]*)"')


def load_units() -> dict[str, dict[str, str]]:
    """Minimal reader for the deliberately flat _data/units.yml.

    Avoids a PyYAML dependency so the check runs on a bare Python 3 in CI. The
    file is kept flat for exactly this reason.
    """
    units: dict[str, dict[str, str]] = {}
    current: dict[str, str] | None = None
    for raw in UNITS.read_text(encoding="utf8").splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        if line.startswith("- "):
            current = {}
            line = line[2:]
        if current is None or ":" not in line:
            continue
        key, _, value = line.partition(":")
        current[key.strip()] = value.strip().strip('"').strip("'")
        if "slug" in current and current["slug"] not in units:
            current["_index"] = str(len(units))
            units[current["slug"]] = current
    return units


def parse_front_matter(text: str) -> dict[str, str]:
    match = FRONT_MATTER.match(text)
    if not match:
        return {}
    out: dict[str, str] = {}
    for line in match.group(1).splitlines():
        if ":" not in line or line.startswith(" "):
            continue
        key, _, value = line.partition(":")
        out[key.strip()] = value.strip().strip('"').strip("'")
    return out


def body_words(text: str) -> int:
    """Word count of the prose a reader actually reads at prose speed."""
    body = FRONT_MATTER.sub("", text)
    body = HTML_COMMENT.sub("", body)
    body = LIQUID_COMMENT.sub("", body)
    body = FENCE.sub("", body)
    body = LIQUID_TAG.sub("", body)
    body = LIQUID_VAR.sub("", body)
    body = MD_LINK.sub(r"\1", body)          # keep link text, drop targets
    body = re.sub(r"[|>#*`_{}-]", " ", body)
    return len([w for w in body.split() if any(c.isalnum() for c in w)])


def attrs(text: str) -> dict[str, str]:
    return {k: v for k, v in ATTR.findall(text)}


def main() -> int:
    failures: list[str] = []
    units = load_units()
    if not units:
        print(f"could not read any unit from {UNITS}")
        return 1

    pages = sorted(p for p in UNITS_DIR.glob("*.md") if p.name != "learn.md")
    if not pages:
        print("no unit pages found under docs/learn/; nothing to check yet")
        return 0

    for page in pages:
        rel = page.relative_to(ROOT)
        text = page.read_text(encoding="utf8")
        fm = parse_front_matter(text)

        if not fm:
            failures.append(f"{rel}: no front matter")
            continue

        for key in REQUIRED_KEYS:
            if key not in fm:
                failures.append(f"{rel}: missing front matter key `{key}`")

        words = body_words(text)
        if not MIN_WORDS <= words <= MAX_WORDS:
            failures.append(
                f"{rel}: body is {words} words, outside the "
                f"{MIN_WORDS}-{MAX_WORDS} reading budget"
            )

        stem = page.stem
        expected_permalink = f"/learn/{stem}.html"
        if fm.get("permalink") != expected_permalink:
            failures.append(
                f"{rel}: permalink is {fm.get('permalink')!r}, "
                f"expected {expected_permalink!r}"
            )

        if "score_version" in fm and fm["score_version"] != PINNED_VERSION:
            failures.append(
                f"{rel}: score_version is {fm['score_version']!r}, "
                f"pinned version is {PINNED_VERSION!r}"
            )

        if not (CHECKS / f"{stem}.md").exists():
            failures.append(f"{rel}: no re-verification note at checks/{stem}.md")

        unit = units.get(stem)
        if unit is None:
            failures.append(f"{rel}: no unit with slug {stem!r} in _data/units.yml")
        else:
            if unit.get("written") != "true":
                failures.append(
                    f"{rel}: page exists but _data/units.yml marks it `written: false`"
                )
            if fm.get("unit") != unit.get("num"):
                failures.append(
                    f"{rel}: front matter unit is {fm.get('unit')!r}, "
                    f"_data/units.yml says {unit.get('num')!r}"
                )
            if fm.get("nav_order") != unit["_index"]:
                failures.append(
                    f"{rel}: nav_order is {fm.get('nav_order')!r}, expected "
                    f"{unit['_index']!r} (position in _data/units.yml)"
                )
            expected_read = f"{unit.get('read')} min"
            if fm.get("reading_time") != expected_read:
                failures.append(
                    f"{rel}: reading_time is {fm.get('reading_time')!r}, "
                    f"_data/units.yml says {expected_read!r}"
                )
            practice = unit.get("practice", "0")
            expected_practice = "none" if practice == "0" else f"{practice} min"
            if fm.get("practice_time") != expected_practice:
                failures.append(
                    f"{rel}: practice_time is {fm.get('practice_time')!r}, "
                    f"_data/units.yml says {expected_practice!r}"
                )
            if unit.get("score") == "true" and "score_version" not in fm:
                failures.append(
                    f"{rel}: _data/units.yml marks this unit `score: true`, so it "
                    f"must declare score_version"
                )

        for slug in sorted(set(UNIT_LINK.findall(text))):
            if slug not in units:
                failures.append(
                    f"{rel}: links to /learn/{slug}.html, which is not a unit in "
                    f"_data/units.yml"
                )

        for raw in SHADER_INCLUDE.findall(text):
            a = attrs(raw)
            shader_id = a.get("id")
            if not shader_id:
                failures.append(f"{rel}: a shader.html include has no id")
                continue
            if not (MANIFESTS / f"{shader_id}.json").exists():
                failures.append(
                    f"{rel}: embeds shader {shader_id!r}, but "
                    f"docs/learn/assets/shaders/{shader_id}.json does not exist. "
                    f"Run scripts/build_shaders.py."
                )

        for raw in FIGURE_INCLUDE.findall(text):
            a = attrs(raw)
            unit_dir = a.get("unit")
            name = a.get("name")
            if not unit_dir or not name:
                failures.append(f"{rel}: a figure.html include needs unit and name")
                continue
            base = ASSETS / unit_dir / name
            if not base.with_suffix(".png").exists():
                failures.append(
                    f"{rel}: figure {name} has no poster at "
                    f"docs/learn/assets/{unit_dir}/{name}.png"
                )
            if a.get("video") == "true" and not base.with_suffix(".mp4").exists():
                failures.append(
                    f"{rel}: figure {name} is a clip but "
                    f"docs/learn/assets/{unit_dir}/{name}.mp4 does not exist"
                )

        print(f"{rel}: {words} words")

    written = [s for s, u in units.items() if u.get("written") == "true"]
    for slug in written:
        if not (UNITS_DIR / f"{slug}.md").exists():
            failures.append(
                f"_data/units.yml marks {slug!r} written, but "
                f"docs/learn/{slug}.md does not exist"
            )

    if failures:
        print("\nFAILED")
        for line in failures:
            print(f"  {line}")
        return 1

    print(f"\nOK: {len(pages)} unit page(s) pass; "
          f"{len(units)} unit(s) declared, {len(written)} written")
    return 0


if __name__ == "__main__":
    sys.exit(main())
