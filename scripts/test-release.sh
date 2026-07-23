#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# Focused test harness for the FockMap release tooling (scripts/lib/release-lib.sh
# and scripts/release.sh). Runs entirely on temp copies — it never mutates the repo,
# never tags, never pushes. Portable across macOS (bash 3.2) and Linux.
#
# Usage: ./scripts/test-release.sh
# Exits non-zero on the first failure.
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

# shellcheck source=scripts/lib/release-lib.sh
source "$REPO_ROOT/scripts/lib/release-lib.sh"

PASS=0
FAIL=0

ok()   { echo "  ✓ $1"; PASS=$((PASS + 1)); }
bad()  { echo "  ✗ $1" >&2; FAIL=$((FAIL + 1)); }

# assert_eq <expected> <actual> <message>
assert_eq() {
    if [[ "$1" == "$2" ]]; then ok "$3"; else bad "$3 (expected '$1', got '$2')"; fi
}
# assert_ok <message> <cmd...>   — command must succeed
assert_ok() {
    local msg="$1"; shift
    if "$@" >/dev/null 2>&1; then ok "$msg"; else bad "$msg (command failed: $*)"; fi
}
# assert_fail <message> <cmd...> — command must fail
assert_fail() {
    local msg="$1"; shift
    if "$@" >/dev/null 2>&1; then bad "$msg (command unexpectedly succeeded: $*)"; else ok "$msg"; fi
}

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

FSPROJ_FIXT="$TMP/Encodings.fsproj"
cat > "$FSPROJ_FIXT" <<'XML'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PackageId>FockMap</PackageId>
    <Version>0.9.0</Version>
  </PropertyGroup>
</Project>
XML

echo "── 1. Portable version extraction (no grep -oP) ──"
assert_eq "0.9.0" "$(rl_extract_fsproj_version "$FSPROJ_FIXT")" "extract <Version> from fsproj"
assert_eq "0.9.0" "$(rl_extract_fsproj_version "$REPO_ROOT/src/Encodings/Encodings.fsproj")" "extract from the real fsproj"

echo "── 2. Semver comparison (current 0.9.0 vs latest v0.8.0) ──"
assert_ok   "0.9.0 > 0.8.0"          rl_version_gt 0.9.0 0.8.0
assert_fail "0.8.0 !> 0.9.0"         rl_version_gt 0.8.0 0.9.0
assert_fail "0.9.0 !> 0.9.0 (equal)" rl_version_gt 0.9.0 0.9.0
assert_ok   "0.10.0 > 0.9.0"         rl_version_gt 0.10.0 0.9.0
assert_ok   "1.0.0 > 0.9.0"          rl_version_gt 1.0.0 0.9.0

echo "── 3. Bump modes unchanged; current/staged does not bump ──"
assert_eq "0.9.0"  "$(rl_compute_next_version 0.9.0 current)" "current → 0.9.0 (no bump)"
assert_eq "0.9.0"  "$(rl_compute_next_version 0.9.0 staged)"  "staged → 0.9.0 (no bump)"
assert_eq "1.0.0"  "$(rl_compute_next_version 0.9.0 major)"   "major → 1.0.0"
assert_eq "0.10.0" "$(rl_compute_next_version 0.9.0 minor)"   "minor → 0.10.0"
assert_eq "0.9.1"  "$(rl_compute_next_version 0.9.0 patch)"   "patch → 0.9.1"

echo "── 4. CITATION.cff date add-or-replace ──"
# 4a: date ABSENT → inserted adjacent to version, exactly once, correct value.
cff_absent="$TMP/absent.cff"
cat > "$cff_absent" <<'YAML'
cff-version: 1.2.0
title: "FockMap"
version: 0.9.0
license: MIT
YAML
assert_ok "set date when absent" rl_set_cff_version_and_date "$cff_absent" 0.9.0 2026-07-23
assert_eq "1" "$(grep -c '^date-released: 2026-07-23$' "$cff_absent")" "date present exactly once"
assert_eq "1" "$(grep -c '^version: 0.9.0$' "$cff_absent")"            "version present exactly once"
# date-released must immediately follow version:
adj="$(awk '/^version:/{getline nl; print (nl ~ /^date-released:/) ? "adjacent" : "not-adjacent"}' "$cff_absent")"
assert_eq "adjacent" "$adj" "date-released inserted adjacent to version:"

# 4b: date PRESENT → replaced in place, exactly once.
cff_present="$TMP/present.cff"
cat > "$cff_present" <<'YAML'
cff-version: 1.2.0
version: 0.8.0
date-released: 2020-01-01
license: MIT
YAML
assert_ok "replace date when present" rl_set_cff_version_and_date "$cff_present" 0.9.0 2026-07-23
assert_eq "1" "$(grep -c '^date-released:' "$cff_present")"            "exactly one date-released after replace"
assert_eq "1" "$(grep -c '^date-released: 2026-07-23$' "$cff_present")" "date-released updated to new value"
assert_eq "0" "$(grep -c '2020-01-01' "$cff_present")"                 "old date removed"

# 4c: DUPLICATE date → must fail.
cff_dup="$TMP/dup.cff"
cat > "$cff_dup" <<'YAML'
version: 0.8.0
date-released: 2020-01-01
date-released: 2021-02-02
YAML
assert_fail "duplicate date-released rejected" rl_set_cff_version_and_date "$cff_dup" 0.9.0 2026-07-23

# 4d: MALFORMED (missing version) → must fail.
cff_nover="$TMP/nover.cff"
cat > "$cff_nover" <<'YAML'
cff-version: 1.2.0
date-released: 2020-01-01
YAML
assert_fail "missing version rejected" rl_set_cff_version_and_date "$cff_nover" 0.9.0 2026-07-23

# 4e: resulting CFF parses as YAML (if PyYAML available) with exactly one date.
if command -v python3 >/dev/null 2>&1 && python3 -c 'import yaml' 2>/dev/null; then
    assert_ok "result parses as YAML with one date" python3 - "$cff_absent" <<'PY'
import sys, yaml
d = yaml.safe_load(open(sys.argv[1]))
assert str(d["version"]) == "0.9.0", d.get("version")
assert str(d["date-released"]) == "2026-07-23", d.get("date-released")
PY
else
    echo "  (skip YAML parse assertion — PyYAML not available)"
fi

echo "── 5. CHANGELOG Unreleased finalization ──"
cl="$TMP/CHANGELOG.md"
cat > "$cl" <<'MD'
# Changelog

All notable changes to FockMap will be documented in this file.

## [0.9.0] - Unreleased

### 💥 BREAKING
- Raw physicist Hamiltonian contract.

## [0.8.0] - 2026-03-17
- Prior release.
MD
assert_ok "finalize Unreleased heading" rl_finalize_changelog "$cl" 0.9.0 2026-07-23
assert_eq "1" "$(grep -c '^## \[0.9.0\] - 2026-07-23$' "$cl")" "0.9.0 heading dated exactly once"
assert_eq "0" "$(grep -c 'Unreleased' "$cl")"                  "no lingering Unreleased"
assert_eq "1" "$(grep -c '^## \[0.8.0\] - 2026-03-17$' "$cl")" "prior 0.8.0 entry untouched"
# Idempotent: running again on the now-dated heading must succeed and not change it.
assert_ok "finalize is idempotent" rl_finalize_changelog "$cl" 0.9.0 2026-07-23
assert_eq "1" "$(grep -c '^## \[0.9.0\] - 2026-07-23$' "$cl")" "still dated exactly once after re-run"
# Missing heading → fail.
cl_missing="$TMP/CHANGELOG-missing.md"
printf '# Changelog\n\n## [0.7.0] - 2020-01-01\n' > "$cl_missing"
assert_fail "missing 0.9.0 heading rejected" rl_finalize_changelog "$cl_missing" 0.9.0 2026-07-23

echo "── 6. End-to-end staged simulation (current 0.9.0 from latest v0.8.0) ──"
# Copy the real staged files into a scratch dir and drive the pure helpers exactly
# as release.sh/current would, asserting the finalized outputs.
sim="$TMP/sim"; mkdir -p "$sim/src/Encodings"
cp "$REPO_ROOT/src/Encodings/Encodings.fsproj" "$sim/src/Encodings/Encodings.fsproj"
cp "$REPO_ROOT/CITATION.cff"                    "$sim/CITATION.cff"
cp "$REPO_ROOT/CHANGELOG.md"                    "$sim/CHANGELOG.md"
sim_ver="$(rl_extract_fsproj_version "$sim/src/Encodings/Encodings.fsproj")"
assert_eq "0.9.0" "$sim_ver" "staged .fsproj version is 0.9.0"
assert_ok "staged version > last tag 0.8.0" rl_version_gt "$sim_ver" 0.8.0
assert_ok "staged CFF finalize"       rl_set_cff_version_and_date "$sim/CITATION.cff" "$sim_ver" 2026-07-23
assert_ok "staged CHANGELOG finalize" rl_finalize_changelog "$sim/CHANGELOG.md" "$sim_ver" 2026-07-23
assert_eq "1" "$(grep -c '^date-released: 2026-07-23$' "$sim/CITATION.cff")" "sim CFF has one finalized date"
assert_eq "1" "$(grep -c '^## \[0.9.0\] - 2026-07-23$' "$sim/CHANGELOG.md")" "sim CHANGELOG heading dated"
# The .fsproj must be unchanged by a staged release (no bump).
assert_ok "sim .fsproj unchanged (no bump)" diff -q "$REPO_ROOT/src/Encodings/Encodings.fsproj" "$sim/src/Encodings/Encodings.fsproj"

echo "── 7. release.sh --dry-run current shows v0.9.0 (real script, this machine) ──"
dry_out="$(printf 'Y\n' | bash "$REPO_ROOT/scripts/release.sh" --dry-run current 2>&1 || true)"
if echo "$dry_out" | grep -q 'would release v0.9.0'; then
    ok "dry-run current → would release v0.9.0"
else
    bad "dry-run current did not report v0.9.0"; echo "$dry_out" | tail -5 >&2
fi
# And a bump mode still computes the next version (major → 1.0.0), unchanged.
dry_major="$(printf 'Y\n' | bash "$REPO_ROOT/scripts/release.sh" --dry-run major 2>&1 || true)"
if echo "$dry_major" | grep -q 'would release v1.0.0'; then
    ok "dry-run major → would release v1.0.0 (bump modes unchanged)"
else
    bad "dry-run major did not report v1.0.0"; echo "$dry_major" | tail -5 >&2
fi

echo ""
echo "─────────────────────────────────────────────"
echo "Release tooling tests: $PASS passed, $FAIL failed."
[[ "$FAIL" -eq 0 ]]
