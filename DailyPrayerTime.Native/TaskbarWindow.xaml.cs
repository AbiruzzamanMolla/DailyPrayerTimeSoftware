using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DailyPrayerTime.Native
{
    public partial class TaskbarWindow : Window
    {
        private DispatcherTimer _posTimer;
        private IntPtr _trayHandle;
        private IntPtr _myHwnd;
        private bool _isFirstPositioning = true;

        public TaskbarWindow()
        {
            InitializeComponent();
            this.SourceInitialized += TaskbarWindow_SourceInitialized;
            
            _posTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _posTimer.Tick += (s, e) => UpdatePosition();
        }

        private void TaskbarWindow_SourceInitialized(object sender, EventArgs e)
        {
            _myHwnd = new WindowInteropHelper(this).Handle;

            // Set extended styles: ToolWindow (no taskbar icon) and NoActivate (don't steal focus)
            int exStyle = NativeMethods.GetWindowLong(_myHwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(_myHwnd, NativeMethods.GWL_EXSTYLE, exStyle | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE);

            // Find the taskbar
            _trayHandle = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (_trayHandle != IntPtr.Zero)
            {
                // Parent to the Taskbar (TrafficMonitor style)
                NativeMethods.SetParent(_myHwnd, _trayHandle);
                UpdatePosition();
                _posTimer.Start();
            }
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
                    Debug.WriteLine($"[TaskbarWindow] UI Updated successfully: {prayer} - {time}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TaskbarWindow] UI Update FAILED: {ex.Message}");
                }
            });
        }

        private void UpdatePosition()
        {
            if (_trayHandle == IntPtr.Zero || _myHwnd == IntPtr.Zero) return;

            Dispatcher.Invoke(() =>
            {
                // Find the notification area (TrayNotifyWnd) to anchor next to it
                IntPtr notifyWnd = NativeMethods.FindWindowEx(_trayHandle, IntPtr.Zero, "TrayNotifyWnd", null);
                if (notifyWnd != IntPtr.Zero)
                {
                    NativeMethods.RECT notifyRect;
                    NativeMethods.GetWindowRect(notifyWnd, out notifyRect);

                    NativeMethods.RECT trayRect;
                    NativeMethods.GetWindowRect(_trayHandle, out trayRect);

                    // Get DPI scaling factor
                    double scaleX = 1.0;
                    double scaleY = 1.0;
                    var source = PresentationSource.FromVisual(this);
                    if (source?.CompositionTarget != null)
                    {
                        scaleX = source.CompositionTarget.TransformToDevice.M11;
                        scaleY = source.CompositionTarget.TransformToDevice.M22;
                    }

                    // Convert logical sizes to physical pixels
                    int physicalWidth = (int)(this.ActualWidth * scaleX);
                    int physicalHeight = (int)(this.ActualHeight * scaleY);

                    if (physicalWidth == 0) physicalWidth = (int)(this.Width * scaleX);
                    if (physicalHeight == 0) physicalHeight = (int)(this.Height * scaleY);

                    // Calculate position relative to Shell_TrayWnd
                    // We want to be just to the left of the notification area
                    int x = (notifyRect.Left - trayRect.Left) - physicalWidth - (int)(10 * scaleX);
                    int y = (trayRect.Height - physicalHeight) / 2;

                    // Ensure we don't end up with negative coordinates if something is weird
                    if (x < 0) x = 0;

                    // Set position without activating, ensuring we use physical pixels for size
                    uint flags = NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOZORDER;
                    
                    if (_isFirstPositioning)
                    {
                        flags |= NativeMethods.SWP_SHOWWINDOW;
                        _isFirstPositioning = false;
                    }

                    NativeMethods.SetWindowPos(_myHwnd, IntPtr.Zero, x, y, physicalWidth, physicalHeight, flags);
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            _posTimer.Stop();
            base.OnClosed(e);
        }
    }
}
