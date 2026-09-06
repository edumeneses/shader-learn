#!/usr/bin/env bash
# Build the Python toolchain's virtual environment.
#
#   ./scripts/setup.sh
#   source .venv/bin/activate
#
# Python 3.11 rather than the system default: the default here is 3.14, which
# has no dev headers installed, and moderngl builds a C extension. If 3.11 is
# gone, any version with matching python3.X-dev works.
set -e
cd "$(dirname "$0")/.."

PY="${PYTHON:-python3.11}"
if ! command -v "$PY" >/dev/null; then
  echo "$PY not found; set PYTHON=python3.X to choose another" >&2
  exit 1
fi

"$PY" -m venv .venv
.venv/bin/python -m pip install --upgrade pip
.venv/bin/python -m pip install moderngl numpy Pillow PyOpenGL

echo
echo "Checking the GPU path:"
.venv/bin/python - <<'PY'
import moderngl
ctx = moderngl.create_context(standalone=True, backend="egl", require=460)
print("  renderer:", ctx.info["GL_RENDERER"])
print("  version: ", ctx.info["GL_VERSION"])
PY

echo
echo "Done. Activate it with:  source .venv/bin/activate"
