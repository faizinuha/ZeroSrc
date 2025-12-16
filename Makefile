# ZeroMix Makefile
# Professional Build Script for Windows
# Prerequisites: Dotnet SDK, Inno Setup 6, osslsigncode

# -----------------------------------------------------------------------------
# CONFIGURATION
# -----------------------------------------------------------------------------

# Versioning
VERSION ?= 2.2.3

# Directories
PROJECT_ROOT := .
BUILD_DIR    := $(PROJECT_ROOT)\build_output
PUBLISH_DIR  := $(PROJECT_ROOT)\publish
EXE_DIR      := $(PROJECT_ROOT)\Exe
TOOLS_DIR    := $(EXE_DIR)\bin

# Files
PROJECT_FILE := $(PROJECT_ROOT)\ZeroMix.csproj
SETUP_SCRIPT := $(EXE_DIR)\Setup.iss
CERT_FILE    := $(EXE_DIR)\ZeroMixCert.pfx

# Credentials (Avoid hardcoding real secrets in production envs)
CERT_PASS    := ZeroMixPass

# Tools
DOTNET       := dotnet
ISCC         := "C:\Program Files (x86)\Inno Setup 6\iscc.exe"
SIGN_TOOL    := $(TOOLS_DIR)\osslsigncode.exe

# -----------------------------------------------------------------------------
# TARGETS
# -----------------------------------------------------------------------------

.PHONY: all clean build installer sign help

# Default target
all: clean build installer sign
	@echo "========================================="
	@echo " BUILD SUCCESSFUL: ZeroMix v$(VERSION)"
	@echo "========================================="

help:
	@echo "ZeroMix Management Makefile"
	@echo "-----------------------------------------"
	@echo "Targets:"
	@echo "  build      : Restore and publish the WPF application"
	@echo "  installer  : Compile the Inno Setup installer"
	@echo "  sign       : Sign the resulting executable"
	@echo "  clean      : Remove build artifacts"
	@echo "  all        : Run full pipeline (clean -> build -> installer -> sign)"
	@echo ""
	@echo "Usage:"
	@echo "  make all VERSION=2.3.0"

# -----------------------------------------------------------------------------
# TASKS
# -----------------------------------------------------------------------------

clean:
	@echo "[CLEAN] Removing previous build artifacts..."
	@if exist "$(PUBLISH_DIR)" rmdir /S /Q "$(PUBLISH_DIR)"
	@if exist "$(BUILD_DIR)" rmdir /S /Q "$(BUILD_DIR)"
	@# Remove old installers if any
	@if exist "$(EXE_DIR)\ZeroMix-Setup-*.exe" del /Q "$(EXE_DIR)\ZeroMix-Setup-*.exe"

build:
	@echo "[BUILD] Publishing Application v$(VERSION)..."
	@$(DOTNET) publish "$(PROJECT_FILE)" -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishTrimmed=true -p:PublishReadyToRun=true --self-contained -o "$(PUBLISH_DIR)\win-x64"

installer:
	@echo "[INSTALLER] Compiling Inno Setup Script..."
	@if not exist $(ISCC) (echo "Error: ISCC not found at $(ISCC)" & exit 1)
	@$(ISCC) "/DAppVersion=$(VERSION)" "$(SETUP_SCRIPT)"

sign:
	@echo "[SIGN] Signing Installer..."
	@if not exist "$(EXE_DIR)\ZeroMix-Setup-v$(VERSION).exe" (echo "Error: Installer not found!" & exit 1)
	@$(SIGN_TOOL) sign -pkcs12 "$(CERT_FILE)" -pass "$(CERT_PASS)" -n "ZeroMix" -i "https://zeromix.pages.dev" -t "http://timestamp.digicert.com" -in "$(EXE_DIR)\ZeroMix-Setup-v$(VERSION).exe" -out "$(EXE_DIR)\ZeroMix-Setup-v$(VERSION)-signed.exe"
	@if exist "$(EXE_DIR)\ZeroMix-Setup-v$(VERSION)-signed.exe" ( \
		del "$(EXE_DIR)\ZeroMix-Setup-v$(VERSION).exe" && \
		move "$(EXE_DIR)\ZeroMix-Setup-v$(VERSION)-signed.exe" "$(EXE_DIR)\ZeroMix-Setup-v$(VERSION).exe" && \
		echo "Signature applied successfully." \
	) else ( \
		echo "Signing failed." & exit 1 \
	)
