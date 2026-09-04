using EmployeeManagementAPI.Data;
using EmployeeManagementAPI.DTOs;
using EmployeeManagementAPI.Models;
using EmployeeManagementAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EmployeeManagementAPI.Tests.Services
{
    public class AuthServiceTests
    {
        private AppDbContext CreateContext()
        {
            var options =
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase(
                        Guid.NewGuid().ToString())
                    .Options;

            return new AppDbContext(options);
        }

        private AuthService CreateService(
            AppDbContext context)
        {
            var passwordService =
                new PasswordService();

            var configuration =
                new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Jwt:Key"] =
                                "ThisIsAVeryLongTestJwtKey123456789",

                            ["Jwt:Issuer"] =
                                "EmployeeManagementAPI",

                            ["Jwt:Audience"] =
                                "EmployeeManagementAPIUsers",

                            ["Jwt:ExpiryMinutes"] =
                                "60"
                        })
                    .Build();

            return new AuthService(
                context,
                passwordService,
                configuration);
        }

        [Fact]
        public async Task RegisterAsync_CreatesUser()
        {
            using var context = CreateContext();
            var service = CreateService(context);

            var dto = new RegisterDto
            {
                Username = "testuser",
                Password = "test123"
            };

            var result =
                await service.RegisterAsync(dto);

            Assert.True(result);

            var user =
                await context.Users
                    .FirstOrDefaultAsync(
                        u => u.Username == "testuser");

            Assert.NotNull(user);
            Assert.Equal("testuser", user.Username);
            Assert.Equal("User", user.Role);

            Assert.NotEqual(
                "test123",
                user.PasswordHash);
        }

        [Fact]
        public async Task RegisterAsync_DuplicateUsername_ReturnsFalse()
        {
            using var context = CreateContext();

            context.Users.Add(new User
            {
                Username = "testuser",
                PasswordHash =
                    new PasswordService()
                        .HashPassword("test123"),
                Role = "User"
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new RegisterDto
            {
                Username = "testuser",
                Password = "another123"
            };

            var result =
                await service.RegisterAsync(dto);

            Assert.False(result);

            var userCount =
                await context.Users.CountAsync();

            Assert.Equal(1, userCount);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsTokens()
        {
            using var context = CreateContext();

            var passwordService =
                new PasswordService();

            var user = new User
            {
                Username = "testuser",
                PasswordHash =
                    passwordService.HashPassword(
                        "test123"),
                Role = "User"
            };

            context.Users.Add(user);

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new LoginDto
            {
                Username = "testuser",
                Password = "test123"
            };

            var result =
                await service.LoginAsync(dto);

            Assert.NotNull(result);

            Assert.Equal(
                "testuser",
                result.Username);

            Assert.Equal(
                "User",
                result.Role);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.AccessToken));

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.RefreshToken));

            var refreshToken =
                await context.RefreshTokens
                    .FirstOrDefaultAsync();

            Assert.NotNull(refreshToken);

            Assert.Equal(
                user.Id,
                refreshToken.UserId);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsNull()
        {
            using var context = CreateContext();

            var passwordService =
                new PasswordService();

            context.Users.Add(new User
            {
                Username = "testuser",
                PasswordHash =
                    passwordService.HashPassword(
                        "test123"),
                Role = "User"
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new LoginDto
            {
                Username = "testuser",
                Password = "wrongpassword"
            };

            var result =
                await service.LoginAsync(dto);

            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_InvalidUsername_ReturnsNull()
        {
            using var context = CreateContext();

            var service = CreateService(context);

            var dto = new LoginDto
            {
                Username = "unknownuser",
                Password = "test123"
            };

            var result =
                await service.LoginAsync(dto);

            Assert.Null(result);
        }

        [Fact]
        public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokens()
        {
            using var context = CreateContext();

            var passwordService =
                new PasswordService();

            var user = new User
            {
                Username = "testuser",
                PasswordHash =
                    passwordService.HashPassword(
                        "test123"),
                Role = "User"
            };

            context.Users.Add(user);

            await context.SaveChangesAsync();

            var oldRefreshToken =
                new RefreshToken
                {
                    Token = "old-refresh-token",

                    ExpiresAt =
                        DateTime.UtcNow.AddDays(7),

                    IsRevoked = false,

                    UserId = user.Id
                };

            context.RefreshTokens.Add(
                oldRefreshToken);

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result =
                await service.RefreshTokenAsync(
                    "old-refresh-token");

            Assert.NotNull(result);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.AccessToken));

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.RefreshToken));

            var oldToken =
                await context.RefreshTokens
                    .FirstAsync(
                        r => r.Token ==
                             "old-refresh-token");

            Assert.True(oldToken.IsRevoked);

            var tokenCount =
                await context.RefreshTokens
                    .CountAsync();

            Assert.Equal(2, tokenCount);
        }

        [Fact]
        public async Task RefreshTokenAsync_InvalidToken_ReturnsNull()
        {
            using var context = CreateContext();

            var service = CreateService(context);

            var result =
                await service.RefreshTokenAsync(
                    "invalid-token");

            Assert.Null(result);
        }

        [Fact]
        public async Task RefreshTokenAsync_RevokedToken_ReturnsNull()
        {
            using var context = CreateContext();

            var user = new User
            {
                Username = "testuser",
                PasswordHash = "hashed-password",
                Role = "User"
            };

            context.Users.Add(user);

            await context.SaveChangesAsync();

            context.RefreshTokens.Add(
                new RefreshToken
                {
                    Token = "revoked-token",

                    ExpiresAt =
                        DateTime.UtcNow.AddDays(7),

                    IsRevoked = true,

                    UserId = user.Id
                });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result =
                await service.RefreshTokenAsync(
                    "revoked-token");

            Assert.Null(result);
        }

        [Fact]
        public async Task LogoutAsync_ValidToken_RevokesToken()
        {
            using var context = CreateContext();

            var user = new User
            {
                Username = "testuser",
                PasswordHash = "hashed-password",
                Role = "User"
            };

            context.Users.Add(user);

            await context.SaveChangesAsync();

            context.RefreshTokens.Add(
                new RefreshToken
                {
                    Token = "logout-token",

                    ExpiresAt =
                        DateTime.UtcNow.AddDays(7),

                    IsRevoked = false,

                    UserId = user.Id
                });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result =
                await service.LogoutAsync(
                    "logout-token");

            Assert.True(result);

            var token =
                await context.RefreshTokens
                    .FirstAsync(
                        r => r.Token ==
                             "logout-token");

            Assert.True(token.IsRevoked);
        }

        [Fact]
        public async Task LogoutAsync_InvalidToken_ReturnsFalse()
        {
            using var context = CreateContext();

            var service = CreateService(context);

            var result =
                await service.LogoutAsync(
                    "invalid-token");

            Assert.False(result);
        }
    }
}