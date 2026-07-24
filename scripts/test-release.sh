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

echo "── 2b. grep helpers standalone-safe under set -euo pipefail (match/no-match/error) ──"
# Call the helpers DIRECTLY inside strict mode: a no-match must NOT exit the shell, and a
# genuine grep error must surface as rc 2 (not be masked as "0 matches").
hf="$TMP/helper.txt"; printf 'alpha\nbeta 0.9.0\ngamma\n' > "$hf"
rc=0
out=$(
    set -euo pipefail
    source "$REPO_ROOT/scripts/lib/release-lib.sh"
    c1=$(_rl_grep_count 'beta' "$hf")           # match → 1
    c0=$(_rl_grep_count 'ZZZ'  "$hf")           # no-match → 0 (must not exit)
    l1=$(_rl_grep_lines 'beta' "$hf")           # match → line 2
    l0=$(_rl_grep_lines 'ZZZ'  "$hf" || echo ERR)  # no-match → empty, rc 0
    printf 'c1=%s c0=%s l1=%s l0=[%s]\n' "$c1" "$c0" "$l1" "$l0"
) || rc=$?
assert_eq "0" "$rc" "helpers do not exit the shell under set -e (match+no-match)"
assert_eq "c1=1 c0=0 l1=2 l0=[]" "$out" "helper match/no-match values correct"
# Error path: shim grep to fail (rc 2); _rl_grep_count / _rl_grep_lines must return 2.
rce=0
oute=$(
    set -euo pipefail
    source "$REPO_ROOT/scripts/lib/release-lib.sh"
    grep() { return 2; }
    gc_rc=0; _rl_grep_count 'x' "$hf" >/dev/null 2>&1 || gc_rc=$?
    gl_rc=0; _rl_grep_lines 'x' "$hf" >/dev/null 2>&1 || gl_rc=$?
    printf 'gc=%s gl=%s\n' "$gc_rc" "$gl_rc"
) || rce=$?
assert_eq "0" "$rce" "error-path harness itself survives set -e"
assert_eq "gc=2 gl=2" "$oute" "grep error surfaces as rc 2 (not masked as 0 matches)"

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

echo "── 3f. Nth-call injection reaches real validators (set -e cleanup) ──"
# Inject a failure at a SPECIFIC downstream stage (not invocation-wide) so the actual
# validators run and their guards are exercised. A marker file proves the injection
# point was reached. Each runs under `set -euo pipefail`; asserts rc≠0, target
# bytes+mode unchanged, and no temp left. Counters are FILE-backed (bump) because the
# validators run inside command substitutions (their own subshells), where a plain
# shell variable would not accumulate.
bump() { local n; n=$(( $(cat "$1" 2>/dev/null || echo 0) + 1 )); echo "$n" > "$1"; echo "$n"; }

# (a) fsproj staged validator: `sed -n ... | head` runs once in extraction (pre-temp)
#     and once in staged post-validation (post-temp). Fail `head` on call #2 → reaches
#     the staged validator AFTER temp creation.
gf="$TMP/inj_fsproj.fsproj"; cp "$FSPROJ_FIXT" "$gf"; chmod 644 "$gf"
bsha=$(sha_of "$gf"); bmode=$(mode_of "$gf"); rm -f "$TMP/hit_head"; echo 0 > "$TMP/cnt_head"
rc=0
( set -euo pipefail
  head() { if [ "$(bump "$TMP/cnt_head")" -eq 2 ]; then : > "$TMP/hit_head"; return 1; fi; command head "$@"; }
  rl_set_fsproj_version "$gf" 9.9.9
) >/dev/null 2>&1 || rc=$?
[[ -f "$TMP/hit_head" ]] && ok "fsproj staged validator (head #2) reached" || bad "fsproj: staged validator not reached"
[[ "$rc" -ne 0 ]] && ok "fsproj staged-validator injection: rc≠0" || bad "fsproj staged-validator injection: rc==0"
assert_eq "$bsha"  "$(sha_of "$gf")"  "fsproj staged-validator injection: bytes unchanged"
assert_eq "$bmode" "$(mode_of "$gf")" "fsproj staged-validator injection: mode unchanged"
assert_eq "0" "$(count_temps "$TMP" "inj_fsproj.fsproj")" "fsproj staged-validator injection: no temp"

# (b) CFF count validator: awk runs as producer (#1) then nv/nd/totd validators (#2..).
#     Fail awk on #2 → reaches the nv validator AFTER temp creation (date present).
gc="$TMP/inj_cff.cff"; printf 'cff-version: 1.2.0\nversion: 0.9.0\ndate-released: 2020-01-01\nlicense: MIT\n' > "$gc"; chmod 644 "$gc"
bsha=$(sha_of "$gc"); bmode=$(mode_of "$gc"); rm -f "$TMP/hit_awk2"; echo 0 > "$TMP/cnt_awk"
rc=0
( set -euo pipefail
  awk() { if [ "$(bump "$TMP/cnt_awk")" -eq 2 ]; then : > "$TMP/hit_awk2"; return 1; fi; command awk "$@"; }
  rl_set_cff_version_and_date "$gc" 0.9.0 2026-07-24
) >/dev/null 2>&1 || rc=$?
[[ -f "$TMP/hit_awk2" ]] && ok "cff count validator (awk #2) reached" || bad "cff: count validator not reached"
[[ "$rc" -ne 0 ]] && ok "cff count-validator injection: rc≠0" || bad "cff count-validator injection: rc==0"
assert_eq "$bsha"  "$(sha_of "$gc")"  "cff count-validator injection: bytes unchanged"
assert_eq "$bmode" "$(mode_of "$gc")" "cff count-validator injection: mode unchanged"
assert_eq "0" "$(count_temps "$TMP" "inj_cff.cff")" "cff count-validator injection: no temp"

# (c) CFF YAML stage: shadow python3 to fail → reaches the YAML validation after the
#     count validators, still before the atomic move.
gc2="$TMP/inj_cff2.cff"; printf 'cff-version: 1.2.0\nversion: 0.9.0\ndate-released: 2020-01-01\n' > "$gc2"; chmod 644 "$gc2"
bsha=$(sha_of "$gc2"); bmode=$(mode_of "$gc2"); rm -f "$TMP/hit_py"
if command -v python3 >/dev/null 2>&1; then
    rc=0
    ( set -euo pipefail
      python3() { : > "$TMP/hit_py"; return 1; }
      rl_set_cff_version_and_date "$gc2" 0.9.0 2026-07-24
    ) >/dev/null 2>&1 || rc=$?
    [[ -f "$TMP/hit_py" ]] && ok "cff YAML validator (python3) reached" || bad "cff: YAML validator not reached"
    [[ "$rc" -ne 0 ]] && ok "cff YAML-validator injection: rc≠0" || bad "cff YAML-validator injection: rc==0"
    assert_eq "$bsha"  "$(sha_of "$gc2")"  "cff YAML-validator injection: bytes unchanged"
    assert_eq "$bmode" "$(mode_of "$gc2")" "cff YAML-validator injection: mode unchanged"
    assert_eq "0" "$(count_temps "$TMP" "inj_cff2.cff")" "cff YAML-validator injection: no temp"
else
    echo "  (skip cff YAML injection: python3 not available)"
fi

echo "── 3f2. grep errors are distinguished from no-match (not masked by || true) ──"
# (d) Prevalidation grep ERROR (rc 2) on the FIRST grep must abort with no temp/no change.
clg1="$TMP/inj_grep1.md"; printf '# CL\n\n## [0.9.0] - Unreleased\n' > "$clg1"; chmod 644 "$clg1"
bsha=$(sha_of "$clg1"); rm -f "$TMP/hit_grep1"; echo 0 > "$TMP/cnt_grepA"
rc=0
( set -euo pipefail
  grep() { if [ "$(bump "$TMP/cnt_grepA")" -eq 1 ]; then : > "$TMP/hit_grep1"; return 2; fi; command grep "$@"; }
  rl_finalize_changelog "$clg1" 0.9.0 2026-07-24
) >/dev/null 2>&1 || rc=$?
[[ -f "$TMP/hit_grep1" ]] && ok "changelog prevalidation grep #1 reached" || bad "changelog: grep #1 not reached"
[[ "$rc" -ne 0 ]] && ok "changelog grep-error(#1): rc≠0 (not masked as 0 matches)" || bad "changelog grep-error(#1): masked"
assert_eq "$bsha"  "$(sha_of "$clg1")"  "changelog grep-error(#1): bytes unchanged"
assert_eq "0" "$(count_temps "$TMP" "inj_grep1.md")" "changelog grep-error(#1): no temp"
# (e) Second grep ERROR (rc 2) must also abort cleanly (fresh file, file-backed counter).
clg2="$TMP/inj_grep2.md"; printf '# CL\n\n## [0.9.0] - Unreleased\n' > "$clg2"; chmod 644 "$clg2"
bsha=$(sha_of "$clg2"); rm -f "$TMP/hit_grep2"; echo 0 > "$TMP/cnt_grepB"
rc=0
( set -euo pipefail
  grep() { if [ "$(bump "$TMP/cnt_grepB")" -eq 2 ]; then : > "$TMP/hit_grep2"; return 2; fi; command grep "$@"; }
  rl_finalize_changelog "$clg2" 0.9.0 2026-07-24
) >/dev/null 2>&1 || rc=$?
[[ -f "$TMP/hit_grep2" ]] && ok "changelog prevalidation grep #2 reached" || bad "changelog: grep #2 not reached"
[[ "$rc" -ne 0 ]] && ok "changelog grep-error(#2): rc≠0" || bad "changelog grep-error(#2): masked"
assert_eq "$bsha" "$(sha_of "$clg2")" "changelog grep-error(#2): bytes unchanged"
assert_eq "0" "$(count_temps "$TMP" "inj_grep2.md")" "changelog grep-error(#2): no temp"
# (f) POST-VALIDATION grep error (on the temp) must clean up the temp and abort. Fail
#     grep only when it targets the temp file (matches .tmp.), reaching post-validation.
clg3="$TMP/inj_grep3.md"; printf '# CL\n\n## [0.9.0] - Unreleased\n' > "$clg3"; chmod 644 "$clg3"
bsha=$(sha_of "$clg3"); rm -f "$TMP/hit_grep_tmp"
rc=0
( set -euo pipefail
  grep() { for a in "$@"; do case "$a" in *.tmp.*) : > "$TMP/hit_grep_tmp"; return 2;; esac; done; command grep "$@"; }
  rl_finalize_changelog "$clg3" 0.9.0 2026-07-24
) >/dev/null 2>&1 || rc=$?
[[ -f "$TMP/hit_grep_tmp" ]] && ok "changelog post-validation grep (on temp) reached" || bad "changelog: post-val grep not reached"
[[ "$rc" -ne 0 ]] && ok "changelog post-val grep-error: rc≠0" || bad "changelog post-val grep-error: masked"
assert_eq "$bsha" "$(sha_of "$clg3")" "changelog post-val grep-error: bytes unchanged"
assert_eq "0" "$(count_temps "$TMP" "inj_grep3.md")" "changelog post-val grep-error: no temp"
# (g) Processor (cut) failure inside _rl_grep_lines must normalize to an error and abort
#     cleanly (this is prevalidation → no temp is created; bytes+mode unchanged).
clg4="$TMP/inj_cut.md"; printf '# CL\n\n## [0.9.0] - Unreleased\n' > "$clg4"; chmod 640 "$clg4"
bsha=$(sha_of "$clg4"); bmode=$(mode_of "$clg4"); rm -f "$TMP/hit_cut"
rc=0
( set -euo pipefail
  cut() { : > "$TMP/hit_cut"; return 1; }
  rl_finalize_changelog "$clg4" 0.9.0 2026-07-24
) >/dev/null 2>&1 || rc=$?
[[ -f "$TMP/hit_cut" ]] && ok "changelog line-extraction (cut) reached" || bad "changelog: cut processor not reached"
[[ "$rc" -ne 0 ]] && ok "changelog cut-processor failure: rc≠0" || bad "changelog cut-processor failure: masked"
assert_eq "$bsha"  "$(sha_of "$clg4")"  "changelog cut-processor failure: bytes unchanged"
assert_eq "$bmode" "$(mode_of "$clg4")" "changelog cut-processor failure: mode unchanged"
assert_eq "0" "$(count_temps "$TMP" "inj_cut.md")" "changelog cut-processor failure: no temp"

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
# Robust variants the exact-anchor-only check would have MISSED → now rejected:
# canonical Unreleased PLUS an extra same-version release token elsewhere.
cl_reject "canonical + extra unreleased variant" '## [0.9.0] - Unreleased
prefix ## [0.9.0] -  Unreleased
'
cl_reject "canonical + malformed dated" '## [0.9.0] - Unreleased
## [0.9.0] - 2020-1-1
'
cl_reject "canonical + odd-spacing heading" '## [0.9.0] - Unreleased
###   [0.9.0]
'
# Broadened grammar (blocker): malformed heading markers / separators / status tokens
# must be rejected even when NOT the canonical form.
cl_reject "prefixed heading marker x## [VER]" 'x## [0.9.0]
## [0.9.0] - Unreleased
'
cl_reject "x## [VER] -- Unreleased (double dash)" 'x## [0.9.0] -- Unreleased
## [0.9.0] - Unreleased
'
cl_reject "x## [VER]: Unreleased (colon sep)" 'x## [0.9.0]: Unreleased
## [0.9.0] - Unreleased
'
cl_reject "x## [VER] - TBD (status token)" 'x## [0.9.0] - TBD
## [0.9.0] - Unreleased
'
cl_reject "bare [VER] -- Unreleased token" 'The [0.9.0] -- Unreleased draft.
## [0.9.0] - Unreleased
'
cl_reject "bare [VER]: TBD token" 'Status [0.9.0]: TBD
## [0.9.0] - Unreleased
'
# Heading marker followed by intervening text then [VER] (marker-then-later token).
cl_reject "### Migration for [VER]" '## [0.9.0] - Unreleased
### Migration for [0.9.0]
'
cl_reject "x## Release [VER]" '## [0.9.0] - Unreleased
x## Release [0.9.0]
'
# Prose-reference policy: plain version mentions (NOT headings, NO release token) are
# acceptable alongside the one canonical heading.
cl_ok "prose mention allowed" '# Changelog

Upgrade to 0.9.0 today; see the [0.9.0] section for details.

## [0.9.0] - Unreleased
- x
'
# Markdown link reference is allowed (bracket token, no heading marker, no status token).
cl_ok "markdown link reference allowed" '# Changelog

Release [0.9.0]: https://github.com/x/encodings/releases/tag/v0.9.0

## [0.9.0] - Unreleased
- x
'
# …but a prose line carrying the reserved release token is rejected.
cl_reject "prose release-token rejected" '# Changelog

The [0.9.0] - Unreleased draft is coming.

## [0.9.0] - Unreleased
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
# Idempotent rejects a second dated heading (duplicate) even with no Unreleased.
cl_reject "duplicate dated (idempotent guard)" '## [0.9.0] - 2026-07-24
## [0.9.0] - 2026-07-25
'

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

echo "── 6b. git skip-guard predicate (cat-file -e 'HEAD^{commit}') ──"
if command -v git >/dev/null 2>&1; then
    # (1) Self-contained repo with a commit → predicate SUCCEEDS (section 7 would run).
    gr="$TMP/selfrepo"; mkdir -p "$gr"
    ( cd "$gr" && git init -q && git config user.email t@example.com && git config user.name t \
      && echo x > f && git add f && git commit -qm init ) >/dev/null 2>&1
    if git -C "$gr" cat-file -e 'HEAD^{commit}' 2>/dev/null; then
        ok "self-contained repo: HEAD commit object present (guard runs)"
    else bad "self-contained repo: predicate should have passed"; fi
    # (2) Broken worktree: .git points at a nonexistent gitdir → predicate FAILS cleanly.
    bw="$TMP/broken"; mkdir -p "$bw"; printf 'gitdir: %s/nonexistent\n' "$TMP" > "$bw/.git"
    if git -C "$bw" cat-file -e 'HEAD^{commit}' 2>/dev/null; then
        bad "broken worktree: predicate should have failed"
    else ok "broken worktree: predicate fails → guard skips cleanly"; fi
    # (3) Missing objects: valid ref but the object store is emptied → predicate FAILS
    #     (this is exactly what `rev-parse` alone would MISS but `cat-file -e` catches).
    mr="$TMP/missingobj"; cp -R "$gr" "$mr" 2>/dev/null; rm -rf "$mr"/.git/objects/* 2>/dev/null || true
    revparse_ok=0; git -C "$mr" rev-parse --verify -q HEAD >/dev/null 2>&1 && revparse_ok=1
    if git -C "$mr" cat-file -e 'HEAD^{commit}' 2>/dev/null; then
        bad "missing objects: cat-file predicate should have failed"
    else ok "missing objects: cat-file predicate fails → guard skips cleanly (rev-parse-ok=$revparse_ok)"; fi
else
    echo "  (skip git-guard predicate tests: git not available)"
fi

echo "── 7. release.sh --dry-run current shows v0.9.0 (real script, needs git) ──"
# The real release.sh needs a resolvable git repo whose HEAD commit OBJECT is present.
# `git cat-file -e 'HEAD^{commit}'` verifies the object is actually readable — stronger
# than `rev-parse`, which can resolve the ref name even when the object store (e.g. a
# worktree's alternates on unavailable host paths inside a container) is missing. Skip
# cleanly when the object is unavailable; a self-contained checkout runs the asserts.
if git -C "$REPO_ROOT" cat-file -e 'HEAD^{commit}' 2>/dev/null; then
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
    echo "  (skip: git HEAD commit object unavailable here — worktree/container/broken alternates)"
fi

echo ""
echo "─────────────────────────────────────────────"
echo "Release tooling tests: $PASS passed, $FAIL failed."
[[ "$FAIL" -eq 0 ]]
