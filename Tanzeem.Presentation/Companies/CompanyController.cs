using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Services.Abstractions.Companies;
using Tanzeem.Shared.Dtos.Companies;

namespace Tanzeem.Presentation.Companies {

    [ApiController]
    [Route("api/[controller]")]
    public class CompanyController(ICompanyService companyService)
        : ControllerBase {

        [HttpGet]
        [Route("Get-Company")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetCurrentCompany() {
            var result = await companyService.GetCompanyAsync();
            return Ok(result);
        }

        [HttpPut]
        [Route("Update-Company")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCompany(CompanyDto companyDto) {

            var result = await companyService.UpdateCompanyAsync(companyDto);
            return Ok(result);
        }

        [HttpDelete]
        [Route("Delete-Company")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCompany() {
            var result = await companyService.DeleteCompanyAsync();
            return Ok(result);
        }

    }
}
