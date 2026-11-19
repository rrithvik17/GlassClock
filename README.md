# GlassClock

A tiny glassy time goblin lives on your desktop. It keeps one eye on the hour, the other on your battery, and its tiny gold hand dramatically points at the seconds.

## What the goblin does

- Floats a dark, glassy analog clock above your windows.
- Adds a little battery readout underneath, updated every five seconds.
- Turns the battery bar gold when things are normal, green when AC power is reported, and red when charge gets low.
- Knows about Windows projection modes: it hides in second-screen-only mode and pops back when the laptop display returns.
- Offers a context menu for disabling the clock, starting with Windows, or telling the goblin to go take a nap (Exit).

Windows sometimes reports “plugged in” without meaning “actively charging.” The status label reflects the power-line information Windows provides, because the battery goblin is not a certified electrician.

## Summon it

You’ll need Windows and the .NET 8 SDK.

1. Run `Build-GlassClock.bat`.
2. When the build finishes, the standalone app will be waiting here:
   `GlassClock\bin\Release\net8.0-windows\win-x64\publish\GlassClock.exe`
3. Double-click the EXE. The clock appears, silently judging your time management.

Prefer the command line? From this folder, run:

```powershell
 dotnet publish .\GlassClock\GlassClock.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Tiny print

This is a separate GlassClock source folder. The other projects stay where they are, doing whatever it is other projects do.

Display-mode behavior depends on Windows and your monitor setup. Battery reporting also depends on what the device exposes to Windows. If either one gets theatrical, the clock itself should still show up and tell time.
