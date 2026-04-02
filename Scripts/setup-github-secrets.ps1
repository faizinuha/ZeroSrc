# Setup GitHub Secrets for Code Signing
# This script helps you setup certificate secrets in GitHub

param(
    [string]$CertPath = "Exe/ZeroMixCert.pfx",
    [string]$CertPassword = "",
    [string]$Repository = ""
)

Write-Host "🔐 GitHub Secrets Setup for ZeroMix Code Signing" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Check if certificate exists
if (-not (Test-Path $CertPath)) {
    Write-Host "❌ Certificate not found at: $CertPath" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Certificate found: $CertPath" -ForegroundColor Green
Write-Host ""

# Encode certificate
Write-Host "🔐 Encoding certificate to base64..." -ForegroundColor Yellow
$certBytes = [IO.File]::ReadAllBytes($CertPath)
$certBase64 = [Convert]::ToBase64String($certBytes)

Write-Host "✅ Certificate encoded" -ForegroundColor Green
Write-Host "   Size: $([math]::Round($certBase64.Length/1KB, 2)) KB"
Write-Host ""

# Save base64 to file
$base64File = "Exe/ZeroMixCert_base64.txt"
$certBase64 | Out-File -FilePath $base64File -Encoding UTF8 -Force
Write-Host "✅ Saved base64 to: $base64File" -ForegroundColor Green
Write-Host ""

# Check if gh CLI is installed
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host "⚠️  GitHub CLI (gh) not found" -ForegroundColor Yellow
    Write-Host "   Install from: https://cli.github.com/" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "📝 Manual Setup Instructions:" -ForegroundColor Cyan
    Write-Host "1. Go to: https://github.com/YOUR_ORG/ZeroMix/settings/secrets/actions"
    Write-Host "2. Click 'New repository secret'"
    Write-Host "3. Name: CERT_BASE64"
    Write-Host "4. Value: (paste content from $base64File)"
    Write-Host "5. Click 'Add secret'"
    Write-Host ""
    Write-Host "6. Click 'New repository secret' again"
    Write-Host "7. Name: CERT_PASSWORD"
    Write-Host "8. Value: (your certificate password)"
    Write-Host "9. Click 'Add secret'"
    Write-Host ""
    exit 0
}

# Get repository if not provided
if ([string]::IsNullOrEmpty($Repository)) {
    Write-Host "🔍 Detecting repository..." -ForegroundColor Yellow
    $Repository = gh repo view --json nameWithOwner -q '.nameWithOwner'
    if ([string]::IsNullOrEmpty($Repository)) {
        Write-Host "❌ Could not detect repository" -ForegroundColor Red
        Write-Host "   Run: gh auth login" -ForegroundColor Yellow
        exit 1
    }
}

Write-Host "📦 Repository: $Repository" -ForegroundColor Green
Write-Host ""

# Prompt for password if not provided
if ([string]::IsNullOrEmpty($CertPassword)) {
    Write-Host "🔑 Enter certificate password (will not be displayed):" -ForegroundColor Yellow
    $CertPassword = Read-Host -AsSecureString
    $CertPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToCoTaskMemUnicode($CertPassword))
}

# Create secrets using gh CLI
Write-Host ""
Write-Host "📤 Creating GitHub Secrets..." -ForegroundColor Yellow
Write-Host ""

try {
    # Create CERT_BASE64 secret
    Write-Host "  Creating CERT_BASE64..." -ForegroundColor Cyan
    $certBase64 | gh secret set CERT_BASE64 --repo $Repository
    Write-Host "  ✅ CERT_BASE64 created" -ForegroundColor Green
    
    # Create CERT_PASSWORD secret
    Write-Host "  Creating CERT_PASSWORD..." -ForegroundColor Cyan
    $CertPassword | gh secret set CERT_PASSWORD --repo $Repository
    Write-Host "  ✅ CERT_PASSWORD created" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "✅ All secrets created successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "🎉 Code signing is now enabled!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📝 Next Steps:" -ForegroundColor Cyan
    Write-Host "1. Create a release tag: git tag v5.1.1"
    Write-Host "2. Push tag: git push origin v5.1.1"
    Write-Host "3. Workflow will automatically sign the executable"
    Write-Host ""
    
} catch {
    Write-Host "❌ Error creating secrets: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "📝 Manual Setup Instructions:" -ForegroundColor Yellow
    Write-Host "1. Go to: https://github.com/$Repository/settings/secrets/actions"
    Write-Host "2. Create CERT_BASE64 secret with content from: $base64File"
    Write-Host "3. Create CERT_PASSWORD secret with your certificate password"
    exit 1
}
