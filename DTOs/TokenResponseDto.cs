namespace EmployeeManagementAPI.DTOs
{
    public class TokenResponseDto
    {
        public string Message { get; set; } = string.Empty;

        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;
    }
}