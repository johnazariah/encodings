.PHONY: all clean tutorial software

all: tutorial software

tutorial:
	cd paper-tutorial && latexmk -pdf -interaction=nonstopmode paper.tex
	cp paper-tutorial/paper.pdf paper-tutorial/fockmap-tutorial.pdf

software:
	cd paper-software && latexmk -pdf -interaction=nonstopmode paper.tex
	cp paper-software/paper.pdf paper-software/fockmap-joss.pdf

clean:
	cd paper-tutorial && latexmk -C && rm -f fockmap-tutorial.pdf
	cd paper-software && latexmk -C && rm -f fockmap-joss.pdf

watch-tutorial:
	cd paper-tutorial && latexmk -pdf -pvc paper.tex

watch-software:
	cd paper-software && latexmk -pdf -pvc paper.tex
