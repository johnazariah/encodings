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

# Portable content hash (macOS `shasum`, GNU `sha256sum`/`sha1sum`, else `cksum`).
sha_of() {
    if   command -v shasum    >/dev/null 2>&1; then shasum    "$1" | awk '{print $1}'
    elif command -v sha256sum >/dev/null 2>&1; then sha256sum "$1" | awk '{print $1}'
    elif command -v sha1sum   >/dev/null 2>&1; then sha1sum   "$1" | awk '{print $1}'
    else cksum "$1" | awk '{print $1"-"$2}'; fi
}
# Portable octal mode reader, INDEPENDENT of the library helper (GNU/busybox `-c`,
# else BSD/macOS `-f`), so mode assertions do not just re-check the code under test.
mode_of() {
    if   stat -c '%a'  "$1" >/dev/null 2>&1; then stat -c '%a'  "$1"
    elif stat -f '%Lp' "$1" >/dev/null 2>&1; then stat -f '%Lp' "$1"
    else _rl_get_mode "$1"; fi
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

echo "── 3b. Portable fsproj version rewrite (no sed -i) ──"
fsproj_bump="$TMP/bump.fsproj"
cp "$FSPROJ_FIXT" "$fsproj_bump"
assert_ok "rewrite <Version> to 1.0.0" rl_set_fsproj_version "$fsproj_bump" 1.0.0
assert_eq "1.0.0" "$(rl_extract_fsproj_version "$fsproj_bump")" "fsproj now reports 1.0.0"
assert_eq "0" "$(grep -c '<Version>0.9.0</Version>' "$fsproj_bump")" "old <Version>0.9.0</Version> gone"

echo "── 3c. Strict prevalidation + atomic unchanged-on-failure ──"
# A helper that asserts rl_set_fsproj_version REJECTS the file AND leaves it byte-for-byte
# unchanged (atomicity). $1=name, $2=file contents.
assert_reject_unchanged() {
    local name="$1" body="$2"
    local f="$TMP/reject.fsproj"
    printf '%s\n' "$body" > "$f"
    local before; before=$(sha_of "$f")
    if rl_set_fsproj_version "$f" 9.9.9 >/dev/null 2>&1; then
        bad "$name: expected rejection but it succeeded"
        return
    fi
    local after; after=$(sha_of "$f")
    if [[ "$before" == "$after" ]]; then ok "$name (rejected; file unchanged)"; else bad "$name: file was mutated on failure"; fi
}
# absent: no <Version> markup at all.
assert_reject_unchanged "absent" '<Project></Project>'
# duplicate: two well-formed <Version> elements.
assert_reject_unchanged "duplicate" '<Version>0.9.0</Version>
<Version>0.8.0</Version>'
# valid + unterminated open tag.
assert_reject_unchanged "valid+unterminated" '<Version>0.9.0</Version>
<Version>'
# valid + orphan close tag.
assert_reject_unchanged "valid+orphan-close" '<Version>0.9.0</Version>
</Version>'
# malformed only: a single unterminated <Version>.
assert_reject_unchanged "malformed-only" '<Version>0.9.0'
# Sanity: a valid single element with surrounding tags is accepted (no false reject).
fsproj_single="$TMP/single.fsproj"
printf '%s\n' '<Project><PropertyGroup><Version>0.9.0</Version></PropertyGroup></Project>' > "$fsproj_single"
assert_ok "valid single element accepted" rl_set_fsproj_version "$fsproj_single" 2.3.4
assert_eq "2.3.4" "$(rl_extract_fsproj_version "$fsproj_single")" "valid single rewritten to 2.3.4"

echo "── 3d. Atomic write: mode preservation, same-dir temp, cleanup ──"
count_temps() { # <dir> <base>
    find "$1" -maxdepth 1 -name ".$2.tmp.*" 2>/dev/null | wc -l | tr -d '[:space:]'
}

# (a) Successful rewrite preserves the file's permission bits (default 0644).
fp="$TMP/mode644.fsproj"; cp "$FSPROJ_FIXT" "$fp"; chmod 644 "$fp"
assert_ok "rewrite (mode 644)" rl_set_fsproj_version "$fp" 1.1.1
assert_eq "644" "$(mode_of "$fp")" "mode 0644 preserved across replacement"

# (b) A non-default / more restrictive mode is preserved too.
fp2="$TMP/mode600.fsproj"; cp "$FSPROJ_FIXT" "$fp2"; chmod 600 "$fp2"
assert_ok "rewrite (mode 600)" rl_set_fsproj_version "$fp2" 1.2.0
assert_eq "600" "$(mode_of "$fp2")" "mode 0600 preserved across replacement"

# (c) An executable / nondefault mode is preserved.
fp3="$TMP/mode755.fsproj"; cp "$FSPROJ_FIXT" "$fp3"; chmod 755 "$fp3"
assert_ok "rewrite (mode 755)" rl_set_fsproj_version "$fp3" 1.3.0
assert_eq "755" "$(mode_of "$fp3")" "mode 0755 preserved across replacement"

# (d) The temp file is created in the SAME DIRECTORY as the target (same device →
#     atomic rename). Capture the source path handed to mv.
fp4="$TMP/samedir.fsproj"; cp "$FSPROJ_FIXT" "$fp4"
: > "$TMP/mv_srcs"
mv() { printf '%s\n' "$1" >> "$TMP/mv_srcs"; command mv "$@"; }
assert_ok "rewrite (capture mv)" rl_set_fsproj_version "$fp4" 1.4.0
unset -f mv
mv_src="$(tail -n1 "$TMP/mv_srcs")"
assert_eq "$(cd "$(dirname "$fp4")" && pwd)" "$(cd "$(dirname "$mv_src")" && pwd)" "temp created in the target's directory"
assert_eq "0" "$(count_temps "$TMP" "samedir.fsproj")" "no temp left after success"

# (e) Induced final-move failure: original bytes AND mode unchanged, no temp left.
fp5="$TMP/movefail.fsproj"; cp "$FSPROJ_FIXT" "$fp5"; chmod 640 "$fp5"
before_sha="$(sha_of "$fp5")"; before_mode="$(mode_of "$fp5")"
mv() { return 1; }   # force the atomic move to fail
if rl_set_fsproj_version "$fp5" 9.9.9 >/dev/null 2>&1; then bad "induced move failure: unexpectedly succeeded"; else ok "induced move failure returns non-zero"; fi
unset -f mv
assert_eq "$before_sha"  "$(sha_of "$fp5")" "original bytes unchanged after move failure"
assert_eq "$before_mode" "$(mode_of "$fp5")"                   "original mode unchanged after move failure"
assert_eq "0" "$(count_temps "$TMP" "movefail.fsproj")"        "no temp left after move failure"

# (f) Rejected malformed/duplicate inputs leave no temp behind either.
fpm="$TMP/malf.fsproj"; printf '%s\n' '<Version>0.9.0' > "$fpm"
rl_set_fsproj_version "$fpm" 9.9.9 >/dev/null 2>&1 || true
assert_eq "0" "$(count_temps "$TMP" "malf.fsproj")" "malformed rejection leaves no temp"
fpd="$TMP/dupe.fsproj"; printf '%s\n%s\n' '<Version>0.9.0</Version>' '<Version>0.8.0</Version>' > "$fpd"
rl_set_fsproj_version "$fpd" 9.9.9 >/dev/null 2>&1 || true
assert_eq "0" "$(count_temps "$TMP" "dupe.fsproj")" "duplicate rejection leaves no temp"

# (g) CFF and CHANGELOG mutators are atomic too: successful runs leave no temp, and
#     preserve mode.
cffm="$TMP/mode.cff"; cat > "$cffm" <<'YAML'
cff-version: 1.2.0
version: 0.9.0
license: MIT
YAML
chmod 644 "$cffm"
assert_ok "cff set (atomic)" rl_set_cff_version_and_date "$cffm" 0.9.0 2026-07-24
assert_eq "644" "$(mode_of "$cffm")" "cff mode preserved"
assert_eq "0" "$(count_temps "$TMP" "mode.cff")" "cff success leaves no temp"
clm="$TMP/mode.CHANGELOG.md"; printf '# Changelog\n\n## [0.9.0] - Unreleased\n- x\n' > "$clm"; chmod 644 "$clm"
assert_ok "changelog finalize (atomic)" rl_finalize_changelog "$clm" 0.9.0 2026-07-24
assert_eq "644" "$(mode_of "$clm")" "changelog mode preserved"
assert_eq "0" "$(count_temps "$TMP" "mode.CHANGELOG.md")" "changelog success leaves no temp"

echo "── 3e. Mode helper yields exactly one octal token ──"
mp="$TMP/probe.file"; printf x > "$mp"
for m in 644 600 755 640; do
    chmod "$m" "$mp"
    got="$(_rl_get_mode "$mp")"
    assert_eq "$m" "$got" "_rl_get_mode returns $m"
    # Exactly one whitespace-delimited token (no filesystem junk / second line).
    assert_eq "1" "$(printf '%s' "$got" | wc -w | tr -d '[:space:]')" "_rl_get_mode($m) is a single token"
    assert_eq "1" "$(_rl_get_mode "$mp" | wc -l | tr -d '[:space:]')" "_rl_get_mode($m) is a single line"
done
# Even with a noisy stderr environment the token stays clean (stderr is suppressed).
got="$(_rl_get_mode "$mp" 2>/dev/null)"
assert_eq "640" "$got" "_rl_get_mode ignores stderr noise"

echo "── 3f. set -e guarded cleanup on injected validator failure ──"
# Run a mutator under `set -euo pipefail` with an injected command failure AFTER temp
# creation; assert it returns non-zero (guard converts the failure into a clean return,
# not a hard set -e exit), leaves the target bytes+mode unchanged, and removes the temp.
# We inject by shadowing `awk` (first post-temp command in both mutators) with a failer.
run_guarded() {  # <name> <mutator> <target> [args...]   (mutator run in a fresh set -e shell)
    local name="$1"; shift
    local target="$2"   # $1 is the mutator function; $2 is its target file
    local before_sha before_mode after_sha after_mode dir base rc
    before_sha=$(sha_of "$target")
    before_mode=$(mode_of "$target")
    dir=$(dirname "$target"); base=$(basename "$target")
    # Subshell with strict mode + awk shadow that fails; call the mutator. `|| rc=$?`
    # both captures the status and keeps the outer set -e from firing.
    rc=0
    ( set -euo pipefail; awk() { return 1; }; "$@" ) >/dev/null 2>&1 || rc=$?
    after_sha=$(sha_of "$target")
    after_mode=$(mode_of "$target")
    if [[ "$rc" -ne 0 ]]; then ok "$name: returns non-zero under set -e"; else bad "$name: unexpectedly succeeded"; fi
    assert_eq "$before_sha"  "$after_sha"  "$name: target bytes unchanged"
    assert_eq "$before_mode" "$after_mode" "$name: target mode unchanged"
    assert_eq "0" "$(find "$dir" -maxdepth 1 -name ".$base.tmp.*" 2>/dev/null | wc -l | tr -d '[:space:]')" "$name: no temp left"
}
gf="$TMP/guard.fsproj"; cp "$FSPROJ_FIXT" "$gf"; chmod 644 "$gf"
run_guarded "fsproj injected-fail" rl_set_fsproj_version "$gf" 9.9.9
gc="$TMP/guard.cff"; printf 'cff-version: 1.2.0\nversion: 0.9.0\nlicense: MIT\n' > "$gc"; chmod 644 "$gc"
run_guarded "cff injected-fail" rl_set_cff_version_and_date "$gc" 0.9.0 2026-07-24
# The injected-failure runs must not have mutated the originals (double-check values).
assert_eq "0.9.0" "$(rl_extract_fsproj_version "$gf")" "fsproj value intact after injected failure"
assert_eq "1" "$(grep -c '^version: 0.9.0$' "$gc")" "cff value intact after injected failure"

echo "── 3g. Strict CHANGELOG heading validation ──"
cl_ok() {  # helper: assert finalize SUCCEEDS and dates the heading
    local name="$1" body="$2"
    local f="$TMP/cl_ok.md"; printf '%s' "$body" > "$f"; chmod 644 "$f"
    if rl_finalize_changelog "$f" 0.9.0 2026-07-24 >/dev/null 2>&1; then
        assert_eq "1" "$(grep -cE '^## \[0.9.0\] - 2026-07-24$' "$f")" "$name: dated exactly once"
    else
        bad "$name: expected success but was rejected"
    fi
}
cl_reject() {  # helper: assert finalize REJECTS and leaves bytes+mode unchanged, no temp
    local name="$1" body="$2"
    local f="$TMP/cl_rej.md"; printf '%s' "$body" > "$f"; chmod 640 "$f"
    local bsha; bsha=$(sha_of "$f"); local bmode; bmode=$(mode_of "$f")
    if rl_finalize_changelog "$f" 0.9.0 2026-07-24 >/dev/null 2>&1; then
        bad "$name: expected rejection but succeeded"
    else
        ok "$name (rejected)"
    fi
    assert_eq "$bsha"  "$(sha_of "$f")" "$name: bytes unchanged"
    assert_eq "$bmode" "$(mode_of "$f")"                   "$name: mode unchanged"
    assert_eq "0" "$(count_temps "$TMP" "cl_rej.md")"      "$name: no temp"
}
# Valid single anchored heading → success.
cl_ok "valid anchored" '# Changelog

## [0.9.0] - Unreleased
- x
'
# Prefixed / suffixed heading (not column-1 full-line) → reject.
cl_reject "prefixed heading"  'x## [0.9.0] - Unreleased
'
cl_reject "suffixed heading"  '## [0.9.0] - Unreleased (rc)
'
# Malformed spacing → reject.
cl_reject "malformed spacing" '##  [0.9.0] - Unreleased
'
# Duplicate anchored Unreleased headings → reject.
cl_reject "duplicate unreleased" '## [0.9.0] - Unreleased
## [0.9.0] - Unreleased
'
# Dated heading already present alongside Unreleased → reject.
cl_reject "dated+unreleased" '## [0.9.0] - 2020-01-01
## [0.9.0] - Unreleased
'
# No heading for this version → reject.
cl_reject "no heading" '# Changelog

## [0.8.0] - 2020-01-01
'
# Post-validation injection: shadow awk so the staged temp keeps "Unreleased" → the
# strict post-validation must fail, clean up, and leave the original unchanged.
clpi="$TMP/cl_postval.md"; printf '# Changelog\n\n## [0.9.0] - Unreleased\n' > "$clpi"; chmod 644 "$clpi"
pbsha=$(sha_of "$clpi")
(
    set -euo pipefail
    # awk that ignores its script and copies the input (last arg) verbatim → no replacement.
    awk() { local a last=""; for a in "$@"; do last="$a"; done; command cat "$last"; }
    rl_finalize_changelog "$clpi" 0.9.0 2026-07-24
) >/dev/null 2>&1 && bad "changelog post-val injection: unexpectedly succeeded" || ok "changelog post-val injection rejected"
assert_eq "$pbsha" "$(sha_of "$clpi")" "changelog post-val injection: bytes unchanged"
assert_eq "0" "$(count_temps "$TMP" "cl_postval.md")"     "changelog post-val injection: no temp"
# Idempotent: an already-dated single heading succeeds without change.
cli="$TMP/cl_idem.md"; printf '# Changelog\n\n## [0.9.0] - 2026-07-24\n' > "$cli"
assert_ok "changelog idempotent (already dated)" rl_finalize_changelog "$cli" 0.9.0 2026-07-24

echo "── 4. CITATION.cff date add-or-replace ──"
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

echo "── 7. release.sh --dry-run current shows v0.9.0 (real script, needs git) ──"
# The real release.sh needs a resolvable git repo. In constrained environments (e.g. a
# git *worktree* mounted into a container, whose .git points at unavailable host paths)
# git cannot resolve HEAD; skip these end-to-end wrapper checks there rather than failing
# — the pure helpers above already cover the logic cross-platform.
if git -C "$REPO_ROOT" rev-parse --verify -q HEAD >/dev/null 2>&1; then
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
else
    echo "  (skip: git cannot resolve HEAD in this environment — worktree/container)"
fi

echo ""
echo "─────────────────────────────────────────────"
echo "Release tooling tests: $PASS passed, $FAIL failed."
[[ "$FAIL" -eq 0 ]]
