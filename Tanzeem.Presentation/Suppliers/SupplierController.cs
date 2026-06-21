using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Constants;
using Tanzeem.Domain.Entities.Suppliers;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Suppliers;
using Tanzeem.Shared.Dtos.Suppliers;
using static System.Net.Mime.MediaTypeNames;

namespace Tanzeem.Presentation.Suppliers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public class SupplierController(ISupplierService _supplierService) : ControllerBase
    {
        [HttpPost]
        //[Authorize(Roles= "")]
        public async Task<IActionResult> AddSupplier(SupplierRequestDto supplierDto)
        {
            var result = await _supplierService.CreateSupplierAsync(supplierDto);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        //[Authorize(Roles = "")]
        public async Task<IActionResult> RemoveSupplier(int id)
        {
            var result = await _supplierService.DeleteSupplierAsync(id);
            if (result == true)
            {
                return Ok(result);
            }
            else
            {
                return NotFound("This Supplier Not found");
            }
        }

        [HttpGet]
        //[Authorize(Roles = "")]
        public async Task<IActionResult> DisplayAllSuppliers([FromQuery(Name = "page_size")] int pageSize=10, [FromQuery(Name ="page")]int page =1,SupplierFilter? supplierFilter = null,[FromQuery(Name = "sortId")] SupplierSort? supplierSort = null, [FromQuery(Name = "searchTerm")] string? searchTerm = null)
        {
            var result = await _supplierService.GetAllSuppliersAsync(page,pageSize,supplierFilter,supplierSort,searchTerm);
            return Ok(result);
        }
        
        [HttpGet("{id}")]
        //[Authorize(Roles = "")]
        public async Task<IActionResult> GetSupplierById(int id)
        {
            var result = await _supplierService.GetSupplierByIdAsync(id);

            if (result == null) return NotFound("This Supplier Not found");

            return Ok(result);
        }

        [HttpPut("{id}")]
        //[Authorize(Roles = "")]
        public async Task<IActionResult> UpdateSupplier(SupplierRequestDto supplierDto,int id)
        {
            var result = await _supplierService.UpdateSupplierAsync(id,supplierDto);

            return Ok(result);
        }

        [HttpGet("lookup")]
        //[Authorize(Roles = "")]
        public async Task<IActionResult> getSupplierNames(string? searchTerm = null)
        {
            var result = await _supplierService.GetSuppliersLookupAsync(searchTerm);
            return Ok(result);
        }

        [HttpGet("mini_dashboard")]
        //[Authorize(Roles = "")]
        public async Task<IActionResult> getCounts()
        {
            var result = await _supplierService.Counts();
            return Ok(result);
        }

        [HttpPost("Import-CSV")]
        public async Task<IActionResult> ImportSuppliers(IFormFile file)
        {
            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only CSV files are allowed.");
            }

            var insertedCount = await _supplierService.ImportSuppliersFromCsvAsync(file);

            return Ok(new { Message = $"Successfully imported {insertedCount} suppliers." });
        }

    }
}
