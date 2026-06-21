using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Constants;
using Tanzeem.Services.Abstractions.Products;
using Tanzeem.Shared.Dtos.Products;

namespace Tanzeem.Presentation.Products
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public class CategoriesController(ICategoryService _categoryService) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryDto dto)
        {
            var id = await _categoryService.CreateAsync(dto);
            return Ok(new { Message = "Category created successfully", Id = id });
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] CategoryDto dto)
        {
            await _categoryService.UpdateAsync(dto);
            return Ok(new { Message = "Category updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _categoryService.DeleteAsync(id);
            return Ok(new { Message = "Category deleted successfully" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _categoryService.GetAllAsync();
            return Ok(categories);
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> Lookup([FromQuery] string? searchTerm)
        {
            var lookupData = await _categoryService.LookupAsync(searchTerm);
            return Ok(lookupData);
        }
    }
}
