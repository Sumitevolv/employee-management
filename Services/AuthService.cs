using EmployeeManagementAPI.Data;
using EmployeeManagementAPI.DTOs;
using EmployeeManagementAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EmployeeManagementAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IConfiguration _configuration;

        public AuthService(
            AppDbContext context,
            IPasswordService passwordService,
            IConfiguration configuration)
        {
            _context = context;
            _passwordService = passwordService;
            _configuration = configuration;
        }

        // ==========================================
        // REGISTER
        // ==========================================
        public async Task<bool> RegisterAsync(RegisterDto dto)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == dto.Username);

            if (existingUser != null)
            {
                return false;
            }

            var user = new User
            {
                Username = dto.Username,
                PasswordHash =
                    _passwordService.HashPassword(dto.Password),
                Role = "User"
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return true;
        }

        // ==========================================
        // ADMIN CREATE USER
        // ==========================================
        public async Task<bool> CreateUserAsync(CreateUserDto dto)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == dto.Username);

            if (existingUser != null)
            {
                return false;
            }

            if (dto.Role != "Admin" &&
                dto.Role != "User")
            {
                throw new ArgumentException(
                    "Role must be Admin or User.");
            }

            var user = new User
            {
                Username = dto.Username,
                PasswordHash =
                    _passwordService.HashPassword(dto.Password),
                Role = dto.Role
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return true;
        }

        // ==========================================
        // LOGIN
        // ==========================================
        public async Task<LoginResponseDto?> LoginAsync(
            LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == dto.Username);

            if (user == null)
            {
                return null;
            }

            var passwordValid =
                _passwordService.VerifyPassword(
                    user.PasswordHash,
                    dto.Password);

            if (!passwordValid)
            {
                return null;
            }

            var accessToken = GenerateJwtToken(user);

            var refreshToken = GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                UserId = user.Id
            };

            _context.RefreshTokens.Add(refreshTokenEntity);

            await _context.SaveChangesAsync();

            return new LoginResponseDto
            {
                Message = "Login successful.",
                Username = user.Username,
                Role = user.Role,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        // ==========================================
        // REFRESH TOKEN
        // ==========================================
        public async Task<TokenResponseDto?> RefreshTokenAsync(
            string refreshToken)
        {
            var storedToken =
                await _context.RefreshTokens
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r =>
                        r.Token == refreshToken);

            if (storedToken == null)
            {
                return null;
            }

            if (storedToken.IsRevoked)
            {
                return null;
            }

            if (storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                return null;
            }

            if (storedToken.User == null)
            {
                return null;
            }

            // Revoke old refresh token
            storedToken.IsRevoked = true;

            // Generate new tokens
            var newAccessToken =
                GenerateJwtToken(storedToken.User);

            var newRefreshToken =
                GenerateRefreshToken();

            var newRefreshTokenEntity = new RefreshToken
            {
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                UserId = storedToken.User.Id
            };

            _context.RefreshTokens.Add(
                newRefreshTokenEntity);

            await _context.SaveChangesAsync();

            return new TokenResponseDto
            {
                Message = "Token refreshed successfully.",
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        // ==========================================
        // LOGOUT
        // ==========================================
        public async Task<bool> LogoutAsync(
            string refreshToken)
        {
            var storedToken =
                await _context.RefreshTokens
                    .FirstOrDefaultAsync(r =>
                        r.Token == refreshToken);

            if (storedToken == null)
            {
                return false;
            }

            storedToken.IsRevoked = true;

            await _context.SaveChangesAsync();

            return true;
        }

        // ==========================================
        // GENERATE JWT ACCESS TOKEN
        // ==========================================
        private string GenerateJwtToken(User user)
        {
            var jwtSettings =
                _configuration.GetSection("Jwt");

            var key = jwtSettings["Key"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException(
                    "JWT Key is not configured.");
            }

            var claims = new[]
            {
                new Claim(
                    ClaimTypes.Name,
                    user.Username),

                new Claim(
                    ClaimTypes.Role,
                    user.Role)
            };

            var securityKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(key));

            var credentials =
                new SigningCredentials(
                    securityKey,
                    SecurityAlgorithms.HmacSha256);

            var expiryMinutes = 60;

            if (int.TryParse(
                jwtSettings["ExpiryMinutes"],
                out var configuredExpiry))
            {
                expiryMinutes = configuredExpiry;
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    expiryMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }

        // ==========================================
        // GENERATE RANDOM REFRESH TOKEN
        // ==========================================
        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];

            using var rng =
                RandomNumberGenerator.Create();

            rng.GetBytes(randomBytes);

            return Convert.ToBase64String(
                randomBytes);
        }
    }
}