.PHONY: all clean tutorial software cookbook emergence explainer

all: tutorial software cookbook algebraic explainer #emergence

tutorial:
	cd paper-tutorial && latexmk -pdf -interaction=nonstopmode paper.tex
	cp paper-tutorial/paper.pdf paper-tutorial/fockmap-tutorial.pdf

# JOSS paper uses Markdown + pandoc via the Open Journals GitHub Action.
# Build is handled by CI (draft-paper.yml); no local build needed.
software:
	@echo "JOSS paper builds via GitHub Actions (draft-paper.yml)."
	@echo "Push to .project/research/paper-software/ to trigger a build."

cookbook:
	cd paper-cookbook && latexmk -pdf -interaction=nonstopmode paper.tex
	cp paper-cookbook/paper.pdf paper-cookbook/fockmap-cookbook.pdf

algebraic:
	cd paper-algebraic && latexmk -pdf -interaction=nonstopmode paper.tex
	cp paper-algebraic/paper.pdf paper-algebraic/fockmap-algebraic.pdf

explainer:
	cd paper-explainer && latexmk -pdf -interaction=nonstopmode explainer.tex
	cp paper-explainer/explainer.pdf paper-explainer/fockmap-explainer.pdf

emergence:
	cd paper-emergence && latexmk -pdf -interaction=nonstopmode paper.tex
	cp paper-emergence/paper.pdf paper-emergence/fockmap-emergence.pdf

clean:
	cd paper-tutorial && latexmk -C && rm -f fockmap-tutorial.pdf
	rm -f paper-software/paper.pdf paper-software/fockmap-joss.pdf
	cd paper-cookbook && latexmk -C && rm -f fockmap-cookbook.pdf
	cd paper-emergence && latexmk -C && rm -f fockmap-emergence.pdf
	cd paper-algebraic && latexmk -C && rm -f fockmap-algebraic.pdf
	cd paper-explainer && latexmk -C && rm -f fockmap-explainer.pdf

watch-tutorial:
	cd paper-tutorial && latexmk -pdf -pvc paper.tex

watch-cookbook:
	cd paper-cookbook && latexmk -pdf -pvc paper.tex

watch-emergence:
	cd paper-emergence && latexmk -pdf -pvc paper.tex

watch-algebraic:
	cd paper-algebraic && latexmk -pdf -pvc paper.tex
