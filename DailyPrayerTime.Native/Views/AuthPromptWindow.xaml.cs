using System;
using System.Threading.Tasks;
using System.Windows;
using DailyPrayerTime.Native.Services;

namespace DailyPrayerTime.Native.Views
{
    public partial class AuthPromptWindow : Window
    {
        public bool AuthSuccessful { get; private set; }

        public AuthPromptWindow()
        {
            InitializeComponent();
        }

        private async void GoogleSignIn_Click(object sender, RoutedEventArgs e)
        {
            GoogleSignInBtn.IsEnabled = false;
            GoogleSignInBtn.Content = "Signing in...";
            ErrorText.Visibility = Visibility.Collapsed;

            try
            {
                var result = await AuthService.Instance.SignInWithGoogleAsync();
                if (result.Success)
                {
                    AuthSuccessful = true;

                    // Push all local data to cloud
                    await Task.Run(async () =>
                    {
                        await CloudSyncService.Instance.SyncAllAsync();
                        await LeaderboardService.Instance.PushMyStatsAsync();
                    });

                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    ErrorText.Text = result.Error ?? "Sign in failed. Please try again.";
                    ErrorText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ErrorText.Text = $"Error: {ex.Message}";
                ErrorText.Visibility = Visibility.Visible;
            }
            finally
            {
                GoogleSignInBtn.IsEnabled = true;
                GoogleSignInBtn.Content = "Sign in with Google";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
