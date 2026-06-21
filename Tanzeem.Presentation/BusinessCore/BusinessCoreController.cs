using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Services.Abstractions.BusinessCore;
using Tanzeem.Shared.Dtos.Branches;
using Tanzeem.Shared.Dtos.Companies;
using Tanzeem.Shared.Dtos.Users;

namespace Tanzeem.Presentation.BusinessCore {

    [ApiController]
    [Route("api/[controller]")]
    public class BusinessCoreController(IBusinessCoreService businessCoreService, IUnitOfWork unitOfWork) : ControllerBase {

        [HttpPost]
        [Route("Create-Employee")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateNewEmployee(EmployeeCreationDto createEmployeeDto) {
            var result = await businessCoreService.CreateNewEmployee(createEmployeeDto);
            return Ok(result);
        }

        [HttpPost]
        [Route("Create-Additional-Branch")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAdditionalBranch(BranchDto branchDto) {
            var result = await businessCoreService.CreateAdditionalBranchAsync(branchDto);
            return Ok(result);
        }

        [HttpPut]
        [Route("Assign-User")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignUserToBranch(int userId, int newBranchId) {
            var result = await businessCoreService.AssignUserToBranch(userId, newBranchId);
            return Ok(result);
        }

        [HttpPut]
        [Route("Switch-Branch")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SwitchBranch(int newBranchId) {
            var token = await businessCoreService.SwitchBranchAsync(newBranchId);
            return Ok(token);
        }

        [HttpPost]
        [Route("Reconcile-Inventory")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReconcileInventory() {
            var result = await businessCoreService.ReconcileCurrentBranchInventoryAsync();
            return Ok(result);
        }

        [HttpGet]
        [Route("Get-Profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile() {
            var profile = await businessCoreService.GetUserProfileAsync();
            return Ok(profile);
        }

        [HttpPut]
        [Route("Update-Profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(UserProfileUpdateDto updateDto) {
            var profile = await businessCoreService.UpdateUserProfileAsync(updateDto);
            return Ok(profile);
        }

        [HttpGet]
        [Route("Get-Employee-Profile/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetEmployeeProfile(int id) {
            var profile = await businessCoreService.GetEmployeeProfileAsync(id);
            return Ok(profile);
        }

        [HttpGet]
        [Route("Get-All-Employees")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllEmployees() {
            var employees = await businessCoreService.GetAllEmployeesAsync();
            return Ok(employees);
        }

        [HttpPut]
        [Route("Update-Employee/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEmployee(int id, EmployeeUpdateDto updateDto) {
            var result = await businessCoreService.UpdateEmployeeAsync(id, updateDto);
            return Ok(result);
        }

        [HttpDelete]
        [Route("Delete-Employee/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TerminateEmployee(int id) {
            var result = await businessCoreService.TerminateEmployeeAsync(id);
            return Ok(result);
        }


    }
}
