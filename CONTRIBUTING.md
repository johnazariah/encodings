# Contributing to FockMap

Thank you for your interest in contributing to FockMap! This document provides guidelines and instructions for contributing.

## How to Report Bugs

If you find a bug, please report it by [opening a GitHub issue](https://github.com/johnaziz57/FockMap/issues/new). Include:

- A clear, descriptive title
- Steps to reproduce the issue
- Expected behavior vs. actual behavior
- Your environment (OS, .NET version, etc.)
- Any relevant code snippets or error messages

## How to Propose Features

Feature requests are welcome! To propose a new feature:

1. Check existing [issues](https://github.com/johnaziz57/FockMap/issues) to see if it's already proposed
2. Open a new issue with the "feature request" label
3. Describe the feature and its use case
4. Explain why this feature would be useful to others

## Code Style Guide

FockMap follows standard F# conventions:

### Naming Conventions

- **Functions and values**: `camelCase`
  ```fsharp
  let createOperator x = ...
  let termCount = 5
  ```

- **Types, modules, and namespaces**: `PascalCase`
  ```fsharp
  type LadderOperator = ...
  module JordanWigner = ...
  ```

### Documentation

All public APIs must have XML documentation comments using `///`:

```fsharp
/// <summary>
/// Encodes a fermionic operator using the Jordan-Wigner transformation.
/// </summary>
/// <param name="operator">The fermionic ladder operator to encode.</param>
/// <returns>The equivalent Pauli operator representation.</returns>
let encode operator = ...
```

### Formatting

- Use 4-space indentation (no tabs)
- Keep lines under 120 characters when practical
- Use blank lines to separate logical sections
- Prefer pipeline operators (`|>`) for data transformations

## Pull Request Process

1. **Fork the repository** and create a feature branch from `main`
2. **Make your changes** following the code style guide
3. **Add or update tests** for any new functionality
4. **Run tests locally** to ensure everything passes:
   ```bash
   dotnet test
   ```
5. **Update documentation** if you're changing public APIs
6. **Submit a pull request** with:
   - A clear title and description
   - Reference to any related issues
   - Summary of changes made

### PR Review Criteria

- Code follows the style guide
- All tests pass
- New code has appropriate test coverage
- Documentation is updated as needed
- No unnecessary changes to unrelated code

## Running Tests Locally

To run the test suite:

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run a specific test project
dotnet test test/Test.Encodings/Test.Encodings.fsproj
```

## Questions?

If you have questions about contributing, feel free to open an issue for discussion.
