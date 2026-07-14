# GitHub Actions CI/CD Automation

## Quick Overview

This repository uses **GitHub Actions** to automatically run tests, measure code coverage, and perform security scanning on every commit and pull request.

## What Happens on Every Commit

When you push code to `main` or `develop` branches (or create a PR):

1. ✅ **Build** - Compiles the project in Release mode
2. ✅ **Tests** - Runs all 113 tests automatically
3. ✅ **Coverage** - Measures code coverage and generates reports
4. ✅ **Quality** - Checks code formatting and style
5. ✅ **Security** - Scans for known vulnerabilities

## Viewing Results

### GitHub UI
1. Go to your repository
2. Click **Actions** tab at the top
3. Select the workflow run to see results

### Test Results
- Shows pass/fail count
- Click any failed test for details
- View full error messages

### Coverage Reports
- Download the markdown summary from **Artifacts** section
- View coverage percentage and trends
- See line-by-line coverage details
- Coverage is calculated from unit tests only, with the AppHost assembly excluded from the report; AppHost-backed integration tests run separately without coverage

### Pull Requests
- Status checks appear on your PR
- Coverage summary posted as comment
- Can see exact coverage change

## Status Badges

Add this to your README to show CI status:

```markdown
[![.NET CI Pipeline](https://github.com/Hana-fubuki/PokemonEncyclopedia/actions/workflows/dotnet-ci.yml/badge.svg?branch=main)](https://github.com/Hana-fubuki/PokemonEncyclopedia/actions/workflows/dotnet-ci.yml)
```

## Configuration

### Codecov Integration (Public Repos)
✅ **Already configured** - works automatically for public repositories. No additional setup needed!

### Codecov Integration (Private Repos)
For private repositories, add your Codecov token:

1. Visit https://codecov.io and sign in with GitHub
2. Find your repository token
3. Go to GitHub Settings > Secrets > Actions
4. Add `CODECOV_TOKEN` secret with your token

### Require Tests Before Merge

To prevent merging without passing tests:

1. Go to **Settings** > **Branches**
2. Click **Add rule** under "Branch protection rules"
3. Set Branch name to `main`
4. Check **Require status checks to pass before merging**
5. Select **build-and-test**
6. Save

Now PRs must pass all tests before merging!

## Workflow File

📁 **Location**: `.github/workflows/dotnet-ci.yml`

The workflow includes:
- Build job (Release mode)
- Test execution with coverage collection
- Code quality analysis
- Security scanning
- Coverage summary generation and upload
- Artifact retention (30 days)

## Pipeline Details

### Build & Test Job
- Restores NuGet packages
- Builds project in Release configuration
- Runs xUnit tests with coverage
- Generates Cobertura coverage reports
- Generates a markdown coverage summary
- Uploads to Codecov.io
- Posts the coverage summary on PRs

### Code Quality Job
- Checks code formatting compliance
- Runs .NET static analyzers
- Reports violations (non-blocking)

### Security Scanning
- Trivy vulnerability scanner
- Detects CVEs in dependencies
- Reports to GitHub Security tab

## Troubleshooting

### Tests Fail in CI but Pass Locally
- Ensure .NET 10.0 is installed locally
- Run `dotnet restore` and `dotnet build`
- Check for environment-specific code
- Verify no hardcoded paths

### Coverage Report Not Generated
- Check `coverlet.collector` package is installed
- Ensure `--collect:"XPlat Code Coverage"` is in test command
- Verify coverage format is `cobertura`

### Codecov Not Uploading
- For public repos: should work automatically
- For private repos: verify CODECOV_TOKEN secret is set
- Check codecov.io dashboard for connection status

### Slow Pipeline Execution
- Typical runtime: 5-10 minutes
- Build: ~2-3 minutes
- Tests: ~2-5 minutes
- Coverage upload: ~1 minute

## Customization

### Change Trigger Branches
Edit `.github/workflows/dotnet-ci.yml`:
```yaml
on:
  push:
    branches: [ main, develop, staging ]
  pull_request:
    branches: [ main, develop, staging ]
```

### Skip CI for Specific Commits
Add `[skip ci]` to commit message:
```bash
git commit -m "docs: Update README [skip ci]"
```

### Change .NET Version
```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v5
  with:
    dotnet-version: '10.0.x'
```

## Artifacts

All workflow artifacts are stored for 30 days:

- **test-results.trx** - Visual Studio Test Results format
- **coverage-report/** - HTML coverage reports with statistics

Download from the Actions run details page.

## Real-Time Monitoring

### GitHub Dashboard
- Go to Actions tab
- See all workflow runs
- Click run to see live logs
- Watch tests execute in real-time

### Email Notifications
GitHub sends emails when:
- ✅ Workflow fails
- ✅ Workflow succeeds after previous failure
- ✅ Configure in your notification settings

### Codecov Dashboard
Visit https://codecov.io to see:
- Coverage trends over time
- Commit-by-commit coverage changes
- Coverage compare for PRs
- Badges for your README

## Best Practices

✅ **Always write tests** - They run automatically on every commit
✅ **Use feature branches** - Push to main only when ready
✅ **Check PR status** - Wait for CI to pass before merging
✅ **Review coverage** - Aim for 80%+ coverage
✅ **Fix failures fast** - Address CI failures immediately
✅ **Use descriptive commits** - Makes troubleshooting easier

## Documentation

For detailed configuration and troubleshooting, see:
- `.github/workflows/dotnet-ci.yml` - Main workflow file
- `GITHUB_ACTIONS_GUIDE.md` - Comprehensive documentation

## Support

For GitHub Actions documentation:
- https://docs.github.com/en/actions
- https://docs.codecov.io/
- https://github.com/aquasecurity/trivy

---

**Status**: ✅ Active and configured

Next commit will trigger the pipeline automatically!
