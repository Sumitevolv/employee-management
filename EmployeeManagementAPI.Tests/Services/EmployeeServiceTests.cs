using EmployeeManagementAPI.Data;
using EmployeeManagementAPI.DTOs;
using EmployeeManagementAPI.Models;
using EmployeeManagementAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EmployeeManagementAPI.Tests.Services
{
    public class EmployeeServiceTests
    {
        // ==========================================
        // CREATE IN-MEMORY DATABASE
        // ==========================================
        private AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        // ==========================================
        // CREATE EMPLOYEE SERVICE
        // ==========================================
        private EmployeeService CreateService(AppDbContext context)
        {
            var logger =
                NullLogger<EmployeeService>.Instance;

            return new EmployeeService(
                context,
                logger);
        }

        // ==========================================
        // TEST 1
        // GET ALL EMPLOYEES
        // ==========================================
        [Fact]
        public async Task GetEmployeesAsync_ReturnsEmployees()
        {
            // Arrange
            using var context = CreateContext();

            context.Employees.AddRange(
                new Employee
                {
                    Name = "Sumit Tiwari",
                    Email = "sumit@example.com",
                    Department = "IT",
                    Salary = 50000
                },
                new Employee
                {
                    Name = "Amit Sharma",
                    Email = "amit@example.com",
                    Department = "IT",
                    Salary = 55000
                });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.GetEmployeesAsync(
                null,
                null,
                1,
                10,
                null,
                "asc");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Data.Count);
            Assert.Equal(2, result.TotalRecords);
        }

        // ==========================================
        // TEST 2
        // FILTER BY NAME
        // ==========================================
        [Fact]
        public async Task GetEmployeesAsync_FilterByName_ReturnsMatchingEmployee()
        {
            // Arrange
            using var context = CreateContext();

            context.Employees.AddRange(
                new Employee
                {
                    Name = "Sumit Tiwari",
                    Email = "sumit@example.com",
                    Department = "IT",
                    Salary = 50000
                },
                new Employee
                {
                    Name = "Amit Sharma",
                    Email = "amit@example.com",
                    Department = "IT",
                    Salary = 55000
                });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.GetEmployeesAsync(
                "Sumit",
                null,
                1,
                10,
                null,
                "asc");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Data);
            Assert.Equal(
                "Sumit Tiwari",
                result.Data.First().Name);
        }

        // ==========================================
        // TEST 3
        // FILTER BY DEPARTMENT
        // ==========================================
        [Fact]
        public async Task GetEmployeesAsync_FilterByDepartment_ReturnsMatchingEmployees()
        {
            // Arrange
            using var context = CreateContext();

            context.Employees.AddRange(
                new Employee
                {
                    Name = "Sumit Tiwari",
                    Email = "sumit@example.com",
                    Department = "IT",
                    Salary = 50000
                },
                new Employee
                {
                    Name = "Rahul Sharma",
                    Email = "rahul@example.com",
                    Department = "HR",
                    Salary = 45000
                });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.GetEmployeesAsync(
                null,
                "IT",
                1,
                10,
                null,
                "asc");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Data);
            Assert.Equal(
                "IT",
                result.Data.First().Department);
        }

        // ==========================================
        // TEST 4
        // GET EMPLOYEE BY ID
        // ==========================================
        [Fact]
        public async Task GetEmployeeByIdAsync_ReturnsEmployee()
        {
            // Arrange
            using var context = CreateContext();

            var employee = new Employee
            {
                Name = "Sumit Tiwari",
                Email = "sumit@example.com",
                Department = "IT",
                Salary = 50000
            };

            context.Employees.Add(employee);

            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result =
                await service.GetEmployeeByIdAsync(employee.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(employee.Id, result.Id);
            Assert.Equal("Sumit Tiwari", result.Name);
            Assert.Equal(
                "sumit@example.com",
                result.Email);
            Assert.Equal("IT", result.Department);
            Assert.Equal(50000, result.Salary);
        }

        // ==========================================
        // TEST 5
        // INVALID EMPLOYEE ID
        // ==========================================
        [Fact]
        public async Task GetEmployeeByIdAsync_InvalidId_ReturnsNull()
        {
            // Arrange
            using var context = CreateContext();

            var service = CreateService(context);

            // Act
            var result =
                await service.GetEmployeeByIdAsync(99999);

            // Assert
            Assert.Null(result);
        }

        // ==========================================
        // TEST 6
        // ADD EMPLOYEE
        // ==========================================
        [Fact]
        public async Task AddEmployeeAsync_CreatesEmployee()
        {
            // Arrange
            using var context = CreateContext();

            var service = CreateService(context);

            var dto = new EmployeeCreateDto
            {
                Name = "Rahul Sharma",
                Email = "rahul@example.com",
                Department = "HR",
                Salary = 45000
            };

            // Act
            var result =
                await service.AddEmployeeAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal(
                "Rahul Sharma",
                result.Name);
            Assert.Equal(
                "rahul@example.com",
                result.Email);
            Assert.Equal(
                "HR",
                result.Department);
            Assert.Equal(
                45000,
                result.Salary);

            var employee =
                await context.Employees
                    .FirstOrDefaultAsync(
                        e => e.Id == result.Id);

            Assert.NotNull(employee);
        }

        // ==========================================
        // TEST 7
        // UPDATE EMPLOYEE
        // ==========================================
        [Fact]
        public async Task UpdateEmployeeAsync_UpdatesEmployee()
        {
            // Arrange
            using var context = CreateContext();

            var employee = new Employee
            {
                Name = "Old Name",
                Email = "old@example.com",
                Department = "IT",
                Salary = 40000
            };

            context.Employees.Add(employee);

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new EmployeeUpdateDto
            {
                Name = "Updated Name",
                Email = "updated@example.com",
                Department = "Development",
                Salary = 60000
            };

            // Act
            var result =
                await service.UpdateEmployeeAsync(
                    employee.Id,
                    dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(
                employee.Id,
                result.Id);
            Assert.Equal(
                "Updated Name",
                result.Name);
            Assert.Equal(
                "updated@example.com",
                result.Email);
            Assert.Equal(
                "Development",
                result.Department);
            Assert.Equal(
                60000,
                result.Salary);
        }

        // ==========================================
        // TEST 8
        // UPDATE INVALID EMPLOYEE
        // ==========================================
        [Fact]
        public async Task UpdateEmployeeAsync_InvalidId_ReturnsNull()
        {
            // Arrange
            using var context = CreateContext();

            var service = CreateService(context);

            var dto = new EmployeeUpdateDto
            {
                Name = "Updated Name",
                Email = "updated@example.com",
                Department = "IT",
                Salary = 50000
            };

            // Act
            var result =
                await service.UpdateEmployeeAsync(
                    99999,
                    dto);

            // Assert
            Assert.Null(result);
        }

        // ==========================================
        // TEST 9
        // DELETE EMPLOYEE
        // ==========================================
        [Fact]
        public async Task DeleteEmployeeAsync_DeletesEmployee()
        {
            // Arrange
            using var context = CreateContext();

            var employee = new Employee
            {
                Name = "Delete Me",
                Email = "delete@example.com",
                Department = "IT",
                Salary = 30000
            };

            context.Employees.Add(employee);

            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result =
                await service.DeleteEmployeeAsync(
                    employee.Id);

            // Assert
            Assert.True(result);

            var deletedEmployee =
                await context.Employees
                    .FirstOrDefaultAsync(
                        e => e.Id == employee.Id);

            Assert.Null(deletedEmployee);
        }

        // ==========================================
        // TEST 10
        // DELETE INVALID EMPLOYEE
        // ==========================================
        [Fact]
        public async Task DeleteEmployeeAsync_InvalidId_ReturnsFalse()
        {
            // Arrange
            using var context = CreateContext();

            var service = CreateService(context);

            // Act
            var result =
                await service.DeleteEmployeeAsync(99999);

            // Assert
            Assert.False(result);
        }
    }
}