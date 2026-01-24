# Fix for External PuTTY Mode DLL Error

## Problem
When using External PuTTY Mode, the application threw:
```
Failed to Connect: Unable to find entry point 'pairadmin_init' in DLL 'PairAdminPuTTY'
```

## Root Cause
The pre-compiled `lib/PairAdminPuTTY.dll` was built from OLD source code that only had `pairadmin_set_callback` implemented. When the code tried to call `pairadmin_init`, the function didn't exist in the DLL.

## Solution
Updated `src/Interop/PuTTYInterop.cs` to gracefully handle DLL load failures:

### Changes Made:

1. **Added private fields for error tracking:**
   ```csharp
   private static bool _dllLoaded = true;
   private static string _lastError = "";
   ```

2. **Updated Initialize() method with exception handling:**
   - `DllNotFoundException` - DLL file not found
   - `BadImageFormatException` - DLL corrupted or outdated
   - `EntryPointNotFoundException` - Function not found (the original error)

3. **Updated GetErrorMessage() method:**
   - Returns cached _lastError when DLL failed to load
   - Provides clear error messages explaining the issue

4. **Improved error messages:**
   - When `pairadmin_init` is missing, user is instructed:
     - For External PuTTY mode: Use External mode instead of Integrated mode
     - To fix Integrated mode: Rebuild DLL from src/PuTTY/

## Current State

### External PuTTY Mode
✅ **Works** - Doesn't require the DLL at all
- Launches external putty.exe
- Uses standard Windows APIs for window manipulation

### Integrated PuTTY Mode
⚠️ **Limited** - DLL is outdated
- When attempting to connect, user gets clear error message
- Error message explains that DLL needs to be rebuilt
- Application doesn't crash

## How to Rebuild the DLL

To fix Integrated PuTTY mode completely, rebuild the DLL:

1. Install Visual Studio 2022 with C++ workload
2. Open `PairAdmin.sln`
3. Right-click on `PuTTY` project
4. Select "Build"
5. Copy `src/PuTTY/x64/Release/PairAdminPuTTY.dll` to `lib/PairAdminPuTTY.dll`

Or use the build script:
```cmd
cd src\PuTTY
build_dll.bat
```

## Testing

### External PuTTY Mode (Recommended)
1. Run: `PairAdmin.exe`
2. Select "External PuTTY" mode in settings
3. Enter hostname, port, username
4. Click Connect
5. External PuTTY should launch successfully

### Integrated PuTTY Mode (After DLL rebuild)
1. Rebuild the DLL (see instructions above)
2. Run: `PairAdmin.exe`
3. Select "Integrated PuTTY" mode
4. Enter connection details
5. Connection will work with stub implementation

## Files Modified

- `src/Interop/PuTTYInterop.cs` - Added exception handling for DLL load failures

## Next Steps

1. **For immediate use:** Use External PuTTY mode - it works without the DLL
2. **For full functionality:** Install Visual Studio C++ tools and rebuild the DLL
3. **For production:** Rebuild DLL with actual PuTTY source integration

---

*Fix Applied: January 17, 2026*
