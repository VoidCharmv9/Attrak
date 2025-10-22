using System.Net.Http;

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
                // Try to ping the server
                var response = await _httpClient.GetAsync($"{_serverBaseUrl}/api/health");
                var wasOnline = _isOnline;
                _isOnline = response.IsSuccessStatusCode;

                // Notify if status changed
                if (wasOnline != _isOnline)
                {
                    ConnectionStatusChanged?.Invoke(this, _isOnline);
                }
            }
            catch
            {
                var wasOnline = _isOnline;
                _isOnline = false;

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
