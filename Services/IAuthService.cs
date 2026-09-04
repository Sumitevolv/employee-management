using EmployeeManagementAPI.DTOs;

namespace EmployeeManagementAPI.Services
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(RegisterDto dto);

        Task<bool> CreateUserAsync(CreateUserDto dto);

        Task<LoginResponseDto?> LoginAsync(LoginDto dto);

        Task<TokenResponseDto?> RefreshTokenAsync(
            string refreshToken);

        Task<bool> LogoutAsync(
            string refreshToken);
    }
}