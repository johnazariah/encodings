#!/usr/bin/env python3

from pathlib import Path

RUNTIME_MARKER = "<!-- fockmap-pages-runtime -->"

PLACEHOLDER_REPLACEMENTS = {
    "{{fsdocs-license-link}}": "https://github.com/johnazariah/encodings/blob/main/LICENSE",
    "{{fsdocs-release-notes-link}}": "https://github.com/johnazariah/encodings/blob/main/CHANGELOG.md",
    "{{fsdocs-repository-link}}": "https://github.com/johnazariah/encodings",
}

RUNTIME_SNIPPET = """\
    <!-- fockmap-pages-runtime -->
    <script>
      window.MathJax = {
        tex: {
          inlineMath: [['$', '$'], ['\\(', '\\)']],
          displayMath: [['$$', '$$'], ['\\[', '\\]']],
          processEscapes: true
        }
      };
    </script>
    <script defer src="https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js"></script>
    <script defer src="https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js"></script>
    <script>
      document.addEventListener('DOMContentLoaded', function () {
        if (window.mermaid) {
          mermaid.initialize({ startOnLoad: false, securityLevel: 'loose' });
          var blocks = document.querySelectorAll('pre > code.language-mermaid, pre > code[lang="mermaid"], table.pre code[lang="mermaid"]');
          blocks.forEach(function (code, index) {
            var containerTarget = code.closest('table.pre') || code.parentElement;
            if (!containerTarget) return;
            var source = code.textContent || '';
            var container = document.createElement('div');
            container.className = 'mermaid';
            container.id = 'mermaid-diagram-' + index;
            container.textContent = source;
            containerTarget.replaceWith(container);
          });
          mermaid.run();
        }
      });
    </script>
"""


def inject_runtime(html_path: Path) -> bool:
    content = html_path.read_text(encoding="utf-8")
    changed = False

    for placeholder, value in PLACEHOLDER_REPLACEMENTS.items():
        if placeholder in content:
            content = content.replace(placeholder, value)
            changed = True

    if RUNTIME_MARKER in content:
        if changed:
            html_path.write_text(content, encoding="utf-8")
        return changed

    head_close = content.lower().find("</head>")
    if head_close == -1:
        if changed:
            html_path.write_text(content, encoding="utf-8")
        return changed

    updated = content[:head_close] + RUNTIME_SNIPPET + "\n" + content[head_close:]
    html_path.write_text(updated, encoding="utf-8")
    return True


def main() -> None:
    docs_output = Path("docs-output")
    if not docs_output.exists():
        raise SystemExit("docs-output directory not found")

    changed = 0
    for html_file in docs_output.rglob("*.html"):
        if inject_runtime(html_file):
            changed += 1

    print(f"Injected runtime into {changed} HTML files")


if __name__ == "__main__":
    main()
