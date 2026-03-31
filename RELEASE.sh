#!/bin/bash
# ZeroMix Release Helper Script
# Quick reference untuk membuat release dengan GitHub Actions

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}╔════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║          ZeroMix Release Helper v1.0              ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════╝${NC}"
echo ""

# Check if we're in git repo
if ! git rev-parse --git-dir > /dev/null 2>&1; then
    echo -e "${RED}❌ Not a git repository!${NC}"
    echo "Please run this script from the ZeroMix repository root"
    exit 1
fi

# Check if in clean state
if [ -n "$(git status -s)" ]; then
    echo -e "${YELLOW}⚠️  Warning: Uncommitted changes found${NC}"
    echo -e "${YELLOW}Please commit all changes before creating a release${NC}"
    echo ""
    git status -s
    echo ""
    read -p "Continue anyway? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

echo -e "${BLUE}→ Current branch:${NC} $(git rev-parse --abbrev-ref HEAD)"
echo -e "${BLUE}→ Latest tag:${NC} $(git describe --tags --abbrev=0 2>/dev/null || echo 'No tags yet')"
echo ""

# Get version input
read -p "Enter version number (e.g., 1.0.0): " VERSION

# Validate version format
if ! [[ $VERSION =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9]+)?$ ]]; then
    echo -e "${RED}❌ Invalid version format!${NC}"
    echo "Use semver: 1.0.0, 1.0.0-alpha, 1.0.0-beta, etc."
    exit 1
fi

TAG="v$VERSION"

# Check if tag already exists
if git rev-parse "$TAG" >/dev/null 2>&1; then
    echo -e "${RED}❌ Tag $TAG already exists!${NC}"
    exit 1
fi

# Get release message
echo ""
echo -e "${BLUE}Enter release message (or press Enter for auto-generated):${NC}"
read -p "> " RELEASE_MSG

if [ -z "$RELEASE_MSG" ]; then
    RELEASE_MSG="Release version $VERSION"
fi

echo ""
echo -e "${YELLOW}Release Summary:${NC}"
echo "  Version:  $VERSION"
echo "  Tag:      $TAG"
echo "  Message:  $RELEASE_MSG"
echo "  Branch:   $(git rev-parse --abbrev-ref HEAD)"
echo ""

# Confirm
read -p "Create release? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Cancelled."
    exit 1
fi

echo ""
echo -e "${BLUE}🏷️  Creating tag...${NC}"
git tag -a "$TAG" -m "$RELEASE_MSG"

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Failed to create tag${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Tag created locally${NC}"
echo ""

echo -e "${BLUE}📤 Pushing tag to GitHub...${NC}"
git push origin "$TAG"

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Failed to push tag${NC}"
    echo -e "${YELLOW}To retry, run: git push origin $TAG${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Tag pushed${NC}"
echo ""

echo -e "${GREEN}╔════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║          🎉 Release created successfully!         ║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════════════════╝${NC}"
echo ""

echo -e "${BLUE}What happens next:${NC}"
echo "1. GitHub Actions workflows start automatically"
echo "2. Project is built and compiled"
echo "3. Changelog is generated from commits"
echo "4. Release page is created with all details"
echo "5. Download links are available"
echo ""

echo -e "${YELLOW}🔗 Links:${NC}"
echo "• Watch build progress:"
echo "  https://github.com/faizinuha/ZeroMix/actions"
echo ""
echo "• View release page:"
echo "  https://github.com/faizinuha/ZeroMix/releases/tag/$TAG"
echo ""
echo "• View commits in this release:"
PREV_TAG=$(git describe --tags --abbrev=0 "$TAG^" 2>/dev/null)
if [ -n "$PREV_TAG" ]; then
    echo "  https://github.com/faizinuha/ZeroMix/compare/$PREV_TAG...$TAG"
else
    echo "  https://github.com/faizinuha/ZeroMix/commits/$TAG"
fi
echo ""

echo -e "${BLUE}💡 Tips:${NC}"
echo "• Make sure all commits follow Conventional Commits format"
echo "  Examples: 'feat(...)', 'fix(...)', 'refactor(...)', etc."
echo "• Changelog is auto-generated from these commit messages"
echo "• Check GitHub Actions for real-time build progress"
echo "• Release is complete when ✅ appears in Actions tab"
echo ""

echo -e "${BLUE}📚 Documentation:${NC}"
echo "• Full guide: see GITHUB_ACTIONS_GUIDE.md"
echo "• Changelog: see CHANGELOG.md"
echo "• Commits: git log"
