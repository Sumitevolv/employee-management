using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EmployeeManagementAPI.Tests.Integration
{
    public class CustomWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.UseSetting(
                "Jwt:Key",
                "ThisIsAVeryLongTestJwtKey123456789");

            builder.UseSetting(
                "Jwt:Issuer",
                "EmployeeManagementAPI");

            builder.UseSetting(
                "Jwt:Audience",
                "EmployeeManagementAPIUsers");

            builder.UseSetting(
                "Jwt:ExpiryMinutes",
                "60");
        }
    }
}