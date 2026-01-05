# PuTTY Integration for PairAdmin

This directory contains the PairAdmin modifications and interface layer for integrating with PuTTY.

## Structure

### Source Files

- **pairadmin.h** - Public header defining PairAdmin callback interface
- **pairadmin.c** - Implementation of callback registration
- **CMakeLists.txt** - CMake build configuration

### Integration Strategy

#### Option A: Source-Level Integration (Recommended)

For production deployment, the actual PuTTY source code will be modified with these callback points:

1. **terminal.c** - Add `term_data_hook` after terminal output processing
2. **ldisc.c** - Add `ldisc_send_hook` before user input transmission
3. **window.c** - Expose terminal window handle via `putty_get_terminal_hwnd`

#### Option B: Stub Implementation (Current)

For development and testing on non-Windows platforms, a stub implementation is provided:
- Callbacks can be registered and invoked manually
- No actual PuTTY integration
- Allows development of C# interop layer

## Building

### On Linux/macOS (Development)

```bash
mkdir build
cd build
cmake ..
make
```

Output: `libPairAdminPuTTY.a` (static library)

### On Windows (Production)

```cmd
mkdir build
cd build
cmake -G "Visual Studio 17 2022" ..
cmake --build . --config Release
```

Output: `PairAdminPuTTY.lib` (static library)

## Usage in PairAdmin

The C# Interop layer (`Interop` project) will:

1. P/Invoke `pairadmin_set_callback` to register callback handler
2. Implement callback delegate that marshals to C#
3. Call `putty_get_terminal_hwnd` to embed PuTTY window

## Example Integration

```csharp
// In Interop project
[DllImport("PairAdminPuTTY", CallingConvention = CallingConvention.Cdecl)]
public static extern void pairadmin_set_callback(IntPtr callback);

// Register callback
var callback = new PairAdminCallback(OnTerminalEvent);
pairadmin_set_callback(Marshal.GetFunctionPointerForDelegate(callback));
```

## Notes

- This is a placeholder/stub for the actual PuTTY modifications
- In production, actual PuTTY source code will be integrated
- Windows-only APIs (HWND) are conditionally compiled
- For cross-platform development, stubs are provided

## Next Steps

1. Complete PuTTY source modifications (actual PuTTY integration)
2. Update CMakeLists.txt to build with actual PuTTY sources
3. Test on Windows with real PuTTY instance
4. Verify callback performance overhead < 1%

---

*Created: January 4, 2026*
