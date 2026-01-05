# Task 10.1 Specification: CI/CD Pipeline

## Task: Implement CI/CD Pipeline

**Phase:** Phase 10: Deployment & Packaging  
**Status:** In Progress  
**Date:** January 4, 2026  
**Prerequisites:** Phase 9 Complete

---

## Description

Implement a comprehensive CI/CD pipeline using GitHub Actions for automated building, testing, and publishing of PairAdmin.

---

## Deliverables

### 1. .github/workflows/ci.yml
Main CI workflow:
- Build on multiple platforms
- Run tests with coverage
- Code quality checks
- Security scanning

### 2. .github/workflows/release.yml
Release workflow:
- Version bumping
- Build installer
- Create releases
- Publish packages

### 3. Directory.Build.props
MSBuild properties:
- Version information
- Package metadata
- Analyzer configuration

### 4. global.json
SDK version specification

---

## Requirements

### CI Workflow Triggers

| Trigger | Description |
|---------|-------------|
| push | On main branch changes |
| pull_request | On PR to main/develop |
| workflow_dispatch | Manual trigger |
| schedule | Daily build (UTC midnight) |

### Build Matrix

| OS | .NET Version | Configuration |
|----|--------------|---------------|
| windows-latest | 8.0 | Debug, Release |
| ubuntu-latest | 8.0 | Debug |
| macos-latest | 8.0 | Debug |

### Jobs

#### Build Job
```yaml
build:
  runs-on: ${{ matrix.os }}
  strategy:
    matrix:
      os: [windows-latest, ubuntu-latest, macos-latest]
  steps:
    - uses: actions/checkout@v4
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 8.0.x
    - name: Restore
      run: dotnet restore
    - name: Build
      run: dotnet build --no-restore -c ${{ matrix.configuration }}
    - name: Test
      run: dotnet test --no-build -c ${{ matrix.configuration }} --collect:"XPlat Code Coverage"
    - name: Upload Coverage
      uses: codecov/codecov-action@v3
```

#### Quality Job
```yaml
quality:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - name: Roslyn Analyzer
      run: dotnet build --no-restore
    - name: Security Audit
      uses: github/codeql-action/analyze@v2
```

#### Pack Job (main branch only)
```yaml
pack:
  needs: [build, quality]
  runs-on: ubuntu-latest
  if: github.ref == 'refs/heads/main'
  steps:
    - uses: actions/checkout@v4
    - name: Pack
      run: dotnet pack -c Release
    - name: Publish to NuGet
      run: dotnet nuget push **/*.nupkg --source https://api.nuget.org/v3/index.json
```

---

## Configuration Files

### Directory.Build.props

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0-windows10.0.17763.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    
    <!-- Version -->
    <Version>1.0.0</Version>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
    <InformationalVersion>1.0.0</InformationalVersion>
    
    <!-- Package -->
    <Authors>PairAdmin Team</Authors>
    <Company>PairAdmin</Company>
    <Product>PairAdmin</Product>
    <Description>AI-Assisted Terminal Administration Extension for PuTTY</Description>
    <Copyright>Copyright © 2024</Copyright>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageIcon>icon.png</PackageIcon>
    <RepositoryUrl>https://github.com/yourusername/PairAdmin</RepositoryUrl>
    
    <!-- Analysis -->
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

### global.json

```json
{
  "sdk": {
    "version": "8.0.100",
    "rollForward": "latestMinor"
  }
}
```

---

## Artifacts

### Build Artifacts

| Artifact | Description | Retention |
|----------|-------------|-----------|
| packages | NuGet packages | 90 days |
| installer | MSI installer | 90 days |
| logs | Build logs | 30 days |
| coverage | Coverage reports | 90 days |

---

## Files Created

```
.github/
├── workflows/
│   ├── ci.yml              # Main CI workflow
│   └── release.yml         # Release workflow
├── dependabot.yml          # Dependency updates

build/
├── Directory.Build.props   # MSBuild properties
└── global.json             # SDK version

installer/
├── PairAdmin.wxs           # WiX source
└── Product.wxs             # Product configuration
```

---

## Estimated Complexity

| File | Complexity | Lines |
|------|------------|-------|
| ci.yml | Medium | ~150 |
| release.yml | Medium | ~100 |
| Directory.Build.props | Low | ~50 |
| global.json | Low | ~10 |

**Total Estimated:** ~310 lines of configuration

---

## Next Steps

After Task 10.1 is complete:
1. Task 10.2: MSI Installer
2. Task 10.3: NuGet Packages
3. Phase 10 Complete Summary

---

## Notes

- Use semantic versioning
- Enable branch protection
- Require status checks
- Use environment protection
- Monitor workflow costs
