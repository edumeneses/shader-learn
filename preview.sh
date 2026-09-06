#!/usr/bin/env bash
# Local preview at http://127.0.0.1:4000, built with _local_config.yml so the
# baseurl is empty and links resolve the way they will once published.
#
#   ./preview.sh            build, serve, and watch; Ctrl-C to stop
#
# Gems are vendored under ./vendor/bundle, so only the first run is slow.
set -e
cd "$(dirname "$0")"
# The system ruby installs the bundler executable under the user gem directory.
export PATH="$(ruby -e 'print Gem.user_dir')/bin:$PATH"
"$BUNDLE" config set --local path 'vendor/bundle' >/dev/null
"$BUNDLE" install
exec "$BUNDLE" exec jekyll serve \
  --config _config.yml,_local_config.yml \
  -H 127.0.0.1 --livereload --incremental
