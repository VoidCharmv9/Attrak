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
                // Simple check: if we have internet access, assume we're online
                var hasInternet = Connectivity.NetworkAccess == NetworkAccess.Internet;
                var wasOnline = _isOnline;
                _isOnline = hasInternet;

                System.Diagnostics.Debug.WriteLine($"ConnectionStatusService: Internet connectivity: {hasInternet}");

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
                _isOnline = true; // Assume online if we can't determine

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
