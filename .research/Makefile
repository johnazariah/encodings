.PHONY: all clean tutorial software cookbook emergence

all: tutorial software cookbook emergence

tutorial:
	cd paper-tutorial && latexmk -pdf -interaction=nonstopmode paper.tex
	cp paper-tutorial/paper.pdf paper-tutorial/fockmap-tutorial.pdf

software:
	cd paper-software && latexmk -pdf -interaction=nonstopmode paper.tex
	cp paper-software/paper.pdf paper-software/fockmap-joss.pdf

cookbook:
	cd paper-cookbook && latexmk -pdf -interaction=nonstopmode paper.tex
	cp paper-cookbook/paper.pdf paper-cookbook/fockmap-cookbook.pdf

emergence:
	cd paper-emergence && latexmk -pdf -interaction=nonstopmode paper.tex
	cp paper-emergence/paper.pdf paper-emergence/fockmap-emergence.pdf

clean:
	cd paper-tutorial && latexmk -C && rm -f fockmap-tutorial.pdf
	cd paper-software && latexmk -C && rm -f fockmap-joss.pdf
	cd paper-cookbook && latexmk -C && rm -f fockmap-cookbook.pdf
	cd paper-emergence && latexmk -C && rm -f fockmap-emergence.pdf

watch-tutorial:
	cd paper-tutorial && latexmk -pdf -pvc paper.tex

watch-software:
	cd paper-software && latexmk -pdf -pvc paper.tex

watch-cookbook:
	cd paper-cookbook && latexmk -pdf -pvc paper.tex

watch-emergence:
	cd paper-emergence && latexmk -pdf -pvc paper.tex
