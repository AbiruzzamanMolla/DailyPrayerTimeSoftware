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
            this.Loaded += (s, e) => Reposition();
            // Watch for taskbar changes
            SystemParameters.StaticPropertyChanged += (s, e) => { if (e.PropertyName == "WorkArea") Reposition(); };
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

        private void Reposition()
        {
            var workArea = SystemParameters.WorkArea;
            // Dock to bottom right, slightly above taskbar
            this.Left = workArea.Right - this.Width - 10; 
            this.Top = workArea.Bottom - this.Height - 5;
        }

        private void Border_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var sb = (Storyboard)this.Resources["ExpandAnim"];
            sb.Begin();
            // When expanding, we might need to shift Left to keep it in view if it's too wide
            var workArea = SystemParameters.WorkArea;
            this.Left = workArea.Right - 350 - 10; 
        }

        private void Border_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var sb = (Storyboard)this.Resources["CollapseAnim"];
            sb.Begin();
            Reposition();
        }
    }
}
