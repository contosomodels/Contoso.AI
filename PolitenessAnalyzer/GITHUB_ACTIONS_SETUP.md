# GitHub Actions Setup Checklist

Before the CI/CD pipeline can work, complete these setup steps:

## ? Required Steps

### 1. Add NuGet API Key Secret
- [ ] Go to repository **Settings** ? **Secrets and variables** ? **Actions**
- [ ] Click **New repository secret**
- [ ] Name: `NUGET_API_KEY`
- [ ] Value: Your NuGet.org API key from https://www.nuget.org/account/apikeys
- [ ] Click **Add secret**

### 2. Configure Workflow Permissions (Optional)
- [ ] Go to repository **Settings** ? **Actions** ? **General**
- [ ] Under **Workflow permissions**:
  - [ ] Default **Read repository contents and packages permissions** is sufficient
  - [ ] No write permissions needed (versions are auto-generated)
- [ ] Click **Save**

### 3. Verify Workflow File
- [ ] The workflow file is at `.github/workflows/build-and-publish.yml`
- [ ] Commit and push this file to your repository

## ?? How It Works

### For Pull Requests ? `main`
1. ? Builds the solution
2. ? Runs console tests
3. ? Does NOT publish packages

### For Push to `main`
1. ? Builds the solution
2. ? Runs console tests
3. ? Generates version number from run number (e.g., `0.1.42-beta`)
4. ? Publishes NuGet package with generated version
5. ? Creates GitHub release

## ?? Testing the Workflow

### Test Pull Request Flow
```bash
git checkout -b test-branch
# Make some changes
git add .
git commit -m "test: verify PR workflow"
git push origin test-branch
# Create PR on GitHub ? workflow should run tests only
```

### Test Push to Main Flow
```bash
git checkout main
git pull
# Merge your PR or make changes directly
git push origin main
# workflow should run tests and publish package
```

## ?? Monitoring

- View workflow runs: Go to **Actions** tab in your GitHub repository
- Check logs for each step
- Verify packages published at: https://www.nuget.org/packages/Contoso.AI.PolitenessAnalyzer/

## ?? Troubleshooting

### Tests Fail
```bash
# Run tests locally first
dotnet run --project Contoso.AI.PolitenessAnalyzer.ConsoleTest --configuration Release
```

### Package Already Exists
- The workflow uses `--skip-duplicate` flag
- If version exists, it will skip publishing without error

### Version Bump Fails
- No longer applicable - versions are generated automatically based on date and run number
- No commits are made to the repository

### NuGet Push Fails
- Verify `NUGET_API_KEY` secret is set
- Check API key hasn't expired
- Ensure API key has push permissions

## ?? Next Steps

After setup is complete:
1. Commit these workflow files
2. Push to main branch
3. Check the Actions tab to see the workflow run
4. Verify package is published to NuGet.org

## ?? Quick Start Commands

```bash
# Add workflow files to git
git add .github/

# Commit with descriptive message
git commit -m "ci: add GitHub Actions workflow for build, test, and publish"

# Push to trigger workflow
git push origin main
```

---

**Note**: Each push to main will generate a unique version based on the GitHub Actions run number. For example, `0.1.42-beta` means the 42nd workflow run.

## ?? Version Format

- **Format**: `0.1.RUN_NUMBER-beta`
- **Example**: `0.1.42-beta`
  - `0.1` - Fixed major.minor version
  - `42` - GitHub Actions run number (auto-increments)
  - `-beta` - Pre-release suffix

**Benefits:**
- ? No version commits cluttering your history
- ? Each build gets a unique version automatically
- ? Simple, predictable versioning (0.1.1, 0.1.2, 0.1.3...)
- ? Run number ensures monotonic increment
- ? Easy to manually bump major.minor when needed (edit workflow file)
