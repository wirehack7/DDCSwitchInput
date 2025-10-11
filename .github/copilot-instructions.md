# Copilot Instructions for DDCSwitchInput

## Project Overview

DDCSwitchInput is a Windows system tray application that allows users to switch display input sources using DDC/CI (Display Data Channel Command Interface) protocol. Users can select a monitor and input source from the tray icon menu, then apply the change with a double-click or menu selection.

## Technology Stack

- **Language**: C# with .NET 8.0
- **Framework**: Windows Forms (WinForms)
- **Target Platform**: Windows (net8.0-windows)
- **Build**: Self-contained single-file executable
- **IDE**: Visual Studio 2022 or compatible

## Project Structure

- `src/` - Source code directory
  - `Program.cs` - Application entry point with single-instance mutex
  - `TrayApp.cs` - Main application context managing the system tray icon and menu
  - `MonitorHelper.cs` - DDC/CI communication and monitor enumeration
  - `EdidReader.cs` - EDID data parsing to retrieve monitor names
  - `AutoStart.cs` - Windows startup registration via Registry
  - `Resources/` - Embedded resources (icon)

## Build Instructions

```bash
# Build the project
dotnet build src/DdcTraySwitcher.csproj

# Publish as single-file executable
dotnet publish src/DdcTraySwitcher.csproj -c Release
```

The published executable will be in `src/publish/DdcTraySwitcher.exe`.

## Important Technical Details

### DDC/CI Protocol
- Uses Windows `dxva2.dll` for DDC/CI communication
- VCP (Virtual Control Panel) code `0x60` controls input source selection
- Monitor capability detection includes 300ms timeout to prevent hangs
- Not all monitors support DDC/CI - the app filters to DDC-capable monitors only

### Windows Registry Usage
- User settings stored in `HKCU\Software\DdcTraySwitcher`
  - `SelectedMonitor` (DWORD) - Index of selected monitor
  - `SelectedInput` (DWORD) - Input source value (e.g., 0x11 for HDMI 1)
- Autostart registry key: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`

### EDID Reading
- Reads monitor names from Windows Registry at `HKLM\SYSTEM\CurrentControlSet\Enum\DISPLAY`
- Parses EDID descriptor blocks (offsets 0x36-0x6C) for display names
- Falls back to "Monitor N" if name cannot be determined

### Single Instance
- Uses a global mutex (`Global\DdcTraySwitcherMutex`) to ensure only one instance runs

## Code Style and Conventions

- Use C# naming conventions (PascalCase for public members, camelCase for private)
- Minimal comments - code should be self-documenting where possible
- Keep error handling with user-friendly MessageBox dialogs
- Use `using` statements for IDisposable resources
- Prefer modern C# features (target expressions, pattern matching, etc.)

## Common Input Values

Standard DDC/CI input source codes:
- `0x0F` - DisplayPort 1
- `0x10` - DisplayPort 2
- `0x11` - HDMI 1
- `0x12` - HDMI 2
- `0x01` - VGA/Analog
- `0x03` - DVI

## Testing

This project currently has no automated test infrastructure. When making changes:
- Manually test on Windows with DDC/CI-compatible monitors
- Verify the system tray icon appears and menu functions correctly
- Test monitor detection and input switching
- Check autostart registration/unregistration
- Ensure single-instance behavior works

## Special Considerations

- **Windows-only**: This application uses Windows-specific APIs and will not work on other platforms
- **Monitor compatibility**: DDC/CI support varies by monitor manufacturer and model
- **Timeout handling**: Monitor communication can hang on some systems, hence the 300ms timeout
- **Icon licensing**: The application icon is licensed under CC BY-NC-ND 4.0 (see README.md)

## Dependencies

The project uses only .NET 8.0 standard libraries:
- `System.Windows.Forms`
- `Microsoft.Win32` (Registry access)
- `System.Runtime.InteropServices` (P/Invoke for native APIs)

No external NuGet packages are required.

## Making Changes

When contributing:
1. Maintain the minimal, focused nature of the codebase
2. Test changes on actual hardware with DDC/CI monitors when possible
3. Follow the existing error handling patterns (MessageBox for user errors)
4. Keep the UI simple and consistent with the existing tray menu design
5. Document any new DDC/CI codes or monitor-specific behavior
6. Be mindful of the icon licensing restrictions (CC BY-NC-ND 4.0)
