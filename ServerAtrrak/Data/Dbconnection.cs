using MySql.Data.MySqlClient;

namespace ServerAtrrak.Data
{
    public class Dbconnection
    {
        public IConfiguration Configuration { get; }
        private readonly string _connectionString;
        private static readonly SemaphoreSlim _connectionSemaphore = new SemaphoreSlim(3, 3); // Max 3 concurrent connections
        
        public Dbconnection(IConfiguration configuration)
        {
            Configuration = configuration;
            _connectionString = Configuration.GetSection("ConnectionStrings").GetSection("dbconstring").Value ?? string.Empty;
        }
        
        public string GetConnection() => _connectionString;
        
        public async Task<MySqlConnection> GetConnectionAsync()
        {
            await _connectionSemaphore.WaitAsync(); // Wait for available connection slot
            
            try
            {
                var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                return connection;
            }
            catch
            {
                _connectionSemaphore.Release(); // Release slot if connection fails
                throw;
            }
        }
        
        public async Task<MySqlConnection> GetConnectionWithRetryAsync(int maxRetries = 5)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await _connectionSemaphore.WaitAsync();
                    
                    var connection = new MySqlConnection(_connectionString);
                    await connection.OpenAsync();
                    return connection;
                }
                catch (MySqlException ex) when (ex.Number == 1226 && attempt < maxRetries) // max_user_connections
                {
                    _connectionSemaphore.Release(); // Release slot before retry
                    
                    // Wait before retrying (exponential backoff with jitter)
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt) + new Random().Next(1, 4));
                    await Task.Delay(delay);
                }
                catch
                {
                    _connectionSemaphore.Release(); // Release slot on other errors
                    throw;
                }
            }
            
            // If all retries failed, throw the last exception
            await _connectionSemaphore.WaitAsync();
            var finalConnection = new MySqlConnection(_connectionString);
            await finalConnection.OpenAsync();
            return finalConnection;
        }
        
        public static void ReleaseConnection()
        {
            _connectionSemaphore.Release();
        }
    }
}
