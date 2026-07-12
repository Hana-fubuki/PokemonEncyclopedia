# Quick Reference: GitHub Actions CI/CD Pipeline

## 🎯 What It Does

Automatically runs on every commit to `main` or `develop`:
- ✅ Builds your project
- ✅ Runs all tests (94+)
- ✅ Measures code coverage
- ✅ Generates reports
- ✅ Scans for vulnerabilities
- ✅ Posts results to GitHub

## 📍 Where to Find It

- **Workflow File**: `.github/workflows/dotnet-ci.yml`
- **View Results**: GitHub > Actions tab
- **Full Docs**: `CI_CD_OVERVIEW.md`

## 🚀 Zero Setup Required (for public repos!)

Just push and it works automatically.

## 🔍 Quick Lookup

### I want to...

**See test results**
→ Actions tab > Click workflow run > View test logs

**Download coverage report**
→ Actions tab > Artifacts > Download "coverage-report"

**Block merge without passing tests**
→ Settings > Branches > Add rule > Require status checks

**Skip CI on a commit**
→ Add `[skip ci]` to commit message

**Check test code coverage**
→ Look for "Code Coverage Report" comment on PR

**View security vulnerabilities**
→ GitHub > Security tab > Advisories

## ⏱️ How Long Does It Take?

Typically **5-10 minutes**:
- Build: 2-3 min
- Tests: 2-5 min
- Reports: 1 min

## 📊 Viewing Results

| What | Where |
|------|-------|
| Test Results | Actions > Job Logs |
| Coverage % | PR Comment or Artifacts |
| Security Issues | GitHub Security tab |
| Performance | Artifacts section |

## 🛠️ Customization

**Change which branches trigger CI:**
Edit `.github/workflows/dotnet-ci.yml`:
```yaml
on:
  push:
    branches: [ main, develop, staging ]
```

**Change .NET version:**
```yaml
- uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '9.0.x'
```

## 🔐 For Private Repos

Add Codecov token (optional, but recommended):

1. Get token from https://codecov.io
2. GitHub > Settings > Secrets > New: `CODECOV_TOKEN`
3. Done!

## 📋 Pipeline Summary

```
┌─ Checkout Code ─┐
│                 ├─ Setup .NET
│                 ├─ Restore Packages
│                 ├─ Build Project
│                 ├─ Run Tests + Coverage
│                 ├─ Generate Reports
│                 ├─ Upload to Codecov
│                 ├─ Archive Results
│                 └─ Post PR Comments
│
├─ Code Quality (non-blocking)
│  ├─ Check Formatting
│  └─ Run Analyzers
│
├─ Security Scan
│  ├─ Trivy Scan
│  └─ SARIF Upload
│
└─ Summary
   └─ Report All Results
```

## ✨ Key Features

- 🔄 **Automatic** - Runs on every commit
- 📊 **Coverage Tracking** - Codecov integration
- 🛡️ **Security** - Trivy CVE scanning
- 💬 **PR Comments** - Shows coverage changes
- 📦 **Artifact Storage** - 30-day retention
- ⚡ **Parallel Jobs** - Faster execution
- 🔗 **GitHub Integration** - Native checks & badges

## 🆘 Troubleshooting

| Problem | Solution |
|---------|----------|
| Tests fail in CI but pass locally | Check .NET version (need 10.0) |
| No coverage report | Ensure coverlet.collector in project |
| Codecov not updating | For private repos: add CODECOV_TOKEN secret |
| Pipeline too slow | Check for large test suites or network calls |

## 📞 Need Help?

- See `CI_CD_OVERVIEW.md` for detailed setup
- See `GITHUB_ACTIONS_GUIDE.md` for comprehensive guide
- GitHub Actions docs: https://docs.github.com/en/actions

---

**Status**: ✅ Live and active!

Push a commit to see it in action.
