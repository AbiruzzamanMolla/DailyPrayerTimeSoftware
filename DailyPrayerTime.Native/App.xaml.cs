using System.Windows;

namespace DailyPrayerTime.Native
{
    public partial class App : System.Windows.Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            SettingsManager.Load();
            LocalizationManager.Instance.SetLanguage(SettingsManager.Current.Language);
            AudioLogger.Log("Application started");
            
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            this.DispatcherUnhandledException += (s, args) =>
            {
                AudioLogger.Log($"UNHANDLED EXCEPTION: {args.Exception}");
                System.Console.WriteLine(args.Exception.ToString());
                System.Windows.MessageBox.Show(
                    string.Format(LocalizationManager.Instance.GetString("Msg_AppErrorGeneral"), args.Exception.ToString()), 
                    LocalizationManager.Instance.GetString("Title_AppCrash"), 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
                args.Handled = true; // Prevents the app from crashing completely
            };

            base.OnStartup(e);

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
                var loader = new LoaderWindow();
                loader.Show();

                // Artificial delay to show branding and support links
                await System.Threading.Tasks.Task.Delay(3000);

                var mainWindow = new MainWindow();
                mainWindow.Show();
                loader.Close();
            }
            else
            {
                // Background start doesn't show loader
                _ = new MainWindow();
            }
        }
    }
}
