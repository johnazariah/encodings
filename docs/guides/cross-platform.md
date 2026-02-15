# Cross-Platform Installation Guide

FockMap runs on .NET 8 (LTS), Microsoft's open-source, cross-platform runtime. This guide covers installation on Windows, macOS, and Linux, plus container-based workflows.

## About the Platform

### .NET 8

.NET 8 is Microsoft's Long-Term Support (LTS) release of the unified .NET platform. Key characteristics:

- **Open source**: MIT licensed, developed on GitHub
- **Cross-platform**: Native support for Windows, macOS, and Linux
- **High performance**: Modern JIT compilation with tiered optimization
- **Unified runtime**: Same codebase runs identically across platforms

### F# Language

FockMap is written in F#, a functional-first language on .NET:

- **MIT licensed** compiler maintained by the F# Software Foundation
- **Member of the .NET Foundation**, ensuring long-term governance
- **Interoperable** with all .NET libraries and tools
- **Strong type inference** catches errors at compile time

## Installation by Platform

### Windows

**Option 1: Visual Studio Installer**

1. Download Visual Studio 2022 (Community is free)
2. In the installer, select the ".NET desktop development" workload
3. Ensure "F# language support" is checked

**Option 2: Standalone SDK**

```powershell
# Download from https://dotnet.microsoft.com/download
# Or use winget:
winget install Microsoft.DotNet.SDK.8
```

Verify installation:
```powershell
dotnet --version
# Should output: 8.0.x
```

### macOS

**Option 1: Homebrew (recommended)**

```bash
brew install dotnet-sdk
```

**Option 2: Official Installer**

1. Download the .pkg installer from https://dotnet.microsoft.com/download
2. Run the installer and follow prompts
3. The SDK installs to `/usr/local/share/dotnet`

**Verification:**
```bash
dotnet --version
# Should output: 8.0.x
```

**Note for Apple Silicon (M1/M2/M3):** The .NET SDK provides native ARM64 builds. Homebrew automatically selects the correct architecture.

### Linux

**Ubuntu/Debian (apt)**

```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

**Fedora/RHEL (dnf)**

```bash
sudo dnf install dotnet-sdk-8.0
```

**Snap (distribution-agnostic)**

```bash
sudo snap install dotnet-sdk --classic --channel=8.0
```

**Verification:**
```bash
dotnet --version
```

## Docker

For reproducible builds and CI/CD, use Microsoft's official .NET SDK image:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app
COPY . .
RUN dotnet build -c Release
```

**Interactive development:**
```bash
docker run -it --rm -v $(pwd):/app -w /app mcr.microsoft.com/dotnet/sdk:8.0 bash
```

**Available image variants:**

| Image | Size | Use Case |
|-------|------|----------|
| `mcr.microsoft.com/dotnet/sdk:8.0` | ~800MB | Full SDK for building |
| `mcr.microsoft.com/dotnet/runtime:8.0` | ~200MB | Running compiled apps |
| `mcr.microsoft.com/dotnet/sdk:8.0-alpine` | ~500MB | Smaller builds, musl libc |

## Building FockMap

Once .NET is installed, clone and build:

```bash
git clone https://github.com/your-org/fockmap.git
cd fockmap

# Restore dependencies and build
dotnet build

# Run tests
dotnet test

# Build release version
dotnet build -c Release
```

## Performance Considerations

.NET 8 uses a tiered JIT (Just-In-Time) compiler that optimizes code progressively:

1. **Tier 0**: Fast initial compilation, minimal optimization
2. **Tier 1**: Recompiles hot paths with full optimizations
3. **On-stack replacement**: Updates running code without restart

For numerically intensive workloads like Hamiltonian construction:

- First execution may be slower as the JIT compiles code
- Subsequent runs benefit from cached native code
- Steady-state performance is comparable to ahead-of-time compiled languages
- SIMD vectorization is automatic for array operations

**Benchmark guidance:**
- Warm up by running the computation once before timing
- Use `BenchmarkDotNet` for rigorous measurements
- Release builds (`-c Release`) enable full optimization

**Memory:**
- .NET's garbage collector is generational and concurrent
- Large array allocations go directly to the large object heap
- For very large Hamiltonians, consider streaming or batching term generation

## IDE Support

Each platform has mature F# development tools:

| Platform | Recommended IDE |
|----------|----------------|
| Windows | Visual Studio 2022, VS Code + Ionide |
| macOS | VS Code + Ionide, Rider |
| Linux | VS Code + Ionide, Rider |

**VS Code + Ionide** provides:
- Syntax highlighting and error checking
- IntelliSense (autocomplete)
- F# Interactive (FSI) integration
- Go to definition, find references

Install Ionide:
```bash
code --install-extension Ionide.Ionide-fsharp
```
