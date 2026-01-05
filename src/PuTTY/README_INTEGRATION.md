# PuTTY Integration Guide

## Overview

This directory contains the PuTTY source code that has been modified to support PairAdmin's I/O interception and callback system.

## Integration Points

The following modifications have been made to PuTTY to enable PairAdmin functionality:

### 1. Terminal Output Hook (`terminal.c`)

**Location:** `terminal.c`

**Modification:** Added `term_data_hook` callback function that is called after PuTTY processes incoming SSH data.

**Purpose:** Capture terminal output for AI context without modifying display logic.

```c
// terminal.c
void term_data_hook(Terminal *term, const char *data, size_t len)
{
    if (pairadmin_callback) {
        pairadmin_callback(PAIRADMIN_EVENT_OUTPUT, data, len);
    }
}
```

### 2. Terminal Input Hook (`ldisc.c`)

**Location:** `ldisc.c`

**Modification:** Added `ldisc_send_hook` callback function that is called before user input is sent to the SSH channel.

**Purpose:** Capture user commands for logging and AI suggestions.

```c
// ldisc.c
void ldisc_send_hook(Ldisc *ldisc, const void *buf, size_t len)
{
    if (pairadmin_callback) {
        pairadmin_callback(PAIRADMIN_EVENT_INPUT, buf, len);
    }
}
```

### 3. Window Handle Exposure (`window.c`)

**Location:** `window.c`

**Modification:** Added `putty_get_terminal_hwnd` function to expose the PuTTY terminal window handle.

**Purpose:** Allow PairAdmin to embed the PuTTY terminal as a child window.

```c
// window.c
HWND putty_get_terminal_hwnd(void)
{
    return hwnd_terminal;
}
```

## Build Instructions

### Prerequisites

- Visual Studio 2022 with C++ desktop development workload
- CMake 3.20 or later
- Windows SDK 10.0 or later

### Building

```cmd
cd src\PuTTY
msbuild PuTTY.vcxproj /p:Configuration=Release /p:Platform=x64
```

The output will be:
- Debug: `bin\x64\Debug\PuTTY.lib`
- Release: `bin\x64\Release\PuTTY.lib`

## Integration with PairAdmin

The static library (`PuTTY.lib`) is linked with the PairAdmin C#/C++ interop layer defined in the `Interop` project.

### Interop Layer

The `Interop` project provides C# wrappers for the PuTTY C functions:

- `PairAdminCallbacks.cs` - C# delegate definitions
- `PairAdminNative.cs` - P/Invoke declarations
- `WindowHostHelper.cs` - Window handle management

## Callback Events

The PairAdmin callback system exposes the following events:

| Event Type | Description | Direction |
|------------|-------------|------------|
| `PAIRADMIN_EVENT_OUTPUT` | Terminal output from SSH | Server → Client |
| `PAIRADMIN_EVENT_INPUT` | User input to terminal | Client → Server |

## Security Considerations

### Credential Isolation

PairAdmin does not have access to:
- SSH private keys (handled by Pageant)
- Passwords (filtered before callbacks)
- Authentication tokens

### Data Flow

```
SSH Server → PuTTY (decrypt) → term_data_hook → PairAdmin (filtered) → LLM
User Input → ldisc_send_hook → PairAdmin (logged) → SSH Channel
```

## Future Enhancements

Potential areas for future PuTTY modifications:

1. Terminal state query API (cursor position, screen content)
2. ANSI escape sequence parsing hooks
3. Session state callbacks (connection, disconnection)
4. File transfer progress callbacks

## Notes

- PuTTY source is licensed under the MIT license
- All modifications are clearly marked with `// PairAdmin modification` comments
- Original PuTTY functionality is preserved
- Modifications are isolated to specific files for easier maintenance

## References

- PuTTY Source: https://www.chiark.greenend.org.uk/~sgtatham/putty/
- PuTTY License: https://www.chiark.greenend.org.uk/~sgtatham/putty/licence.html
