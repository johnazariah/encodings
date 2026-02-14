#!/usr/bin/env python3

from pathlib import Path

PLACEHOLDER_REPLACEMENTS = {
    "{{fsdocs-license-link}}": "https://github.com/johnazariah/encodings/blob/main/LICENSE",
    "{{fsdocs-release-notes-link}}": "https://github.com/johnazariah/encodings/blob/main/CHANGELOG.md",
    "{{fsdocs-repository-link}}": "https://github.com/johnazariah/encodings",
}

ASSET_REPLACEMENTS = {
  '/encodings/img/logo.png': '/encodings/content/img/fockmap-logo.svg',
  '/encodings/img/favicon.ico': '/encodings/content/img/fockmap-icon.svg',
}


def inject_runtime(html_path: Path) -> bool:
    content = html_path.read_text(encoding="utf-8")
    changed = False

    for placeholder, value in PLACEHOLDER_REPLACEMENTS.items():
        if placeholder in content:
            content = content.replace(placeholder, value)
            changed = True

    for original, replacement in ASSET_REPLACEMENTS.items():
      if original in content:
        content = content.replace(original, replacement)
        changed = True

    if changed:
      html_path.write_text(content, encoding="utf-8")

    return changed


def main() -> None:
    docs_output = Path("docs-output")
    if not docs_output.exists():
        raise SystemExit("docs-output directory not found")

    changed = 0
    for html_file in docs_output.rglob("*.html"):
        if inject_runtime(html_file):
            changed += 1

    print(f"Post-processed {changed} HTML files")


if __name__ == "__main__":
    main()
