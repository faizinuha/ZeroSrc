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

# Targets
.PHONY: all clean build installer msi msix sign help

# Default target: Build everything (EXE, MSI, MSIX)
all: clean build installer msi msix sign
	@echo "========================================="
	@echo " ALL BUILD SUCCESSFUL: ZeroMix v$(VERSION)"
	@echo " Format Available: EXE, MSI, MSIX"
	@echo "========================================="

help:
	@echo "ZeroMix Management Makefile"
	@echo "-----------------------------------------"
	@echo "Targets:"
	@echo "  build      : Restore and publish the WPF application"
	@echo "  installer  : Compile the Inno Setup installer (EXE)"
	@echo "  msi        : Build MSI Installer (Requires WiX v4)"
	@echo "  msix       : Build MSIX Modern Package (Windows 10/11)"
	@echo "  sign       : Sign all resulting packages"
	@echo "  clean      : Remove build artifacts"
	@echo "  all        : Run full pipeline for all formats"

# -----------------------------------------------------------------------------
# TASKS
# -----------------------------------------------------------------------------

clean:
	@echo "[CLEAN] Removing previous build artifacts..."
	@if exist "$(PUBLISH_DIR)" rmdir /S /Q "$(PUBLISH_DIR)"
	@if exist "$(BUILD_DIR)" rmdir /S /Q "$(BUILD_DIR)"
	@if exist "$(EXE_DIR)\ZeroMix-Setup-*.exe" del /Q "$(EXE_DIR)\ZeroMix-Setup-*.exe"
	@if exist "$(EXE_DIR)\ZeroMix-*.msi" del /Q "$(EXE_DIR)\ZeroMix-*.msi"
	@if exist "$(EXE_DIR)\ZeroMix-*.msix" del /Q "$(EXE_DIR)\ZeroMix-*.msix"

build:
	@echo "[BUILD] Publishing Application v$(VERSION)..."
	@$(DOTNET) publish "$(PROJECT_FILE)" -c Release -r win-x64 -p:PublishReadyToRun=true --self-contained -o "$(PUBLISH_DIR)\win-x64"

installer:
	@echo "[INSTALLER] Compiling Inno Setup Script..."
	@if not exist $(ISCC) (echo "Error: ISCC not found!" & exit 1)
	@$(ISCC) "/DAppVersion=$(VERSION)" "$(EXE_DIR)\Setup.iss"

msi:
	@echo "[MSI] Building MSI with WiX..."
	@# Script ini berasumsi 'wix' ada di PATH (WiX Toolset v4)
	-dotnet wix build "$(EXE_DIR)\ZeroMix.wxs" -o "$(EXE_DIR)\ZeroMix-v$(VERSION).msi"

msix:
	@echo "[MSIX] Creating MSIX Package..."
	@# Kita copy file publish ke folder sementara untuk dipadatkan jadi MSIX
	@if not exist "$(BUILD_DIR)\msix" mkdir "$(BUILD_DIR)\msix"
	@xcopy /E /Y "$(PUBLISH_DIR)\win-x64\*" "$(BUILD_DIR)\msix\" >nul
	@copy /Y "$(EXE_DIR)\Package.appxmanifest" "$(BUILD_DIR)\msix\AppxManifest.xml" >nul
	@copy /Y "$(EXE_DIR)\zeromix.ico" "$(BUILD_DIR)\msix\zeromix.ico" >nul
	@# Menggunakan MakeAppx (biasanya di Windows SDK)
	-makeappx pack /d "$(BUILD_DIR)\msix" /p "$(EXE_DIR)\ZeroMix-v$(VERSION).msix"

sign:
	@echo "[SIGN] Signing all installers..."
	@# Sign EXE
	@if exist "$(EXE_DIR)\ZeroMix-Setup-v$(VERSION).exe" $(SIGN_TOOL) sign -pkcs12 "$(CERT_FILE)" -pass "$(CERT_PASS)" -n "ZeroMix" -in "$(EXE_DIR)\ZeroMix-Setup-v$(VERSION).exe" -out "$(EXE_DIR)\ZeroMix-Setup-v$(VERSION)-signed.exe"
	@# Sign MSI
	@if exist "$(EXE_DIR)\ZeroMix-v$(VERSION).msi" $(SIGN_TOOL) sign -pkcs12 "$(CERT_FILE)" -pass "$(CERT_PASS)" -n "ZeroMix" -in "$(EXE_DIR)\ZeroMix-v$(VERSION).msi" -out "$(EXE_DIR)\ZeroMix-v$(VERSION)-signed.msi"
	@# Sign MSIX (PENTING!)
	@if exist "$(EXE_DIR)\ZeroMix-v$(VERSION).msix" $(SIGN_TOOL) sign -pkcs12 "$(CERT_FILE)" -pass "$(CERT_PASS)" -n "ZeroMix" -in "$(EXE_DIR)\ZeroMix-v$(VERSION).msix" -out "$(EXE_DIR)\ZeroMix-v$(VERSION)-signed.msix"
	@echo "Signing complete. Moving files..."
	@-move /Y "$(EXE_DIR)\*-signed.*" "$(EXE_DIR)\"
