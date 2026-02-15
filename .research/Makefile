.PHONY: all clean tutorial software cookbook emergence

all: tutorial software cookbook #emergence

tutorial:
	cd paper-tutorial && latexmk -pdf -interaction=nonstopmode paper.tex
	cp paper-tutorial/paper.pdf paper-tutorial/fockmap-tutorial.pdf

# JOSS paper uses Markdown + pandoc via the Open Journals Docker image.
# Falls back to a simple note if Docker is unavailable.
software:
	@if command -v docker >/dev/null 2>&1; then \
	  docker run --rm \
	    --volume $(CURDIR)/paper-software:/data \
	    --user $$(id -u):$$(id -g) \
	    --env JOURNAL=joss \
	    openjournals/inara && \
	  cp paper-software/paper.pdf paper-software/fockmap-joss.pdf; \
	else \
	  echo "WARNING: Docker not available. Skipping JOSS PDF build."; \
	  echo "Install Docker or use the JOSS GitHub Action to build the PDF."; \
	fi

cookbook:
	cd paper-cookbook && latexmk -pdf -interaction=nonstopmode paper.tex
	cp paper-cookbook/paper.pdf paper-cookbook/fockmap-cookbook.pdf

emergence:
	cd paper-emergence && latexmk -pdf -interaction=nonstopmode paper.tex
	cp paper-emergence/paper.pdf paper-emergence/fockmap-emergence.pdf

clean:
	cd paper-tutorial && latexmk -C && rm -f fockmap-tutorial.pdf
	rm -f paper-software/paper.pdf paper-software/fockmap-joss.pdf
	cd paper-cookbook && latexmk -C && rm -f fockmap-cookbook.pdf
	cd paper-emergence && latexmk -C && rm -f fockmap-emergence.pdf

watch-tutorial:
	cd paper-tutorial && latexmk -pdf -pvc paper.tex

watch-cookbook:
	cd paper-cookbook && latexmk -pdf -pvc paper.tex

watch-emergence:
	cd paper-emergence && latexmk -pdf -pvc paper.tex
