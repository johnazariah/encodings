#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

rm -rf api-output docs/reference

# Build the library so fsdocs can crack the project
dotnet build src/Encodings/Encodings.fsproj --configuration Release

# Generate API reference docs
dotnet fsdocs build --output api-output \
  --properties Configuration=Release \
  --parameters \
  root /encodings/ \
  fsdocs-logo-src content/img/fockmap-logo.svg \
  fsdocs-favicon-src content/img/fockmap-icon.svg \
  fsdocs-logo-link /encodings/

mkdir -p docs/reference
if [[ -d api-output/reference ]]; then
  cp -R api-output/reference/. docs/reference/
fi
if [[ -d api-output/content ]]; then
  mkdir -p docs/content
  cp -R api-output/content/. docs/content/
fi

echo "Prepared Pages source: markdown content in docs/, API reference in docs/reference/."
