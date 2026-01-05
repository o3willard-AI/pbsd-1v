#ifndef PAIRADMIN_H
#define PAIRADMIN_H

#include <stddef.h>

#ifdef _WIN32
#include <windows.h>
#ifdef __cplusplus
extern "C" {
#endif
#endif

// PairAdmin event types
typedef enum {
    PAIRADMIN_EVENT_OUTPUT = 1,  // Terminal output from SSH
    PAIRADMIN_EVENT_INPUT = 2     // User input to terminal
} PairAdminEventType;

// Callback function type
typedef void (*PairAdminCallback)(PairAdminEventType event, const void *data, size_t len);

// Global callback pointer - to be set by PairAdmin
extern PairAdminCallback pairadmin_callback;

// Function to register callback
extern void pairadmin_set_callback(PairAdminCallback callback);

// Function to get terminal window handle (Windows only)
#ifdef _WIN32
typedef void* HWND;
extern HWND putty_get_terminal_hwnd(void);
#endif

#ifdef _WIN32
#ifdef __cplusplus
}
#endif
#endif

#endif // PAIRADMIN_H
