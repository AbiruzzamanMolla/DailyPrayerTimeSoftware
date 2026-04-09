using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DailyPrayerTime.Native
{
    public partial class TaskbarWindow : Window
    {
        private DispatcherTimer _posTimer;
        private IntPtr _myHwnd;

        // Cache position to avoid unnecessary SetWindowPos calls
        private int _lastX = -1;
        private int _lastY = -1;
        private int _lastW = -1;
        private int _lastH = -1;

        public TaskbarWindow()
        {
            InitializeComponent();
            this.SourceInitialized += TaskbarWindow_SourceInitialized;

            // Poll position every 2 seconds (only need to react to taskbar moves/resizes)
            _posTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _posTimer.Tick += (s, e) => RepositionOnTaskbar();
        }

        private void TaskbarWindow_SourceInitialized(object sender, EventArgs e)
        {
            _myHwnd = new WindowInteropHelper(this).Handle;

            // Set extended styles: ToolWindow (no taskbar icon) + NoActivate (don't steal focus)
            int exStyle = NativeMethods.GetWindowLong(_myHwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(_myHwnd, NativeMethods.GWL_EXSTYLE,
                exStyle | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE);

            // Do NOT use SetParent on Windows 11 — it causes constant flickering.
            // Instead, position ourselves as a top-level topmost window over the taskbar.
            RepositionOnTaskbar();
            _posTimer.Start();
        }

        public void UpdateData(string time, string prayer)
        {
            if (_myHwnd == IntPtr.Zero) return;

            Dispatcher.Invoke(() =>
            {
                try
                {
                    TimerText.Text = time;
                    PrayerLabel.Text = prayer;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TaskbarWindow] UI Update FAILED: {ex.Message}");
                }
            });
        }

        private void RepositionOnTaskbar()
        {
            if (_myHwnd == IntPtr.Zero) return;

            try
            {
                IntPtr trayHandle = NativeMethods.FindWindow("Shell_TrayWnd", null);
                if (trayHandle == IntPtr.Zero) return;

                IntPtr notifyWnd = NativeMethods.FindWindowEx(trayHandle, IntPtr.Zero, "TrayNotifyWnd", null);
                if (notifyWnd == IntPtr.Zero) return;

                NativeMethods.GetWindowRect(trayHandle, out NativeMethods.RECT trayRect);
                NativeMethods.GetWindowRect(notifyWnd, out NativeMethods.RECT notifyRect);

                // Get DPI scaling
                double scaleX = 1.0, scaleY = 1.0;
                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget != null)
                {
                    scaleX = source.CompositionTarget.TransformToDevice.M11;
                    scaleY = source.CompositionTarget.TransformToDevice.M22;
                }

                int physicalWidth = (int)(this.ActualWidth * scaleX);
                int physicalHeight = (int)(this.ActualHeight * scaleY);

                if (physicalWidth <= 0) physicalWidth = (int)(this.Width * scaleX);
                if (physicalHeight <= 0) physicalHeight = (int)(this.Height * scaleY);

                // Position: just to the left of the notification area, vertically centered on the taskbar
                // These are SCREEN coordinates (not relative to a parent)
                int x = notifyRect.Left - physicalWidth - (int)(6 * scaleX);
                int y = trayRect.Top + (trayRect.Height - physicalHeight) / 2;

                if (x < trayRect.Left) x = trayRect.Left;

                // Only call SetWindowPos if position actually changed
                if (x != _lastX || y != _lastY || physicalWidth != _lastW || physicalHeight != _lastH)
                {
                    NativeMethods.SetWindowPos(_myHwnd, NativeMethods.HWND_TOPMOST,
                        x, y, physicalWidth, physicalHeight,
                        NativeMethods.SWP_NOACTIVATE);

                    _lastX = x;
                    _lastY = y;
                    _lastW = physicalWidth;
                    _lastH = physicalHeight;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TaskbarWindow] Reposition failed: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _posTimer.Stop();
            base.OnClosed(e);
        }
    }
}
