# GitHub Upload Commands

After creating your GitHub repository, run these commands:

## 1. Add Remote Repository
```bash
git remote add origin https://github.com/Hasan-Zayni/RevitDtools.git
```

## 2. Push to GitHub
```bash
git branch -M main
git push -u origin main
```

## 3. Verify Upload
Check your GitHub repository at: https://github.com/Hasan-Zayni/RevitDtools

## 4. Create First Release
1. Go to your repository on GitHub
2. Click "Releases" on the right side
3. Click "Create a new release"
4. Fill in:
   - **Tag version**: `v1.1.0`
   - **Release title**: `RevitDtools v1.1.0 - Major Fixes & Enhancements`
   - **Description**: Copy from CHANGELOG.md v1.1.0 section
   - **Attach files**: Upload the built DLL and .addin file

## 5. Enable Issues and Discussions
1. Go to Settings > Features
2. Enable "Issues" for bug tracking
3. Enable "Discussions" for community questions

## Alternative: Use GitHub CLI (if installed)
```bash
# Create repository directly from command line
gh repo create RevitDtools --public --description "RevitDtools - Comprehensive Revit add-in inspired by DTools"

# Push code
git remote add origin https://github.com/Hasan-Zayni/RevitDtools.git
git branch -M main
git push -u origin main
```