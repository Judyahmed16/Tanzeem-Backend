using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Enums;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.Products;
using Tanzeem.Shared.Dtos.Suppliers;

namespace Tanzeem.Services.Abstractions.Suppliers
{
    public interface ISupplierService
    {
        Task<SupplierResponseDto> GetSupplierByIdAsync(int id);

        Task<PaginationResponseDto<SupplierResponseDto>> GetAllSuppliersAsync(int page, int pageSize,SupplierFilter? supplierFilter = null , SupplierSort ? supplierSort = null, string? searchTerm = null);

        Task<int> CreateSupplierAsync(SupplierRequestDto supplierDto);
       
        // Task<int> CsvUploadAsync(string filePath);

        Task<int> UpdateSupplierAsync(int id, SupplierRequestDto supplierDto);

        Task<bool> DeleteSupplierAsync(int id);

        Task<IEnumerable<SupplierLookupDto>> GetSuppliersLookupAsync(string? searchTerm = null);

        public Task<SupplierCountsDto> Counts();
        public Task<int> ImportSuppliersFromCsvAsync(IFormFile file);
    }
}
