# MinimalSnapTimer

[简体中文](README.zh-CN.md) | English

MinimalSnapTimer is a lightweight Windows desktop timer built with C#, .NET 8, and WPF.  
It is designed for offline countdown use, sit/stand workflow reminders, and a minimal pure-time display mode.

## Features

- Sit / stand quick-start timers
- Standard countdown and stopwatch mode
- Pure-time mode with optional click-through
- Tray recovery and tray menu controls
- Multi-monitor window placement and recovery
- Light / dark theme with basic system-follow support
- Simplified Chinese and English UI
- Local JSON settings
- System notification and tray fallback reminders

## Release

The project currently ships only one Windows package:

- `MinimalSnapTimer_v0.1.0-beta_win-x64.zip`

This package contains a self-contained Windows x64 single-file executable.  
The file size is larger than a typical native utility because the .NET 8 desktop runtime and WPF dependencies are bundled into the executable.

Screenshots coming soon.

## Run

1. Download the release zip from GitHub Releases, or use the curated zip under `release/v0.1.0-beta`.
2. Extract the zip.
3. Double-click `MinimalSnapTimer_v0.1.0-beta_win-x64.exe`.

## Portable mode

Create an empty file named `portable.flag` next to the executable.  
The app will then store `settings.json` in the same folder instead of `%APPDATA%`.

## Development

- Windows 10 / Windows 11
- .NET 8 SDK
- WPF

### Build

```powershell
pwsh .\build.ps1
```

### Test

```powershell
dotnet test .\MinimalSnapTimer.sln -c Debug
```

### Publish

```powershell
pwsh .\publish-win-x64.ps1
```

Equivalent command:

```powershell
dotnet publish .\src\MinimalSnapTimer\MinimalSnapTimer.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true
```

## Repository layout

```text
src/MinimalSnapTimer
tests/MinimalSnapTimer.Tests
docs
release/v0.1.0-beta
```

## Known limitations

- System-follow theme is not a full real-time OS theme listener.
- A small number of edge-case English strings may still need polish.
- Native toast support is intentionally lightweight.
- There is currently no installer or auto-update channel.

## License

MIT. See `LICENSE`.
