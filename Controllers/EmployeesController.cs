using EmployeeManagementAPI.DTOs;
using EmployeeManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagementAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeesController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        // ==========================================
        // GET ALL EMPLOYEES
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetEmployees(
            string? name,
            string? department,
            int page = 1,
            int pageSize = 10,
            string? sortBy = null,
            string? sortOrder = "asc")
        {
            if (page < 1)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    message = "Page must be greater than 0.",
                    timestamp = DateTime.UtcNow,
                    path = HttpContext.Request.Path.ToString()
                });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    message = "PageSize must be between 1 and 100.",
                    timestamp = DateTime.UtcNow,
                    path = HttpContext.Request.Path.ToString()
                });
            }

            var employees =
                await _employeeService.GetEmployeesAsync(
                    name,
                    department,
                    page,
                    pageSize,
                    sortBy,
                    sortOrder);

            return Ok(employees);
        }

        // ==========================================
        // GET EMPLOYEE BY ID
        // ==========================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            var employee =
                await _employeeService.GetEmployeeByIdAsync(id);

            if (employee == null)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = $"Employee with ID {id} not found.",
                    timestamp = DateTime.UtcNow,
                    path = HttpContext.Request.Path.ToString()
                });
            }

            return Ok(employee);
        }

        // ==========================================
        // ADD EMPLOYEE - ADMIN ONLY
        // ==========================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddEmployee(
            EmployeeCreateDto dto)
        {
            var employee =
                await _employeeService.AddEmployeeAsync(dto);

            return CreatedAtAction(
                nameof(GetEmployee),
                new { id = employee.Id },
                employee);
        }

        // ==========================================
        // UPDATE EMPLOYEE - ADMIN ONLY
        // ==========================================
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(
            int id,
            EmployeeUpdateDto dto)
        {
            var employee =
                await _employeeService.UpdateEmployeeAsync(
                    id,
                    dto);

            if (employee == null)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = $"Employee with ID {id} not found.",
                    timestamp = DateTime.UtcNow,
                    path = HttpContext.Request.Path.ToString()
                });
            }

            return Ok(employee);
        }

        // ==========================================
        // DELETE EMPLOYEE - ADMIN ONLY
        // ==========================================
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(
            int id)
        {
            var deleted =
                await _employeeService.DeleteEmployeeAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = $"Employee with ID {id} not found.",
                    timestamp = DateTime.UtcNow,
                    path = HttpContext.Request.Path.ToString()
                });
            }

            return Ok(new
            {
                statusCode = 200,
                message = $"Employee with ID {id} deleted successfully.",
                timestamp = DateTime.UtcNow
            });
        }
    }
}