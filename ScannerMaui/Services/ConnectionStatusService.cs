using System.Net.Http;
using Microsoft.Maui.Networking;

namespace ScannerMaui.Services
{
    public class ConnectionStatusService
    {
        private readonly HttpClient _httpClient;
        private readonly string _serverBaseUrl;
        private bool _isOnline = false;
        private bool _isChecking = false;

        public event EventHandler<bool>? ConnectionStatusChanged;

        public bool IsOnline => _isOnline;
        public bool IsChecking => _isChecking;
        public string StatusText => _isChecking ? "Checking..." : (_isOnline ? "Online" : "Offline");

        public ConnectionStatusService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _serverBaseUrl = "https://attrak.onrender.com";
        }

        public async Task CheckConnectionAsync()
        {
            if (_isChecking) return;

            _isChecking = true;
            ConnectionStatusChanged?.Invoke(this, _isOnline);

            try
            {
                // More reliable connection check: test actual server connectivity
                var hasInternet = Connectivity.NetworkAccess == NetworkAccess.Internet;
                var wasOnline = _isOnline;
                
                // Test actual server connectivity if we have internet
                if (hasInternet)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        var response = await _httpClient.GetAsync($"{_serverBaseUrl}/api/health", cts.Token);
                        _isOnline = response.IsSuccessStatusCode;
                        System.Diagnostics.Debug.WriteLine($"Server connectivity test: {_isOnline} (Status: {response.StatusCode})");
                    }
                    catch (Exception serverEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Server connectivity test failed: {serverEx.Message}");
                        _isOnline = false; // If server test fails, we're offline
                    }
                }
                else
                {
                    _isOnline = false;
                }

                System.Diagnostics.Debug.WriteLine($"ConnectionStatusService: Internet: {hasInternet}, Server: {_isOnline}");

                // Notify if status changed
                if (wasOnline != _isOnline)
                {
                    System.Diagnostics.Debug.WriteLine($"Connection status changed: {(_isOnline ? "Online" : "Offline")}");
                    ConnectionStatusChanged?.Invoke(this, _isOnline);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Connection check error: {ex.Message}");
                var wasOnline = _isOnline;
                _isOnline = false; // Assume offline if we can't determine

                // Notify if status changed
                if (wasOnline != _isOnline)
                {
                    ConnectionStatusChanged?.Invoke(this, _isOnline);
                }
            }
            finally
            {
                _isChecking = false;
                ConnectionStatusChanged?.Invoke(this, _isOnline);
            }
        }

        public async Task<bool> TestServerConnectionAsync(string serverUrl = null)
        {
            try
            {
                var url = serverUrl ?? _serverBaseUrl;
                var response = await _httpClient.GetAsync($"{url}/api/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
