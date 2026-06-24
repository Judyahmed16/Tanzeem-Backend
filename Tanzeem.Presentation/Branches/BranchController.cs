using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Services.Abstractions.Branches;
using Tanzeem.Shared.Dtos.Branches;

namespace Tanzeem.Presentation.Branches {

    [ApiController]
    [Route("api/[controller]")]
    public class BranchController(IBranchService branchService) : ControllerBase {

        [HttpGet]
        [Route("Get-Branch/{id}")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> GetBranch(int id) {
            var branch = await branchService.GetBranchAsync(id);
            return Ok(branch);
        }

        [HttpGet]
        [Route("Get-Current-Branch")]
        [Authorize(Roles = "Admin, Manager, Staff")]
        public async Task<IActionResult> GetCurrentBranch() {
            var branch = await branchService.GetCurrentBranchAsync();
            return Ok(branch);
        }

        [HttpGet]
        [Route("Get-Branches")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetBranches() {
            var branches = await branchService.GetCompanyBranchesAsync();
            return Ok(branches);
        }
        
        [HttpGet]
        [Route("Get-Branches-Menu")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetBranchesMenu() {
            var branches = await branchService.GetBranchesList();
            return Ok(branches);
        }

        [HttpPut]
        [Route("Update-Branch/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBranch(int id, BranchDto branchDto) {
            var result = await branchService.UpdateBranchAsync(id, branchDto);
            return Ok(result);
        }

        [HttpDelete]
        [Route("Delete-Branch/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBranch(int id) {
            var result = await branchService.DeleteBranchAsync(id);
            return Ok(result);
        }

    }
}
