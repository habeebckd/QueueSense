using JobFlow.Core.Models;

namespace JobFlow.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<UserProfileResponse?> GetUserProfileAsync(Guid userId);
}
