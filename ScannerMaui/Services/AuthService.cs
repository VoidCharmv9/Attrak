using AttrackSharedClass.Models;
using System.Text.Json;

namespace ScannerMaui.Services
{
    public class AuthService
    {
        private UserInfo? _currentUser;
        private const string USER_KEY = "current_user";

        public bool IsAuthenticated => _currentUser != null;
        public event Action<bool> AuthenticationStateChanged = delegate { };

        public async Task<UserInfo?> GetCurrentUserAsync()
        {
            return await Task.FromResult(_currentUser);
        }

        public async Task<TeacherInfo?> GetCurrentTeacherAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user?.UserType == UserType.Teacher && !string.IsNullOrEmpty(user.TeacherId))
            {
                // For now, return basic info from user
                // In a real app, you would make an API call to get full teacher details
                return new TeacherInfo
                {
                    TeacherId = user.TeacherId,
                    FullName = user.Username, // This should come from Teacher table
                    Email = user.Email,
                    SchoolName = "Sample School" // This should come from Teacher table
                };
            }
            return null;
        }

        public void SetCurrentUser(UserInfo user)
        {
            _currentUser = user;
            SaveUserToStorage();
            AuthenticationStateChanged.Invoke(true);
        }

        public void Logout()
        {
            _currentUser = null;
            ClearUserFromStorage();
            AuthenticationStateChanged.Invoke(false);
        }

        private void SaveUserToStorage()
        {
            if (_currentUser != null)
            {
                var userJson = JsonSerializer.Serialize(_currentUser);
                // In a real app, you would save to secure storage
                // For now, we'll just keep it in memory
            }
        }

        private void LoadUserFromStorage()
        {
            // In a real app, you would load from secure storage
            // For now, we'll start with no user
            _currentUser = null;
        }

        private void ClearUserFromStorage()
        {
            // Clear from storage
        }
    }
}
