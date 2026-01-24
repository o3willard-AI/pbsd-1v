@echo off
REM ============================================================
REM Build PairAdminPuTTY.dll
REM ============================================================

setlocal enabledelayedexpansion

set SRC_DIR=%~dp0
set BUILD_DIR=%SRC_DIR%build
set OUTPUT_DIR=%SRC_DIR%..\..\lib
set VCPKG_ROOT=%SRC_DIR%..\..\..\vcpkg

echo Building PairAdminPuTTY.dll...

REM Create directories
if not exist "%BUILD_DIR%" mkdir "%BUILD_DIR%"
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

REM Find Visual Studio installation
set VS2022=
for /f "usebackq delims=" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64`) do (
    set VS2022=%%i
)

if not defined VS2022 (
    echo ERROR: Visual Studio 2022 with C++ workload not found
    echo Please install Visual Studio 2022 with Desktop development with C++
    exit /b 1
)

echo Found Visual Studio at: %VS2022%

REM Setup VS environment
call "%VS2022%\VC\Auxiliary\Build\vcvars64.bat" >nul 2>&1

REM Compile the DLL
echo.
echo Compiling PairAdminPuTTY.dll...
echo.

cl.exe /c /nologo /O2 /MD /DNDEBUG /D_CRT_SECURE_NO_WARNINGS /DPAIRADMIN_EXPORTS ^
    /I"%SRC_DIR%" ^
    /Fo"%BUILD_DIR%\pairadmin.obj" ^
    "%SRC_DIR%pairadmin.c"

if errorlevel 1 (
    echo ERROR: Compilation failed
    exit /b 1
)

REM Link the DLL
echo.
echo Linking PairAdminPuTTY.dll...
echo.

link.exe /nologo /DLL /OUT:"%OUTPUT_DIR%\PairAdminPuTTY.dll" /DEF:"%SRC_DIR%pairadmin.def" ^
    "%BUILD_DIR%\pairadmin.obj" kernel32.lib user32.lib /IMPLIB:"%OUTPUT_DIR%\PairAdminPuTTY.lib"

if errorlevel 1 (
    echo ERROR: Linking failed
    exit /b 1
)

REM Clean up
if exist "%BUILD_DIR%\pairadmin.obj" del "%BUILD_DIR%\pairadmin.obj"

echo.
echo ============================================================
echo Build successful!
echo DLL:    %OUTPUT_DIR%\PairAdminPuTTY.dll
echo Import: %OUTPUT_DIR%\PairAdminPuTTY.lib
echo ============================================================

endlocal
