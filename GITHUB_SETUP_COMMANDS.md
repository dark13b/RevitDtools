# GitHub Setup Commands

After creating the repository on GitHub, run these commands in order:

```bash
# Add the GitHub remote (replace YOUR_USERNAME with your actual GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/RevitDtools.git

# Rename branch to main (GitHub's default)
git branch -M main

# Push all your code to GitHub
git push -u origin main
```

## Alternative: If you get authentication errors

If you get authentication errors, you may need to:

1. **Use GitHub CLI** (recommended):
   ```bash
   # Install GitHub CLI first: https://cli.github.com/
   gh auth login
   gh repo create RevitDtools --public --source=. --remote=origin --push
   ```

2. **Or use Personal Access Token**:
   - Go to GitHub Settings → Developer settings → Personal access tokens
   - Generate a new token with repo permissions
   - Use it as your password when prompted

## After Successful Push

Your repository will be live at: `https://github.com/YOUR_USERNAME/RevitDtools`