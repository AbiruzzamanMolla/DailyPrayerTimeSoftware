using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Wpf;

namespace DailyPrayerTime.Native.Views
{
    public class GoogleAuthWindow : Window
    {
        private readonly WebView2 _webView;
        private readonly TaskCompletionSource<string?> _tcs = new();

        public GoogleAuthWindow(string startUri)
        {
            Title = "Sign In with Google";
            Width = 500;
            Height = 650;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            // Apply application dark theme background
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 46));
            
            _webView = new WebView2();
            Content = _webView;

            _webView.NavigationStarting += (s, e) =>
            {
                if (e.Uri != null && e.Uri.Contains("__/auth/handler") && 
                    (e.Uri.Contains("?code=") || e.Uri.Contains("&code=") || e.Uri.Contains("?error=") || e.Uri.Contains("&error=")))
                {
                    _tcs.TrySetResult(e.Uri);
                    Close();
                }
            };

            Loaded += async (s, e) =>
            {
                try
                {
                    await _webView.EnsureCoreWebView2Async();
                    _webView.Source = new Uri(startUri);
                }
                catch (Exception ex)
                {
                    _tcs.TrySetException(ex);
                    Close();
                }
            };

            Closed += (s, e) =>
            {
                _tcs.TrySetResult(null);
            };
        }

        public Task<string?> WaitForRedirectAsync() => _tcs.Task;
    }
}
