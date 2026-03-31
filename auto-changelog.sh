#!/bin/bash

# Auto-Generate CHANGELOG Script
# Membaca latest release dan generate CHANGELOG dari commits setelahnya
# Usage: ./auto-changelog.sh

set -e

echo "🚀 Starting Auto-Changelog Generation..."
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Setup Git config
echo -e "${BLUE}🔧 Setting up Git config...${NC}"
git config user.email "changelog-bot@zeromix.local" || true
git config user.name "Changelog Bot" || true

# Get latest release
echo -e "${BLUE}📦 Fetching latest release...${NC}"
LATEST_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "")

if [ -z "$LATEST_TAG" ]; then
    echo -e "${YELLOW}⚠️  No existing releases found${NC}"
    COMMIT_RANGE="HEAD"
    NEXT_VERSION="v1.0.0"
else
    echo -e "${GREEN}✅ Latest release: $LATEST_TAG${NC}"
    COMMIT_RANGE="${LATEST_TAG}..HEAD"
    
    # Parse current version and increment patch
    VERSION="${LATEST_TAG#v}"
    MAJOR=$(echo $VERSION | cut -d. -f1)
    MINOR=$(echo $VERSION | cut -d. -f2)
    PATCH=$(echo $VERSION | cut -d. -f3 | cut -d- -f1)
    NEXT_PATCH=$((PATCH + 1))
    NEXT_VERSION="v${MAJOR}.${MINOR}.${NEXT_PATCH}"
fi

echo -e "${BLUE}📌 Next version: $NEXT_VERSION${NC}"
echo ""

# Check if there are new commits
COMMIT_COUNT=$(git rev-list --count $COMMIT_RANGE 2>/dev/null || echo "0")
if [ "$COMMIT_COUNT" -eq 0 ]; then
    echo -e "${YELLOW}ℹ️  No new commits since $LATEST_TAG${NC}"
    exit 0
fi

echo -e "${BLUE}📊 Found $COMMIT_COUNT new commits${NC}"
echo ""

# Generate changelog
echo -e "${BLUE}📋 Generating changelog...${NC}"
TEMP_CHANGELOG="/tmp/zeromix_changelog_$$.md"

echo "## [$NEXT_VERSION] - $(date +%Y-%m-%d)" > $TEMP_CHANGELOG
echo "" >> $TEMP_CHANGELOG

# Features
echo "### ✨ Features" >> $TEMP_CHANGELOG
git log $COMMIT_RANGE --oneline --grep="^feat" --format="- %h: %s" 2>/dev/null >> $TEMP_CHANGELOG || echo "- No new features" >> $TEMP_CHANGELOG
echo "" >> $TEMP_CHANGELOG

# Bug fixes
echo "### 🐛 Bug Fixes" >> $TEMP_CHANGELOG
git log $COMMIT_RANGE --oneline --grep="^fix" --format="- %h: %s" 2>/dev/null >> $TEMP_CHANGELOG || echo "- No bug fixes" >> $TEMP_CHANGELOG
echo "" >> $TEMP_CHANGELOG

# Other changes
echo "### 🔧 Changes" >> $TEMP_CHANGELOG
git log $COMMIT_RANGE --oneline --all-match --grep="^chore\|^refactor\|^docs\|^style" --format="- %h: %s" 2>/dev/null >> $TEMP_CHANGELOG || echo "- No other changes" >> $TEMP_CHANGELOG
echo "" >> $TEMP_CHANGELOG

# Summary
echo "### 📊 Commit Summary" >> $TEMP_CHANGELOG
echo "- Total commits: **$COMMIT_COUNT**" >> $TEMP_CHANGELOG
echo "- Range: $LATEST_TAG..HEAD" >> $TEMP_CHANGELOG
echo "- Generated: $(date)" >> $TEMP_CHANGELOG

# Append existing changelog
if [ -f "CHANGELOG.md" ]; then
    echo "" >> $TEMP_CHANGELOG
    cat CHANGELOG.md >> $TEMP_CHANGELOG
fi

# Replace CHANGELOG.md
mv $TEMP_CHANGELOG CHANGELOG.md

echo -e "${GREEN}✅ CHANGELOG.md updated${NC}"
echo ""

# Show preview
echo -e "${BLUE}📄 Preview (first 30 lines):${NC}"
head -30 CHANGELOG.md
echo ""
echo "..."
echo ""

# Auto-commit
echo -e "${BLUE}💾 Auto-committing changes...${NC}"
if git diff --quiet CHANGELOG.md 2>/dev/null; then
    echo -e "${YELLOW}ℹ️  No changes to commit${NC}"
else
    git add CHANGELOG.md
    git commit -m "docs(changelog): update for $NEXT_VERSION" \
        -m "Auto-generated from $COMMIT_COUNT commits" \
        -m "Range: $LATEST_TAG..HEAD"
    
    echo -e "${GREEN}✅ Changes committed${NC}"
    echo ""
    echo -e "${BLUE}📤 Ready to push? Run:${NC}"
    echo "   git push origin $(git rev-parse --abbrev-ref HEAD)"
fi

echo ""
echo -e "${GREEN}🎉 Done!${NC}"
echo ""
echo "📊 Summary:"
echo "├─ Latest release: $LATEST_TAG"
echo "├─ Next version: $NEXT_VERSION"
echo "├─ New commits: $COMMIT_COUNT"
echo "└─ File: CHANGELOG.md"
