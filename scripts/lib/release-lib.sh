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
#   rl_set_fsproj_version <fsproj> <new>            -> rewrite <Version> (portable)
#   rl_set_cff_version_and_date <cff> <ver> <date>  -> add-or-replace version + date
#   rl_finalize_changelog <changelog> <ver> <date>  -> Unreleased -> date (idempotent)
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

# Set the <Version> element of an fsproj to a new value (portable temp+mv; no
# GNU-only `sed -i`). Replaces the first <Version>…</Version> content and
# post-validates the written value.
rl_set_fsproj_version() {
    local fsproj="$1" newver="$2"
    if [[ ! -f "$fsproj" ]]; then
        echo "rl_set_fsproj_version: file not found: $fsproj" >&2
        return 1
    fi
    local tmp; tmp=$(mktemp)
    sed "s|<Version>[^<]*</Version>|<Version>${newver}</Version>|" "$fsproj" > "$tmp"
    mv "$tmp" "$fsproj"
    local got
    got=$(rl_extract_fsproj_version "$fsproj") || return 1
    if [[ "$got" != "$newver" ]]; then
        echo "rl_set_fsproj_version: post-check failed (got '$got', want '$newver')" >&2
        return 1
    fi
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

    local vcount dcount indented
    vcount=$(grep -c '^version:' "$cff" || true)
    dcount=$(grep -c '^date-released:' "$cff" || true)
    indented=$(grep -cE '^[[:space:]]+date-released:' "$cff" || true)

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

    local tmp; tmp=$(mktemp)
    # Replace version (and date-released if present) in a single pass.
    awk -v ver="$version" -v dt="$date" '
        /^version:/       { print "version: " ver; next }
        /^date-released:/ { print "date-released: " dt; next }
        { print }
    ' "$cff" > "$tmp"

    # When date-released was absent, insert it adjacent to version:.
    if [[ "$dcount" -eq 0 ]]; then
        local tmp2; tmp2=$(mktemp)
        awk -v dt="$date" '
            /^version:/ { print; print "date-released: " dt; next }
            { print }
        ' "$tmp" > "$tmp2"
        mv "$tmp2" "$tmp"
    fi

    mv "$tmp" "$cff"

    # Post-validate exact counts and values (exact string match, not regex).
    local nv nd totd
    nv=$(awk -v v="version: $version" '$0==v{c++} END{print c+0}' "$cff")
    nd=$(awk -v d="date-released: $date" '$0==d{c++} END{print c+0}' "$cff")
    totd=$(awk '/^date-released:/{c++} END{print c+0}' "$cff")
    if [[ "$nv" -ne 1 || "$nd" -ne 1 || "$totd" -ne 1 ]]; then
        echo "rl_set_cff_version_and_date: post-validation failed (version=$nv date=$nd total-date=$totd)" >&2
        return 1
    fi

    # Best-effort YAML parse validation (only if python3 + PyYAML are present).
    if command -v python3 >/dev/null 2>&1; then
        if ! python3 - "$cff" <<'PY'
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
            echo "rl_set_cff_version_and_date: resulting CITATION.cff does not parse as YAML" >&2
            return 1
        fi
    fi
}

# Finalize a `## [VERSION] - Unreleased` changelog heading to a dated one.
# Idempotent: if the heading is already dated, succeeds without change. Fails only
# when there is no `## [VERSION]` heading at all.
rl_finalize_changelog() {
    local cl="$1" version="$2" date="$3"
    if [[ ! -f "$cl" ]]; then
        echo "rl_finalize_changelog: file not found: $cl" >&2
        return 1
    fi

    local unreleased="## [$version] - Unreleased"
    if grep -Fq "$unreleased" "$cl"; then
        local tmp; tmp=$(mktemp)
        awk -v want="$unreleased" -v repl="## [$version] - $date" '
            index($0, want) == 1 { print repl; next }
            { print }
        ' "$cl" > "$tmp"
        mv "$tmp" "$cl"
        return 0
    fi

    # Already dated? Accept (idempotent). Otherwise fail — current/staged mode
    # requires the changelog to already carry a matching heading.
    if grep -Eq "^## \[$(printf '%s' "$version" | sed 's/\./\\./g')\] - [0-9]{4}-[0-9]{2}-[0-9]{2}" "$cl"; then
        return 0
    fi
    echo "rl_finalize_changelog: no '## [$version]' heading found to finalize" >&2
    return 1
}
