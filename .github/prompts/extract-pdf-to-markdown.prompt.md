# Extract PDF to Markdown

Extract text content from a PDF file and convert it into a clean, well-structured markdown document.

---

## Step 1: Create Output Directory

Create a directory named after the PDF file (without extension):

```bash
# Get the PDF filename without extension
PDF_NAME=$(basename "<PDF_PATH>" .pdf)

# Create output directory
mkdir -p "<PARENT_DIR>/${PDF_NAME}"
```

**Directory structure:**
```
<PARENT_DIR>/
├── original.pdf
└── original/                    # Directory named after PDF
    └── Title_Author_arXivID.md  # Markdown with descriptive name
```

---

## Step 2: Extract Raw Text

Use `pdftotext` to extract the PDF content:

```bash
# Check if poppler-utils is available
which pdftotext || sudo apt-get install -y poppler-utils

# Extract with layout preservation
pdftotext -layout "<PDF_PATH>" "<OUTPUT_TXT_PATH>"
```

---

## Step 3: Read and Analyze the Content

1. Read the extracted text file
2. Identify the document structure:
   - **Title** — for the output filename
   - **Authors** — for the output filename
   - **arXiv ID / DOI** — include in filename if available
   - Abstract
   - Section headings
   - Equations and formulas
   - Figure captions (note locations for reference)
   - Tables and their captions
   - Bibliography/References

3. Determine the output filename:
   - Format: `Title_Author(s)_arXivID.md`
   - Use underscores, remove special characters
   - Examples:
     - `Computational_Mechanics_Shalizi_Crutchfield_9907176.md`
     - `Calculus_Of_Emergence_Crutchfield_1994.md`

---

## Step 4: Convert to Markdown

Create a new `.md` file with proper formatting:

### Header Template

```markdown
# [Paper Title]

**Authors:** [Author 1], [Author 2], ...
**Affiliation:** [Institution(s)]
**Source:** [Journal/arXiv ID/DOI]
**Date:** [Publication date]

---

## Abstract

[Abstract text]

---
```

### Formatting Guidelines

| Element | Markdown Format |
|---------|-----------------|
| Main title | `# Title` |
| Section headings | `## Section` |
| Subsections | `### Subsection` |
| Inline math | `$equation$` |
| Display math | `$$equation$$` |
| Citations | `[N]` or `[Author Year]` |
| Emphasis | `*italics*` or `**bold**` |
| Lists | `- item` or `1. item` |
| Code/variables | `` `variable` `` |
| Figure refs | `[Figure N: Caption — see original PDF]` |

### Structure the Content

1. **Front matter**: Title, authors, abstract
2. **Body**: Convert sections with proper heading levels
3. **Equations**: Format with LaTeX math notation
4. **Figures**: Note figure locations with captions
   - Use: `[Figure N: Caption — see original PDF]`
5. **Tables**: Recreate as markdown tables where possible
6. **References**: Clean up bibliography at the end
7. **Key concepts table** (optional): Summarize main definitions

---

## Step 5: Save the Markdown File

Save inside the output directory with a descriptive filename:

```bash
# Save to: <PARENT_DIR>/<PDF_NAME>/Title_Author_arXivID.md
#
# Examples:
#   references/9907176v2/Computational_Mechanics_Shalizi_Crutchfield_9907176.md
#   references/crutchfield1994/Calculus_Of_Emergence_Crutchfield_1994.md
```

**Filename format:** `Title_Author(s)_Identifier.md`
- Title: Short form of paper title
- Authors: First author or key authors
- Identifier: arXiv ID, year, or DOI suffix

**Do NOT use:**
- arXiv IDs alone (e.g., `9907176v2.md`)
- Generic names (e.g., `paper.md`)
- Very long titles (abbreviate if needed)

---

## Step 6: Clean Up

Remove intermediate files:

```bash
# Remove extracted text file
rm "<OUTPUT_TXT_PATH>"
```

---

## Quality Checklist

- [ ] **Directory** created with PDF name (e.g., `9907176v2/`)
- [ ] **Filename** follows `Title_Author_ID.md` convention
- [ ] Title and authors correctly formatted
- [ ] Abstract present and readable
- [ ] Section structure matches original
- [ ] Equations use proper LaTeX notation
- [ ] Figure captions noted with references to original PDF
- [ ] Tables recreated or noted
- [ ] References/bibliography included
- [ ] No broken/garbled text from PDF extraction
- [ ] Greek letters and symbols correctly converted
