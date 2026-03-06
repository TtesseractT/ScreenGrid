<p align="center">
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-blue?logo=windows" alt="Platform">
  <img src="https://img.shields.io/badge/.NET-9.0-purple?logo=dotnet" alt=".NET 9">
  <img src="https://img.shields.io/github/license/TtesseractT/ScreenGrid" alt="License">
</p>

# ScreenGrid - Window Manager

<p align="center">
  <a href="https://github.com/TtesseractT/ScreenGrid/releases/latest/download/ScreenGridSetup.exe">
    <img src="https://img.shields.io/badge/⬇_Download_Installer-66_MB-blue?style=for-the-badge&logo=windows" alt="Download Installer">
  </a>
  &nbsp;
  <a href="https://github.com/TtesseractT/ScreenGrid/releases/latest/download/ScreenGrid-standalone.exe">
    <img src="https://img.shields.io/badge/⬇_Standalone_EXE-71_MB-green?style=for-the-badge" alt="Download Standalone">
  </a>
  &nbsp;
  <a href="https://github.com/TtesseractT/ScreenGrid/releases/latest/download/ScreenGrid-small.exe">
    <img src="https://img.shields.io/badge/⬇_Small_EXE_(needs_.NET)-200_KB-orange?style=for-the-badge" alt="Download Small">
  </a>
</p>

A lightweight, open-source window-snapping tool for **ultrawide monitors** on Windows. Hold **Shift** while dragging any window to see a customizable grid overlay, then drop onto a zone to snap instantly.

Built for displays like 5120×1440 where Windows' built-in snap (halves/quarters) leaves too much wasted space.

![ScreenGrid overlay demo](docs/demo.png)
<!-- Replace with an actual screenshot once you have one -->

---
## ⬇️ Download

> **[Download the latest release](https://github.com/TtesseractT/ScreenGrid/releases/latest)** - no build tools needed.

| File | Size | Description |
|------|------|-------------|
| **ScreenGridSetup.exe** | ~66 MB | **Installer** - installs to Program Files, adds to Windows startup, creates Start Menu & optional desktop shortcut. Includes uninstaller. |
| **ScreenGrid-standalone.exe** | ~70 MB | Portable single-file exe, no .NET required |
| **ScreenGrid-small.exe** | ~200 KB | Portable single-file exe, needs [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) |

**Recommended:** Use the installer for an automatic startup experience.

---
## Features

- **Shift + Drag** to activate - zero interference with normal window management
- **11 built-in grid rows**: Halves, Thirds, 4:3 (left/center/right), Quarters, Fifths - each with a ½-height variant
- **Height splits**: top/bottom halves for partial-height zones
- **Scroll to page** through grid variants (2 rows per page) with dot pagination indicators
- **Custom grids** - create any ratio (2:1, 3:2:1, 16:9, etc.) via the built-in editor
- **Save / Load** grid layouts as `.screengrid` JSON files
- **Customizable colors** - per-element RGB sliders for zone, highlight, and snap preview colors with real-time preview
- **Full-height snap preview** with pixel dimensions shown on hover
- **Smart window filtering** - only activates for real app windows, ignores games and fullscreen apps
- **DPI-aware** (PerMonitorV2) - works on mixed-DPI multi-monitor setups
- **Click-through overlay** - never steals focus or interferes with your drag
- **System tray only** - no visible window, runs silently in the background
- **Run at Startup** toggle - enable/disable from the tray menu or via the installer
- **Windows installer** - proper install/uninstall with auto-startup support
- **~200 KB** framework-dependent exe (or ~70 MB self-contained)

---

## Quick Start

### Option 1: Download a Release

> **[⬇️ Download the latest release](https://github.com/TtesseractT/ScreenGrid/releases/latest)** and run the `.exe`. It sits in your system tray.

### Option 2: Build from Source

**Prerequisites:** [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0), Windows 10+

```powershell
git clone https://github.com/TtesseractT/ScreenGrid.git
cd ScreenGrid
dotnet run -c Release
```

---

## Usage

| Action | Result |
|--------|--------|
| **Shift + Drag** a window | Grid overlay appears |
| **Scroll wheel** while overlay is shown | Page through grid variant pairs |
| **Hover** over a zone | Zone highlights, full-height snap preview shown |
| **Release mouse** on a zone | Window snaps to that column (full height) |
| **Release Shift** while dragging | Cancel - overlay hides, no snap |

### System Tray Menu (right-click)

| Option | Description |
|--------|-------------|
| **Create / Edit Grid** | Open the grid editor to add, remove, reorder rows |
| **Load Grid from File…** | Import a `.screengrid` JSON layout |
| **Reset Grid to Defaults** | Restore all 5 built-in grid rows |
| **Run at Startup** | Toggle Windows startup registration (checked = enabled) |
| **How to use** | Quick usage guide |
| **Exit** | Close ScreenGrid |

---

## Custom Grids

Right-click the tray icon → **Create / Edit Grid** to open the editor:

- Use **preset buttons** to quickly add common rows (Halves, Thirds, 4:3, etc.)
- Click **+ Custom…** to enter any ratio - e.g. `3:2:1` or `16:9`
- **Reorder** rows with ▲/▼ - top row appears at the top of the overlay
- **Rename** rows to anything you like
- Click **Apply & Close** to activate immediately
- Click **Save to File…** to export and share your layout

Grid configs are stored as simple JSON:

```json
{
  "name": "My Layout",
  "rows": [
    { "name": "HALVES", "ratios": [1, 1] },
    { "name": "HALVES ½H", "ratios": [1, 1], "heightRatios": [1, 1] },
    { "name": "Wide + Sidebar", "ratios": [3, 1] },
    { "name": "THIRDS", "ratios": [1, 1, 1] },
    { "name": "16:9 Split", "ratios": [16, 9] }
  ]
}
```

---

## Default Grid Layout (5120 × 1440)

The overlay shows **2 rows at a time**. Scroll the mouse wheel to page through all 11 rows.

```
 Page 1 (scroll ↑↓ to change)
┌──────────────────────────────────────────────────────────────┐
│  HALVES (full height)                                        │
│  ┌────────────────────────┐ ┌────────────────────────┐       │
│  │     1/2  (2560px)      │ │     2/2  (2560px)      │       │
│  └────────────────────────┘ └────────────────────────┘       │
├──────────────────────────────────────────────────────────────┤
│  HALVES ½H (top/bottom split)                                │
│  ┌───────────┐ ┌───────────┐                                 │
│  │ 1/2 Top   │ │ 2/2 Top   │                                 │
│  ├───────────┤ ├───────────┤                                 │
│  │ 1/2 Bot   │ │ 2/2 Bot   │                                 │
│  └───────────┘ └───────────┘                                 │
└──────────────────────────────────────────────────────────────┘

 Page 2
┌──────────────────────────────────────────────────────────────┐
│  THIRDS (full height)                                        │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐         │
│  │    1/3       │ │    2/3       │ │    3/3       │         │
│  └──────────────┘ └──────────────┘ └──────────────┘         │
├──────────────────────────────────────────────────────────────┤
│  THIRDS ½H (top/bottom split)                                │
│  ┌────────┐ ┌────────┐ ┌────────┐                           │
│  │1/3 Top │ │2/3 Top │ │3/3 Top │                           │
│  ├────────┤ ├────────┤ ├────────┤                           │
│  │1/3 Bot │ │2/3 Bot │ │3/3 Bot │                           │
│  └────────┘ └────────┘ └────────┘                           │
└──────────────────────────────────────────────────────────────┘

 Page 3
┌──────────────────────────────────────────────────────────────┐
│  4:3 LEFT                                                    │
│  ┌─────────────────┐ ┌────────────┐                         │
│  │       4         │ │     3      │                         │
│  └─────────────────┘ └────────────┘                         │
├──────────────────────────────────────────────────────────────┤
│  4:3 CENTER                                                  │
│  ┌─────────┐ ┌─────────────┐ ┌─────────┐                   │
│  │    3    │ │      4      │ │    3    │                   │
│  └─────────┘ └─────────────┘ └─────────┘                   │
└──────────────────────────────────────────────────────────────┘

 Page 4
┌──────────────────────────────────────────────────────────────┐
│  4:3 RIGHT                                                   │
│  ┌────────────┐ ┌─────────────────┐                         │
│  │     3      │ │       4         │                         │
│  └────────────┘ └─────────────────┘                         │
├──────────────────────────────────────────────────────────────┤
│  QUARTERS (full height)                                      │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐       │
│  │   1/4    │ │   2/4    │ │   3/4    │ │   4/4    │       │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘       │
└──────────────────────────────────────────────────────────────┘

 Page 5
┌──────────────────────────────────────────────────────────────┐
│  QUARTERS ½H (top/bottom split)                              │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐                       │
│  │1/4 T │ │2/4 T │ │3/4 T │ │4/4 T │                       │
│  ├──────┤ ├──────┤ ├──────┤ ├──────┤                       │
│  │1/4 B │ │2/4 B │ │3/4 B │ │4/4 B │                       │
│  └──────┘ └──────┘ └──────┘ └──────┘                       │
├──────────────────────────────────────────────────────────────┤
│  FIFTHS (full height)                                        │
│  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐   │
│  │  1/5   │ │  2/5   │ │  3/5   │ │  4/5   │ │  5/5   │   │
│  └────────┘ └────────┘ └────────┘ └────────┘ └────────┘   │
└──────────────────────────────────────────────────────────────┘

 Page 6
┌──────────────────────────────────────────────────────────────┐
│  FIFTHS ½H (top/bottom split)                                │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐             │
│  │1/5 T │ │2/5 T │ │3/5 T │ │4/5 T │ │5/5 T │             │
│  ├──────┤ ├──────┤ ├──────┤ ├──────┤ ├──────┤             │
│  │1/5 B │ │2/5 B │ │3/5 B │ │4/5 B │ │5/5 B │             │
│  └──────┘ └──────┘ └──────┘ └──────┘ └──────┘             │
│                         (last page shows single row)         │
└──────────────────────────────────────────────────────────────┘
```

Full-height rows snap windows to the **full height** of the work area.
½H rows split each zone into **top/bottom halves** for partial-height snapping.

---

## Publishing

### Framework-dependent (~200 KB, requires .NET 9 on target)

```powershell
dotnet publish -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=false -o ./publish-small
```

### Self-contained (~70 MB, no .NET required)

```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ./publish
```

### Installer (~66 MB, includes auto-startup)

Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php):

```powershell
# First publish the self-contained exe (above), then:
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer/ScreenGridSetup.iss
# Output: installer-output/ScreenGridSetup.exe
```

---

## Run at Windows Startup

**Option A: Use the installer** (recommended)

Download `ScreenGridSetup.exe` — during installation, the "Run at startup" option is checked by default.

**Option B: Toggle from the app**

Right-click the tray icon → check **Run at Startup**. This writes to `HKCU\...\Run` (current user only, no admin required).

**Option C: Manual shortcut**

1. Press `Win+R` → type `shell:startup` → Enter
2. Create a shortcut to `ScreenGrid.exe` in that folder

---

## Architecture

```
ScreenGrid/
├── App.xaml.cs                 # Entry point, tray icon, WinEvent hook, drag tracking
├── OverlayWindow.xaml.cs       # Full-screen transparent overlay, grid rendering
├── GridEditorWindow.xaml.cs    # Grid editor UI (add/remove/reorder rows, height ratios)
├── CustomRatioDialog.xaml.cs   # Dialog for entering custom ratios
├── GridConfig.cs               # Grid layout model, JSON serialization
├── GridZone.cs                 # Individual snap zone model
├── NativeMethods.cs            # Win32 P/Invoke declarations
├── StartupManager.cs           # Windows startup registry management
├── ScreenGrid.csproj           # .NET 9 WPF project
├── installer/                  # Inno Setup installer script
└── tests/                      # xUnit test suite
```

**Key Win32 APIs:**
- `SetWinEventHook` - detects window drag start/end system-wide
- `GetAsyncKeyState` - polls Shift key state at 60 fps
- `GetCursorPos` - tracks cursor position during drag
- `MoveWindow` - snaps the window to the target zone
- `DwmGetWindowAttribute` - compensates for invisible DWM borders
- `GetMonitorInfo` / `GetDpiForMonitor` - multi-monitor and DPI support

---

## Contributing

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

## License

[MIT](LICENSE) - free to use, modify, and distribute.
