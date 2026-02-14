#!/bin/bash
set -euo pipefail

# FockMap Release Automation Script
# Usage: ./scripts/release.sh [--dry-run]
#
# This script:
# 1. Analyzes commits since last release
# 2. Proposes version bump (PATCH/MINOR/MAJOR)
# 3. Updates version in .fsproj
# 4. Generates CHANGELOG entry
# 5. Commits, tags, and pushes
# 6. Monitors CI until completion

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

DRY_RUN=false
if [[ "${1:-}" == "--dry-run" ]]; then
    DRY_RUN=true
    echo "🔍 DRY RUN MODE - no changes will be made"
fi

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

# Parse current version from .fsproj
FSPROJ="$REPO_ROOT/src/Encodings/Encodings.fsproj"
CURRENT_VERSION=$(grep -oP '(?<=<Version>)[^<]+' "$FSPROJ")
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
# Step 3: Determine version bump type
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

# Parse version components
IFS='.' read -r MAJOR MINOR PATCH <<< "$CURRENT_VERSION"

# Determine recommended bump
if $FIRST_RELEASE; then
    # First release: use current version from .fsproj
    RECOMMENDED="INITIAL"
    NEW_VERSION="$CURRENT_VERSION"
elif [[ -n "$BREAKING_CHANGES" ]]; then
    RECOMMENDED="MAJOR"
    NEW_MAJOR=$((MAJOR + 1))
    NEW_MINOR=0
    NEW_PATCH=0
    NEW_VERSION="${NEW_MAJOR}.${NEW_MINOR}.${NEW_PATCH}"
elif [[ -n "$FEATURES" ]]; then
    RECOMMENDED="MINOR"
    NEW_MAJOR=$MAJOR
    NEW_MINOR=$((MINOR + 1))
    NEW_PATCH=0
    NEW_VERSION="${NEW_MAJOR}.${NEW_MINOR}.${NEW_PATCH}"
else
    RECOMMENDED="PATCH"
    NEW_MAJOR=$MAJOR
    NEW_MINOR=$MINOR
    NEW_PATCH=$((PATCH + 1))
    NEW_VERSION="${NEW_MAJOR}.${NEW_MINOR}.${NEW_PATCH}"
fi

echo -e "${YELLOW}═══════════════════════════════════════════════════════════════════${NC}"
if $FIRST_RELEASE; then
    echo -e "${YELLOW}  First release: v${NEW_VERSION}${NC}"
else
    echo -e "${YELLOW}  Recommended: ${RECOMMENDED} release${NC}"
    echo -e "${YELLOW}  Version: ${CURRENT_VERSION} → ${NEW_VERSION}${NC}"
fi
echo -e "${YELLOW}═══════════════════════════════════════════════════════════════════${NC}"
echo ""

# Ask for confirmation
read -p "Accept this version? [Y/n/custom version]: " CONFIRM
case "$CONFIRM" in
    n|N|no|No)
        echo "Options: MAJOR ($((MAJOR + 1)).0.0), MINOR ($MAJOR.$((MINOR + 1)).0), PATCH ($MAJOR.$MINOR.$((PATCH + 1)))"
        read -p "Enter version type or custom version: " CUSTOM
        case "$CUSTOM" in
            MAJOR|major) NEW_VERSION="$((MAJOR + 1)).0.0" ;;
            MINOR|minor) NEW_VERSION="$MAJOR.$((MINOR + 1)).0" ;;
            PATCH|patch) NEW_VERSION="$MAJOR.$MINOR.$((PATCH + 1))" ;;
            *) NEW_VERSION="$CUSTOM" ;;
        esac
        ;;
    ""|y|Y|yes|Yes)
        # Use recommended
        ;;
    *)
        NEW_VERSION="$CONFIRM"
        ;;
esac

log_info "Releasing version: $NEW_VERSION"

if $DRY_RUN; then
    log_warn "DRY RUN - would release v$NEW_VERSION"
    exit 0
fi

# ═══════════════════════════════════════════════════════════════════
# Step 4: Update version in .fsproj
# ═══════════════════════════════════════════════════════════════════

log_info "Updating version in Encodings.fsproj..."
sed -i "s|<Version>$CURRENT_VERSION</Version>|<Version>$NEW_VERSION</Version>|" "$FSPROJ"
log_success "Updated version to $NEW_VERSION"

# ═══════════════════════════════════════════════════════════════════
# Step 5: Generate CHANGELOG entry
# ═══════════════════════════════════════════════════════════════════

CHANGELOG="$REPO_ROOT/CHANGELOG.md"
DATE=$(date +%Y-%m-%d)

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

# ═══════════════════════════════════════════════════════════════════
# Step 6: Update CITATION.cff version
# ═══════════════════════════════════════════════════════════════════

CITATION="$REPO_ROOT/CITATION.cff"
if [[ -f "$CITATION" ]]; then
    log_info "Updating CITATION.cff..."
    sed -i "s|^version: .*|version: $NEW_VERSION|" "$CITATION"
    sed -i "s|^date-released: .*|date-released: $DATE|" "$CITATION"
    log_success "Updated CITATION.cff"
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
