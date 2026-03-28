# ZeroMix Makefile
# Professional Build Script for Windows
# Prerequisites: Dotnet SDK, Inno Setup 6

# -----------------------------------------------------------------------------
# CONFIGURATION
# -----------------------------------------------------------------------------

# Versioning
VERSION ?= 5.0.0

# Directories
PROJECT_ROOT := .
BUILD_DIR    := $(PROJECT_ROOT)\build_output
PUBLISH_DIR  := $(PROJECT_ROOT)\publish
EXE_DIR      := $(PROJECT_ROOT)\Exe
TOOLS_DIR    := $(EXE_DIR)\bin

# Files
PROJECT_FILE := $(PROJECT_ROOT)\ZeroMix.csproj
SETUP_SCRIPT := $(EXE_DIR)\Setup.iss

# Tools
DOTNET       := dotnet
ISCC         := iscc

# Targets
.PHONY: all clean build installer help

# Default target: Build EXE
all: clean build installer
	@echo "========================================="
	@echo " BUILD SUCCESSFUL: ZeroMix v$(VERSION)"
	@echo " Format Available: Standard EXE"
	@echo "========================================="

help:
	@echo "ZeroMix Management Makefile"
	@echo "-----------------------------------------"
	@echo "Targets:"
	@echo "  build      : Restore and publish the WPF application"
	@echo "  installer  : Compile the Inno Setup installer (EXE)"
	@echo "  clean      : Remove build artifacts"
	@echo "  all        : Run full pipeline for EXE"

# -----------------------------------------------------------------------------
# TASKS
# -----------------------------------------------------------------------------

clean:
	@echo "[CLEAN] Removing previous build artifacts..."
	@if exist "$(PUBLISH_DIR)" rmdir /S /Q "$(PUBLISH_DIR)"
	@if exist "$(BUILD_DIR)" rmdir /S /Q "$(BUILD_DIR)"
	@if exist "$(EXE_DIR)\ZeroMix-Setup-*.exe" del /Q "$(EXE_DIR)\ZeroMix-Setup-*.exe"

build:
	@echo "[BUILD] Publishing Application v$(VERSION)..."
	@$(DOTNET) publish "$(PROJECT_FILE)" -c Release -r win-x64 -p:PublishReadyToRun=true --self-contained -o "$(PUBLISH_DIR)\win-x64"

installer:
	@echo "[INSTALLER] Compiling Inno Setup Script..."
	@$(ISCC) "/DAppVersion=$(VERSION)" "$(EXE_DIR)\Setup.iss"
