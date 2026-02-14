.PHONY: all clean tutorial software

all: tutorial software

tutorial:
	cd paper-tutorial && latexmk -pdf -interaction=nonstopmode paper.tex

software:
	cd paper-software && latexmk -pdf -interaction=nonstopmode paper.tex

clean:
	cd paper-tutorial && latexmk -C
	cd paper-software && latexmk -C

watch-tutorial:
	cd paper-tutorial && latexmk -pdf -pvc paper.tex

watch-software:
	cd paper-software && latexmk -pdf -pvc paper.tex
