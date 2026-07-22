#!/usr/bin/env bash
# Extract every ```fsharp code block from the README and each cookbook chapter,
# concatenate the blocks per document, reference the built Encodings assembly, and
# execute the result with `dotnet fsi`. Any compile or runtime error fails the run.
#
# This is a *semantic* documentation check: the snippets must not only compile but
# also run to completion. A few documents additionally assert expected printed
# output (e.g. the canonical 15-term H2 Hamiltonian) so a silent coefficient
# regression is caught, not just a syntax error.
#
# Usage:
#   ./scripts/check-doc-samples.sh            # build if needed, then check all docs
#   ./scripts/check-doc-samples.sh --no-build # assume the assembly is already built
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

CONFIG="Debug"
DLL="src/Encodings/bin/${CONFIG}/net10.0/Encodings.dll"

if [[ "${1:-}" != "--no-build" ]]; then
    echo "Building Encodings ($CONFIG)…"
    dotnet build src/Encodings/Encodings.fsproj -c "$CONFIG" -v quiet >/dev/null
fi

if [[ ! -f "$DLL" ]]; then
    echo "ERROR: $DLL not found (build first, or drop --no-build)." >&2
    exit 1
fi
DLL_ABS="$REPO_ROOT/$DLL"

# Documents to check: README first, then every numbered cookbook chapter.
DOCS=("README.md")
for md in docs/guides/cookbook/[0-9]*.md; do DOCS+=("$md"); done

# Optional semantic assertion per document (portable to bash 3.2 — no assoc arrays).
expected_for() {
    case "$1" in
        "docs/guides/cookbook/10-building-hamiltonian.md") echo "15 Pauli terms" ;;
        *) echo "" ;;
    esac
}

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

fail=0
checked=0
for md in "${DOCS[@]}"; do
    [[ -f "$md" ]] || continue
    awk '/^```fsharp/{f=1;next} /^```/{if(f){f=0}} f' "$md" > "$TMP/block.fsx"
    if [[ ! -s "$TMP/block.fsx" ]]; then
        echo "skip  $md (no fsharp blocks)"
        continue
    fi
    checked=$((checked + 1))
    { echo "#r \"$DLL_ABS\""; cat "$TMP/block.fsx"; } > "$TMP/run.fsx"
    if out="$(dotnet fsi "$TMP/run.fsx" 2>&1)"; then
        expected="$(expected_for "$md")"
        if [[ -n "$expected" && "$out" != *"$expected"* ]]; then
            echo "FAIL  $md (missing expected output: '$expected')"
            fail=1
        else
            echo "ok    $md${expected:+  [asserted: $expected]}"
        fi
    else
        echo "FAIL  $md"
        echo "$out" | grep -E "error|Exception|Unhandled" | head -5 | sed 's/^/        /'
        fail=1
    fi
done

echo "─────────────────────────────────────────────"
if [[ $fail -eq 0 ]]; then
    echo "All $checked documents executed successfully."
else
    echo "One or more documents failed."
fi
exit $fail
