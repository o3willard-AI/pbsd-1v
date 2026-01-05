using System;
using System.Runtime.InteropServices;

namespace PairAdmin.Interop;

/// <summary>
/// Interop layer for PuTTY library integration
/// This provides P/Invoke declarations for PuTTY functions modified for PairAdmin
/// </summary>
public static class PuTTYInterop
{
    #region PairAdmin Callback Interface

    /// <summary>
    /// PairAdmin event types for terminal I/O
    /// </summary>
    public enum PairAdminEventType : byte
    {
        /// <summary>Terminal output from SSH server (after processing)</summary>
        Output = 1,

        /// <summary>Terminal input from user (before transmission)</summary>
        Input = 2
    }

    /// <summary>
    /// Delegate for PairAdmin callbacks from PuTTY
    /// </summary>
    /// <param name="eventType">Type of event (Output or Input)</param>
    /// <param name="data">Pointer to event data</param>
    /// <param name="length">Length of data in bytes</param>
    public delegate void PairAdminCallback(
        PairAdminEventType eventType,
        IntPtr data,
        int length);

    #endregion

    #region PairAdmin Functions (PuTTY modifications)

    /// <summary>
    /// Register callback for PuTTY events
    /// </summary>
    /// <param name="callback">Callback function to register</param>
    [DllImport("PairAdminPuTTY", CallingConvention = CallingConvention.Cdecl)]
    public static extern void pairadmin_set_callback(
        [MarshalAs(UnmanagedType.FunctionPtr)]
        PairAdminCallback callback);

    /// <summary>
    /// Get PuTTY terminal window handle
    /// </summary>
    /// <returns>Handle to PuTTY terminal window</returns>
    [DllImport("PairAdminPuTTY", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr putty_get_terminal_hwnd();

    #endregion

    #region Windows APIs for Window Management

    /// <summary>
    /// Set parent window for a child window
    /// </summary>
    /// <param name="hWndChild">Child window handle</param>
    /// <param name="hWndNewParent">New parent window handle</param>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    /// <summary>
    /// Set window position and size
    /// </summary>
    /// <param name="hWnd">Window handle</param>
    /// <param name="hWndInsertAfter">Window to place after</param>
    /// <param name="X">X position</param>
    /// <param name="Y">Y position</param>
    /// <param name="cx">Width</param>
    /// <param name="cy">Height</param>
    /// <param name="uFlags">Position flags</param>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags);

    /// <summary>
    /// Set window flags (show/hide, enable/disable)
    /// </summary>
    /// <param name="hWnd">Window handle</param>
    /// <param name="nIndexShow">Show command index</param>
    /// <returns>Previous show state</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    /// <summary>
    /// Get window rectangle
    /// </summary>
    /// <param name="hWnd">Window handle</param>
    /// <param name="lpRect">Rectangle structure</param>
    /// <returns>True if successful</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    /// <summary>
    /// Window rectangle structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    #endregion

    #region ShowWindow Constants

    public static class ShowWindowCommands
    {
        public const int SW_HIDE = 0;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_SHOW = 5;
        public const int SW_MINIMIZE = 6;
        public const int SW_SHOWMINNOACTIVE = 7;
        public const int SW_SHOWNA = 8;
        public const int SW_RESTORE = 9;
        public const int SW_SHOWDEFAULT = 10;
        public const int SW_FORCEMINIMIZE = 11;
        public const int SW_MAX = 11;
        public const int SW_SHOWNA = 12;
        public const int SW_SHOWNA = 13;
        public const int SW_RESTOREMAXIMIZED = 13;
        public const int SW_SHOWNA = 14;
        public const int SW_SHOWNA = 15;
    }

    #endregion

    #region SetWindowPos Flags

    public static class SetWindowPosFlags
    {
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint SWP_NOOWNERZORDER = 0x0200;
        public const uint SWP_SHOWWINDOW = 0x0040;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Register PuTTY callback for terminal events
    /// </summary>
    /// <param name="callback">Callback handler for terminal I/O events</param>
    public static void RegisterCallback(PairAdminCallback callback)
    {
        pairadmin_set_callback(callback);
    }

    /// <summary>
    /// Set parent window relationship
    /// </summary>
    /// <param name="childHandle">Child window handle (PuTTY)</param>
    /// <param name="parentHandle">Parent window handle (TerminalPane)</param>
    public static void SetParentWindow(IntPtr childHandle, IntPtr parentHandle)
    {
        if (childHandle != IntPtr.Zero && parentHandle != IntPtr.Zero)
        {
            SetParent(childHandle, parentHandle);
        }
    }

    /// <summary>
    /// Resize child window to fit in parent
    /// </summary>
    /// <param name="childHandle">Child window handle</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Width</param>
    /// <param name="height">Height</param>
    public static void ResizeChildWindow(IntPtr childHandle, int x, int y, int width, int height)
    {
        if (childHandle != IntPtr.Zero)
        {
            SetWindowPos(
                childHandle,
                IntPtr.Zero,
                x, y,
                width, height,
                SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);
        }
    }

    /// <summary>
    /// Show window (unhide)
    /// </summary>
    /// <param name="handle">Window handle</param>
    public static void Show(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
        {
            ShowWindow(handle, ShowWindowCommands.SW_SHOWNORMAL);
        }
    }

    /// <summary>
    /// Hide window
    /// </summary>
    /// <param name="handle">Window handle</param>
    public static void Hide(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
        {
            ShowWindow(handle, ShowWindowCommands.SW_HIDE);
        }
    }

    /// <summary>
    /// Get window rectangle
    /// </summary>
    /// <param name="handle">Window handle</param>
    /// <returns>Window rectangle (left, top, width, height)</returns>
    public static RECT GetWindowRect(IntPtr handle)
    {
        RECT rect;
        GetWindowRect(handle, out rect);
        return rect;
    }

    #endregion

    #region Logging Helpers

    /// <summary>
    /// Log last Win32 error
    /// </summary>
    /// <returns>Error message</returns>
    public static string GetLastWin32Error()
    {
        int errorCode = Marshal.GetLastWin32Error();
        return $"Win32 Error Code: {errorCode}";
    }

    #endregion
}
