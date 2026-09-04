using EmployeeManagementAPI.DTOs;
using EmployeeManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // ==========================================
        // REGISTER
        // POST: api/Auth/register
        // ==========================================
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(
            RegisterDto dto)
        {
            var registered =
                await _authService.RegisterAsync(dto);

            if (!registered)
            {
                return Conflict(new
                {
                    message = "Username already exists."
                });
            }

            return Ok(new
            {
                message = "User registered successfully.",
                username = dto.Username,
                role = "User"
            });
        }

        // ==========================================
        // ADMIN CREATE USER
        // POST: api/Auth/create-user
        // ==========================================
        [HttpPost("create-user")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser(
            CreateUserDto dto)
        {
            try
            {
                var created =
                    await _authService.CreateUserAsync(dto);

                if (!created)
                {
                    return Conflict(new
                    {
                        message = "Username already exists."
                    });
                }

                return Ok(new
                {
                    message = "User created successfully.",
                    username = dto.Username,
                    role = dto.Role
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // ==========================================
        // LOGIN
        // POST: api/Auth/login
        // ==========================================
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(
            LoginDto dto)
        {
            var result =
                await _authService.LoginAsync(dto);

            if (result == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid username or password."
                });
            }

            return Ok(result);
        }

        // ==========================================
        // REFRESH TOKEN
        // POST: api/Auth/refresh
        // ==========================================
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken(
            string refreshToken)
        {
            var result =
                await _authService.RefreshTokenAsync(
                    refreshToken);

            if (result == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid or expired refresh token."
                });
            }

            return Ok(result);
        }

        // ==========================================
        // LOGOUT
        // POST: api/Auth/logout
        // ==========================================
        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout(
            string refreshToken)
        {
            var loggedOut =
                await _authService.LogoutAsync(
                    refreshToken);

            if (!loggedOut)
            {
                return NotFound(new
                {
                    message = "Refresh token not found."
                });
            }

            return Ok(new
            {
                message = "Logout successful."
            });
        }
    }
}