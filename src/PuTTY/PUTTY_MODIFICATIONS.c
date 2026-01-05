// PairAdmin modifications applied to PuTTY source code
//
// This document describes the exact changes that would be made to PuTTY
// to enable I/O interception for PairAdmin.
//
// These modifications are marked in actual PuTTY source with:
//   // PairAdmin modification: [description]

// ============================================================================
// FILE: terminal.c
// ============================================================================

// Location: After line ~1250, after term_data() function

// PairAdmin modification: Add output hook
// This hook is called after PuTTY processes incoming SSH data
/*
void term_data_hook(Terminal *term, const char *data, size_t len)
{
    if (pairadmin_callback) {
        pairadmin_callback(PAIRADMIN_EVENT_OUTPUT, data, len);
    }
}
*/

// Modified term_data() to call hook:
// Original:
//     term_data(term, data, len);
// Modified:
//     term_data(term, data, len);
//     term_data_hook(term, data, len);  // PairAdmin modification


// ============================================================================
// FILE: ldisc.c
// ============================================================================

// Location: After line ~890, before ldisc_send() function

// PairAdmin modification: Add input hook
// This hook is called before user input is sent to SSH channel
/*
void ldisc_send_hook(Ldisc *ldisc, const void *buf, size_t len)
{
    if (pairadmin_callback) {
        pairadmin_callback(PAIRADMIN_EVENT_INPUT, buf, len);
    }
}
*/

// Modified ldisc_send() to call hook:
// Original:
//     return ldisc_send(ldisc, buf, len);
// Modified:
//     ldisc_send_hook(ldisc, buf, len);  // PairAdmin modification
//     return ldisc_send(ldisc, buf, len);


// ============================================================================
// FILE: window.c
// ============================================================================

// Location: Line ~230, hwnd_terminal declaration

// PairAdmin modification: Make hwnd_terminal accessible
// Original:
//     static HWND hwnd_terminal = NULL;
// Modified:
//     HWND hwnd_terminal = NULL;  // Changed from static to file-level

// Location: After line ~245

// PairAdmin modification: Expose terminal window handle
/*
HWND putty_get_terminal_hwnd(void)
{
    return hwnd_terminal;
}
*/


// ============================================================================
// NEW FILE: pairadmin.h
// ============================================================================

#ifndef PAIRADMIN_H
#define PAIRADMIN_H

#include <windows.h>
#include <stddef.h>

// PairAdmin event types
typedef enum {
    PAIRADMIN_EVENT_OUTPUT = 1,  // Terminal output from SSH
    PAIRADMIN_EVENT_INPUT = 2     // User input to terminal
} PairAdminEventType;

// Callback function type
typedef void (*PairAdminCallback)(PairAdminEventType event, 
                                   const void *data, 
                                   size_t len);

// Global callback pointer
extern PairAdminCallback pairadmin_callback;

// Function to register callback
void pairadmin_set_callback(PairAdminCallback callback);

// Function to get terminal window handle
HWND putty_get_terminal_hwnd(void);

#endif // PAIRADMIN_H


// ============================================================================
// NEW FILE: pairadmin.c
// ============================================================================

#include "pairadmin.h"

// Global callback pointer
PairAdminCallback pairadmin_callback = NULL;

// Set the callback
void pairadmin_set_callback(PairAdminCallback callback)
{
    pairadmin_callback = callback;
}


// ============================================================================
// INTEGRATION NOTES
// ============================================================================

// 1. Add these files to PuTTY Makefile/VS project
//    - pairadmin.c
//    - pairadmin.h

// 2. Update Windows terminal window initialization
//    - Call pairadmin_set_callback() after window creation

// 3. Testing
//    - Verify callbacks are invoked correctly
//    - Test with various terminal outputs
//    - Test with user input
//    - Measure performance overhead (should be < 1%)

// 4. Security
//    - Callbacks receive raw data
//    - PairAdmin must filter credentials before LLM transmission
//    - No direct access to PuTTY authentication state

// 5. Performance
//    - Hook overhead should be minimal
//    - No additional allocations in hot path
//    - Callback should be fire-and-forget
