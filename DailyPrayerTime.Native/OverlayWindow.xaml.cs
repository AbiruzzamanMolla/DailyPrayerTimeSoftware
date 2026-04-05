using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Runtime.InteropServices;

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
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOACTIVATE = 0x0010;

        public OverlayWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => LoadPosition();
            // Watch for taskbar changes to ensure it's still visible
            SystemParameters.StaticPropertyChanged += (s, e) => { if (e.PropertyName == "WorkArea") EnsureVisible(); };
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
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        private void LoadPosition()
        {
            var s = SettingsManager.Current;
            // Force reset on first launch after update to fix the Top docking issue
            if (s.OverlayX == -1 || s.OverlayY == -1 || s.OverlayY > SystemParameters.WorkArea.Bottom - 90)
            {
                RepositionDefault();
            }
            else
            {
                this.Left = s.OverlayX;
                this.Top = s.OverlayY;
            }
        }

        private void RepositionDefault()
        {
            var workArea = SystemParameters.WorkArea;
            this.Left = workArea.Right - this.Width - 10; 
            this.Top = workArea.Bottom - this.Height; // Use full height (90) to stay docked at bottom
            SavePosition();
        }

        private void EnsureVisible()
        {
            var workArea = SystemParameters.WorkArea;
            if (this.Left + this.Width > workArea.Right) this.Left = workArea.Right - this.Width - 10;
            if (this.Top + this.Height > workArea.Bottom) this.Top = workArea.Bottom - this.Height;
        }

        private void SavePosition()
        {
            SettingsManager.Current.OverlayX = this.Left;
            SettingsManager.Current.OverlayY = this.Top;
            SettingsManager.Save();
        }

        private void Border_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var sb = (Storyboard)this.Resources["ExpandAnim"];
            sb.Begin();
        }

        private void Border_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var sb = (Storyboard)this.Resources["CollapseAnim"];
            sb.Begin();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
                SavePosition();
            }
        }
    }
}
