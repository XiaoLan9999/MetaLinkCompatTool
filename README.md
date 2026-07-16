# Meta Link Compatibility Tool

Windows GUI helper for Meta Horizon / Quest Link compatibility recovery.

## What it does

- Detects CPU and GPU hardware through WMI.
- Provides Chinese, Japanese, and English UI language switching.
- Uses a warm atelier-inspired visual design with responsive typography and spacing.
- Reflows two-column sections into a single column on narrower windows.
- Uses a scrollable dashboard so smaller windows can still reach every control.
- Links to the author's homepage at `https://xiaolan9999.net` from the top navigation.
- Lets the user select a GPU and write that CPU/GPU into Meta's local compatibility lists:
  - `%LOCALAPPDATA%\Oculus\Compatibility.json`
  - `%ProgramFiles%\Meta Horizon\Support\oculus-runtime\Compatibility.json`
- Creates rollback backups before every write.
- Can restore a selected backup.
- Can reset Link encoder values to safe HEVC defaults:
  - `HEVC=1`
  - `BitrateMbps=0`
  - `EncodeWidth=0`
  - `DBR=0`
- Adds a separate Tools page for Meta / Oculus Debug Tool helpers:
  - Link bitrate, encode width, HEVC, and dynamic bitrate registry tuning.
  - A 120 Hz-oriented Quest Link preset (`HEVC=1`, `BitrateMbps=500`, `EncodeWidth=0`, `DBR=0`).
  - Oculus Debug Tool launch button.
  - Oculus Debug Tool CLI runtime commands for pixel density override, ASW mode, and output color space.
- Can stop/restart Meta runtime services and related processes.
- Can start `highwind_service.exe`.
- Can install/remove a current-user startup entry that starts highwind silently.
- Includes a ready-to-send Meta feedback text file.

## Notes

This is not a system-wide hardware spoofer. It only modifies Meta Horizon Link's local compatibility inputs and current-user Link encoder settings.

The 120 Hz headset refresh-rate toggle is still controlled by Meta's device graphics settings. The tool only provides Link streaming parameters and Debug Tool calls that are convenient for a 120 Hz setup.

Writing to Program Files and stopping `OVRService` require administrator rights. The app prompts to relaunch elevated when needed.

## Build

```powershell
dotnet restore
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o .\dist\MetaLinkCompatTool-win-x64
```

## Runtime

Normal GUI:

```powershell
MetaLinkCompatTool.exe
```

Silent highwind startup mode:

```powershell
MetaLinkCompatTool.exe --start-highwind --quiet
```
