using System.Net.NetworkInformation;
using System.Net.Http;

namespace ScannerMaui.Services
{
    public class ConnectionService
    {
        private readonly HttpClient _httpClient;
        private bool _isOnline = false;
        private bool _isCheckingConnection = false;

        public event EventHandler<bool>? ConnectionStatusChanged;

        public bool IsOnline 
        { 
            get => _isOnline; 
            private set
            {
                if (_isOnline != value)
                {
                    _isOnline = value;
                    ConnectionStatusChanged?.Invoke(this, _isOnline);
                    System.Diagnostics.Debug.WriteLine($"Connection status changed: {(_isOnline ? "Online" : "Offline")}");
                }
            }
        }

        public ConnectionService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
            
            // Start monitoring connection
            StartConnectionMonitoring();
        }

        private async void StartConnectionMonitoring()
        {
            // Check connection immediately
            await CheckConnectionAsync();

            // Check connection every 30 seconds
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(30000); // 30 seconds
                    await CheckConnectionAsync();
                }
            });
        }

        public async Task<bool> CheckConnectionAsync()
        {
            if (_isCheckingConnection)
                return _isOnline;

            _isCheckingConnection = true;

            try
            {
                // First check basic network connectivity
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    IsOnline = false;
                    return false;
                }

                // Try to ping a reliable server
                var ping = new Ping();
                var reply = await ping.SendPingAsync("8.8.8.8", 3000); // Google DNS
                
                if (reply.Status == IPStatus.Success)
                {
                    // Additional check: try to reach a web endpoint
                    try
                    {
                        var response = await _httpClient.GetAsync("https://httpbin.org/status/200");
                        IsOnline = response.IsSuccessStatusCode;
                    }
                    catch
                    {
                        // If web check fails but ping works, consider it online
                        IsOnline = true;
                    }
                }
                else
                {
                    IsOnline = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking connection: {ex.Message}");
                IsOnline = false;
            }
            finally
            {
                _isCheckingConnection = false;
            }

            return IsOnline;
        }

        public async Task<bool> TestServerConnectionAsync(string serverUrl)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                
                var response = await client.GetAsync($"{serverUrl}/api/health");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error testing server connection: {ex.Message}");
                return false;
            }
        }

        public string GetConnectionStatusText()
        {
            return IsOnline ? "🟢 Online" : "🔴 Offline";
        }

        public string GetConnectionStatusColor()
        {
            return IsOnline ? "Green" : "Red";
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
