# FockMap Development Container
# Pre-built image: .NET 10 + 8.0, Python 3.13, uv, ruff, pyright, fsdocs, LaTeX
#
# Multi-arch: builds for linux/amd64 (Windows/Podman) + linux/arm64 (Mac/OrbStack)
#
# Build:
#   docker buildx build --platform linux/amd64,linux/arm64 -t fockmap-dev .devcontainer/
#
# Or just open in VS Code / OrbStack — devcontainer.json points here automatically.

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-noble AS base

# ── Avoid interactive prompts ───────────────────────────────────
ENV DEBIAN_FRONTEND=noninteractive

# ── System packages ─────────────────────────────────────────────
RUN apt-get update && apt-get install -y --no-install-recommends \
        # Python (Noble ships 3.12; uv manages versions if 3.13+ needed)
        python3 python3-venv python3-pip \
        # LaTeX (full for papers targeting AJP, PRA, JOSS)
        texlive-full \
        # Utilities
        git curl ca-certificates sudo locales gpg \
        # Build tools
        build-essential \
    && locale-gen en_US.UTF-8 \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

ENV LANG=en_US.UTF-8

# ── GitHub CLI (gh) ─────────────────────────────────────────────
RUN curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg \
        | gpg --dearmor -o /usr/share/keyrings/githubcli-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" \
        > /etc/apt/sources.list.d/github-cli.list \
    && apt-get update && apt-get install -y gh \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# ── Azure CLI (az) ──────────────────────────────────────────────
RUN curl -sL https://aka.ms/InstallAzureCLIDeb | bash

# ── Azure Developer CLI (azd) ──────────────────────────────────
RUN curl -fsSL https://aka.ms/install-azd.sh | bash

# ── .NET 8.0 SDK (side-by-side for current TargetFramework) ────
COPY --from=mcr.microsoft.com/dotnet/sdk:8.0 /usr/share/dotnet /usr/share/dotnet

# ── Non-root user (vscode, matches devcontainer convention) ─────
# Handle case where GID/UID 1000 may already exist in base image
RUN if ! getent group 1000 >/dev/null; then groupadd --gid 1000 vscode; fi \
    && if ! getent passwd 1000 >/dev/null; then useradd --uid 1000 --gid 1000 -m -s /bin/bash vscode; \
       else usermod -l vscode -d /home/vscode -m $(getent passwd 1000 | cut -d: -f1) 2>/dev/null || true; fi \
    && echo "vscode ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers.d/vscode

USER vscode
ENV HOME=/home/vscode
ENV PATH="${HOME}/.local/bin:${HOME}/.dotnet/tools:${PATH}"

# ── uv (Python package/tool manager) ───────────────────────────
RUN curl -LsSf https://astral.sh/uv/install.sh | sh

# ── Python tools via uv ────────────────────────────────────────
RUN uv tool install ruff \
    && uv tool install pyright

# ── Python virtual environment (activated by default) ──────────
RUN uv venv ${HOME}/.venv
ENV VIRTUAL_ENV=${HOME}/.venv
ENV PATH="${VIRTUAL_ENV}/bin:${PATH}"

# ── .NET global tools ──────────────────────────────────────────
RUN dotnet tool install --global fsdocs-tool \
    && dotnet tool install --global dotnet-repl

# ── Pre-warm NuGet cache with common test packages ─────────────
RUN dotnet new console -n _warmup --no-restore -o /tmp/_warmup \
    && cd /tmp/_warmup \
    && dotnet add package xunit --version 2.7.0 --no-restore \
    && dotnet add package FsCheck.Xunit --version 2.16.6 --no-restore \
    && dotnet restore \
    && rm -rf /tmp/_warmup

WORKDIR /workspace
