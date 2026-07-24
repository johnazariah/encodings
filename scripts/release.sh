#!/bin/bash
set -euo pipefail

# FockMap Release Automation Script
# Usage:
#   ./scripts/release.sh [--dry-run] [MODE]
#
# MODE is one of:
#   auto              (default) infer bump from conventional commits
#   current | staged  release the CURRENT project version with NO bump
#                      (finalizes a staged version, e.g. the 0.9.0 breaking release)
#   major | minor | patch   force a specific bump
#
# Flags and MODE may appear in any order, e.g.:
#   ./scripts/release.sh --dry-run current
#   ./scripts/release.sh current --dry-run
#
# This script:
# 1. Analyzes commits since last release
# 2. Determines the release version (bump, or the staged current version)
# 3. Updates version in .fsproj
# 4. Generates/finalizes the CHANGELOG entry
# 5. Adds-or-replaces the CITATION.cff date-released
# 6. Commits, tags, and pushes
# 7. Monitors CI until completion

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

# Portable, tested release helpers (version extraction, semver compare, CFF
# date add-or-replace, changelog finalization). Shared with the CI workflow.
# shellcheck source=scripts/lib/release-lib.sh
source "$REPO_ROOT/scripts/lib/release-lib.sh"

DRY_RUN=false
MODE="auto"
for arg in "$@"; do
    case "$arg" in
        --dry-run) DRY_RUN=true ;;
        auto|current|staged|major|minor|patch) MODE="$arg" ;;
        *) echo "Unknown argument: $arg" >&2
           echo "Usage: ./scripts/release.sh [--dry-run] [auto|current|staged|major|minor|patch]" >&2
           exit 2 ;;
    esac
done
$DRY_RUN && echo "🔍 DRY RUN MODE - no changes will be made"
echo "Release mode: $MODE"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log_info() { echo -e "${BLUE}ℹ${NC} $1"; }
log_success() { echo -e "${GREEN}✓${NC} $1"; }
log_warn() { echo -e "${YELLOW}⚠${NC} $1"; }
log_error() { echo -e "${RED}✗${NC} $1"; }

# ═══════════════════════════════════════════════════════════════════
# Step 1: Get current and last release versions
# ═══════════════════════════════════════════════════════════════════

log_info "Checking for previous releases..."

# Get the latest tag (if any)
LAST_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "")
FIRST_RELEASE=false
if [[ -z "$LAST_TAG" ]]; then
    log_warn "No previous release found. This will be the first release."
    LAST_TAG="$(git rev-list --max-parents=0 HEAD)"  # First commit
    LAST_VERSION="0.0.0"
    FIRST_RELEASE=true
else
    LAST_VERSION="${LAST_TAG#v}"
    log_info "Last release: $LAST_TAG (version $LAST_VERSION)"
fi

# Parse current version from .fsproj (portable — no PCRE `grep -oP`).
FSPROJ="$REPO_ROOT/src/Encodings/Encodings.fsproj"
CURRENT_VERSION=$(rl_extract_fsproj_version "$FSPROJ")
log_info "Current version in .fsproj: $CURRENT_VERSION"

# ═══════════════════════════════════════════════════════════════════
# Step 2: Analyze commits since last release
# ═══════════════════════════════════════════════════════════════════

log_info "Analyzing commits since $LAST_TAG..."

if [[ "$LAST_TAG" == "$(git rev-list --max-parents=0 HEAD)" ]]; then
    COMMITS=$(git log --oneline)
else
    COMMITS=$(git log "${LAST_TAG}..HEAD" --oneline)
fi

if [[ -z "$COMMITS" ]]; then
    log_error "No commits since last release. Nothing to release."
    exit 1
fi

COMMIT_COUNT=$(echo "$COMMITS" | wc -l)
log_info "Found $COMMIT_COUNT commits since last release:"
echo "$COMMITS" | head -20
if [[ $COMMIT_COUNT -gt 20 ]]; then
    echo "  ... and $((COMMIT_COUNT - 20)) more"
fi
echo ""

# ═══════════════════════════════════════════════════════════════════
# Step 3: Determine release version (staged current, or a bump)
# ═══════════════════════════════════════════════════════════════════

# Analyze commit messages for conventional commits
BREAKING_CHANGES=$(echo "$COMMITS" | grep -iE '(BREAKING|!:)' || true)
FEATURES=$(echo "$COMMITS" | grep -iE '^[a-f0-9]+ feat' || true)
FIXES=$(echo "$COMMITS" | grep -iE '^[a-f0-9]+ fix' || true)
DOCS=$(echo "$COMMITS" | grep -iE '^[a-f0-9]+ docs' || true)
CHORES=$(echo "$COMMITS" | grep -iE '^[a-f0-9]+ (chore|refactor|ci|test)' || true)

log_info "Commit analysis:"
[[ -n "$BREAKING_CHANGES" ]] && echo "  🔴 Breaking changes: $(echo "$BREAKING_CHANGES" | wc -l)"
[[ -n "$FEATURES" ]] && echo "  🟢 Features: $(echo "$FEATURES" | wc -l)"
[[ -n "$FIXES" ]] && echo "  🟡 Fixes: $(echo "$FIXES" | wc -l)"
[[ -n "$DOCS" ]] && echo "  📚 Docs: $(echo "$DOCS" | wc -l)"
[[ -n "$CHORES" ]] && echo "  🔧 Chores: $(echo "$CHORES" | wc -l)"
echo ""

# Parse version components (for the interactive menu hints)
IFS='.' read -r MAJOR MINOR PATCH <<< "$CURRENT_VERSION"

# STAGED_RELEASE finalizes the CURRENT project version with no bump. This is how
# a deliberately staged version (e.g. the 0.9.0 breaking release, whose version
# and BREAKING changelog are already committed) is shipped WITHOUT auto-bumping
# it to 1.0.0.
STAGED_RELEASE=false

if [[ "$MODE" == "current" || "$MODE" == "staged" ]]; then
    STAGED_RELEASE=true
    RECOMMENDED="CURRENT (staged)"
    NEW_VERSION="$CURRENT_VERSION"

    # Validate: current must be strictly greater than the latest released tag,
    # and the staged CFF/CHANGELOG must already match the current version.
    if ! $FIRST_RELEASE; then
        if ! rl_version_gt "$CURRENT_VERSION" "$LAST_VERSION"; then
            log_error "current/staged release requires .fsproj version ($CURRENT_VERSION) > last tag ($LAST_VERSION)."
            exit 1
        fi
    fi
    CFF_VERSION=$(awk -F': ' '/^version:/{print $2; exit}' "$REPO_ROOT/CITATION.cff" 2>/dev/null || echo "")
    if [[ -n "$CFF_VERSION" && "$CFF_VERSION" != "$CURRENT_VERSION" ]]; then
        log_error "CITATION.cff version ($CFF_VERSION) does not match .fsproj ($CURRENT_VERSION); align them before a staged release."
        exit 1
    fi
    if ! grep -Eq "^## \[$(printf '%s' "$CURRENT_VERSION" | sed 's/\./\\./g')\]" "$REPO_ROOT/CHANGELOG.md"; then
        log_error "CHANGELOG.md has no '## [$CURRENT_VERSION]' heading; add the staged entry before a staged release."
        exit 1
    fi
    log_success "Staged release validated: $CURRENT_VERSION (> $LAST_VERSION), CFF/CHANGELOG aligned."
elif $FIRST_RELEASE; then
    RECOMMENDED="INITIAL"
    NEW_VERSION="$CURRENT_VERSION"
elif [[ "$MODE" == "major" ]]; then
    RECOMMENDED="MAJOR"
    NEW_VERSION=$(rl_compute_next_version "$CURRENT_VERSION" major)
elif [[ "$MODE" == "minor" ]]; then
    RECOMMENDED="MINOR"
    NEW_VERSION=$(rl_compute_next_version "$CURRENT_VERSION" minor)
elif [[ "$MODE" == "patch" ]]; then
    RECOMMENDED="PATCH"
    NEW_VERSION=$(rl_compute_next_version "$CURRENT_VERSION" patch)
elif [[ -n "$BREAKING_CHANGES" ]]; then
    RECOMMENDED="MAJOR"
    NEW_VERSION=$(rl_compute_next_version "$CURRENT_VERSION" major)
elif [[ -n "$FEATURES" ]]; then
    RECOMMENDED="MINOR"
    NEW_VERSION=$(rl_compute_next_version "$CURRENT_VERSION" minor)
else
    RECOMMENDED="PATCH"
    NEW_VERSION=$(rl_compute_next_version "$CURRENT_VERSION" patch)
fi

echo -e "${YELLOW}═══════════════════════════════════════════════════════════════════${NC}"
if $FIRST_RELEASE; then
    echo -e "${YELLOW}  First release: v${NEW_VERSION}${NC}"
elif $STAGED_RELEASE; then
    echo -e "${YELLOW}  Staged release (no bump): v${NEW_VERSION}${NC}"
else
    echo -e "${YELLOW}  Recommended: ${RECOMMENDED} release${NC}"
    echo -e "${YELLOW}  Version: ${CURRENT_VERSION} → ${NEW_VERSION}${NC}"
fi
echo -e "${YELLOW}═══════════════════════════════════════════════════════════════════${NC}"
echo ""

# Ask for confirmation (staged/bump modes are non-interactive-friendly: a staged
# release is already fully determined, so we still confirm before mutating).
read -p "Accept this version? [Y/n/custom version]: " CONFIRM
case "$CONFIRM" in
    n|N|no|No)
        echo "Options: MAJOR ($((MAJOR + 1)).0.0), MINOR ($MAJOR.$((MINOR + 1)).0), PATCH ($MAJOR.$MINOR.$((PATCH + 1))), CURRENT ($CURRENT_VERSION)"
        read -p "Enter version type or custom version: " CUSTOM
        case "$CUSTOM" in
            MAJOR|major) NEW_VERSION="$((MAJOR + 1)).0.0"; STAGED_RELEASE=false ;;
            MINOR|minor) NEW_VERSION="$MAJOR.$((MINOR + 1)).0"; STAGED_RELEASE=false ;;
            PATCH|patch) NEW_VERSION="$MAJOR.$MINOR.$((PATCH + 1))"; STAGED_RELEASE=false ;;
            CURRENT|current|staged) NEW_VERSION="$CURRENT_VERSION"; STAGED_RELEASE=true ;;
            *) NEW_VERSION="$CUSTOM"; STAGED_RELEASE=false ;;
        esac
        ;;
    ""|y|Y|yes|Yes)
        # Use recommended
        ;;
    *)
        NEW_VERSION="$CONFIRM"; STAGED_RELEASE=false
        ;;
esac

log_info "Releasing version: $NEW_VERSION"

if $DRY_RUN; then
    log_warn "DRY RUN - would release v$NEW_VERSION"
    exit 0
fi

# ═══════════════════════════════════════════════════════════════════
# Step 4: Update version in .fsproj (portable in-place rewrite)
# ═══════════════════════════════════════════════════════════════════

log_info "Updating version in Encodings.fsproj..."
if [[ "$NEW_VERSION" != "$CURRENT_VERSION" ]]; then
    rl_set_fsproj_version "$FSPROJ" "$NEW_VERSION"
    log_success "Updated version to $NEW_VERSION"
else
    log_info "Version already at $NEW_VERSION (staged release) — no .fsproj change needed."
fi

# ═══════════════════════════════════════════════════════════════════
# Step 5: Generate or finalize the CHANGELOG entry
# ═══════════════════════════════════════════════════════════════════

CHANGELOG="$REPO_ROOT/CHANGELOG.md"
DATE=$(date +%Y-%m-%d)

if $STAGED_RELEASE; then
    # The staged entry (e.g. "## [0.9.0] - Unreleased") already exists with its
    # curated BREAKING notes — finalize its date rather than prepend a duplicate.
    log_info "Finalizing staged CHANGELOG heading for v$NEW_VERSION..."
    rl_finalize_changelog "$CHANGELOG" "$NEW_VERSION" "$DATE"
    log_success "Finalized CHANGELOG heading to $DATE"
else
    log_info "Generating CHANGELOG entry..."

# Create changelog entry
ENTRY="## [$NEW_VERSION] - $DATE

"

if [[ -n "$BREAKING_CHANGES" ]]; then
    ENTRY+="### ⚠ BREAKING CHANGES

"
    while IFS= read -r commit; do
        ENTRY+="- ${commit#* }
"
    done <<< "$BREAKING_CHANGES"
    ENTRY+="
"
fi

if [[ -n "$FEATURES" ]]; then
    ENTRY+="### ✨ Features

"
    while IFS= read -r commit; do
        ENTRY+="- ${commit#* }
"
    done <<< "$FEATURES"
    ENTRY+="
"
fi

if [[ -n "$FIXES" ]]; then
    ENTRY+="### 🐛 Bug Fixes

"
    while IFS= read -r commit; do
        ENTRY+="- ${commit#* }
"
    done <<< "$FIXES"
    ENTRY+="
"
fi

if [[ -n "$DOCS" ]]; then
    ENTRY+="### 📚 Documentation

"
    while IFS= read -r commit; do
        ENTRY+="- ${commit#* }
"
    done <<< "$DOCS"
    ENTRY+="
"
fi

if [[ -n "$CHORES" ]]; then
    ENTRY+="### 🔧 Maintenance

"
    while IFS= read -r commit; do
        ENTRY+="- ${commit#* }
"
    done <<< "$CHORES"
    ENTRY+="
"
fi

# Create or update CHANGELOG.md
if [[ -f "$CHANGELOG" ]]; then
    # Insert after the header
    HEADER=$(head -n 5 "$CHANGELOG")
    BODY=$(tail -n +6 "$CHANGELOG")
    echo "$HEADER" > "$CHANGELOG"
    echo "" >> "$CHANGELOG"
    echo "$ENTRY" >> "$CHANGELOG"
    echo "$BODY" >> "$CHANGELOG"
else
    cat > "$CHANGELOG" << EOF
# Changelog

All notable changes to FockMap will be documented in this file.

$ENTRY
EOF
fi

log_success "Updated CHANGELOG.md"
fi  # end: staged (finalize) vs bump (generate) CHANGELOG

# ═══════════════════════════════════════════════════════════════════
# Step 6: Update CITATION.cff version and date (add-or-replace)
# ═══════════════════════════════════════════════════════════════════

CITATION="$REPO_ROOT/CITATION.cff"
if [[ -f "$CITATION" ]]; then
    log_info "Updating CITATION.cff (version + date-released, add-or-replace)..."
    rl_set_cff_version_and_date "$CITATION" "$NEW_VERSION" "$DATE"
    log_success "Updated CITATION.cff to v$NEW_VERSION ($DATE)"
fi

# ═══════════════════════════════════════════════════════════════════
# Step 7: Commit, tag, and push
# ═══════════════════════════════════════════════════════════════════

log_info "Committing release changes..."
git add "$FSPROJ" "$CHANGELOG" "$CITATION" 2>/dev/null || true
git commit -m "chore(release): v$NEW_VERSION

- Update version to $NEW_VERSION
- Generate CHANGELOG entry
- Update CITATION.cff"

log_info "Creating tag v$NEW_VERSION..."
git tag -a "v$NEW_VERSION" -m "Release v$NEW_VERSION"

log_info "Pushing to origin..."
git push origin HEAD
git push origin "v$NEW_VERSION"

log_success "Pushed release v$NEW_VERSION"

# ═══════════════════════════════════════════════════════════════════
# Step 8: Monitor CI workflow
# ═══════════════════════════════════════════════════════════════════

log_info "Monitoring release workflow..."
echo ""

# Wait for workflow to start
sleep 5

# Get the workflow run ID for our tag
RUN_ID=""
for i in {1..12}; do
    RUN_ID=$(gh run list --workflow=release.yml --limit=1 --json databaseId,headBranch | \
             jq -r '.[0].databaseId // empty')
    if [[ -n "$RUN_ID" ]]; then
        break
    fi
    log_info "Waiting for workflow to start... ($i/12)"
    sleep 5
done

if [[ -z "$RUN_ID" ]]; then
    log_error "Could not find workflow run. Check manually:"
    echo "  gh run list --workflow=release.yml"
    exit 1
fi

log_info "Workflow started: https://github.com/johnazariah/encodings/actions/runs/$RUN_ID"
echo ""

# Watch the workflow
gh run watch "$RUN_ID" --exit-status

if [[ $? -eq 0 ]]; then
    echo ""
    log_success "═══════════════════════════════════════════════════════════════════"
    log_success "  Release v$NEW_VERSION published successfully!"
    log_success "═══════════════════════════════════════════════════════════════════"
    echo ""
    echo "  📦 NuGet: https://www.nuget.org/packages/FockMap/$NEW_VERSION"
    echo "  🏷️  GitHub: https://github.com/johnazariah/encodings/releases/tag/v$NEW_VERSION"
    echo ""
else
    log_error "Release workflow failed. Check the logs:"
    echo "  gh run view $RUN_ID --log-failed"
    exit 1
fi
