# PuTTY Integration Build Script

This script demonstrates the build process for PuTTY integration on Windows.

```batch
REM ============================================================
REM Build PairAdmin PuTTY Integration
REM ============================================================

echo Building PuTTY integration for PairAdmin...

REM Set paths
set SRC_DIR=%~dp0
set BUILD_DIR=%SRC_DIR%build
set LIB_DIR=%SRC_DIR%lib

REM Create build directory
if not exist "%BUILD_DIR%" mkdir "%BUILD_DIR%"
if not exist "%LIB_DIR%" mkdir "%LIB_DIR%"

REM Generate build files with CMake
echo Generating build files...
cd /d "%BUILD_DIR%"
cmake -G "Visual Studio 17 2022" ^
      -A x64 ^
      -DCMAKE_BUILD_TYPE=Release ^
      "%SRC_DIR%"

if errorlevel 1 (
    echo ERROR: CMake generation failed
    exit /b 1
)

REM Build library
echo Building library...
cmake --build . --config Release

if errorlevel 1 (
    echo ERROR: Build failed
    exit /b 1
)

REM Copy output to lib directory
echo Copying library...
copy /Y Release\PairAdminPuTTY.lib "%LIB_DIR%\"

if errorlevel 1 (
    echo ERROR: Copy failed
    exit /b 1
)

echo.
echo ============================================================
echo Build successful!
echo Library: %LIB_DIR%\PairAdminPuTTY.lib
echo ============================================================

cd /d "%SRC_DIR%"
exit /b 0
```

## PowerShell Version

```powershell
# ============================================================
# Build PairAdmin PuTTY Integration
# ============================================================

$ErrorActionPreference = "Stop"

$srcDir = $PSScriptRoot
$buildDir = Join-Path $srcDir "build"
$libDir = Join-Path $srcDir "lib"

Write-Host "Building PuTTY integration for PairAdmin..." -ForegroundColor Green

# Create directories
New-Item -ItemType Directory -Force -Path $buildDir | Out-Null
New-Item -ItemType Directory -Force -Path $libDir | Out-Null

# Generate build files
Write-Host "Generating build files..." -ForegroundColor Cyan
Push-Location $buildDir
cmake -G "Visual Studio 17 2022" `
      -A x64 `
      -DCMAKE_BUILD_TYPE=Release `
      $srcDir

# Build library
Write-Host "Building library..." -ForegroundColor Cyan
cmake --build . --config Release

# Copy output
Write-Host "Copying library..." -ForegroundColor Cyan
Copy-Item -Path "Release\PairAdminPuTTY.lib" `
           -Destination $libDir `
           -Force

Pop-Location

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "Build successful!" -ForegroundColor Green
Write-Host "Library: $libDir\PairAdminPuTTY.lib" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
```

## Linux/macOS Build (Development Only)

```bash
#!/bin/bash
# ============================================================
# Build PairAdmin PuTTY Integration
# ============================================================

set -e

echo "Building PuTTY integration for PairAdmin..."

SRC_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$SRC_DIR/build"
LIB_DIR="$SRC_DIR/lib"

# Create directories
mkdir -p "$BUILD_DIR"
mkdir -p "$LIB_DIR"

# Generate build files
echo "Generating build files..."
cd "$BUILD_DIR"
cmake -DCMAKE_BUILD_TYPE=Release "$SRC_DIR"

# Build library
echo "Building library..."
cmake --build . --config Release

# Copy output
echo "Copying library..."
cp libPairAdminPuTTY.a "$LIB_DIR/"

echo ""
echo "============================================================"
echo "Build successful!"
echo "Library: $LIB_DIR/libPairAdminPuTTY.a"
echo "============================================================"
```

## Makefile Shortcut

```makefile
.PHONY: all clean install

all: build/libPairAdminPuTTY.a

build/libPairAdminPuTTY.a:
	@echo "Building PuTTY integration..."
	@mkdir -p build
	@cd build && cmake -DCMAKE_BUILD_TYPE=Release .. && make
	@mkdir -p lib
	@cp build/libPairAdminPuTTY.a lib/

clean:
	@echo "Cleaning..."
	@rm -rf build lib

install: all
	@echo "Installing..."
	@cp lib/libPairAdminPuTTY.a /usr/local/lib/
	@cp pairadmin.h /usr/local/include/PairAdminPuTTY/

test: all
	@echo "Testing..."
	@cd build && ctest --output-on-failure
```

## Notes

1. **Windows (Production)**
   - Requires Visual Studio 2022
   - CMake must be in PATH
   - Builds PairAdminPuTTY.lib

2. **Linux/macOS (Development)**
   - Stub implementation only
   - Cannot integrate with actual PuTTY on these platforms
   - Used for developing Interop layer

3. **Prerequisites**
   - CMake 3.20 or later
   - C++17 compatible compiler
   - Windows SDK 10.0 (Windows only)

4. **Output**
   - Windows: `lib/PairAdminPuTTY.lib`
   - Linux: `lib/libPairAdminPuTTY.a`
   - macOS: `lib/libPairAdminPuTTY.a`

## Integration with PairAdmin Solution

Once built, the library is linked with the Interop project:

In `src/Interop/Interop.csproj`:
```xml
<ItemGroup>
  <ProjectReference Include="..\PuTTY\PuTTY.vcxproj" />
</ItemGroup>
```

---

*Last Updated: January 4, 2026*
