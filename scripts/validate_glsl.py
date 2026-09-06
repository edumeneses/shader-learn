#!/usr/bin/env python3
"""Compile every shader manifest with glslangValidator, on the CPU.

The GPU renderer already proves a shader compiles, but it only runs on the
author's machine. This runs anywhere, so CI can refuse a shader that would
fail in a reader's browser. glslang is the reference front end for GLSL, and
it is stricter than NVIDIA's driver: a shader that passes here compiles on
Mesa, on Apple, and in WebGL, where NVIDIA's tolerance of a missing precision
qualifier or an implicit int-to-float conversion would have let it through.

    python3 scripts/validate_glsl.py

Needs `glslangValidator`, from the glslang-tools package. Without it the check
reports that it was skipped and exits zero, so a contributor without the tool
is not blocked; CI installs it, so nothing reaches main unvalidated.
"""

from __future__ import annotations

import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MANIFESTS = ROOT / "docs" / "learn" / "assets" / "shaders"


def main() -> int:
    tool = shutil.which("glslangValidator")
    if tool is None:
        print("glslangValidator not found; skipping GLSL validation.")
        print("Install it with:  sudo apt install glslang-tools")
        return 0

    files = sorted(p for p in MANIFESTS.glob("*.json") if p.name != "index.json")
    if not files:
        print("no shader manifests to validate")
        return 0

    failures: list[str] = []
    with tempfile.TemporaryDirectory() as tmp:
        tmpdir = Path(tmp)
        for path in files:
            manifest = json.loads(path.read_text(encoding="utf8"))
            if manifest.get("mode") == "compute":
                # A compute shader's GLSL is assembled by ossia score from the
                # RESOURCES block, not by this project, so there is no single
                # translated source to hand to glslang.
                print(f"{path.stem}: compute, skipped")
                continue
            for stage, suffix in (("vertex", ".vert"), ("fragment", ".frag")):
                source = manifest.get(stage)
                if not source:
                    continue
                target = tmpdir / f"{path.stem}{suffix}"
                target.write_text(source, encoding="utf8")
                result = subprocess.run(
                    [tool, str(target)],
                    capture_output=True, text=True,
                )
                if result.returncode:
                    failures.append(
                        f"{manifest.get('sourcePath', path.stem)} ({stage}):\n"
                        + _indent(result.stdout + result.stderr)
                    )
            print(f"{path.stem}: ok")

    if failures:
        print("\nFAILED")
        for line in failures:
            print(line)
        return 1

    print(f"\nOK: {len(files)} shader(s) validated")
    return 0


def _indent(text: str) -> str:
    return "\n".join("    " + line for line in text.strip().splitlines())


if __name__ == "__main__":
    sys.exit(main())
