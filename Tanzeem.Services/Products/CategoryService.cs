using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Domain.Exceptions;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Abstractions.Products;
using Tanzeem.Shared.Dtos.Products;

namespace Tanzeem.Services.Products
{
    public class CategoryService(IUnitOfWork _unitOfWork, ICurrentService _currentService) : ICategoryService
    {
        private int GetCompanyId() =>
            _currentService.CompanyId ?? throw new UnauthorizedAccessException("No company assigned.");

        public async Task<int> CreateAsync(CategoryDto dto)
        {
            int companyId = GetCompanyId();

            bool exists = await _unitOfWork.GetRepository<Category>().GetAllAsIQueryable()
                .AnyAsync(c => c.CompanyId == companyId && c.Name.ToLower() == dto.Name.ToLower());

            if (exists)
                throw new ValidationException($"Category '{dto.Name}' already exists.");

            var category = new Category
            {
                Name = dto.Name,
                CompanyId = companyId
            };

            await _unitOfWork.GetRepository<Category>().AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            return category.Id;
        }

        public async Task UpdateAsync(CategoryDto dto)
        {
            int companyId = GetCompanyId();

            var category = await _unitOfWork.GetRepository<Category>().GetAllAsIQueryable()
                .AsTracking()
                .FirstOrDefaultAsync(c => c.Id == dto.Id && c.CompanyId == companyId);

            if (category == null)
                throw new KeyNotFoundException("Category not found.");

            bool nameExists = await _unitOfWork.GetRepository<Category>().GetAllAsIQueryable()
                .AnyAsync(c => c.CompanyId == companyId && c.Id != dto.Id && c.Name.ToLower() == dto.Name.ToLower());

            if (nameExists)
                throw new ValidationException($"Category '{dto.Name}' already exists.");

            category.Name = dto.Name;

            _unitOfWork.GetRepository<Category>().UpdateAsync(category);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            int companyId = GetCompanyId();

            var category = await _unitOfWork.GetRepository<Category>().GetAllAsIQueryable()
                .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

            if (category == null)
                throw new KeyNotFoundException("Category not found.");

            bool hasProducts = await _unitOfWork.GetRepository<Product>().GetAllAsIQueryable()
                .AnyAsync(p => p.CategoryId == id);

            if (hasProducts)
                throw new ValidationException("Cannot delete this category because it contains products.");

            _unitOfWork.GetRepository<Category>().DeleteAsync(category);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            int companyId = GetCompanyId();

            return await _unitOfWork.GetRepository<Category>().GetAllAsIQueryable()
                .Where(c => c.CompanyId == companyId)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<CategoryDto>> LookupAsync(string? searchTerm = null)
        {
            int companyId = GetCompanyId();

            var query = _unitOfWork.GetRepository<Category>().GetAllAsIQueryable()
                .Where(c => c.CompanyId == companyId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c => c.Name.Contains(searchTerm));
            }

            return await query
                .OrderBy(c => c.Name)
                .ThenBy(c => c.Id)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .Take(50)
                .ToListAsync();
        }
    }
}
