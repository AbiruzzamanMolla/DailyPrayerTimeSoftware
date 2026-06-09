using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace DailyPrayerTime.Native
{
    public partial class EnhancedTaskbarWindow : Window
    {
        private DispatcherTimer _posTimer;
        private IntPtr _myHwnd;
        private int _lastX = -1, _lastY = -1, _lastW = -1, _lastH = -1;

        public EnhancedTaskbarWindow()
        {
            InitializeComponent();
            SourceInitialized += OnSourceInitialized;
            _posTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _posTimer.Tick += (s, e) => Reposition();
            MouseRightButtonUp += (s, e) => ShowRightClickMenu();
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            _myHwnd = new WindowInteropHelper(this).Handle;

            int exStyle = NativeMethods.GetWindowLong(_myHwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(_myHwnd, NativeMethods.GWL_EXSTYLE,
                exStyle | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE);

            Reposition();
            _posTimer.Start();
        }

        public void UpdateData(string compactLine, string statusColorHex)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    DisplayText.Text = compactLine;
                    if (!string.IsNullOrEmpty(statusColorHex))
                    {
                        if (new BrushConverter().ConvertFrom(statusColorHex) is SolidColorBrush brush)
                            DisplayText.Foreground = brush;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ETaskbar] UI: {ex.Message}");
                }
            });
        }

        private void Reposition()
        {
            if (_myHwnd == IntPtr.Zero) return;
            try
            {
                IntPtr trayHandle = NativeMethods.FindWindow("Shell_TrayWnd", null);
                if (trayHandle == IntPtr.Zero) return;
                NativeMethods.GetWindowRect(trayHandle, out NativeMethods.RECT tr);

                double sx = 1.0, sy = 1.0;
                var src = PresentationSource.FromVisual(this);
                if (src?.CompositionTarget != null)
                {
                    sx = src.CompositionTarget.TransformToDevice.M11;
                    sy = src.CompositionTarget.TransformToDevice.M22;
                }

                int scw = Math.Max((int)(DisplayText.ActualWidth * sx) + (int)(16 * sx), (int)(60 * sx));
                int sch = (int)(ActualHeight * sy);
                if (sch <= 0) sch = (int)(Height * sy);

                IntPtr notifyWnd = NativeMethods.FindWindowEx(trayHandle, IntPtr.Zero, "TrayNotifyWnd", null);
                int notifyLeft = tr.Right;
                if (notifyWnd != IntPtr.Zero)
                {
                    NativeMethods.GetWindowRect(notifyWnd, out NativeMethods.RECT nr);
                    notifyLeft = nr.Left;
                }

                int x, y;
                string pos = SettingsManager.Current.EnhancedTaskbarPosition;
                int gap = (int)(6 * sx);

                switch (pos)
                {
                    case "RightOfTray":
                        x = notifyLeft + scw + gap;
                        if (x + scw > tr.Right) x = notifyLeft - scw - gap;
                        break;
                    case "Center":
                        x = tr.Left + (tr.Width - scw) / 2;
                        if (x + scw > notifyLeft) x = notifyLeft - scw - gap;
                        break;
                    case "Left":
                        x = tr.Left + (int)(120 * sx);
                        break;
                    default:
                        x = notifyLeft - scw - gap;
                        break;
                }

                if (x < tr.Left) x = tr.Left + (int)(20 * sx);

                y = tr.Top + (tr.Height - sch) / 2;

                // Always call SetWindowPos to re-assert topmost status, preventing Windows taskbar from hiding it.

                NativeMethods.SetWindowPos(_myHwnd, NativeMethods.HWND_TOPMOST,
                    x, y, scw, sch,
                    NativeMethods.SWP_NOACTIVATE);

                _lastX = x;
                _lastY = y;
                _lastW = scw;
                _lastH = sch;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ETaskbar] Pos: {ex.Message}");
            }
        }

        private void ShowRightClickMenu()
        {
            var cms = new ContextMenuStrip();
            var posItem = new ToolStripMenuItem("Position");
            posItem.DropDownItems.Add("Left of Tray", null, (s, e) => SetPos("LeftOfTray"));
            posItem.DropDownItems.Add("Right of Tray", null, (s, e) => SetPos("RightOfTray"));
            posItem.DropDownItems.Add("Center", null, (s, e) => SetPos("Center"));
            posItem.DropDownItems.Add("Left (Near Start)", null, (s, e) => SetPos("Left"));
            cms.Items.Add(posItem);
            cms.Items.Add("Settings", null, (s, e) =>
            {
                if (System.Windows.Application.Current.MainWindow is MainWindow mw)
                    mw.OpenSettings();
            });
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add("Hide", null, (s, e) =>
            {
                SettingsManager.Current.UseEnhancedTaskbar = false;
                SettingsManager.Save();
                if (System.Windows.Application.Current.MainWindow is MainWindow mw)
                    mw.ManageEnhancedTaskbar();
            });
            cms.Show(Control.MousePosition);
        }

        private void SetPos(string p)
        {
            SettingsManager.Current.EnhancedTaskbarPosition = p;
            SettingsManager.Save();
            _lastX = -1;
            Reposition();
        }

        protected override void OnClosed(EventArgs e)
        {
            _posTimer.Stop();
            base.OnClosed(e);
        }
    }
}
