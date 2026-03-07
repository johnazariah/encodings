#!/usr/bin/env bash
# Build the Leanpub manuscript from book/ sources.
#
# Outputs:
#   book/output/manuscript.zip   — Leanpub upload-ready ZIP
#   book/output/manuscript.pdf   — standalone PDF (via pandoc + LaTeX)
#
# Usage:
#   ./scripts/build-book.sh              # build ZIP only (fast)
#   ./scripts/build-book.sh --pdf        # build ZIP + PDF (needs LaTeX)
#   ./scripts/build-book.sh --clean      # remove output/ first
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BOOK_DIR="$REPO_ROOT/book"
OUTPUT_DIR="$BOOK_DIR/output"
BUILD_PDF=false
PUBLISH=false

for arg in "$@"; do
    case "$arg" in
        --pdf)     BUILD_PDF=true ;;
        --clean)   rm -rf "$OUTPUT_DIR"; echo "Cleaned $OUTPUT_DIR" ;;
        --publish) PUBLISH=true ;;
    esac
done

mkdir -p "$OUTPUT_DIR/manuscript"

# ── Assemble manuscript from Book.txt ───────────────────────────
echo "Assembling manuscript..."
CHAPTER_NUM=0
while IFS= read -r file; do
    # Skip blank lines
    [[ -z "$file" ]] && continue

    SRC="$BOOK_DIR/$file"
    if [[ ! -f "$SRC" ]]; then
        # Try pulling from docs/pipeline/ (once restructured)
        ALT="$REPO_ROOT/docs/pipeline/$file"
        if [[ -f "$ALT" ]]; then
            SRC="$ALT"
        else
            echo "  SKIP: $file (not yet written)"
            continue
        fi
    fi

    cp "$SRC" "$OUTPUT_DIR/manuscript/$file"
    CHAPTER_NUM=$((CHAPTER_NUM + 1))
    echo "  [$CHAPTER_NUM] $file"
done < "$BOOK_DIR/Book.txt"

# Copy Book.txt and Sample.txt into the manuscript dir (Leanpub needs them)
cp "$BOOK_DIR/Book.txt" "$OUTPUT_DIR/manuscript/"
cp "$BOOK_DIR/Sample.txt" "$OUTPUT_DIR/manuscript/"

# ── Build ZIP for Leanpub upload ────────────────────────────────
echo "Building manuscript.zip..."
(cd "$OUTPUT_DIR" && zip -r manuscript.zip manuscript/ -x '*.DS_Store')
echo "  → $OUTPUT_DIR/manuscript.zip"

# ── Optionally build PDF ────────────────────────────────────────
if $BUILD_PDF; then
    echo "Building PDF..."
    CHAPTERS=()
    while IFS= read -r file; do
        [[ -z "$file" ]] && continue
        [[ -f "$OUTPUT_DIR/manuscript/$file" ]] && CHAPTERS+=("$OUTPUT_DIR/manuscript/$file")
    done < "$BOOK_DIR/Book.txt"

    if [[ ${#CHAPTERS[@]} -gt 0 ]]; then
        pandoc "${CHAPTERS[@]}" \
            --from=markdown \
            --to=pdf \
            --pdf-engine=lualatex \
            --toc \
            --number-sections \
            -V geometry:margin=1in \
            -V fontsize=11pt \
            -V documentclass=book \
            -V title="From Molecules to Quantum Circuits" \
            -V subtitle="A Computational Guide to Fermion-to-Qubit Encodings" \
            -V author="John S Azariah" \
            -o "$OUTPUT_DIR/manuscript.pdf"
        echo "  → $OUTPUT_DIR/manuscript.pdf"
    else
        echo "  No chapters available for PDF build."
    fi
fi

echo ""
echo "Done."
echo "  ZIP:  $OUTPUT_DIR/manuscript.zip"
echo ""
echo "To publish:"
echo "  1. Go to https://leanpub.com/author/book/from-molecules-to-quantum-circuits/home"
echo "  2. Click 'Versions' in the left sidebar"
echo "  3. Upload $OUTPUT_DIR/manuscript.zip"
echo "  4. Click 'Create Preview' to build"

# ── Optionally trigger a preview build via API ──────────────────
if $PUBLISH; then
    if [[ -z "${LEANPUB_API_KEY:-}" ]]; then
        echo ""
        echo "ERROR: LEANPUB_API_KEY environment variable is not set."
        echo "Set it with:  export LEANPUB_API_KEY=\"your-key-here\""
        exit 1
    fi

    SLUG="from-molecules-to-quantum-circuits"

    echo ""
    echo "Triggering preview build via API..."
    RESPONSE=$(curl -s -w "\n%{http_code}" -X POST -d "" \
        "https://leanpub.com/$SLUG/preview.json?api_key=$LEANPUB_API_KEY")

    HTTP_CODE=$(echo "$RESPONSE" | tail -1)
    BODY=$(echo "$RESPONSE" | head -n -1)

    if echo "$BODY" | grep -q '"success":true'; then
        echo "  ✓ Preview build triggered. Check https://leanpub.com/$SLUG"
    else
        echo "  ✗ Preview trigger failed (HTTP $HTTP_CODE)"
        echo "  Response: $BODY"
    fi

    echo ""
    echo "NOTE: Manuscript upload via API is not currently supported by Leanpub."
    echo "Upload the ZIP manually, then re-run with --publish to trigger a build."
fi
