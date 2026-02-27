.PHONY: all clean review software explainer

all: review software explainer

# The main arXiv paper — tutorial review with original contributions.
review:
	cd paper-review && latexmk -pdf -interaction=nonstopmode -f paper.tex
	cp paper-review/paper.pdf paper-review/fockmap-review.pdf

# JOSS paper uses Markdown + pandoc via the Open Journals GitHub Action.
# Build is handled by CI (draft-paper.yml); no local build needed.
software:
	@echo "JOSS paper builds via GitHub Actions (draft-paper.yml)."
	@echo "Push to .project/research/paper-software/ to trigger a build."

# Private explainer — for teaching, presentations, adviser meetings.
explainer:
	cd paper-explainer && latexmk -pdf -interaction=nonstopmode explainer.tex
	cp paper-explainer/explainer.pdf paper-explainer/fockmap-explainer.pdf

clean:
	cd paper-review && latexmk -C && rm -f fockmap-review.pdf
	rm -f paper-software/paper.pdf paper-software/fockmap-joss.pdf
	cd paper-explainer && latexmk -C && rm -f fockmap-explainer.pdf

watch-review:
	cd paper-review && latexmk -pdf -pvc paper.tex

watch-explainer:
	cd paper-explainer && latexmk -pdf -pvc explainer.tex
