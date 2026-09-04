using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using EmployeeManagementAPI.DTOs;

using Microsoft.AspNetCore.Mvc.Testing;

using Xunit;

namespace EmployeeManagementAPI.Tests.Integration
{
    public class AuthorizationTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AuthorizationTests(
            CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // =========================================================
        // CREATE CLIENT
        // =========================================================

        private HttpClient CreateClient()
        {
            return _factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress =
                        new Uri("http://localhost")
                });
        }

        // =========================================================
        // LOGIN
        // =========================================================

        private async Task<LoginResponseDto> LoginAsync(
            string username,
            string password)
        {
            var client = CreateClient();

            var response =
                await client.PostAsJsonAsync(
                    "/api/Auth/login",
                    new LoginDto
                    {
                        Username = username,
                        Password = password
                    });

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content
                    .ReadFromJsonAsync<LoginResponseDto>();

            Assert.NotNull(result);

            return result!;
        }

        // =========================================================
        // CREATE ADMIN CLIENT
        // =========================================================

        private async Task<HttpClient>
            CreateAdminClientAsync()
        {
            var login =
                await LoginAsync(
                    "admin",
                    "admin123");

            var client = CreateClient();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    login.AccessToken);

            return client;
        }

        // =========================================================
        // CREATE USER CLIENT
        // =========================================================

        private async Task<HttpClient>
            CreateUserClientAsync()
        {
            var client = CreateClient();

            var username =
                $"user_{Guid.NewGuid():N}";

            var registerResponse =
                await client.PostAsJsonAsync(
                    "/api/Auth/register",
                    new RegisterDto
                    {
                        Username = username,
                        Password = "test123"
                    });

            Assert.Equal(
                HttpStatusCode.OK,
                registerResponse.StatusCode);

            var login =
                await LoginAsync(
                    username,
                    "test123");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    login.AccessToken);

            return client;
        }

        // =========================================================
        // CREATE EMPLOYEE AS ADMIN
        // =========================================================

        private async Task<int>
            CreateEmployeeAsync(
                HttpClient adminClient)
        {
            var employee =
                new EmployeeCreateDto
                {
                    Name =
                        "Integration Test Employee",

                    Email =
                        $"employee_{Guid.NewGuid():N}@example.com",

                    Department = "IT",

                    Salary = 50000
                };

            var response =
                await adminClient.PostAsJsonAsync(
                    "/api/Employees",
                    employee);

            Assert.Equal(
                HttpStatusCode.Created,
                response.StatusCode);

            var result =
                await response.Content
                    .ReadFromJsonAsync<EmployeeResponseDto>();

            Assert.NotNull(result);

            return result!.Id;
        }

        // =========================================================
        // USER CANNOT CREATE EMPLOYEE
        // =========================================================

        [Fact]
        public async Task User_Cannot_Create_Employee()
        {
            var client =
                await CreateUserClientAsync();

            var employee =
                new EmployeeCreateDto
                {
                    Name =
                        "Unauthorized Employee",

                    Email =
                        $"unauthorized_{Guid.NewGuid():N}@example.com",

                    Department = "IT",

                    Salary = 50000
                };

            var response =
                await client.PostAsJsonAsync(
                    "/api/Employees",
                    employee);

            Assert.Equal(
                HttpStatusCode.Forbidden,
                response.StatusCode);
        }

        // =========================================================
        // ADMIN CAN CREATE EMPLOYEE
        // =========================================================

        [Fact]
        public async Task Admin_Can_Create_Employee()
        {
            var client =
                await CreateAdminClientAsync();

            var employee =
                new EmployeeCreateDto
                {
                    Name =
                        "Admin Created Employee",

                    Email =
                        $"admin_{Guid.NewGuid():N}@example.com",

                    Department = "IT",

                    Salary = 60000
                };

            var response =
                await client.PostAsJsonAsync(
                    "/api/Employees",
                    employee);

            Assert.Equal(
                HttpStatusCode.Created,
                response.StatusCode);

            var result =
                await response.Content
                    .ReadFromJsonAsync<EmployeeResponseDto>();

            Assert.NotNull(result);

            Assert.Equal(
                employee.Name,
                result!.Name);

            Assert.Equal(
                employee.Email,
                result.Email);

            Assert.Equal(
                employee.Department,
                result.Department);

            Assert.Equal(
                employee.Salary,
                result.Salary);

            Assert.True(result.Id > 0);
        }

        // =========================================================
        // USER CANNOT UPDATE EMPLOYEE
        // =========================================================

        [Fact]
        public async Task User_Cannot_Update_Employee()
        {
            var adminClient =
                await CreateAdminClientAsync();

            var employeeId =
                await CreateEmployeeAsync(
                    adminClient);

            var userClient =
                await CreateUserClientAsync();

            var update =
                new EmployeeUpdateDto
                {
                    Name =
                        "Updated By User",

                    Email =
                        $"updated_{Guid.NewGuid():N}@example.com",

                    Department = "HR",

                    Salary = 75000
                };

            var response =
                await userClient.PutAsJsonAsync(
                    $"/api/Employees/{employeeId}",
                    update);

            Assert.Equal(
                HttpStatusCode.Forbidden,
                response.StatusCode);
        }

        // =========================================================
        // USER CANNOT DELETE EMPLOYEE
        // =========================================================

        [Fact]
        public async Task User_Cannot_Delete_Employee()
        {
            var adminClient =
                await CreateAdminClientAsync();

            var employeeId =
                await CreateEmployeeAsync(
                    adminClient);

            var userClient =
                await CreateUserClientAsync();

            var response =
                await userClient.DeleteAsync(
                    $"/api/Employees/{employeeId}");

            Assert.Equal(
                HttpStatusCode.Forbidden,
                response.StatusCode);
        }

        // =========================================================
        // INVALID PASSWORD
        // =========================================================

        [Fact]
        public async Task Login_With_Invalid_Password_Returns_Unauthorized()
        {
            var client = CreateClient();

            var response =
                await client.PostAsJsonAsync(
                    "/api/Auth/login",
                    new LoginDto
                    {
                        Username = "admin",
                        Password = "wrong-password"
                    });

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        // =========================================================
        // INVALID USERNAME
        // =========================================================

        [Fact]
        public async Task Login_With_Invalid_Username_Returns_Unauthorized()
        {
            var client = CreateClient();

            var response =
                await client.PostAsJsonAsync(
                    "/api/Auth/login",
                    new LoginDto
                    {
                        Username =
                            "user_that_does_not_exist",

                        Password = "test123"
                    });

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        // =========================================================
        // NO JWT
        // =========================================================

        [Fact]
        public async Task Protected_Endpoint_Without_Token_Returns_Unauthorized()
        {
            var client = CreateClient();

            var response =
                await client.GetAsync(
                    "/api/Employees");

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        // =========================================================
        // INVALID JWT
        // =========================================================

        [Fact]
        public async Task Protected_Endpoint_With_Invalid_Token_Returns_Unauthorized()
        {
            var client = CreateClient();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    "this-is-not-a-valid-jwt");

            var response =
                await client.GetAsync(
                    "/api/Employees");

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        // =========================================================
        // INVALID REFRESH TOKEN
        // =========================================================

        [Fact]
        public async Task Refresh_With_Invalid_Token_Returns_BadRequest()
        {
            var client = CreateClient();

            var response =
                await client.PostAsJsonAsync(
                    "/api/Auth/refresh",
                    new
                    {
                        RefreshToken =
                            "invalid-refresh-token"
                    });

            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        }
    }
}