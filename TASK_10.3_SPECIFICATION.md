# Task 10.3 Specification: NuGet Packages

## Task: Implement NuGet Package Configuration

**Phase:** Phase 10: Deployment & Packaging  
**Status:** In Progress  
**Date:** January 4, 2026  
**Prerequisites:** Task 10.1 Complete

---

## Description

Configure PairAdmin for NuGet package publishing including package metadata, versioning strategy, and symbol package handling.

---

## Deliverables

### 1. Core Library Package
- PairAdmin.Core.dll
- All shared components
- Source link enabled

### 2. Shared Components Package
- Common utilities
- Extension methods
- Helper classes

### 3. Source Generators Package (optional)
- Code generation tools
- Analyzer packages

---

## Package Structure

### PairAdmin.Core

```
Package: PairAdmin.Core
ID: PairAdmin.Core
Version: 1.0.0
Dependencies:
  - Microsoft.Extensions.Logging (8.0.0)
  - System.Text.Json (8.0.0)

Contains:
  - Commands/
  - Context/
  - Security/
  - Help/
  - LLMGateway/
```

### PairAdmin.Shared

```
Package: PairAdmin.Shared
ID: PairAdmin.Shared
Version: 1.0.0
Dependencies: None

Contains:
  - DataStructures/
  - Interop/
  - Configuration/
```

---

## Versioning Strategy

### Semantic Versioning

```
MAJOR.MINOR.PATCH[-PRERELEASE+BUILD]
1.0.0              - Stable release
1.0.0-alpha.1     - Alpha release
1.0.0-beta.2      - Beta release
1.0.0-rc.1        - Release candidate
```

### Version Branches

| Branch | Package Version | SemVer |
|--------|-----------------|--------|
| main | 1.0.0-* | Prerelease |
| release/* | 1.0.0 | Stable |

---

## NuGet Configuration

### NuGet.config

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <add key="GitHub" value="https://nuget.pkg.github.com/OWNER/index.json" />
  </packageSources>
  
  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
    <packageSource key="GitHub">
      <package pattern="PairAdmin.*" />
    </packageSource>
  </packageSourceMapping>
  
  <config>
    <add key="globalPackagesFolder" value="packages" />
  </config>
</configuration>
```

---

## Build Commands

```bash
# Version bump
dotnet tools install nbgv --local
nbgv version

# Pack with version
dotnet pack -c Release -p:Version=1.0.0

# Pack with auto-versioning
dotnet pack -c Release

# Push to NuGet
dotnet nuget push *.nupkg --source https://api.nuget.org/v3/index.json --api-key YOUR_API_KEY

# Push symbols
dotnet nuget push *.snupkg --source https://api.nuget.org/v3/index.json --api-key YOUR_API_KEY
```

---

## Symbol Packages

### Source Link Configuration

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SourceLink.GitHub" Version="1.1.1" PrivateAssets="All" />
</ItemGroup>

<ItemGroup>
  <SourceLinkGitHubRepository Include="https://github.com/yourusername/PairAdmin" />
</ItemGroup>
```

---

## Files Created

```
nuget/
├── NuGet.config                 # NuGet configuration
└── README.md                    # Package README template

src/PairAdmin.Core/
└── PairAdmin.Core.csproj        # Core library package

src/PairAdmin.Shared/
└── PairAdmin.Shared.csproj      # Shared components package
```

---

## Estimated Complexity

| Item | Complexity | Lines |
|------|------------|-------|
| NuGet.config | Low | ~20 |
| Core csproj | Low | ~30 |
| Shared csproj | Low | ~30 |

**Total Estimated:** ~80 lines of configuration

---

## Next Steps

After Task 10.3 is complete:
1. Phase 10 Complete Summary

---

## Notes

- Enable deterministic builds
- Use source link for debugging
- Include XML documentation
- Add package badges to README
- Monitor package statistics
