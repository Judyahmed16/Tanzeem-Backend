using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Domain.Enums;
using Tanzeem.Shared.Dtos.Branches;
using Tanzeem.Shared.Dtos.Inventories;
using Tanzeem.Shared.Dtos.Users;


namespace Tanzeem.Services.Abstractions.BusinessCore {
    public interface IBusinessCoreService {
        Task<int> CreateNewEmployee(EmployeeCreationDto employeeCreationDto);
        Task<bool> AssignUserToBranch(int userId, int newBranchId);
        Task<string> SwitchBranchAsync(int newBranchId);
        Task<int> CreateAdditionalBranchAsync(BranchDto branchDto);
        Task<UserProfileDto> GetUserProfileAsync();
        Task<UserProfileDto> UpdateUserProfileAsync(UserProfileUpdateDto updatedProfileDto);
        Task<UserProfileDto> GetEmployeeProfileAsync(int id);
        Task<List<UserProfileDto>> GetAllEmployeesAsync();
        Task<bool> TerminateEmployeeAsync(int employeeId);
        Task<bool> UpdateEmployeeAsync(int employeeId, EmployeeUpdateDto updatedEmployeeDto);
        Task<InventoryReconciliationDto> ReconcileCurrentBranchInventoryAsync();

    }
}
