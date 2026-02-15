.PHONY: all clean watch

MAIN  = paper
TEX   = $(MAIN).tex
PDF   = $(MAIN).pdf
BIB   = $(MAIN).bib
DEPS  = $(wildcard drafts/*.tex) $(wildcard figures/*.tex) $(BIB)

all: $(PDF)

$(PDF): $(TEX) $(DEPS)
	latexmk -pdf -f -interaction=nonstopmode $(TEX)

watch:
	latexmk -pdf -pvc $(TEX)

clean:
	latexmk -C
	rm -f *.bbl *.run.xml *.synctex.gz

wordcount:
	@texcount -inc -brief $(TEX) 2>/dev/null || echo "(install texcount for word counts)"
