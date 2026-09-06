#!/usr/bin/env python3
"""Compile every course shader into the manifests the site's player reads.

The browser never parses ISF. It reads a manifest produced here, which carries
the translated GLSL ES 3.00, the input list the control panel is built from,
the pass structure, and the original ISF source so a page can show the reader
what they would paste into ossia score. One parser, in Python, feeding both
the offline renderer and the live player, is what keeps a figure and the
player beside it showing the same thing.

The output is committed. CI re-runs this with --check and fails if the
committed manifests do not match the sources, which is a text comparison and
needs no GPU, so the check runs on an ordinary runner.

    python3 scripts/build_shaders.py            # write the manifests
    python3 scripts/build_shaders.py --check    # verify without writing
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(Path(__file__).resolve().parent))

import isf  # noqa: E402

SOURCES = ROOT / "library" / "shaders"
OUTPUT = ROOT / "docs" / "learn" / "assets" / "shaders"
REGISTRY = OUTPUT / "index.json"


def shader_id(path: Path) -> str:
    """A stable id from the path under library/shaders: `03/warp` -> `03-warp`."""
    rel = path.relative_to(SOURCES).with_suffix("")
    return "-".join(rel.parts)


def build_one(path: Path) -> dict:
    shader = isf.load(path)
    compiled = isf.compile_shader(shader, name=shader_id(path))
    manifest = dict(compiled.manifest)
    manifest["id"] = shader_id(path)
    manifest["source"] = path.read_text(encoding="utf8")
    manifest["sourcePath"] = str(path.relative_to(ROOT))
    if shader.vertex_body is not None:
        manifest["vertexSource"] = shader.vertex_body
    return manifest


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--check", action="store_true",
                    help="fail if the committed manifests are out of date")
    args = ap.parse_args(argv)

    if not SOURCES.exists():
        print(f"no shader sources under {SOURCES.relative_to(ROOT)}")
        return 0

    paths = sorted(p for p in SOURCES.rglob("*.fs"))
    stale: list[str] = []
    registry: list[dict] = []
    errors: list[str] = []

    for path in paths:
        try:
            manifest = build_one(path)
        except isf.ISFError as exc:
            errors.append(f"{path.relative_to(ROOT)}: {exc}")
            continue

        out = OUTPUT / f"{manifest['id']}.json"
        text = json.dumps(manifest, indent=1, sort_keys=True) + "\n"

        if args.check:
            if not out.exists():
                stale.append(f"{out.relative_to(ROOT)} is missing")
            elif out.read_text(encoding="utf8") != text:
                stale.append(f"{out.relative_to(ROOT)} is out of date")
        else:
            out.parent.mkdir(parents=True, exist_ok=True)
            out.write_text(text, encoding="utf8")

        registry.append({
            "id": manifest["id"],
            "description": manifest["description"],
            "credit": manifest["credit"],
            "categories": manifest["categories"],
            "mode": manifest["mode"],
            "inputs": len(manifest["inputs"]),
            "passes": len(manifest["passes"]),
            "source": manifest["sourcePath"],
        })

    registry_text = json.dumps(registry, indent=1, sort_keys=True) + "\n"
    if args.check:
        if not REGISTRY.exists():
            stale.append(f"{REGISTRY.relative_to(ROOT)} is missing")
        elif REGISTRY.read_text(encoding="utf8") != registry_text:
            stale.append(f"{REGISTRY.relative_to(ROOT)} is out of date")
    else:
        REGISTRY.parent.mkdir(parents=True, exist_ok=True)
        REGISTRY.write_text(registry_text, encoding="utf8")

    if errors:
        print("FAILED to compile:")
        for line in errors:
            print(f"  {line}")
        return 1

    if stale:
        print("FAILED: run `python3 scripts/build_shaders.py` and commit the result")
        for line in stale:
            print(f"  {line}")
        return 1

    verb = "checked" if args.check else "wrote"
    print(f"OK: {verb} {len(registry)} shader manifest(s)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
