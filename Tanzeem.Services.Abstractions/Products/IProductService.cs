using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Shared.Dtos.Products;

namespace Tanzeem.Services.Abstractions.Products {
    public interface IProductService {

        // Get
        Task<ProductDto> GetProductByIdAsync(int id);
        Task<IEnumerable<ProductDto>> GetAllProductsAsync(int? sortId, int? filterId, string? searchQuery);
        Task<IEnumerable<ProductDropdownMenuDto>> GetAllProductsMenuAsync(string? searchQuery);

        // Post
        Task<int> CreateProductAsync(ProductDto productDto);
        Task<int> CsvUploadAsync(IFormFile file);

        // Put
        Task<int> UpdateProductAsync(int id, ProductDto productDto);

        // Delete
        Task<bool> DeletedProductAsync(int id);

    }
}
