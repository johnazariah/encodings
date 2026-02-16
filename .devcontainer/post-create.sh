#!/usr/bin/env bash
# Post-create setup for the FockMap devcontainer.
# Everything is pre-baked in the Dockerfile — this just builds the project.
set -euo pipefail

echo "══════════════════════════════════════════════"
echo "  FockMap — post-create setup"
echo "══════════════════════════════════════════════"

# ── Git Configuration ───────────────────────────────────────────
echo "→ Configuring git…"

git config user.name "John S Azariah"
git config user.email "john.azariah@student.uts.edu.au"
git config --global init.defaultBranch main
git config --global push.autoSetupRemote true

if command -v gh &> /dev/null; then
    gh auth setup-git 2>/dev/null || echo "⚠  GitHub CLI not authenticated — run 'gh auth login'"
fi

# ── Build & Test ────────────────────────────────────────────────
echo "→ Restoring NuGet packages…"
dotnet restore Encodings.sln

echo "→ Building…"
dotnet build Encodings.sln --no-restore

echo "→ Running tests…"
dotnet test Encodings.sln --no-build --verbosity normal

echo ""
echo "══════════════════════════════════════════════"
echo "  ✓ Ready. ALL tests need to be green above."
echo "══════════════════════════════════════════════"
