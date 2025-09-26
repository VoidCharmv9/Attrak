using MySql.Data.MySqlClient;
using ServerAtrrak.Data;

namespace ServerAtrrak.Services
{
    public class ConnectionManagerService
    {
        private readonly Dbconnection _dbConnection;
        private readonly ILogger<ConnectionManagerService> _logger;
        private static readonly SemaphoreSlim _globalSemaphore = new SemaphoreSlim(3, 3); // Global limit of 3 connections
        private static int _activeConnections = 0;
        private static readonly object _lock = new object();

        public ConnectionManagerService(Dbconnection dbConnection, ILogger<ConnectionManagerService> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public async Task<T> ExecuteWithConnectionAsync<T>(Func<MySqlConnection, Task<T>> operation, int maxRetries = 3)
        {
            await _globalSemaphore.WaitAsync();
            
            lock (_lock)
            {
                _activeConnections++;
                _logger.LogInformation("Active connections: {Count}", _activeConnections);
            }

            try
            {
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        using var connection = new MySqlConnection(_dbConnection.GetConnection());
                        await connection.OpenAsync();
                        
                        var result = await operation(connection);
                        return result;
                    }
                    catch (MySqlException ex) when (ex.Number == 1226 && attempt < maxRetries) // max_user_connections
                    {
                        _logger.LogWarning("Connection limit exceeded, attempt {Attempt}/{MaxRetries}. Waiting before retry...", attempt, maxRetries);
                        
                        // Exponential backoff with jitter
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt) + new Random().Next(1, 4));
                        await Task.Delay(delay);
                    }
                }
                
                // Final attempt
                using var finalConnection = new MySqlConnection(_dbConnection.GetConnection());
                await finalConnection.OpenAsync();
                return await operation(finalConnection);
            }
            finally
            {
                lock (_lock)
                {
                    _activeConnections--;
                    _logger.LogInformation("Active connections: {Count}", _activeConnections);
                }
                _globalSemaphore.Release();
            }
        }

        public async Task ExecuteWithConnectionAsync(Func<MySqlConnection, Task> operation, int maxRetries = 3)
        {
            await ExecuteWithConnectionAsync(async connection =>
            {
                await operation(connection);
                return true; // Dummy return value
            }, maxRetries);
        }

        public static int GetActiveConnections()
        {
            lock (_lock)
            {
                return _activeConnections;
            }
        }
    }
}
