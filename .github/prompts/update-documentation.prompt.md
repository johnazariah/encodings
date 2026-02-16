```prompt
# Update Public-Facing Documentation

Systematically review and update all documentation to ensure it accurately reflects the current state of the codebase.

**Goal**: Every public API, feature, and capability should be documented in:
1. XML doc comments (source of truth)
2. Cookbook/user guides (how to use it)
3. API reference via fsdocs (complete reference)
4. README (high-level overview)

---

## Phase 1: Discover Public API

Build a complete picture of what needs to be documented.

1. **Find all public types and functions**:
   ```bash
   # Public types (excluding internal)
   grep -rE "^type " src/Encodings/ --include="*.fs" | head -30

   # Public let bindings
   grep -rE "^let " src/Encodings/ --include="*.fs" | grep -v "private\|internal" | head -30
   ```

2. **Check XML doc comment coverage**:
   ```bash
   # Files with good docs
   grep -rlc "/// <summary>" src/Encodings/ --include="*.fs" | head -20

   # Files lacking docs
   for f in src/Encodings/*.fs; do
       COUNT=$(grep -c "/// <summary>" "$f" 2>/dev/null || echo 0)
       echo "$f: $COUNT doc comments"
   done
   ```

3. **Create inventory** — list every public item that needs documentation:
   - Discriminated unions (C, P, S, IxOp, etc.)
   - Modules and their public functions
   - Type extensions

---

## Phase 2: Audit XML Doc Comments

XML doc comments are the source of truth. They must be **complete** AND **accurate**.

### 2.1 Verify doc comments match implementation

For each public type/function, verify the doc comment is **accurate**:

1. **Read the implementation** alongside the doc comment
2. **Check parameter documentation matches actual parameters**
3. **Verify described behavior matches actual behavior**

### 2.2 Check for stale documentation

- Parameters that were renamed or removed
- Return types that changed
- Behavior that was modified
- New members not yet documented

### 2.3 Doc comment completeness checklist

| Check | Type | Function | Module |
|-------|------|----------|--------|
| `<summary>` | ✓ | ✓ | ✓ |
| `<param>` for all parameters | ✓ | ✓ | - |
| `<returns>` | - | ✓ | - |
| `<example>` (if complex) | ✓ | ✓ | - |
| `<remarks>` with math context | ✓ | ✓ | - |
| DU case documentation | ✓ | - | - |

---

## Phase 3: Audit Cookbook / User Guides

User guides explain *how* and *when* to use features.

### 3.1 List all guide pages

```bash
find docs/guides -name "*.md" -exec echo "=== {} ===" \; -exec head -10 {} \;
```

### 3.2 For each guide, verify coverage

- All encoding functions mentioned and demonstrated
- All type constructors explained
- Code examples are runnable
- Navigation links work (Next/Back)

### 3.3 Verify cookbook chapter index

```bash
cat docs/guides/cookbook/index.md
```

Ensure the chapter list, encoding function table, and type table are up to date.

---

## Phase 4: Audit API Reference (fsdocs)

### 4.1 Build docs and check for warnings

```bash
dotnet build && dotnet fsdocs build --clean --strict 2>&1
```

### 4.2 Verify all public types appear in generated output

```bash
ls -la docs-output/
```

### 4.3 Check for broken links

Spot-check navigation links in the generated HTML output.

---

## Phase 5: Audit README

README is the first thing users see — keep it current but concise.

1. **Verify sections**:
   - [ ] **Features list** — mentions all major capabilities
   - [ ] **Installation** — correct commands
   - [ ] **Quick start** — working example
   - [ ] **Supported encodings** table — complete and accurate
   - [ ] **Links** — point to correct documentation URLs

2. **Test quick start example**:
   ```bash
   # Ensure the example in README compiles
   dotnet fsi examples/H2_Encoding.fsx
   ```

3. **Update if needed** — README should be high-level; details go in guides.

---

## Phase 6: Build and Validate

1. **Build docs with strict mode**:
   ```bash
   dotnet build && dotnet fsdocs build --clean --strict
   ```

2. **Fix any warnings or errors**

3. **Run all tests**:
   ```bash
   dotnet test
   ```

4. **Run all examples**:
   ```bash
   for f in examples/*.fsx; do dotnet fsi "$f" && echo "✓ $f" || echo "✗ $f"; done
   ```

---

## Output: Summary Report

After completing all phases, provide a summary:

```markdown
## Documentation Update Summary

### XML Doc Comments
- Fixed: X types/functions
- Status: ✅ Complete / ⚠️ Issues remain

### Cookbook / User Guides
- Updated: [list of files]
- Added sections for: [list of features]
- Status: ✅ Complete / ⚠️ Issues remain

### API Reference
- fsdocs build: ✅ Clean / ⚠️ Warnings
- Status: ✅ Complete / ⚠️ Issues remain

### README
- Changes: [summary]
- Status: ✅ Current / ⚠️ Needs update

### Validation
- [ ] fsdocs build --strict passes
- [ ] All examples execute
- [ ] All tests pass

### Remaining Issues
- [any issues that need follow-up]
```
```
