using EmployeeManagementAPI.DTOs;

namespace EmployeeManagementAPI.Services
{
    public interface IEmployeeService
    {
        Task<PagedResult<EmployeeResponseDto>> GetEmployeesAsync(
            string? name,
            string? department,
            int page,
            int pageSize,
            string? sortBy,
            string? sortOrder);

        Task<EmployeeResponseDto?> GetEmployeeByIdAsync(int id);

        Task<EmployeeResponseDto> AddEmployeeAsync(
            EmployeeCreateDto dto);

        Task<EmployeeResponseDto?> UpdateEmployeeAsync(
            int id,
            EmployeeUpdateDto dto);

        Task<bool> DeleteEmployeeAsync(int id);
    }
}