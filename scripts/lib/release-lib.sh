#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# FockMap release helper library (sourceable, side-effect free on source).
#
# Portable across macOS (bash 3.2 / BSD sed+awk) and Linux (bash 4+/GNU). No
# `grep -oP`, no GNU-only `sed -i`. Every mutation writes a temp file and moves
# it into place, so it works identically on both platforms.
#
# Functions:
#   rl_extract_fsproj_version <fsproj>              -> echoes <Version> text
#   rl_version_gt <a> <b>                           -> exit 0 iff a > b (semver core)
#   rl_compute_next_version <current> <mode>        -> echoes next version
#   rl_set_fsproj_version <fsproj> <new>            -> rewrite <Version> (atomic)
#   rl_set_cff_version_and_date <cff> <ver> <date>  -> add-or-replace version + date (atomic)
#   rl_finalize_changelog <changelog> <ver> <date>  -> Unreleased -> date (atomic, idempotent)
#
# Atomic-write contract (all mutators): inputs are fully validated BEFORE any write;
# the new content is built in a temp file created in the SAME DIRECTORY as the target
# (so the final rename is a same-filesystem atomic mv); the target's permission bits
# are preserved; the temp file is removed on EVERY failure path (write, validation,
# chmod, or move); and the target is left byte-for-byte unchanged whenever the call
# fails. Ownership: the temp is created by the invoking user, so ownership is preserved
# for the normal same-user case; changing to a different original owner would require
# root and is out of scope (no chown is attempted).
#
# All functions return non-zero and print to stderr on error.
# ─────────────────────────────────────────────────────────────────────────────

# Extract the <Version>…</Version> value from an fsproj without PCRE.
# Portable BRE sed; takes the first match only.
rl_extract_fsproj_version() {
    local fsproj="$1"
    if [[ ! -f "$fsproj" ]]; then
        echo "rl_extract_fsproj_version: file not found: $fsproj" >&2
        return 1
    fi
    local v
    v=$(sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' "$fsproj" | head -n1)
    if [[ -z "$v" ]]; then
        echo "rl_extract_fsproj_version: no <Version> element in $fsproj" >&2
        return 1
    fi
    printf '%s\n' "$v"
}

# Numeric semver-core comparison (major.minor.patch; pre-release/build ignored).
# Returns 0 (true) iff $1 > $2.
rl_version_gt() {
    local a="$1" b="$2"
    local amaj amin apat bmaj bmin bpat
    IFS=. read -r amaj amin apat <<<"$a"
    IFS=. read -r bmaj bmin bpat <<<"$b"
    # Strip any pre-release/build suffix from the patch component.
    apat=${apat%%[-+]*}; bpat=${bpat%%[-+]*}
    amaj=${amaj:-0}; amin=${amin:-0}; apat=${apat:-0}
    bmaj=${bmaj:-0}; bmin=${bmin:-0}; bpat=${bpat:-0}
    if (( 10#$amaj != 10#$bmaj )); then (( 10#$amaj > 10#$bmaj )); return; fi
    if (( 10#$amin != 10#$bmin )); then (( 10#$amin > 10#$bmin )); return; fi
    (( 10#$apat > 10#$bpat ))
}

# Compute the next version from a current version and a mode.
# Modes: current|staged (no bump), major, minor, patch.
rl_compute_next_version() {
    local cur="$1" mode="$2"
    local maj min pat
    IFS=. read -r maj min pat <<<"$cur"
    pat=${pat%%[-+]*}
    maj=${maj:-0}; min=${min:-0}; pat=${pat:-0}
    case "$mode" in
        current|staged) printf '%s\n' "$cur" ;;
        major)          printf '%s\n' "$((maj + 1)).0.0" ;;
        minor)          printf '%s\n' "$maj.$((min + 1)).0" ;;
        patch)          printf '%s\n' "$maj.$min.$((pat + 1))" ;;
        *) echo "rl_compute_next_version: unknown mode '$mode'" >&2; return 2 ;;
    esac
}

# ── Shared atomic-write primitives ──────────────────────────────────────────────

# Count occurrences of an ERE ($1) in file ($2). Robust under `set -e`/`pipefail`:
# always returns 0 and prints the count (0 when there are no matches).
_rl_count() {
    grep -oE "$1" "$2" 2>/dev/null | wc -l | tr -d '[:space:]'
    return 0
}

# Count lines matching an ERE ($1) in file ($2), distinguishing NO-MATCH from ERROR.
# `grep -c` exits 0 (>=1 match), 1 (no match), or >=2 (real error, e.g. unreadable
# file / bad pattern). This prints the count and returns 0 for match/no-match, but
# returns 2 (no output) on a genuine grep error — so callers can clean up and fail
# instead of masking the error as "0 matches" (which `|| true` would do).
_rl_grep_count() {
    local out rc
    out=$(grep -cE "$1" "$2" 2>/dev/null); rc=$?
    case "$rc" in
        0|1) printf '%s\n' "$out"; return 0 ;;
        *)   return 2 ;;
    esac
}

# Print the matching LINE NUMBERS of an ERE ($1) in file ($2), one per line (nothing
# when there is no match). Returns 0 for match/no-match, 2 on a genuine grep error.
# Used to build error-aware unions without masking failures via `|| true` or tripping
# `pipefail` on a no-match inside a pipeline.
_rl_grep_lines() {
    local out rc
    out=$(grep -nE "$1" "$2" 2>/dev/null); rc=$?
    case "$rc" in
        0) printf '%s\n' "$out" | cut -d: -f1 ;;
        1) : ;;
        *) return 2 ;;
    esac
    return 0
}

# Echo the permission bits of a file as EXACTLY ONE octal token (e.g. 644).
#
# Platform is detected once via `uname` so the wrong-platform `stat` is never run
# (GNU `stat -f '%Lp' FILE` would print filesystem status for FILE to stdout — junk,
# not a mode — before failing; running it and salvaging the fallback risks emitting
# two tokens). The chosen `stat` output is then validated to be a single octal token;
# anything else (empty, multi-line, non-octal) falls through to a portable Python
# reader. Returns non-zero (no output) if no reader yields a valid octal token.
_rl_get_mode() {
    local f="$1" m
    case "$(uname -s 2>/dev/null || echo unknown)" in
        Darwin|*BSD*|DragonFly)
            m=$(stat -f '%Lp' "$f" 2>/dev/null)
            ;;
        *)
            m=$(stat -c '%a' "$f" 2>/dev/null)
            ;;
    esac
    if [[ "$m" =~ ^[0-7]{1,4}$ ]]; then
        printf '%s\n' "$m"
        return 0
    fi
    if command -v python3 >/dev/null 2>&1; then
        m=$(python3 -c 'import os,sys; print("%o" % (os.stat(sys.argv[1]).st_mode & 0o7777))' "$f" 2>/dev/null)
        if [[ "$m" =~ ^[0-7]{1,4}$ ]]; then
            printf '%s\n' "$m"
            return 0
        fi
    fi
    return 1
}

# Create a temp file in the SAME DIRECTORY as the target, so a subsequent `mv` is a
# same-filesystem atomic rename. Echoes the temp path.
_rl_mktemp_beside() {
    local target="$1" dir base
    dir=$(dirname "$target")
    base=$(basename "$target")
    mktemp "${dir}/.${base}.tmp.XXXXXX"
}

# Finalize: copy the target's mode onto the temp, then atomically move temp -> target.
# On ANY failure the temp is removed and the target is left untouched (non-zero return).
_rl_finalize_move() {
    local target="$1" tmp="$2" mode
    mode=$(_rl_get_mode "$target") || { rm -f "$tmp"; return 1; }
    if [[ -n "$mode" ]]; then
        chmod "$mode" "$tmp" || { rm -f "$tmp"; return 1; }
    fi
    mv "$tmp" "$target" || { rm -f "$tmp"; return 1; }
}

# Set the <Version> element of an fsproj to a new value.
#
# Strict + atomic:
#   * PREVALIDATES the file and REFUSES to touch it unless it contains EXACTLY ONE
#     well-formed <Version>…</Version> element and NO other `<Version>`/`</Version>`
#     tags. Rejects: absent, duplicate, unterminated open tag, orphan close tag, or
#     otherwise malformed version markup.
#   * On ANY failure the file is left BYTE-FOR-BYTE UNCHANGED (validation happens
#     before any write; the new content is staged in a temp file and only moved into
#     place once it is fully built and post-validated).
#   * Replaces the single occurrence LITERALLY (no sed/awk metacharacter surprises),
#     then post-validates that the file now reports exactly the requested version.
# Portable: no GNU-only `sed -i`, no PCRE.
rl_set_fsproj_version() {
    local fsproj="$1" newver="$2"
    if [[ ! -f "$fsproj" ]]; then
        echo "rl_set_fsproj_version: file not found: $fsproj" >&2
        return 1
    fi

    # Count open tags, close tags, and well-formed single-line pairs across the file
    # (robust under set -e/pipefail — see _rl_count).
    local opens closes pairs
    opens=$(_rl_count '<Version>'  "$fsproj")
    closes=$(_rl_count '</Version>' "$fsproj")
    pairs=$(_rl_count '<Version>[^<]*</Version>' "$fsproj")

    if [[ "$opens" -eq 0 && "$closes" -eq 0 ]]; then
        echo "rl_set_fsproj_version: no <Version> element in $fsproj (unchanged)" >&2
        return 1
    fi
    if [[ "$opens" -ne 1 || "$closes" -ne 1 || "$pairs" -ne 1 ]]; then
        echo "rl_set_fsproj_version: refusing to edit — expected exactly one well-formed <Version>…</Version> element, found opens=$opens closes=$closes well-formed=$pairs (file unchanged)" >&2
        return 1
    fi

    # Exactly one well-formed element. Read its current value and build the new one.
    local oldver oldtag newtag
    oldver=$(rl_extract_fsproj_version "$fsproj") || return 1
    oldtag="<Version>${oldver}</Version>"
    newtag="<Version>${newver}</Version>"

    # Stage the new content in a temp file BESIDE the target (same filesystem).
    local tmp; tmp=$(_rl_mktemp_beside "$fsproj") || {
        echo "rl_set_fsproj_version: could not create temp beside $fsproj (unchanged)" >&2
        return 1
    }
    # Literal single-occurrence replacement (index/substr — no regex metacharacters).
    if ! awk -v old="$oldtag" -v new="$newtag" '
        !done { i = index($0, old); if (i > 0) { $0 = substr($0, 1, i-1) new substr($0, i+length(old)); done = 1 } }
        { print }
    ' "$fsproj" > "$tmp"; then
        rm -f "$tmp"
        echo "rl_set_fsproj_version: failed to write staged content (unchanged)" >&2
        return 1
    fi

    # Post-validate the staged content BEFORE swapping it into place, so a bad write
    # never replaces the good original. Guard the substitution so a pipeline failure
    # under `set -e`/`pipefail` still removes the temp instead of hard-exiting.
    local staged
    if ! staged=$(sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' "$tmp" | head -n1); then
        rm -f "$tmp"
        echo "rl_set_fsproj_version: failed to read staged version (unchanged)" >&2
        return 1
    fi
    if [[ "$staged" != "$newver" ]]; then
        rm -f "$tmp"
        echo "rl_set_fsproj_version: post-check failed (staged '$staged', want '$newver'); file unchanged" >&2
        return 1
    fi

    # Preserve mode, then atomic same-filesystem move (temp removed on any failure).
    _rl_finalize_move "$fsproj" "$tmp" || {
        echo "rl_set_fsproj_version: atomic replace failed (file unchanged)" >&2
        return 1
    }
}

# Set CITATION.cff `version:` and `date-released:`.
#   * Requires exactly one top-level `version:` key.
#   * Replaces exactly one existing top-level `date-released:` key, OR inserts one
#     immediately after `version:` when absent.
#   * Fails on duplicate or malformed (indented) date-released keys.
#   * Post-validates the result (exactly one of each, correct values) and, when a
#     YAML parser is available, that the file still parses.
rl_set_cff_version_and_date() {
    local cff="$1" version="$2" date="$3"
    if [[ ! -f "$cff" ]]; then
        echo "rl_set_cff_version_and_date: file not found: $cff" >&2
        return 1
    fi

    # Prevalidation counts, error-aware (a genuine grep failure aborts rather than
    # being masked as "0 matches").
    local vcount dcount indented
    if ! vcount=$(_rl_grep_count '^version:' "$cff") \
       || ! dcount=$(_rl_grep_count '^date-released:' "$cff") \
       || ! indented=$(_rl_grep_count '^[[:space:]]+date-released:' "$cff"); then
        echo "rl_set_cff_version_and_date: grep error reading '$cff'" >&2
        return 1
    fi

    if [[ "$vcount" -ne 1 ]]; then
        echo "rl_set_cff_version_and_date: expected exactly one top-level 'version:' key, found $vcount" >&2
        return 1
    fi
    if [[ "$dcount" -gt 1 ]]; then
        echo "rl_set_cff_version_and_date: duplicate 'date-released:' keys (found $dcount)" >&2
        return 1
    fi
    if [[ "$indented" -gt 0 ]]; then
        echo "rl_set_cff_version_and_date: malformed (indented) 'date-released' key present" >&2
        return 1
    fi

    # Stage into a temp file BESIDE the target (same filesystem).
    local tmp; tmp=$(_rl_mktemp_beside "$cff") || {
        echo "rl_set_cff_version_and_date: could not create temp beside $cff (unchanged)" >&2
        return 1
    }
    # Replace version (and date-released if present) in a single pass.
    if ! awk -v ver="$version" -v dt="$date" '
        /^version:/       { print "version: " ver; next }
        /^date-released:/ { print "date-released: " dt; next }
        { print }
    ' "$cff" > "$tmp"; then
        rm -f "$tmp"; echo "rl_set_cff_version_and_date: staged write failed (unchanged)" >&2; return 1
    fi

    # When date-released was absent, insert it adjacent to version: (second staged pass).
    if [[ "$dcount" -eq 0 ]]; then
        local tmp2; tmp2=$(_rl_mktemp_beside "$cff") || { rm -f "$tmp"; echo "rl_set_cff_version_and_date: temp2 failed (unchanged)" >&2; return 1; }
        if ! awk -v dt="$date" '
            /^version:/ { print; print "date-released: " dt; next }
            { print }
        ' "$tmp" > "$tmp2"; then
            rm -f "$tmp" "$tmp2"; echo "rl_set_cff_version_and_date: staged insert failed (unchanged)" >&2; return 1
        fi
        rm -f "$tmp"; tmp="$tmp2"
    fi

    # Post-validate the STAGED temp (exact string match), BEFORE swapping it in.
    # Guard each substitution so an awk failure under `set -e`/`pipefail` removes the
    # temp instead of hard-exiting.
    local nv nd totd
    if ! nv=$(awk -v v="version: $version" '$0==v{c++} END{print c+0}' "$tmp") \
       || ! nd=$(awk -v d="date-released: $date" '$0==d{c++} END{print c+0}' "$tmp") \
       || ! totd=$(awk '/^date-released:/{c++} END{print c+0}' "$tmp"); then
        rm -f "$tmp"
        echo "rl_set_cff_version_and_date: staged validation read failed; file unchanged" >&2
        return 1
    fi
    if [[ "$nv" -ne 1 || "$nd" -ne 1 || "$totd" -ne 1 ]]; then
        rm -f "$tmp"
        echo "rl_set_cff_version_and_date: post-validation failed (version=$nv date=$nd total-date=$totd); file unchanged" >&2
        return 1
    fi

    # Best-effort YAML parse validation of the STAGED temp (only if python3 + PyYAML).
    if command -v python3 >/dev/null 2>&1; then
        if ! python3 - "$tmp" <<'PY'
import sys
try:
    import yaml
except Exception:
    sys.exit(0)  # PyYAML absent: skip (portable best-effort)
try:
    with open(sys.argv[1]) as fh:
        yaml.safe_load(fh)
except Exception as exc:
    sys.stderr.write("CFF YAML parse error: %s\n" % exc)
    sys.exit(3)
PY
        then
            rm -f "$tmp"
            echo "rl_set_cff_version_and_date: staged CITATION.cff does not parse as YAML; file unchanged" >&2
            return 1
        fi
    fi

    # Preserve mode, then atomic same-filesystem move (temp removed on any failure).
    _rl_finalize_move "$cff" "$tmp" || {
        echo "rl_set_cff_version_and_date: atomic replace failed (file unchanged)" >&2
        return 1
    }
}

# Finalize a `## [VERSION] - Unreleased` changelog heading to a dated one, strictly.
#
# Strict + atomic:
#   * PREVALIDATES robustly. Exactly one canonical `^## \[VERSION\] - Unreleased$`
#     heading must be present, AND no OTHER version-referencing release line may exist.
#     A "version-referencing release line" is any of:
#       - a Markdown heading line mentioning `[VERSION]`   (`^[[:space:]]*#+.*\[VERSION\]`)
#       - a release token `\[VERSION\][[:space:]]*-[[:space:]]*Unreleased` anywhere
#         (catches prefixed/suffixed/odd-spacing Unreleased variants)
#       - a release token `\[VERSION\][[:space:]]*-[[:space:]]*[0-9]` anywhere
#         (catches canonical or malformed dated variants)
#     Any occurrence beyond the single canonical line (prefixed/suffixed/spacing/
#     malformed/duplicate/dated-alongside-Unreleased) is rejected. PROSE POLICY: a plain
#     prose mention of the version (e.g. "upgrade to 0.9.0", "see [0.9.0]") is fine — it
#     is only rejected if it is a heading or carries a `- Unreleased` / `- <digit>`
#     release token, which are reserved for the changelog heading.
#   * IDEMPOTENT: if there is no Unreleased heading but exactly one canonical dated
#     `^## \[VERSION\] - YYYY-MM-DD$` (and no other version-referencing release line),
#     succeeds without change.
#   * Rewrites into a same-dir temp (exact full-line match only), then POST-VALIDATES
#     the STAGED temp — exactly one dated heading for VERSION and ZERO Unreleased for
#     VERSION — BEFORE the atomic move. Preserves mode; removes the temp on any failure.
#   * grep reads distinguish no-match from error (see _rl_grep_count): a genuine grep
#     failure aborts without masking it as "0 matches".
rl_finalize_changelog() {
    local cl="$1" version="$2" date="$3"
    if [[ ! -f "$cl" ]]; then
        echo "rl_finalize_changelog: file not found: $cl" >&2
        return 1
    fi

    # Regex-escape the version for anchored matching (dots etc. are literal).
    local esc
    esc=$(printf '%s' "$version" | sed 's#[][\\.^$*+?(){}|-]#\\&#g')

    # Robust prevalidation counts (error-aware). `total` counts DISTINCT lines that
    # reference this version as a heading or release token — collected without masking
    # grep errors (`_rl_grep_lines`) and without tripping pipefail on a no-match.
    local canon_unrel canon_dated l_head l_unrel l_dated total
    if ! canon_unrel=$(_rl_grep_count "^## \[${esc}\] - Unreleased$" "$cl"); then
        echo "rl_finalize_changelog: grep error reading '$cl'" >&2; return 1; fi
    if ! canon_dated=$(_rl_grep_count "^## \[${esc}\] - [0-9]{4}-[0-9]{2}-[0-9]{2}$" "$cl"); then
        echo "rl_finalize_changelog: grep error reading '$cl'" >&2; return 1; fi
    if ! l_head=$(_rl_grep_lines "^[[:space:]]*#+.*\[${esc}\]" "$cl") \
       || ! l_unrel=$(_rl_grep_lines "\[${esc}\][[:space:]]*-[[:space:]]*Unreleased" "$cl") \
       || ! l_dated=$(_rl_grep_lines "\[${esc}\][[:space:]]*-[[:space:]]*[0-9]" "$cl"); then
        echo "rl_finalize_changelog: grep error scanning '$cl'" >&2; return 1; fi
    total=$(printf '%s\n%s\n%s\n' "$l_head" "$l_unrel" "$l_dated" \
            | awk 'NF>0 && !seen[$0]++ {c++} END{print c+0}')

    # Finalize case: exactly one canonical Unreleased line and NOTHING else references
    # this version.
    if [[ "$canon_unrel" -eq 1 && "$total" -eq 1 ]]; then
        : # proceed to rewrite below
    elif [[ "$canon_unrel" -eq 0 && "$canon_dated" -eq 1 && "$total" -eq 1 ]]; then
        # Idempotent: already finalized to a single canonical dated heading.
        return 0
    else
        echo "rl_finalize_changelog: refusing to edit — expected exactly one canonical '^## [$version] - Unreleased$' heading and no other '[$version]' release line (canonical-unreleased=$canon_unrel canonical-dated=$canon_dated version-referencing-lines=$total); file unchanged" >&2
        return 1
    fi

    # Stage the rewrite: replace ONLY the exact full-line heading.
    local tmp; tmp=$(_rl_mktemp_beside "$cl") || {
        echo "rl_finalize_changelog: could not create temp beside $cl (unchanged)" >&2
        return 1
    }
    if ! awk -v ver="$version" -v dt="$date" '
        $0 == ("## [" ver "] - Unreleased") { print "## [" ver "] - " dt; next }
        { print }
    ' "$cl" > "$tmp"; then
        rm -f "$tmp"; echo "rl_finalize_changelog: staged write failed (unchanged)" >&2; return 1
    fi

    # Post-validate the STAGED temp BEFORE the atomic move: exactly one dated heading
    # for VERSION and zero remaining Unreleased for VERSION. Reads are error-aware.
    local pd pu
    if ! pd=$(_rl_grep_count "^## \[${esc}\] - ${date}$" "$tmp"); then
        rm -f "$tmp"; echo "rl_finalize_changelog: grep error validating staged temp; file unchanged" >&2; return 1; fi
    if ! pu=$(_rl_grep_count "^## \[${esc}\] - Unreleased$" "$tmp"); then
        rm -f "$tmp"; echo "rl_finalize_changelog: grep error validating staged temp; file unchanged" >&2; return 1; fi
    if [[ "$pd" -ne 1 || "$pu" -ne 0 ]]; then
        rm -f "$tmp"
        echo "rl_finalize_changelog: post-validation failed (dated=$pd unreleased=$pu); file unchanged" >&2
        return 1
    fi

    _rl_finalize_move "$cl" "$tmp" || {
        echo "rl_finalize_changelog: atomic replace failed (file unchanged)" >&2
        return 1
    }
}
