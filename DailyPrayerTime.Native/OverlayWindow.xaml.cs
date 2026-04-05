using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace DailyPrayerTime.Native
{
    public partial class OverlayWindow : Window
    {
        const int GWL_EXSTYLE = -20;
        const int WS_EX_NOACTIVATE = 0x08000000;
        const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOACTIVATE = 0x0010;

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        public OverlayWindow()
        {
            InitializeComponent();
            this.Loaded += OverlayWindow_Loaded;
            this.MouseLeftButtonDown += Overlay_MouseDown;
            this.MouseLeftButtonUp += Overlay_MouseUp;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
        }

        public void ForceTopmost()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 1, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
            catch (Exception)
            {
                // Ignore errors during window position updates
            }
        }

        private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Set position from settings or default to bottom right
            double x = SettingsManager.Current.OverlayX;
            double y = SettingsManager.Current.OverlayY;

            if (Math.Abs(x - (-1)) < 0.01 || Math.Abs(y - (-1)) < 0.01)
            {
                // Default to bottom right of primary screen, above taskbar
                var workArea = SystemParameters.WorkArea;
                x = workArea.Right - this.Width - 100;
                y = workArea.Bottom - this.Height - 10;
            }

            this.Left = x;
            this.Top = y;
        }

        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Overlay_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Save absolute position when drag finishes
            SettingsManager.Current.OverlayX = this.Left;
            SettingsManager.Current.OverlayY = this.Top;
            SettingsManager.Save();
        }
    }
}
