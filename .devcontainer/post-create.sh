#!/usr/bin/env bash
# Post-create setup for the FockMap devcontainer.
# Everything is pre-baked in the Dockerfile — this just builds the project.
set -euo pipefail

echo "══════════════════════════════════════════════"
echo "  FockMap — post-create setup"
echo "══════════════════════════════════════════════"

echo "→ Restoring NuGet packages…"
dotnet restore Encodings.sln

echo "→ Building…"
dotnet build Encodings.sln --no-restore

echo "→ Running tests…"
dotnet test Encodings.sln --no-build --verbosity normal

echo ""
echo "══════════════════════════════════════════════"
echo "  ✓ Ready. 303 tests should be green above."
echo "══════════════════════════════════════════════"
