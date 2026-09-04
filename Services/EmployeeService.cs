using EmployeeManagementAPI.Data;
using EmployeeManagementAPI.DTOs;
using EmployeeManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementAPI.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(
            AppDbContext context,
            ILogger<EmployeeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET all employees with filtering and pagination
        public async Task<PagedResult<EmployeeResponseDto>> GetEmployeesAsync(
    string? name,
    string? department,
    int page,
    int pageSize,
    string? sortBy,
    string? sortOrder)
        {
            _logger.LogInformation(
                "Getting employees. Name: {Name}, Department: {Department}, Page: {Page}, PageSize: {PageSize}, SortBy: {SortBy}, SortOrder: {SortOrder}",
                name,
                department,
                page,
                pageSize,
                sortBy,
                sortOrder);

            var query = _context.Employees.AsQueryable();

            // Filtering
            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(e => e.Name.Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(department))
            {
                query = query.Where(e => e.Department == department);
            }

            // Sorting
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                query = query.OrderBy(e => e.Id);
            }
            else
            {
                var descending =
                    string.Equals(
                        sortOrder,
                        "desc",
                        StringComparison.OrdinalIgnoreCase);

                switch (sortBy.ToLower())
                {
                    case "name":
                        query = descending
                            ? query.OrderByDescending(e => e.Name)
                            : query.OrderBy(e => e.Name);
                        break;

                    case "salary":
                        query = descending
                            ? query.OrderByDescending(e => e.Salary)
                            : query.OrderBy(e => e.Salary);
                        break;

                    case "department":
                        query = descending
                            ? query.OrderByDescending(e => e.Department)
                            : query.OrderBy(e => e.Department);
                        break;

                    case "id":
                        query = descending
                            ? query.OrderByDescending(e => e.Id)
                            : query.OrderBy(e => e.Id);
                        break;

                    default:
                        query = query.OrderBy(e => e.Id);
                        break;
                }
            }

            var totalRecords = await query.CountAsync();

            var employees = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EmployeeResponseDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Email = e.Email,
                    Department = e.Department,
                    Salary = e.Salary
                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling(
                (double)totalRecords / pageSize);

            return new PagedResult<EmployeeResponseDto>
            {
                Data = employees,
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages
            };
        }
        // GET employee by ID
        public async Task<EmployeeResponseDto?> GetEmployeeByIdAsync(int id)
        {
            _logger.LogInformation(
                "Getting employee with ID {EmployeeId}",
                id);

            return await _context.Employees
                .Where(e => e.Id == id)
                .Select(e => new EmployeeResponseDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Email = e.Email,
                    Department = e.Department,
                    Salary = e.Salary
                })
                .FirstOrDefaultAsync();
        }

        // POST - Add employee
        public async Task<EmployeeResponseDto> AddEmployeeAsync(
            EmployeeCreateDto dto)
        {
            _logger.LogInformation(
                "Creating employee with email {Email}",
                dto.Email);

            var employee = new Employee
            {
                Name = dto.Name,
                Email = dto.Email,
                Department = dto.Department,
                Salary = dto.Salary
            };

            _context.Employees.Add(employee);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Employee created successfully with ID {EmployeeId}",
                employee.Id);

            return new EmployeeResponseDto
            {
                Id = employee.Id,
                Name = employee.Name,
                Email = employee.Email,
                Department = employee.Department,
                Salary = employee.Salary
            };
        }

        // PUT - Update employee
        public async Task<EmployeeResponseDto?> UpdateEmployeeAsync(
            int id,
            EmployeeUpdateDto dto)
        {
            _logger.LogInformation(
                "Updating employee with ID {EmployeeId}",
                id);

            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
            {
                _logger.LogWarning(
                    "Employee with ID {EmployeeId} was not found",
                    id);

                return null;
            }

            employee.Name = dto.Name;
            employee.Email = dto.Email;
            employee.Department = dto.Department;
            employee.Salary = dto.Salary;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Employee with ID {EmployeeId} updated successfully",
                id);

            return new EmployeeResponseDto
            {
                Id = employee.Id,
                Name = employee.Name,
                Email = employee.Email,
                Department = employee.Department,
                Salary = employee.Salary
            };
        }

        // DELETE - Delete employee
        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            _logger.LogInformation(
                "Deleting employee with ID {EmployeeId}",
                id);

            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
            {
                _logger.LogWarning(
                    "Employee with ID {EmployeeId} was not found",
                    id);

                return false;
            }

            _context.Employees.Remove(employee);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Employee with ID {EmployeeId} deleted successfully",
                id);

            return true;
        }
    }
}