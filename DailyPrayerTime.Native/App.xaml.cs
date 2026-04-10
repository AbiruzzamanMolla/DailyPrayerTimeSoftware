using System.Windows;

namespace DailyPrayerTime.Native
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            SettingsManager.Load();
            LocalizationManager.Instance.SetLanguage(SettingsManager.Current.Language);
            
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            this.DispatcherUnhandledException += (s, args) =>
            {
                System.Windows.MessageBox.Show(
                    string.Format(LocalizationManager.Instance.GetString("Msg_PlayFailed"), args.Exception.Message), 
                    LocalizationManager.Instance.GetString("Title_AppCrash"), 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                args.Handled = true; // Prevents the app from crashing completely
            };

            base.OnStartup(e);

            var mainWindow = new MainWindow();
            bool isSilent = false;
            
            foreach (var arg in e.Args)
            {
                if (arg.Equals("-silent", System.StringComparison.OrdinalIgnoreCase) || 
                    arg.Equals("--silent", System.StringComparison.OrdinalIgnoreCase))
                {
                    isSilent = true;
                    break;
                }
            }

            if (!isSilent)
            {
                mainWindow.Show();
            }
        }
    }
}
