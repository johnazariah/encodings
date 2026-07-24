#!/usr/bin/env bash
# Semantic documentation check.
#
# Extracts every ```fsharp block from the README and each cookbook chapter,
# concatenates the blocks per document, references the freshly-built Encodings
# assembly, and executes the result with `dotnet fsi`. A compile OR runtime error
# fails the run. Selected documents additionally assert EXACT expected output values
# — including exact occurrence multiplicities — so a silent coefficient / metric
# regression is caught, not merely "it compiles".
#
# Hardening:
#   * `set -euo pipefail`; the build step is mandatory and failing it aborts.
#   * The assembly is always rebuilt (never a stale DLL) unless `--no-build` is
#     given, in which case the DLL must already exist or the run aborts.
#   * Each snippet runs with its cwd inside a throwaway temp dir, so file-writing
#     chapters (e.g. ch17 -> circuit.qasm/.qs/.json) never litter the repo. The
#     temp dir is removed on exit, and the repo is verified clean afterwards.
#   * A SINGLE production evaluator (`evaluate_doc`) applies all assertions and is
#     used by BOTH the main loop and `--selftest`. Documents declared in
#     REQUIRE_ASSERTS must contribute at least one assertion, so the assertion path
#     can never silently no-op.
#
# Usage:
#   ./scripts/check-doc-samples.sh              # build, then check all docs
#   ./scripts/check-doc-samples.sh --no-build   # require an existing built assembly
#   ./scripts/check-doc-samples.sh --selftest   # verify the harness fails on bad input
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

# ── Per-document assertion tables ──────────────────────────────────────
# exec_asserts: extended-regex patterns that must appear (≥1) in stdout.
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
          '-0\.8122  IIII' ;;
      *) : ;;
    esac
}

# file_asserts: fixed strings that must appear (≥1) in the .md source (for prose
# values not produced by executed code, e.g. the ch13 CNOT column).
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

# count_out: "N::regex" — exactly N stdout lines must match (occurrence multiplicity).
count_out () {
    case "$1" in
      "docs/guides/cookbook/13-grand-finale.md")
        printf '%s\n' \
          '3::Terms: 15' \
          '1::Avg Pauli weight: 2\.13' \
          '2::Avg Pauli weight: 2\.40' ;;
      *) : ;;
    esac
}

# count_file: "N::fixed" — exactly N .md source lines must contain the string.
count_file () {
    case "$1" in
      "docs/guides/cookbook/13-grand-finale.md")
        printf '%s\n' \
          '1::| 36 |' \
          '2::| 44 |' ;;
      *) : ;;
    esac
}

# bind_asserts: "encoder-name||exact-metric-substring" — binds each encoder HEADING
# ("═══ <name> ═══") to the metric line immediately following it, so that a
# count-preserving swap of two encoders' metrics (which the multiplicity checks alone
# would accept) is rejected.
bind_asserts () {
    case "$1" in
      "docs/guides/cookbook/13-grand-finale.md")
        printf '%s\n' \
          'Jordan-Wigner||Terms: 15    Avg Pauli weight: 2.13' \
          'Bravyi-Kitaev||Terms: 15    Avg Pauli weight: 2.40' \
          'Ternary Tree||Terms: 15    Avg Pauli weight: 2.40' ;;
      *) : ;;
    esac
}

# Extract the metric line bound to an encoder heading: the first line containing
# "Terms:" that follows the "═══ <name> ═══" header in <stdout>.
bound_metric () {
    awk -v name="$2" '
        index($0, "═══ " name " ═══") { f = 1; next }
        f && /Terms:/ { print; f = 0 }
    ' <<<"$1" | head -1
}

# Documents that MUST contribute at least one assertion (guards against a silent
# no-op if an assertion table is ever emptied by mistake).
is_required () {
    case "$1" in
      "docs/guides/cookbook/09-trees.md"|\
      "docs/guides/cookbook/10-building-hamiltonian.md"|\
      "docs/guides/cookbook/13-grand-finale.md") return 0 ;;
      *) return 1 ;;
    esac
}

# ── Single production evaluator, used by the main loop AND the self-test ─
# Args: <md> <stdout>. Sets global ASSERTED to the number of assertions applied.
# Emits FAIL lines. Returns 0 iff every assertion for the document holds AND (when
# the document is required) at least one assertion was applied.
ASSERTED=0
evaluate_doc () {
    local md="$1" out="$2"
    local ok=1
    ASSERTED=0
    local pat lit spec n actual
    while IFS= read -r pat; do
        [[ -z "$pat" ]] && continue
        ASSERTED=$((ASSERTED + 1))
        if ! grep -Eq -e "$pat" <<<"$out"; then
            echo "FAIL  $md (stdout missing: /$pat/)"; ok=0
        fi
    done < <(exec_asserts "$md")
    while IFS= read -r lit; do
        [[ -z "$lit" ]] && continue
        ASSERTED=$((ASSERTED + 1))
        if ! grep -Fq -e "$lit" "$md"; then
            echo "FAIL  $md (source missing: '$lit')"; ok=0
        fi
    done < <(file_asserts "$md")
    while IFS= read -r spec; do
        [[ -z "$spec" ]] && continue
        ASSERTED=$((ASSERTED + 1))
        n="${spec%%::*}"; pat="${spec#*::}"
        actual="$(grep -Ec -e "$pat" <<<"$out" || true)"
        if [[ "$actual" != "$n" ]]; then
            echo "FAIL  $md (expected $n stdout line(s) matching /$pat/, got $actual)"; ok=0
        fi
    done < <(count_out "$md")
    while IFS= read -r spec; do
        [[ -z "$spec" ]] && continue
        ASSERTED=$((ASSERTED + 1))
        n="${spec%%::*}"; lit="${spec#*::}"
        actual="$(grep -Fc -e "$lit" "$md" || true)"
        if [[ "$actual" != "$n" ]]; then
            echo "FAIL  $md (expected $n source line(s) containing '$lit', got $actual)"; ok=0
        fi
    done < <(count_file "$md")
    # Heading→metric bindings: each encoder's metric line must sit under its own
    # header (rejects a count-preserving swap that the multiplicity checks accept).
    local name want got
    while IFS= read -r spec; do
        [[ -z "$spec" ]] && continue
        ASSERTED=$((ASSERTED + 1))
        name="${spec%%||*}"; want="${spec#*||}"
        got="$(bound_metric "$out" "$name")"
        if [[ "$got" != *"$want"* ]]; then
            echo "FAIL  $md (binding '$name' expected metric '$want', got '$got')"; ok=0
        fi
    done < <(bind_asserts "$md")
    # No-op guard: a required document must have contributed assertions.
    if is_required "$md" && [[ "$ASSERTED" -eq 0 ]]; then
        echo "FAIL  $md (required document contributed no assertions — evaluator silently no-op)"; ok=0
    fi
    [[ $ok -eq 1 ]]
}

# ── Self-test: the production evaluator must reject bad input ───────────
if [[ "$MODE" == "selftest" ]]; then
    rc=0

    echo "Self-test 1: a broken snippet must fail execution."
    printf '```fsharp\nlet x : int = "not an int"\n```\n' > "$TMP/bad.md"
    if out="$(run_doc "$TMP/bad.md")"; then
        echo "  SELFTEST FAILED: broken snippet was accepted." >&2; rc=1
    else
        echo "  OK: broken snippet rejected."
    fi

    echo "Self-test 2: wrong output must fail the SAME production evaluator."
    # Feed ch13's real assertion table against deliberately wrong stdout.
    wrong_out="$(printf 'Terms: 7\n  Terms: 7    Avg Pauli weight: 9.99\n+0.0000  IIII\n')"
    if evaluate_doc "docs/guides/cookbook/13-grand-finale.md" "$wrong_out" >/dev/null 2>&1; then
        echo "  SELFTEST FAILED: evaluate_doc accepted wrong output." >&2; rc=1
    else
        echo "  OK: evaluate_doc rejected wrong output (production evaluator)."
    fi

    echo "Self-test 3: required documents actually contribute assertions (no silent no-op)."
    for md in docs/guides/cookbook/09-trees.md \
              docs/guides/cookbook/10-building-hamiltonian.md \
              docs/guides/cookbook/13-grand-finale.md; do
        # Evaluate against empty stdout: assertions will FAIL, but ASSERTED must be > 0.
        evaluate_doc "$md" "" >/dev/null 2>&1 || true
        if [[ "$ASSERTED" -eq 0 ]]; then
            echo "  SELFTEST FAILED: $md contributed 0 assertions." >&2; rc=1
        else
            echo "  OK: $md contributes $ASSERTED assertion(s)."
        fi
    done

    echo "Self-test 5: a COUNT-PRESERVING encoder swap must be rejected by the binding."
    # JW and BK metrics are swapped. The multiset is unchanged — one 2.13, two 2.40,
    # three 'Terms: 15' — so the multiplicity checks alone WOULD accept it; only the
    # heading→metric binding catches the mismatch.
    swapped="$(printf '%s\n' \
        '═══ Jordan-Wigner ═══' \
        '  Terms: 15    Avg Pauli weight: 2.40' \
        '    -0.8122  IIII' \
        '═══ Bravyi-Kitaev ═══' \
        '  Terms: 15    Avg Pauli weight: 2.13' \
        '    -0.8122  IIII' \
        '═══ Ternary Tree ═══' \
        '  Terms: 15    Avg Pauli weight: 2.40' \
        '    -0.8122  IIII')"
    # Prove the multiset is preserved (the multiplicity assertions would pass).
    c213="$(grep -Ec 'Avg Pauli weight: 2\.13' <<<"$swapped" || true)"
    c240="$(grep -Ec 'Avg Pauli weight: 2\.40' <<<"$swapped" || true)"
    c15="$(grep -Ec 'Terms: 15' <<<"$swapped" || true)"
    if [[ "$c213" == "1" && "$c240" == "2" && "$c15" == "3" ]]; then
        echo "  (multiset preserved: one 2.13, two 2.40, three Terms:15 — counts alone would pass)"
    else
        echo "  SELFTEST FAILED: swapped output was not count-preserving ($c213/$c240/$c15)." >&2; rc=1
    fi
    # The SAME production evaluator must nonetheless reject it (binding mismatch).
    if evaluate_doc "docs/guides/cookbook/13-grand-finale.md" "$swapped" >/dev/null 2>&1; then
        echo "  SELFTEST FAILED: evaluate_doc accepted a count-preserving swap." >&2; rc=1
    else
        echo "  OK: evaluate_doc rejected the swap (heading→metric binding)."
    fi

    echo "Self-test 4: the no-op guard fires for a required doc with an empty table."
    # A required path whose tables are all empty must be flagged by evaluate_doc.
    exec_asserts () { : ; }; file_asserts () { : ; }
    count_out () { : ; }; count_file () { : ; }; bind_asserts () { : ; }
    if evaluate_doc "docs/guides/cookbook/13-grand-finale.md" "anything" >/dev/null 2>&1; then
        echo "  SELFTEST FAILED: empty-table required doc was accepted." >&2; rc=1
    else
        echo "  OK: empty-table required doc rejected by the no-op guard."
    fi

    if [[ $rc -eq 0 ]]; then echo "Self-test passed."; else echo "Self-test FAILED." >&2; fi
    exit $rc
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
    if evaluate_doc "$md" "$out"; then
        if [[ "$ASSERTED" -gt 0 ]]; then
            echo "ok    $md  [$ASSERTED exact assertion(s)]"
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
