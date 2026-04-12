# ZeroMix Makefile
# Professional Build Script for Windows
# Prerequisites: Dotnet SDK, Inno Setup 6

# -----------------------------------------------------------------------------
# CONFIGURATION
# -----------------------------------------------------------------------------

# Auto-detect version dari CHANGELOG.md (ambil versi pertama yang ada)
VERSION ?= $(shell powershell -NoProfile -Command \
  "(Select-String -Path 'Docs/CHANGELOG.md' -Pattern '\#\# \[v?(\d+\.\d+\.\d+)\]' | Select-Object -First 1).Matches.Groups[1].Value")

# Fallback jika powershell tidak tersedia
ifeq ($(VERSION),)
  VERSION := 5.5.0
endif

# Directories
PROJECT_ROOT := .
PUBLISH_DIR  := $(PROJECT_ROOT)\publish
EXE_DIR      := $(PROJECT_ROOT)\Exe

# Files
PROJECT_FILE := $(PROJECT_ROOT)\ZeroMix.csproj
SETUP_SCRIPT := $(EXE_DIR)\Setup.iss

# Tools
DOTNET       := dotnet
ISCC         := iscc

# Targets
.PHONY: all clean build installer version help

# Default target
all: clean build installer
	@echo "========================================="
	@echo " BUILD SUCCESSFUL: ZeroMix v$(VERSION)"
	@echo "========================================="

help:
	@echo "ZeroMix Build Makefile"
	@echo "-----------------------------------------"
	@echo "  make           : Full build pipeline"
	@echo "  make build     : Publish application only"
	@echo "  make installer : Compile Inno Setup installer"
	@echo "  make clean     : Remove build artifacts"
	@echo "  make version   : Show detected version"

version:
	@echo "Detected version: $(VERSION)"

# -----------------------------------------------------------------------------
# TASKS
# -----------------------------------------------------------------------------

clean:
	@echo "[CLEAN] Removing previous build artifacts..."
	@if exist "$(PUBLISH_DIR)" rmdir /S /Q "$(PUBLISH_DIR)"
	@if exist "$(EXE_DIR)\ZeroMix-Setup-*.exe" del /Q "$(EXE_DIR)\ZeroMix-Setup-*.exe"

build:
	@echo "[BUILD] Publishing ZeroMix v$(VERSION)..."
	@$(DOTNET) publish "$(PROJECT_FILE)" -c Release -r win-x64 \
	  -p:Version=$(VERSION) \
	  -p:PublishReadyToRun=true \
	  --self-contained \
	  -o "$(PUBLISH_DIR)\win-x64"

installer:
	@echo "[INSTALLER] Compiling Inno Setup v$(VERSION)..."
	@$(ISCC) "/DAppVersion=$(VERSION)" "$(SETUP_SCRIPT)"
