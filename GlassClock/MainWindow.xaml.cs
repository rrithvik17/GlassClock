using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace GlassClock;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer;
    private bool _disabledByUser;
    private bool _projectionHidden;

    private const double ClockSize = 104;
    private const double WidgetWidth = 130;
    private const double WidgetHeight = 144;
    private const double DisabledSize = 34;
    private readonly DispatcherTimer _batteryTimer;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += (_, _) =>
        {
            UpdateClock();
            ApplyProjectionVisibility();
        };
        _timer.Start();

        _batteryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _batteryTimer.Tick += (_, _) => UpdateBattery();
        _batteryTimer.Start();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        SetStartupRegistry(IsStartupEnabled());
        UpdateClock();
        UpdateBattery();
        ApplyProjectionVisibility();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _timer.Stop();
        _batteryTimer.Stop();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            // Windows may raise the event before the new topology is fully settled.
            // A short delay makes mode switching and unplug/replug transitions reliable.
            Dispatcher.BeginInvoke(new Action(ApplyProjectionVisibility),
                DispatcherPriority.ApplicationIdle);
        }));
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TimeZoneInfo.ClearCachedData();
                UpdateClock();
            }));
        }
    }

    private void ApplyProjectionVisibility()
    {
        var topology = ProjectionManager.GetTopology();

        // Second-screen-only means the laptop panel is not part of the desktop.
        // Hide the entire overlay, including the disabled indicator, but keep the
        // process alive so it can return automatically when the laptop is active.
        if (topology == ProjectionTopology.ExternalOnly)
        {
            HideForProjection();
            return;
        }

        if (_disabledByUser)
        {
            ShowDisabledIndicator();
            return;
        }

        // INTERNAL, CLONE and EXTEND keep the clock on the laptop/primary display.
        // In clone mode Windows mirrors that same clock onto the external display.
        ShowForProjection();
    }

    private void HideForProjection()
    {
        _projectionHidden = true;
        WidgetRoot.Visibility = Visibility.Collapsed;
        Width = Height = 1;
        Opacity = 0;
        ShowInTaskbar = false;
    }

    private void ShowForProjection()
    {
        if (_projectionHidden)
        {
            _projectionHidden = false;
            WidgetRoot.Visibility = Visibility.Visible;
            BatteryPanel.Visibility = Visibility.Visible;
            ClockRoot.Width = ClockSize;
            ClockRoot.Height = ClockSize;
            Width = WidgetWidth;
            Height = WidgetHeight;
            Opacity = 1;
        }

        Width = WidgetWidth;
        Height = WidgetHeight;
        WidgetRoot.Width = WidgetWidth;
        WidgetRoot.Height = WidgetHeight;
        BatteryPanel.Visibility = Visibility.Visible;
        ClockRoot.Width = ClockSize;
        ClockRoot.Height = ClockSize;
        PositionOnLaptopDisplay();
    }

    private void ShowDisabledIndicator()
    {
        _projectionHidden = false;
        WidgetRoot.Visibility = Visibility.Visible;
        BatteryPanel.Visibility = Visibility.Collapsed;
        ClockRoot.Visibility = Visibility.Visible;
        Opacity = 0.62;
        Width = Height = DisabledSize;
        WidgetRoot.Width = DisabledSize;
        WidgetRoot.Height = DisabledSize;
        ClockRoot.Width = DisabledSize;
        ClockRoot.Height = DisabledSize;
        PositionDisabledIndicator();
    }

    private void PositionOnLaptopDisplay()
    {
        var screen = Forms.Screen.PrimaryScreen;
        if (screen == null)
            return;

        // WPF uses device-independent units; Screen.Bounds is physical pixels.
        var source = PresentationSource.FromVisual(this);
        double scaleX = source?.CompositionTarget?.TransformFromDevice.M11 ?? 1.0;
        double scaleY = source?.CompositionTarget?.TransformFromDevice.M22 ?? 1.0;

        const int taskbarSafeMarginPx = 12;

        Left = screen.WorkingArea.Right * scaleX - WidgetWidth - taskbarSafeMarginPx * scaleX;
        Top = screen.WorkingArea.Bottom * scaleY - WidgetHeight - taskbarSafeMarginPx * scaleY;
    }

    private void UpdateClock()
    {
        if (_projectionHidden || _disabledByUser)
            return;

        // Windows is the source of truth. Clear the .NET cached local zone so
        // changing Windows time zone/DST is reflected immediately.
        TimeZoneInfo.ClearCachedData();
        DateTime now = DateTime.Now;

        double seconds = now.Second + now.Millisecond / 1000.0;
        double minutes = now.Minute + seconds / 60.0;
        double hours = (now.Hour % 12) + minutes / 60.0;

        ClockCanvas.Children.Clear();

        // Ticks
        for (int i = 0; i < 60; i++)
        {
            double angle = i * 6.0 * Math.PI / 180.0;
            bool major = i % 5 == 0;

            double outer = 46;
            double inner = major ? 39 : 42;

            double x1 = 52 + Math.Sin(angle) * inner;
            double y1 = 52 - Math.Cos(angle) * inner;
            double x2 = 52 + Math.Sin(angle) * outer;
            double y2 = 52 - Math.Cos(angle) * outer;

            var line = new Line
            {
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                Stroke = new SolidColorBrush(major ? System.Windows.Media.Color.FromArgb(205, 255, 255, 255)
                                                     : System.Windows.Media.Color.FromArgb(82, 255, 255, 255)),
                StrokeThickness = major ? 1.25 : 0.65,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            ClockCanvas.Children.Add(line);
        }

        AddHand(hours * 30.0, 25, 2.8, "#F2F4F5");
        AddHand(minutes * 6.0, 34, 2.0, "#FFFFFF");
        AddHand(seconds * 6.0, 38, 0.9, "#D8B66A");

        var centerRing = new Ellipse
        {
            Width = 7, Height = 7,
            Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(230, 12, 13, 16)),
            Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(235, 216, 183, 112)),
            StrokeThickness = 1
        };
        Canvas.SetLeft(centerRing, 48.5);
        Canvas.SetTop(centerRing, 48.5);
        ClockCanvas.Children.Add(centerRing);
    }

    private void AddHand(double degrees, double length, double thickness, string color)
    {
        double angle = degrees * Math.PI / 180.0;
        double x = 52 + Math.Sin(angle) * length;
        double y = 52 - Math.Cos(angle) * length;

        var line = new Line
        {
            X1 = 52, Y1 = 52, X2 = x, Y2 = y,
            Stroke = (SolidColorBrush)new BrushConverter().ConvertFromString(color)!,
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        ClockCanvas.Children.Add(line);
    }

    private void UpdateBattery()
    {
        try
        {
            var power = Forms.SystemInformation.PowerStatus;
            float level = power.BatteryLifePercent;

            if (level < 0 || float.IsNaN(level))
            {
                BatteryPercentText.Text = "--%";
                BatteryStatusText.Text = "NO BATTERY";
                BatteryGlyph.Text = "—";
                BatteryFill.Width = 0;
                return;
            }

            int percent = Math.Clamp((int)Math.Round(level * 100), 0, 100);
            bool pluggedIn = power.PowerLineStatus == Forms.PowerLineStatus.Online;

            BatteryPercentText.Text = $"{percent}%";
            BatteryStatusText.Text = pluggedIn ? "CHARGING / AC" : "BATTERY";
            BatteryGlyph.Text = pluggedIn ? "⚡" : "▰";

            BatteryFill.Width = Math.Max(0, BatteryTrack.ActualWidth) * percent / 100.0;
            BatteryFill.Background = percent <= 15
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(238, 104, 112))
                : pluggedIn
                    ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(143, 204, 159))
                    : new SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 215, 165));
        }
        catch
        {
            BatteryPercentText.Text = "--%";
            BatteryStatusText.Text = "UNAVAILABLE";
            BatteryGlyph.Text = "—";
            BatteryFill.Width = 0;
        }
    }

    private void DisableClock_Click(object sender, RoutedEventArgs e)
    {
        _disabledByUser = true;
        _projectionHidden = false;
        WidgetRoot.Visibility = Visibility.Visible;
        BatteryPanel.Visibility = Visibility.Collapsed;
        ClockRoot.Visibility = Visibility.Visible;
        Width = Height = DisabledSize;
        WidgetRoot.Width = DisabledSize;
        WidgetRoot.Height = DisabledSize;
        ClockRoot.Width = DisabledSize;
        ClockRoot.Height = DisabledSize;
        Opacity = 0.62;
        PositionDisabledIndicator();
    }

    private void EnableClock_Click(object sender, RoutedEventArgs e)
    {
        _disabledByUser = false;
        Opacity = 1;
        WidgetRoot.Width = WidgetWidth;
        WidgetRoot.Height = WidgetHeight;
        ClockRoot.Width = ClockSize;
        ClockRoot.Height = ClockSize;
        BatteryPanel.Visibility = Visibility.Visible;
        ApplyProjectionVisibility();
    }

    private void PositionDisabledIndicator()
    {
        var screen = Forms.Screen.PrimaryScreen;
        if (screen == null) return;

        var source = PresentationSource.FromVisual(this);
        double sx = source?.CompositionTarget?.TransformFromDevice.M11 ?? 1.0;
        double sy = source?.CompositionTarget?.TransformFromDevice.M22 ?? 1.0;

        Left = screen.WorkingArea.Right * sx - DisabledSize - 12 * sx;
        Top = screen.WorkingArea.Bottom * sy - DisabledSize - 12 * sy;
    }

    private void Startup_Click(object sender, RoutedEventArgs e)
    {
        bool enabled = ((System.Windows.Controls.MenuItem)sender).IsChecked;
        SetStartupRegistry(enabled);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ClockRoot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_disabledByUser)
        {
            _disabledByUser = false;
            Opacity = 1;
            ApplyProjectionVisibility();
        }
    }

    private bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", false);

        string? current = key?.GetValue("GlassClock") as string;
        return string.Equals(current, Environment.ProcessPath, StringComparison.OrdinalIgnoreCase);
    }

    private void SetStartupRegistry(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run");

        if (enabled && Environment.ProcessPath != null)
            key.SetValue("GlassClock", Environment.ProcessPath);
        else
            key.DeleteValue("GlassClock", false);

        if (ContextMenu != null && ContextMenu.Items.Count > 0 &&
            ContextMenu.Items[3] is System.Windows.Controls.MenuItem startupItem)
        {
            startupItem.IsChecked = enabled;
        }
    }
}
