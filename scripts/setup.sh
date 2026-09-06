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

# glslang, the reference GLSL front end. It is stricter than the NVIDIA driver
# the figures are rendered on, and CI runs it, so having it locally is the
# difference between finding a portability bug now and finding it in a failed
# build. The Debian package needs root; the Khronos release does not, so it goes
# in the venv alongside everything else.
GLSLANG_VERSION="16.5.0"
if ! command -v glslangValidator >/dev/null && [ ! -x .venv/bin/glslangValidator ]; then
  echo
  echo "Fetching glslang $GLSLANG_VERSION:"
  mkdir -p .venv/opt
  if curl -sSLf "https://github.com/KhronosGroup/glslang/releases/download/${GLSLANG_VERSION}/glslang-${GLSLANG_VERSION}-linux-x86_64-release.tar.gz" \
      | tar xz -C .venv/opt; then
    ln -sf "$PWD/.venv/opt/bin/glslangValidator" .venv/bin/glslangValidator
    echo "  ok"
  else
    echo "  could not fetch it; scripts/validate_glsl.py will skip and CI will not."
  fi
fi

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
