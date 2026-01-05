# Task 10.2 Specification: MSI Installer

## Task: Implement Windows MSI Installer

**Phase:** Phase 10: Deployment & Packaging  
**Status:** In Progress  
**Date:** January 4, 2026  
**Prerequisites:** Task 10.1 Complete

---

## Description

Create a Windows MSI installer for PairAdmin using WiX Toolset, including installation UI, shortcuts, registry entries, and uninstall support.

---

## Deliverables

### 1. installer/PairAdmin.wxs
Main installer source file:
- Feature definition
- Directory structure
- Component registration
- Custom actions

### 2. installer/Product.wxs
Product configuration:
- Version information
- Upgrade rules
- License agreement
- UI customization

### 3. installer/EULA.rtf
End User License Agreement

### 4. installer/UI.wxs
Custom UI sequence

---

## Requirements

### Features

| Feature | Description |
|---------|-------------|
| Main Application | Core PairAdmin application |
| Documentation | Help files and guides |
| Shortcuts | Desktop and Start Menu |
| File Associations | None (no file types) |

### Installation Requirements

| Requirement | Description |
|-------------|-------------|
| OS | Windows 10/11 |
| .NET | .NET 8.0 Runtime |
| Disk Space | ~50 MB |
| Admin Rights | Not required |

### Shortcuts

| Shortcut | Location | Description |
|----------|----------|-------------|
| PairAdmin | Desktop | Launch application |
| PairAdmin | Start Menu | Launch application |
| Documentation | Start Menu | Open help docs |
| Uninstall | Start Menu | Remove application |

### Registry Entries

| Key | Value | Description |
|-----|-------|-------------|
| HKCU\Software\PairAdmin | InstallPath | Installation directory |
| HKCU\Software\PairAdmin | Version | Version number |
| HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\PairAdmin | DisplayName | App name |
| HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\PairAdmin | UninstallString | Command |

---

## WiX Configuration

### Product.wxs

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="*" 
           Name="PairAdmin" 
           Language="1033" 
           Version="$(var.ProductVersion)" 
           Manufacturer="PairAdmin Team" 
           UpgradeCode="PUT-GUID-HERE">
    
    <Package InstallerVersion="200" 
             Compressed="yes" 
             InstallScope="perUser" />
    
    <MajorUpgrade 
        DowngradeErrorMessage="A newer version is already installed." />
    
    <MediaTemplate EmbedCab="yes" />
    
    <!-- Features -->
    <Feature Id="ProductFeature" Title="PairAdmin" Level="1">
      <ComponentGroupRef Id="ProductComponents" />
      <ComponentRef Id="ApplicationShortcutDesktop" />
      <ComponentRef Id="ApplicationShortcutStartMenu" />
    </Feature>
    
    <!-- License Agreement -->
    <Property Id="WIX_LICENSE_RTF" Value="EULA.rtf" />
    
    <!-- Installation UI -->
    <UIRef Id="WixUI_InstallDir" />
    
    <Property Id="WIXUI_INSTALLDIR" Value="INSTALLFOLDER" />
    
  </Product>

  <Fragment>
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFilesFolder">
        <Directory Id="INSTALLFOLDER" Name="PairAdmin" />
      </Directory>
      <Directory Id="ProgramMenuFolder">
        <Directory Id="ApplicationProgramsFolder" Name="PairAdmin" />
      </Directory>
      <Directory Id="DesktopFolder" Name="Desktop" />
    </Directory>
  </Fragment>
</Wix>
```

### Components.wxs (auto-generated)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Fragment>
    <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
      <!-- Application -->
      <Component Guid="*">
        <File Source="..\bin\Release\net8.0-windows\PairAdmin.exe" />
      </Component>
      
      <!-- Dependencies -->
      <Component Guid="*">
        <File Source="..\bin\Release\net8.0-windows\*.dll" />
      </Component>
      
      <!-- Configuration -->
      <Component Guid="*">
        <File Source="..\bin\Release\net8.0-windows\appsettings.json" />
      </Component>
      
      <!-- Help Files -->
      <Component Guid="*">
        <DirectoryCreate />
        <File Source="docs\*.md" />
      </Component>
    </ComponentGroup>

    <!-- Desktop Shortcut -->
    <DirectoryRef Id="DesktopFolder">
      <Component Id="ApplicationShortcutDesktop" Guid="*">
        <Shortcut Id="ApplicationShortcutDesktop"
                  Name="PairAdmin"
                  Description="AI-Assisted Terminal Administration"
                  Target="[INSTALLFOLDER]PairAdmin.exe"
                  WorkingDirectory="INSTALLFOLDER" />
        <RemoveFolder Id="CleanUpShortCut" Directory="DesktopFolder" On="uninstall" />
        <RegistryValue Root="HKCU" Key="Software\PairAdmin" 
                       Name="installed" Type="integer" Value="1" KeyPath="yes" />
      </Component>
    </DirectoryRef>

    <!-- Start Menu Shortcuts -->
    <DirectoryRef Id="ApplicationProgramsFolder">
      <Component Id="ApplicationShortcutStartMenu" Guid="*">
        <Shortcut Id="ApplicationStartMenuShortcut"
                  Name="PairAdmin"
                  Description="AI-Assisted Terminal Administration"
                  Target="[INSTALLFOLDER]PairAdmin.exe"
                  WorkingDirectory="INSTALLFOLDER" />
        <Shortcut Id="ApplicationUninstallShortcut"
                  Name="Uninstall PairAdmin"
                  Description="Remove PairAdmin from your computer"
                  Target="[System64Folder]msiexec.exe"
                  Arguments="/x [ProductCode]" />
        <RemoveFolder Id="CleanUpStartMenuFolder" Directory="ApplicationProgramsFolder" On="uninstall" />
        <RegistryValue Root="HKCU" Key="Software\PairAdmin" 
                       Name="installed" Type="integer" Value="1" KeyPath="yes" />
      </Component>
    </DirectoryRef>
  </Fragment>
</Wix>
```

---

## Build Commands

```bash
# Install WiX
choco install wixtoolset -y

# Generate components
heat.exe dir bin\Release\net8.0-windows -dr INSTALLFOLDER -cg PairAdminComp -srd -srl -ag -var var.SourceDir -out installer\Components.wxs

# Build installer
candle.exe -dVersion=1.0.0 -dSourceDir=bin\Release\net8.0-windows installer\Product.wxs installer\Components.wxs

# Link installer
light.exe -dWixUILicenseRtf=installer\EULA.rtf -o installer\PairAdmin-1.0.0.msi Product.wixobj Components.wixobj
```

---

## Files Created

```
installer/
├── PairAdmin.wxs              # Main installer (100 lines)
├── Product.wxs                # Product config (80 lines)
├── Components.wxs             # Components (auto-generated)
├── UI.wxs                     # Custom UI (60 lines)
└── EULA.rtf                   # License agreement
```

---

## Estimated Complexity

| File | Complexity | Lines |
|------|------------|-------|
| PairAdmin.wxs | Medium | ~100 |
| Product.wxs | Low | ~80 |
| Components.wxs | Low | ~60 |
| UI.wxs | Low | ~60 |

**Total Estimated:** ~300 lines of WiX XML

---

## Next Steps

After Task 10.2 is complete:
1. Task 10.3: NuGet Packages
2. Phase 10 Complete Summary

---

## Notes

- Use WiX 4.0 for modern features
- Test on Windows 10 and 11
- Verify uninstall removes all files
- Include EULA in installer
- Test upgrade scenario
