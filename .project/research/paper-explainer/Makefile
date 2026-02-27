.PHONY: all clean watch

MAIN  = explainer
TEX   = $(MAIN).tex
PDF   = $(MAIN).pdf
BIB   = ../shared/bibliography/references.bib

all: $(PDF)

$(PDF): $(TEX) $(BIB) $(wildcard figures/*.tex)
	latexmk -pdf -f -interaction=nonstopmode $(TEX)

watch:
	latexmk -pdf -pvc $(TEX)

clean:
	latexmk -C
	rm -f *.bbl *.run.xml *.synctex.gz
