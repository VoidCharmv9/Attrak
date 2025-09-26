using AttrackSharedClass.Models;

namespace Attrak.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task LogoutAsync();
        Task<UserInfo?> GetCurrentUserAsync();
        bool IsAuthenticated { get; }
        event Action<bool> AuthenticationStateChanged;
    }
}
