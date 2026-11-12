#!/usr/bin/env bash
# Build and test EF8-EF11 in Default and NativeOnly query modes (8 parallel runs, each with its own Docker container).
# Thin wrapper around tests/tools/test-all.py; run `./test-all.sh --help` for the options.
set -euo pipefail
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
script="$here/tests/tools/test-all.py"
if [ -n "${PYTHON:-}" ]; then
  exec "$PYTHON" "$script" "$@"
fi
for c in python3 python; do
  # Skip stubs (e.g. the Windows Store alias) and Python 2: the candidate must really run Python 3.9+.
  if command -v "$c" >/dev/null 2>&1 && "$c" -c 'import sys; sys.exit(0 if sys.version_info >= (3, 9) else 1)' >/dev/null 2>&1; then
    exec "$c" "$script" "$@"
  fi
done
if command -v py >/dev/null 2>&1 && py -3 -c 'import sys; sys.exit(0 if sys.version_info >= (3, 9) else 1)' >/dev/null 2>&1; then
  exec py -3 "$script" "$@"
fi
echo "No Python 3.9+ interpreter found (tried python3, python, py -3); set PYTHON=<command>." >&2
exit 2
