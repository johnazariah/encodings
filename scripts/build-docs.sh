#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

dotnet fsdocs build --output docs-output --parameters \
  root /encodings/ \
  fsdocs-logo-src img/fockmap-logo.svg \
  fsdocs-favicon-src img/fockmap-icon.svg \
  fsdocs-logo-link /encodings/

python3 scripts/inject_pages_runtime.py

echo "Docs built in docs-output/ with branding + runtime injection."
