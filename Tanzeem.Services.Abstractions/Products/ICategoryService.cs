using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Shared.Dtos.Products;

namespace Tanzeem.Services.Abstractions.Products
{
    public interface ICategoryService
    {
        Task<int> CreateAsync(CategoryDto dto);
        Task UpdateAsync(CategoryDto dto);
        Task DeleteAsync(int id);
        Task<IEnumerable<CategoryDto>> GetAllAsync();
        Task<IEnumerable<CategoryDto>> LookupAsync(string? searchTerm = null);
    }
}
