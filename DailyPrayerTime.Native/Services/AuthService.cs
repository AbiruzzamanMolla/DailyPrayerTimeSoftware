using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DailyPrayerTime.Native.Helpers;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Auth.Repository;
using Newtonsoft.Json;

namespace DailyPrayerTime.Native.Services
{
    public class AuthService
    {
        private static readonly Lazy<AuthService> _instance = new(() => new());
        public static AuthService Instance => _instance.Value;

        private FirebaseAuthClient? _client;
        private static readonly HttpClient _http = new();

        public bool IsSignedIn => !string.IsNullOrEmpty(SettingsManager.Current.FirebaseUid);
        public string? Uid => SettingsManager.Current.FirebaseUid;
        public string? Email => SettingsManager.Current.FirebaseEmail;
        public string? DisplayName => SettingsManager.Current.FirebaseDisplayName;

        public event Action? AuthStateChanged;

        private AuthService()
        {
            _client = new FirebaseAuthClient(new FirebaseAuthConfig
            {
                ApiKey = Helpers.FirebaseConfig.ApiKey,
                AuthDomain = Helpers.FirebaseConfig.AuthDomain,
                Providers = new FirebaseAuthProvider[]
                {
                    new GoogleProvider().AddScopes("email", "profile"),
                    new EmailProvider()
                },
                UserRepository = new FileUserRepository("DailyPrayerTime")
            });
        }

        public async Task<AuthResult> SignInWithGoogleAsync()
        {
            try
            {
                var userCredential = await _client!.SignInWithRedirectAsync(FirebaseProviderType.Google, async uri =>
                {
                    string? redirectUrl = null;
                    if (System.Windows.Application.Current.Dispatcher.CheckAccess())
                    {
                        var activeWindow = System.Windows.Application.Current.MainWindow;
                        foreach (System.Windows.Window w in System.Windows.Application.Current.Windows)
                        {
                            if (w.IsActive)
                            {
                                activeWindow = w;
                                break;
                            }
                        }

                        var win = new Views.GoogleAuthWindow(uri)
                        {
                            Owner = activeWindow
                        };
                        win.ShowDialog();
                        redirectUrl = await win.WaitForRedirectAsync();
                    }
                    else
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            var activeWindow = System.Windows.Application.Current.MainWindow;
                            foreach (System.Windows.Window w in System.Windows.Application.Current.Windows)
                            {
                                if (w.IsActive)
                                {
                                    activeWindow = w;
                                    break;
                                }
                            }

                            var win = new Views.GoogleAuthWindow(uri)
                            {
                                Owner = activeWindow
                            };
                            win.ShowDialog();
                            redirectUrl = win.WaitForRedirectAsync().GetAwaiter().GetResult();
                        });
                    }
                    return redirectUrl;
                });

                var user = userCredential?.User;
                if (user != null)
                {
                    SettingsManager.Current.FirebaseUid = user.Uid;
                    SettingsManager.Current.FirebaseEmail = user.Info?.Email ?? "";
                    SettingsManager.Current.FirebaseDisplayName = user.Info?.DisplayName ?? "";
                    SettingsManager.Current.CloudSyncEnabled = true;
                    SettingsManager.Save();
                    AuthStateChanged?.Invoke();
                    return new AuthResult { Success = true };
                }
                return new AuthResult { Success = false, Error = "No user returned" };
            }
            catch (Exception ex)
            {
                return new AuthResult { Success = false, Error = ex.Message };
            }
        }

        public async Task<AuthResult> SignInWithEmailAsync(string email, string password)
        {
            try
            {
                var userCredential = await _client!.SignInWithEmailAndPasswordAsync(email, password);
                var user = userCredential?.User;
                if (user != null)
                {
                    SettingsManager.Current.FirebaseUid = user.Uid;
                    SettingsManager.Current.FirebaseEmail = user.Info?.Email ?? email;
                    SettingsManager.Current.FirebaseDisplayName = user.Info?.DisplayName ?? email;
                    SettingsManager.Current.CloudSyncEnabled = true;
                    SettingsManager.Save();
                    AuthStateChanged?.Invoke();
                    return new AuthResult { Success = true };
                }
                return new AuthResult { Success = false, Error = "No user returned" };
            }
            catch (Exception ex)
            {
                return new AuthResult { Success = false, Error = ex.Message };
            }
        }

        public async Task<string?> GetIdTokenAsync()
        {
            try
            {
                if (_client?.User != null)
                {
                    var token = await _client.User.GetIdTokenAsync();
                    return token;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public void SignOut()
        {
            _client?.SignOut();
            SettingsManager.Current.FirebaseUid = "";
            SettingsManager.Current.FirebaseEmail = "";
            SettingsManager.Current.FirebaseDisplayName = "";
            SettingsManager.Current.CloudSyncEnabled = false;
            SettingsManager.Save();
            AuthStateChanged?.Invoke();
        }
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}
