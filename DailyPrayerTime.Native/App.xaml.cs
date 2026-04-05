using System.Windows;

namespace DailyPrayerTime.Native
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            this.DispatcherUnhandledException += (s, args) =>
            {
                System.Windows.MessageBox.Show("An unexpected error occurred: " + args.Exception.Message, "Crash Avoided", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true; // Prevents the app from crashing completely
            };

            base.OnStartup(e);
        }
    }
}
