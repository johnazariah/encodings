#!/usr/bin/env bash
# Semantic documentation check.
#
# Extracts every ```fsharp block from the README and each cookbook chapter,
# concatenates the blocks per document, references the freshly-built Encodings
# assembly, and executes the result with `dotnet fsi`. A compile OR runtime error
# fails the run. Selected documents additionally assert EXACT expected output values
# (not merely "compiles"), so a silent coefficient / metric regression is caught.
#
# Hardening:
#   * `set -euo pipefail`; the build step is mandatory and failing it aborts.
#   * The assembly is always rebuilt (never a stale DLL) unless `--no-build` is
#     given, in which case the DLL must already exist or the run aborts.
#   * Each snippet runs with its cwd inside a throwaway temp dir, so file-writing
#     chapters (e.g. ch17 -> circuit.qasm/.qs/.json) never litter the repo. The
#     temp dir is removed on exit, and the repo is verified clean afterwards.
#
# Usage:
#   ./scripts/check-doc-samples.sh              # build, then check all docs
#   ./scripts/check-doc-samples.sh --no-build   # require an existing built assembly
#   ./scripts/check-doc-samples.sh --selftest   # verify the harness fails on a bad snippet
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

CONFIG="Debug"
DLL="src/Encodings/bin/${CONFIG}/net10.0/Encodings.dll"
MODE="run"
BUILD=1

for arg in "$@"; do
    case "$arg" in
        --no-build) BUILD=0 ;;
        --selftest) MODE="selftest" ;;
        *) echo "unknown argument: $arg" >&2; exit 2 ;;
    esac
done

if [[ "$BUILD" -eq 1 ]]; then
    echo "Building Encodings ($CONFIG)…"
    if ! dotnet build src/Encodings/Encodings.fsproj -c "$CONFIG" -v quiet >/dev/null; then
        echo "ERROR: build failed — aborting doc-sample check." >&2
        exit 1
    fi
fi

if [[ ! -f "$DLL" ]]; then
    echo "ERROR: $DLL not found (build first, or drop --no-build)." >&2
    exit 1
fi
DLL_ABS="$REPO_ROOT/$DLL"

TMP="$(mktemp -d)"
WORK="$TMP/work"        # per-run cwd so file-writing snippets stay out of the repo
mkdir -p "$WORK"
trap 'rm -rf "$TMP"' EXIT

# Run one document's concatenated fsharp blocks; echo stdout, return fsi's status.
run_doc () {
    local md="$1"
    awk '/^```fsharp/{f=1;next} /^```/{if(f){f=0}} f' "$md" > "$TMP/block.fsx"
    [[ -s "$TMP/block.fsx" ]] || return 200   # no fsharp blocks
    { echo "#r \"$DLL_ABS\""; cat "$TMP/block.fsx"; } > "$TMP/run.fsx"
    ( cd "$WORK" && dotnet fsi "$TMP/run.fsx" ) 2>&1
}

# Exact stdout assertions (grep -E patterns) per document.
exec_asserts () {
    case "$1" in
      "docs/guides/cookbook/09-trees.md")
        printf '%s\n' \
          'Jordan-Wigner .*Terms: 15  MaxWt: 4  AvgWt: 2\.13' \
          'Bravyi-Kitaev .*Terms: 15  MaxWt: 4  AvgWt: 2\.40' \
          'Balanced Ternary .*Terms: 15  MaxWt: 4  AvgWt: 2\.40' \
          'Vlasov Tree .*Terms: 15  MaxWt: 4  AvgWt: 2\.40' ;;
      "docs/guides/cookbook/10-building-hamiltonian.md")
        printf '%s\n' \
          'H. Hamiltonian: 15 Pauli terms' \
          '-0\.8122  IIII' \
          '-0\.0453  XXYY' \
          '\+0\.0453  XYYX' ;;
      "docs/guides/cookbook/13-grand-finale.md")
        printf '%s\n' \
          'Terms: 15    Avg Pauli weight: 2\.13' \
          'Terms: 15    Avg Pauli weight: 2\.40' \
          '-0\.8122  IIII' ;;
      *) : ;;
    esac
}

# Exact source-table assertions (grep -F strings) per document, for prose values
# not produced by executed code (e.g. the ch13 CNOT column).
file_asserts () {
    case "$1" in
      "docs/guides/cookbook/13-grand-finale.md")
        printf '%s\n' \
          '| Jordan–Wigner | 15 | 4 | 2.13 | 36 |' \
          '| Bravyi–Kitaev | 15 | 4 | 2.40 | 44 |' \
          '| Ternary Tree | 15 | 4 | 2.40 | 44 |' ;;
      *) : ;;
    esac
}

# ── Self-test: prove the harness actually fails on a broken snippet ─────
if [[ "$MODE" == "selftest" ]]; then
    echo "Self-test: a deliberately broken snippet must be reported as FAIL."
    printf '```fsharp\nlet x : int = "not an int"\n```\n' > "$TMP/bad.md"
    if out="$(run_doc "$TMP/bad.md")"; then
        echo "SELFTEST FAILED: broken snippet was accepted." >&2
        exit 1
    fi
    echo "Self-test on execution failure: OK (broken snippet rejected)."
    # And an exec-assertion mismatch must also be caught.
    printf '```fsharp\nprintfn "Terms: 7"\n```\n' > "$TMP/wrong.md"
    wrong_out="$(run_doc "$TMP/wrong.md")"
    if grep -Eq 'Terms: 15' <<<"$wrong_out"; then
        echo "SELFTEST FAILED: wrong output unexpectedly matched." >&2
        exit 1
    fi
    echo "Self-test on assertion mismatch: OK (wrong output would not satisfy 'Terms: 15')."
    echo "Self-test passed."
    exit 0
fi

# ── Main run ───────────────────────────────────────────────────────────
DOCS=("README.md")
for md in docs/guides/cookbook/[0-9]*.md; do DOCS+=("$md"); done

fail=0
checked=0
for md in "${DOCS[@]}"; do
    [[ -f "$md" ]] || continue
    set +e
    out="$(run_doc "$md")"; status=$?
    set -e
    if [[ $status -eq 200 ]]; then
        echo "skip  $md (no fsharp blocks)"
        continue
    fi
    checked=$((checked + 1))
    if [[ $status -ne 0 ]]; then
        echo "FAIL  $md (execution error)"
        grep -E "error|Exception|Unhandled" <<<"$out" | head -5 | sed 's/^/        /'
        fail=1
        continue
    fi
    # Exact stdout assertions.
    doc_ok=1
    asserted=0
    while IFS= read -r pat; do
        [[ -z "$pat" ]] && continue
        asserted=$((asserted + 1))
        if ! grep -Eq -e "$pat" <<<"$out"; then
            echo "FAIL  $md (stdout missing: /$pat/)"
            doc_ok=0
        fi
    done < <(exec_asserts "$md")
    # Exact source-table assertions.
    while IFS= read -r lit; do
        [[ -z "$lit" ]] && continue
        asserted=$((asserted + 1))
        if ! grep -Fq -e "$lit" "$md"; then
            echo "FAIL  $md (source missing: '$lit')"
            doc_ok=0
        fi
    done < <(file_asserts "$md")
    if [[ $doc_ok -eq 1 ]]; then
        if [[ $asserted -gt 0 ]]; then
            echo "ok    $md  [$asserted exact assertion(s)]"
        else
            echo "ok    $md"
        fi
    else
        fail=1
    fi
done

# ── No stray generated artifacts left in the repo ──────────────────────
strays="$(git -C "$REPO_ROOT" status --porcelain --untracked-files=all \
          | grep -E 'circuit\.(qasm|qs|json)$' || true)"
if [[ -n "$strays" ]]; then
    echo "FAIL  generated circuit artifacts were left in the repo:" >&2
    echo "$strays" | sed 's/^/        /' >&2
    fail=1
fi

echo "─────────────────────────────────────────────"
if [[ $fail -eq 0 ]]; then
    echo "All $checked documents executed and asserted successfully; repo clean."
else
    echo "One or more documents failed."
fi
exit $fail
