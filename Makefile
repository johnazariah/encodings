# ══════════════════════════════════════════════════════════════
# From Molecules to Quantum Circuits — Build System
# ══════════════════════════════════════════════════════════════
#
# Usage:
#   make              Build manuscript.pdf
#   make clean        Remove generated files
#   make word-count   Print word counts per chapter
#   make diagrams     Render mermaid diagrams only (no PDF)
#   make data         Generate H₂ and H₂O data files
#
# Prerequisites:
#   pandoc, xelatex, mmdc (mermaid-cli), python3, pyscf (for data)
#   Playwright chromium: python3 -m playwright install chromium

SHELL := /bin/bash

# ── Directories ──
BOOK_DIR    := book
CODE_DIR    := $(BOOK_DIR)/code
IMG_DIR     := $(BOOK_DIR)/mermaid-images
OUT         := $(BOOK_DIR)/manuscript.pdf

# ── Source files (from Book.txt) ──
CHAPTERS    := $(shell cat $(BOOK_DIR)/Book.txt | sed 's|^|$(BOOK_DIR)/|')

# ── Pandoc settings ──
PANDOC      := pandoc
ENGINE      := xelatex
LUA_FILTER  := $(BOOK_DIR)/mermaid.lua
PREAMBLE    := $(BOOK_DIR)/preamble.tex

PANDOC_OPTS := \
  --pdf-engine=$(ENGINE) \
  --lua-filter=$(LUA_FILTER) \
  -H $(PREAMBLE) \
  -V geometry:margin=1in \
  -V fontsize=11pt \
  -V classoption=oneside \
  -V mainfont="Latin Modern Roman" \
  -V sansfont="Latin Modern Sans" \
  -V monofont="Latin Modern Mono" \
  -V mathfont="Latin Modern Math" \
  -V title="From Molecules to Quantum Circuits" \
  -V subtitle="A Practical Guide to Fermion-to-Qubit Encodings" \
  -V author="John S Azariah" \
  -V date="March 2026" \
  --toc \
  --toc-depth=2 \
  --highlight-style=tango \
  --top-level-division=chapter \
  -V colorlinks=true \
  -V linkcolor=blue \
  -V urlcolor=blue

# ══════════════════════════════════════════════════════════════
#  Targets
# ══════════════════════════════════════════════════════════════

.PHONY: all clean word-count diagrams data

all: $(OUT)

$(OUT): $(CHAPTERS) $(LUA_FILTER) $(PREAMBLE) $(BOOK_DIR)/Book.txt
	@echo "Building manuscript..."
	@rm -rf $(IMG_DIR)
	$(PANDOC) $(CHAPTERS) -o $(OUT) $(PANDOC_OPTS)
	@echo "Done: $$(python3 -c "import pymupdf; d=pymupdf.open('$(OUT)'); print(f'{d.page_count} pages'); d.close()" 2>/dev/null || echo '(install pymupdf for page count)')"
	@ls -lh $(OUT)

clean:
	rm -rf $(IMG_DIR) $(OUT)

word-count:
	@echo "Chapter word counts:"
	@for f in $(CHAPTERS); do \
	  printf "  %-40s %5d\n" "$$(basename $$f)" "$$(wc -w < $$f)"; \
	done
	@echo "  ────────────────────────────────────────────────"
	@printf "  %-40s %5d\n" "TOTAL" "$$(cat $(CHAPTERS) | wc -w)"

diagrams:
	@echo "Rendering mermaid diagrams..."
	@rm -rf $(IMG_DIR) && mkdir -p $(IMG_DIR)
	@$(PANDOC) $(CHAPTERS) -t native --lua-filter=$(LUA_FILTER) > /dev/null 2>&1
	@echo "Rendered $$(ls $(IMG_DIR)/*.png 2>/dev/null | wc -l) diagrams"

# ── Data generation (requires pyscf) ──
data: $(CODE_DIR)/h2_dissociation.csv $(CODE_DIR)/h2o_bond_angle_coarse.csv

$(CODE_DIR)/h2_dissociation.csv: $(CODE_DIR)/ch17-dissociation-scan.py
	python3 $<

$(CODE_DIR)/h2o_bond_angle_coarse.csv: $(CODE_DIR)/ch18-bond-angle-scan.py
	python3 $<
