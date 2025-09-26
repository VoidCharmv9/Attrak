using AttrackSharedClass.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace Attrak.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private UserInfo? _currentUser;
        private const string USER_KEY = "current_user";

        public bool IsAuthenticated => _currentUser != null;
        public event Action<bool> AuthenticationStateChanged = delegate { };

        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            LoadUserFromStorage();
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                Console.WriteLine($"Attempting login for user: {request.Username}");
                Console.WriteLine($"API URL: {_httpClient.BaseAddress}api/auth/login");
                
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
                
                Console.WriteLine($"Response Status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    Console.WriteLine($"Login Response: {JsonSerializer.Serialize(loginResponse)}");
                    
                    if (loginResponse?.Success == true && loginResponse.User != null)
                    {
                        _currentUser = loginResponse.User;
                        SaveUserToStorage();
                        AuthenticationStateChanged.Invoke(true);
                    }
                    return loginResponse ?? new LoginResponse { Success = false, Message = "Login failed" };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error Response: {errorContent}");
                    
                    var errorResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    return errorResponse ?? new LoginResponse { Success = false, Message = $"Login failed with status: {response.StatusCode}" };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during login: {ex.Message}");
                return new LoginResponse { Success = false, Message = $"Network error occurred: {ex.Message}" };
            }
        }

        public async Task LogoutAsync()
        {
            _currentUser = null;
            ClearUserFromStorage();
            AuthenticationStateChanged.Invoke(false);
            await Task.CompletedTask;
        }

        public async Task<UserInfo?> GetCurrentUserAsync()
        {
            return await Task.FromResult(_currentUser);
        }

        private void SaveUserToStorage()
        {
            if (_currentUser != null)
            {
                var userJson = JsonSerializer.Serialize(_currentUser);
                // Using browser's localStorage (in a real app, you might want to use secure storage)
                // For now, we'll store it in memory
            }
        }

        private void LoadUserFromStorage()
        {
            // In a real application, you would load from secure storage
            // For now, we'll start with no user
            _currentUser = null;
        }

        private void ClearUserFromStorage()
        {
            // Clear from storage
        }
    }
}
