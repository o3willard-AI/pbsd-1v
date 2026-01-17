# PairAdmin Debug Status - Session Checkpoint

**Date:** January 15, 2026
**Issue:** Application crashes when attempting SSH connection (both Integrated and External PuTTY modes)

## Current Status: AWAITING TEST

The latest fix has been applied and built, but has **not yet been tested** due to rate limiting interruptions.

## Problem Summary

PairAdmin is a WPF application that embeds PuTTY for SSH terminal functionality. Two modes:
1. **Integrated Mode** - Uses PairAdminPuTTY.dll (modified PuTTY as DLL)
2. **External Mode** - Launches external putty.exe process

Both modes were crashing on connection attempt.

## Fixes Applied (in order)

### Fix 1: Missing `do_defaults()` call
- **File:** `putty-src/windows/window.c` (line ~6104)
- **Problem:** `conf_cache_data()` was crashing because config values like `CONF_cursor_type` and `CONF_vtmode` weren't initialized
- **Solution:** Added `do_defaults(NULL, wgs->conf)` before setting SSH-specific config overrides

### Fix 2: WndProc NULL pointer crash
- **File:** `putty-src/windows/window.c` (line ~2206-2215)
- **Problem:** During `CreateWindowExW`, Windows sends messages to `WndProc` before `GWLP_USERDATA` is set, causing `wgs` to be NULL when message handlers try to access it
- **Solution:** Added NULL check at start of WndProc:
```c
if (!wgs) {
    return DefWindowProcW(hwnd, message, wParam, lParam);
}
```

## Build Status

- **PairAdminPuTTY.dll:** Rebuilt 1/15/2026 1:06:17 PM with both fixes
- **C# Application:** Built 1/15/2026 9:58:46 AM (includes logging)
- **DLL copied to:** `src/PairAdmin/bin/Release/net8.0-windows/PairAdminPuTTY.dll`

## Executable Location

```
C:\Users\stephen\code\bsdcl\pbsd-1v\src\PairAdmin\bin\Release\net8.0-windows\PairAdmin.exe
```

## Log Files (for debugging)

After running the app, check:
- **C/DLL log:** `%LOCALAPPDATA%\PairAdmin\pairadmin_debug.log`
- **C# log:** `%LOCALAPPDATA%\PairAdmin\pairadmin_csharp.log`

## What to Do Next

1. **Run the application** and attempt an SSH connection
2. **If it crashes:** Check the log files above and share contents
3. **If it works:** The integrated PuTTY terminal should appear embedded in the WPF window

## Key Files Modified

| File | Purpose |
|------|---------|
| `putty-src/windows/window.c` | PuTTY window creation, message handling, pairadmin_thread_proc |
| `putty-src/pairadmin.c` | Logging system, callback implementation |
| `putty-src/pairadmin.h` | API declarations for PairAdmin integration |
| `src/Interop/PuTTYInterop.cs` | C# P/Invoke declarations for DLL functions |
| `src/UI/Controls/TerminalPane.xaml.cs` | WPF control that hosts terminal, includes C# logging |

## Test Connection Details (from logs)

- Host: 192.168.101.88
- Port: 22
- Username: sblanken

## Architecture Overview

```
PairAdmin (C# WPF)
    └── TerminalPane.xaml.cs
        └── PuTTYInterop.cs (P/Invoke)
            └── PairAdminPuTTY.dll
                └── pairadmin_init() → creates thread
                └── pairadmin_thread_proc() → runs PuTTY message loop
                └── pairadmin_connect() → initiates SSH connection
```

## Previous Crash Points (resolved)

1. ~~`conf_cache_data()` - missing default config~~ ✅ Fixed
2. ~~`CreateWindowExW()` - WndProc NULL pointer~~ ✅ Fixed (awaiting test)

## Notes

- .NET 8 SDK was installed during this session via winget
- Build commands:
  - DLL: `MSBuild putty-src/build/windows/PairAdminPuTTY.vcxproj /p:Configuration=Release /p:Platform=x64`
  - C#: `dotnet build PairAdmin.sln --configuration Release`
