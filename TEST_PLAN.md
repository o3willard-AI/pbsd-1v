# PairAdmin Test Plan

## Build Information
- **Build Date:** January 17, 2026
- **Location:** src/PairAdmin/bin/Release/net8.0-windows/PairAdmin.exe
- **Configuration:** Release, .NET 8.0, Windows x64
- **PairAdminPuTTY.dll:** Stub implementation (placeholder, not actual PuTTY)

## Test Scenarios

### 1. Application Startup
**Steps:**
1. Navigate to: `src\PairAdmin\bin\Release\net8.0-windows\`
2. Run: `PairAdmin.exe`
3. Wait for main window to load

**Expected:**
- Window opens successfully
- UI elements load (chat pane, terminal placeholder, command input)
- No crash or error messages

**If Fail:** Check `C:\Users\Stephen Blankenship\AppData\Local\PairAdmin\pairadmin_csharp.log`

---

### 2. Terminal Mode Configuration
**Steps:**
1. Click "Settings" button or press `/config`
2. Verify terminal mode options appear

**Expected:**
- Three terminal modes available:
  - **Not Configured** (default on first run)
  - **Integrated PuTTY** (uses PairAdminPuTTY.dll)
  - **External PuTTY** (uses putty.exe)
- Mode selection works
- Settings are saved

---

### 3. External PuTTY Mode Test (Recommended for Testing)
**Steps:**
1. Select "External PuTTY" mode in settings
2. Enter connection details:
   - Hostname: Your SSH server
   - Port: 22 (or your SSH port)
   - Username: Your SSH username
3. Click "Connect"

**Expected:**
- External PuTTY executable launches
- PuTTY window opens
- Connection bar shows "Connected to [hostname]"
- Terminal appears in window (may be separate window)

**Note:** External mode uses your installed PuTTY, so should work correctly.

---

### 4. Integrated PuTTY Mode Test (Stub Implementation)
**Steps:**
1. Select "Integrated PuTTY" mode in settings
2. Enter connection details
3. Click "Connect"

**Expected Behavior (Stub):**
- DLL is loaded successfully
- Connect call succeeds (stub returns success)
- State shows "Connected"
- **Limitation:** No actual terminal window appears (stub implementation)
- Connection status updates properly

**Expected Errors:**
- No crash
- DLL loads correctly (all functions found)
- No "function not found" errors

**Log Files to Check:**
- `C:\Users\Stephen Blankenship\AppData\Local\PairAdmin\pairadmin_csharp.log`
- `C:\Users\Stephen Blankenship\AppData\Local\PairAdmin\pairadmin_dll.log` (may be empty in stub)

---

### 5. Slash Commands Test
**Test these commands:**
```
/help          - Show help
/config        - Open settings
/clear          - Clear chat
/status         - Show status
/mode           - Switch terminal mode
```

**Expected:**
- Commands execute without errors
- Help displays properly
- Settings dialog opens

---

### 6. AI Integration Test (Optional)
**Steps:**
1. Open appsettings.json
2. Add OpenAI API key:
```json
"OpenAI": {
  "ApiKey": "sk-your-actual-key-here"
}
```
3. Restart application
4. Try: "help me set up a firewall rule"

**Expected:**
- AI responds with helpful command
- Chat pane shows AI response

**Without API Key:**
- App starts without AI (graceful degradation)
- Terminal and commands still work

---

## Known Limitations

### Integrated PuTTY Mode (Stub)
- ❌ No actual SSH connection (simulated)
- ❌ No terminal window display
- ✅ State management works
- ✅ DLL interop works
- ✅ Callback system works

### External PuTTY Mode (Should Work)
- ✅ Uses installed PuTTY
- ✅ Real SSH connections
- ✅ Terminal window integration (attempted via SetParent)
- ⚠️  Window embedding may not work perfectly (depends on PuTTY version)

---

## Log File Locations

### C# Log
`C:\Users\Stephen Blankenship\AppData\Local\PairAdmin\pairadmin_csharp.log`

### DLL Log
`C:\Users\Stephen Blankenship\AppData\Local\PairAdmin\pairadmin_dll.log`

---

## Success Criteria

### Minimum Success
- [ ] Application starts without crash
- [ ] Settings dialog opens and closes
- [ ] Terminal mode can be switched
- [ ] External PuTTY launches (if PuTTY installed)

### Full Success
- [ ] External PuTTY connects successfully
- [ ] Terminal window integrates into UI
- [ ] Commands execute properly
- [ ] AI integration works (with API key)

### Known Issues Expected
- Integrated PuTTY mode shows placeholder (no real terminal)
- Window embedding with external PuTTY may be incomplete

---

## Next Steps After Testing

1. **If External PuTTY works:**
   - Document working features
   - Focus on improving window embedding

2. **If DLL crashes:**
   - Check pairadmin_csharp.log for error details
   - Verify DLL exports match P/Invoke declarations

3. **If Application crashes on startup:**
   - Check .NET runtime requirements
   - Verify all dependencies present

---

*Test Plan Created: January 17, 2026*
