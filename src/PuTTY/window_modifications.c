/*
 * window.c - PuTTY Window Handle Hook (PairAdmin Modification)
 *
 * This file demonstrates the modifications to be made to PuTTY's window.c
 * to expose the terminal window handle for embedding in PairAdmin.
 *
 * PairAdmin Integration Point:
 *   Add putty_get_terminal_hwnd() function to return terminal window handle
 *   Change hwnd_terminal from static to file-level static for external access
 *
 * Location: After line ~230 for hwnd_terminal declaration, add new function
 */

/* Original PuTTY code would have:

static HWND hwnd_terminal = NULL;

// ... other code ...
*/

/* MODIFIED CODE 1: Change hwnd_terminal visibility:

// PairAdmin modification: Make hwnd_terminal file-level static
// Change from:
//   static HWND hwnd_terminal = NULL;
// To:
HWND hwnd_terminal = NULL;

/* MODIFIED CODE 2: Add new function at end of file:

// PairAdmin modification: Add function to get terminal window handle
HWND putty_get_terminal_hwnd(void)
{
    return hwnd_terminal;
}

/* END MODIFICATIONS */

/*
 * Notes for Integration:
 *
 * 1. In window.c, locate hwnd_terminal declaration (around line 230)
 * 2. Change from static HWND hwnd_terminal to HWND hwnd_terminal
 * 3. Add putty_get_terminal_hwnd() function at end of file
 * 4. Ensure pairadmin.h is included in window.c
 * 5. Compile PuTTY with PAIRADMIN_INTEGRATION defined
 *
 * Effect: PairAdmin can retrieve the PuTTY terminal window handle via
 * putty_get_terminal_hwnd(). This allows embedding the PuTTY window
 * as a child window in PairAdmin's WPF application using WindowsFormsHostElement
 * or SetParent() Win32 API.
 *
 * Window Parent-Child Relationship:
 * - Parent: PairAdmin's MainWindow (WPF window)
 * - Child: PuTTY terminal window
 * - Operations:
 *   - SetParent(): Establish parent-child relationship
 *   - SetWindowPos(): Position child window
 *   - Resize child: Synchronize sizes
 *   - Focus management: Transfer focus to child
 *   - Cleanup: Destroy child when parent is destroyed
 *
 * Performance Impact:
 * - No performance impact
 * - Single function call to get handle
 * - Window handle already exists, just changed visibility
 *
 * Testing:
 * - putty_get_terminal_hwnd() returns valid handle
 * - Window can be successfully embedded as child
 * - Window resize synchronization works
 * - Parent window can control child window lifecycle
 *
 * Security Considerations:
 * - PairAdmin should not attempt to modify PuTTY's internal state
 * - Window handle should be cached to avoid repeated queries
 * - Handle should be validated (HWND != NULL) before use
 *
 * Alternative Implementation (SetParent):
 * - Instead of WindowsFormsHostElement, can use SetParent() Win32 API
 * - More flexible but requires more interop code
 * - See PuTTYInterop.cs for SetParent() implementation
 *
 * Known Limitations:
 * - hwnd_terminal should only be accessed when PuTTY window is created
 * - Handle may become invalid if PuTTY window is destroyed
 * - Thread-safety considerations when accessing from multiple threads
 */
