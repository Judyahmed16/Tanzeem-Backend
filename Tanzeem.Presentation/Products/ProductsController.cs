using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Services.Abstractions.Products;
using Tanzeem.Shared.Dtos.Products;

namespace Tanzeem.Presentation.Products {

    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController(IProductService productService) : ControllerBase {

        [HttpGet]
        [Route("Get-Products")]
        [Authorize]
        public async Task<IActionResult> GetAllProducts(int? sortId, int? filterId, string? searchQuery) {
            var result = await productService.GetAllProductsAsync(sortId, filterId, searchQuery);
            return Ok(result);
        }

        [HttpGet]
        [Route("Get-Products-Dropdown-Menu")]
        [Authorize]
        public async Task<IActionResult> GetAllProductsMenu(string? searchQuery) {
            var result = await productService.GetAllProductsMenuAsync(searchQuery);
            return Ok(result);
        }

        [HttpPost]
        [Route("Create-Product")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> CreateProduct(ProductDto dto) {
            var result = await productService.CreateProductAsync(dto);
            return Ok(result);
        }

        [HttpGet]
        [Route("Get-Product/{id}")]
        [Authorize]
        public async Task<IActionResult> GetProductById(int id) {
            var result = await productService.GetProductByIdAsync(id);
            return Ok(result);
        }

        [HttpPut]
        [Route("Update-Product/{id}")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> UpdateProduct(int id, ProductDto dto) {
            var result = await productService.UpdateProductAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete]
        [Route("Delete-Product/{id}")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> DeleteProduct(int id) {
            var result = await productService.DeletedProductAsync(id);
            return Ok(result);
        }
        [HttpPost]
        [Route("Import-CSV")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> ImportProducts(IFormFile file) {
            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only CSV files are allowed.");
            }

            var insertedCount = await productService.CsvUploadAsync(file);

            return Ok(new { Message = $"Successfully imported {insertedCount} products." });
        }


    }
}
