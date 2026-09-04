using EmployeeManagementAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace EmployeeManagementAPI.Services
{
    public class PasswordService : IPasswordService
    {
        private readonly PasswordHasher<User> _passwordHasher;

        public PasswordService()
        {
            _passwordHasher = new PasswordHasher<User>();
        }

        public string HashPassword(string password)
        {
            var user = new User();

            return _passwordHasher.HashPassword(
                user,
                password);
        }

        public bool VerifyPassword(
            string hashedPassword,
            string password)
        {
            var user = new User();

            var result =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    hashedPassword,
                    password);

            return result ==
                       PasswordVerificationResult.Success
                   ||
                   result ==
                       PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}