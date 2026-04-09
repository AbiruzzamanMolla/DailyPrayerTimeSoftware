using System;
using System.Runtime.InteropServices;
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
            TimerText.Text = time;
            PrayerLabel.Text = prayer;
        }

        private void UpdatePosition()
        {
            if (_trayHandle == IntPtr.Zero || _myHwnd == IntPtr.Zero) return;

            // Find the notification area (TrayNotifyWnd) to anchor next to it
            IntPtr notifyWnd = NativeMethods.FindWindowEx(_trayHandle, IntPtr.Zero, "TrayNotifyWnd", null);
            if (notifyWnd != IntPtr.Zero)
            {
                NativeMethods.RECT notifyRect;
                NativeMethods.GetWindowRect(notifyWnd, out notifyRect);

                NativeMethods.RECT trayRect;
                NativeMethods.GetWindowRect(_trayHandle, out trayRect);

                // Calculate position relative to Shell_TrayWnd
                // We want to be just to the left of the notification area
                int x = (notifyRect.Left - trayRect.Left) - (int)this.ActualWidth - 10;
                int y = (trayRect.Height - (int)this.ActualHeight) / 2;

                // Set position without activating
                NativeMethods.SetWindowPos(_myHwnd, IntPtr.Zero, x, y, (int)this.Width, (int)this.Height, 
                    NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _posTimer.Stop();
            base.OnClosed(e);
        }
    }
}
