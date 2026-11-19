# GlassClock — Original Design + Battery

This project is based on the uploaded `GlassClock_FINAL_STARTUP_FIXED` source.
The original clock face, dark radial glass styling, tick marks, gold second hand,
context menu, Windows-time handling, and display-topology code are retained.
A compact battery panel has been added below the clock in the same window.

## Build and run
1. Extract this folder anywhere.
2. Double-click `Build-GlassClock.bat`, or open CMD in this folder and run it.
3. The standalone EXE is created at:
   `GlassClock\bin\Release\net8.0-windows\win-x64\publish\GlassClock.exe`

The old projects are not changed. This is a separate source folder.

## Battery behavior
- Shows the battery percentage and a level bar.
- Shows plugged-in/AC status when Windows reports the power line online.
- Uses a muted gold fill to match the original clock, green while plugged in, and red at low charge.
- Updates every five seconds.
- Shows an unavailable state when Windows cannot report battery information.

Note: Windows Forms' PowerStatus reports external power-line status; it may not distinguish every charging state on every device.

The display topology code is inherited from the supplied original project. Validate the monitor modes on your laptop after the first build before enabling Start with Windows.
