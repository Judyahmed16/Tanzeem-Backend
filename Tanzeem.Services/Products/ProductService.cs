using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.AIDemand;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Domain.Entities.Suppliers;
using Tanzeem.Domain.Exceptions;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Abstractions.Products;
using Tanzeem.Services.Current;
using Tanzeem.Shared.Dtos.Products;
using ValidationException = Tanzeem.Domain.Exceptions.ValidationException;

namespace Tanzeem.Services.Products {
    public class ProductService(IUnitOfWork _unitOfWork,
        ProductHelperService productHelperService,
        ICurrentService currentService) : IProductService {

        public async Task<ProductDto> GetProductByIdAsync(int id) {

            #region Retrieval

            var companyId = currentService.CompanyId
                ?? throw new UnauthorizedAccessException("CompanyId not found");

            var branchId = currentService.BranchId
                ?? throw new UnauthorizedAccessException("BranchId not found");

            // Single query — loads Category via Include, scoped to company
            var product = await _unitOfWork.GetRepository<Product>()
                .GetAllAsIQueryable()
                .Include(p => p.Category)
                .Include(p => p.InventoryBatches)
                .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

            if (product is null)
                throw new KeyNotFoundException("Product not found");

            // Branch-scoped inventory
            var inventory = await _unitOfWork.GetRepository<Inventory>()
                .GetAsync(i => i.ProductId == id && i.BranchId == branchId);

            if (inventory is null)
                throw new Exception("Inventory not found for this branch");
            
            #endregion

            return new ProductDto {
                Id = product.Id,
                ProductNumber = inventory.ProductNumber ?? BuildProductNumber(branchId, inventory.Id),
                Name = product.Name,
                SKU = product.SKU,
                Category = product.Category?.Name ?? "-",
                CategoryId = product.CategoryId,
                Stock = GetBranchBatchQuantity(product, branchId, inventory.Quantity ?? 0),
                CostPrice = product.CostPrice,
                SellingPrice = product.SellingPrice,
                ExpiryDate = GetNearestBranchExpiry(product, branchId) ?? product.ExpiryDate,
                Barcode = product.Barcode,
                Description = product.Description,
                ReorderLevel = product.ReorderLevel,
                Status = product.Status,
                Batches = MapBranchBatches(product, branchId)
            };
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync(int? sortId, int? filterId, string? searchQuery) {

            var products = await productHelperService.GetAllProducts(sortId, filterId, searchQuery);

            return products.Select(product => {
                var inventory = product.Inventories
                    .FirstOrDefault(i => i.BranchId == currentService.BranchId);

                return new ProductDto {
                    Id = product.Id,
                    ProductNumber = inventory?.ProductNumber ?? (inventory is null ? null : BuildProductNumber(inventory.BranchId, inventory.Id)),
                    Name = product.Name,
                    SKU = product.SKU,
                    Category = product.Category?.Name ?? "Uncategorized",
                    CategoryId = product.CategoryId,
                    CostPrice = product.CostPrice,
                    SellingPrice = product.SellingPrice,
                    ExpiryDate = GetNearestBranchExpiry(product, currentService.BranchId ?? 0) ?? product.ExpiryDate,
                    Barcode = product.Barcode,
                    Description = product.Description,
                    ReorderLevel = product.ReorderLevel,
                    Status = product.Status,
                    Stock = GetBranchBatchQuantity(
                        product,
                        currentService.BranchId ?? 0,
                        inventory?.Quantity ?? 0),
                    Batches = MapBranchBatches(product, currentService.BranchId ?? 0)
                };
            });
        }

        public async Task<IEnumerable<ProductDropdownMenuDto>> GetAllProductsMenuAsync(string? searchQuery) {

            var companyId = currentService.CompanyId
                ?? throw new UnauthorizedAccessException("CompanyId not found");

            var branchId = currentService.BranchId
                ?? throw new UnauthorizedAccessException("BranchId not found");

            var products = await _unitOfWork.GetRepository<Product>()
                .GetAllAsIQueryable()
                .Where(p => p.CompanyId == companyId
                    && p.Inventories.Any(i => i.BranchId == branchId)
                    && (string.IsNullOrEmpty(searchQuery) ||
                    p.Name.Contains(searchQuery) ||
                    p.SKU.Contains(searchQuery) ||
                    p.Barcode.Contains(searchQuery) ||
                    p.Inventories.Any(i => i.BranchId == branchId
                        && i.ProductNumber != null
                        && i.ProductNumber.Contains(searchQuery))
                ))
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Id)
                .Select(p => new {
                    Id = p.Id,
                    ProductNumber = p.Inventories
                        .Where(i => i.BranchId == branchId)
                        .Select(i => i.ProductNumber)
                        .FirstOrDefault(),
                    InventoryId = p.Inventories
                        .Where(i => i.BranchId == branchId)
                        .Select(i => i.Id)
                        .FirstOrDefault(),
                    Name = p.Name,
                    SKU = p.SKU,
                    Barcode = p.Barcode,
                    Stock = p.InventoryBatches
                              .Where(i => i.BranchId == branchId)
                              .Sum(i => i.Quantity),
                    Price = p.SellingPrice
                })
                .Take(15)
                .ToListAsync();

            return products.Select(p => new ProductDropdownMenuDto {
                Id = p.Id,
                ProductNumber = p.ProductNumber ?? BuildProductNumber(branchId, p.InventoryId),
                Name = p.Name,
                SKU = p.SKU,
                Barcode = p.Barcode,
                Stock = p.Stock,
                Price = p.Price
            });

        }

        public async Task<int> CreateProductAsync(ProductDto productDto) {

            #region Retrieval

            var companyId = currentService.CompanyId
                ?? throw new UnauthorizedAccessException("CompanyId not found");

            var branchId = currentService.BranchId
                ?? throw new UnauthorizedAccessException("BranchId not found");

            #endregion

            #region Product already registered to this company

            var existingProduct = await _unitOfWork.GetRepository<Product>()
                .GetAsync(p => p.SKU == productDto.SKU && p.CompanyId == companyId);

            if (existingProduct is not null) {

                var isFoundInInventory = await _unitOfWork.GetRepository<Inventory>()
                    .GetAsync(i => i.ProductId == existingProduct.Id && i.BranchId == branchId);

                if (isFoundInInventory is not null)
                    throw new KeyNotFoundException("Product with the same SKU already exists in this branch.");

                var tranac = await _unitOfWork.BeginTransactionAsync();
                try {
                    await AddProductToBranchAsync(branchId, existingProduct, productDto.Stock ?? 0, productDto);
                    await _unitOfWork.SaveChangesAsync();
                    await tranac.CommitAsync();
                    return existingProduct.Id;
                }
                catch {
                    await tranac.RollbackAsync();
                    throw;
                }
            }

            #endregion

            #region New product — not yet registered to this company

            var transc = await _unitOfWork.BeginTransactionAsync();
            try {

                if (string.IsNullOrWhiteSpace(productDto.Category))
                    throw new ValidationException("Category name cannot be empty");

                var category = await _unitOfWork.GetRepository<Category>()                   
                    .GetAllAsIQueryable()
                    .Where(c => c.Name == productDto.Category && c.CompanyId == companyId)
                    .FirstOrDefaultAsync();

                var existingCategoryId = category?.Id;
                if (category is null) {
                    category = new Category { Name = productDto.Category , CompanyId = companyId};
                    await _unitOfWork.GetRepository<Category>().AddAsync(category);
                }

                var product = new Product {
                    Name = productDto.Name,
                    SKU = productDto.SKU,
                    CostPrice = productDto.CostPrice,
                    SellingPrice = productDto.SellingPrice,
                    Barcode = productDto.Barcode,
                    Description = productDto.Description,
                    ReorderLevel = productDto.ReorderLevel,
                    Status = productDto.Status,
                    CompanyId = companyId
                };
                if (existingCategoryId.HasValue)
                    product.CategoryId = existingCategoryId.Value;
                else
                    product.Category = category;

                await AddProductToBranchAsync(branchId, product, productDto.Stock ?? 0, productDto);
                await _unitOfWork.GetRepository<Product>().AddAsync(product);
                await _unitOfWork.SaveChangesAsync();
                await transc.CommitAsync();

                return product.Id;
            }
            catch {
                await transc.RollbackAsync();
                throw;
            }

            #endregion
        
        }
    
        private async Task AddProductToBranchAsync(int branchId, Product product, int initialQuantity, ProductDto productDto) {
            product.Inventories ??= new List<Inventory>();
            product.Inventories.Add(new Inventory {
                ProductNumber = await GenerateProductNumberAsync(branchId),
                Quantity = initialQuantity,
                BranchId = branchId
            });

            product.InventoryBatches ??= new List<InventoryBatch>();
            if (initialQuantity > 0)
            {
                product.InventoryBatches.Add(new InventoryBatch
                {
                    Quantity = initialQuantity,
                    BranchId = branchId,
                    BatchNumber = BuildDefaultBatchNumber(productDto),
                    ExpiryDate = productDto.ExpiryDate,
                    CostPrice = productDto.CostPrice,
                    ReceivedAt = DateTime.UtcNow
                });
            }
        }
    
        public async Task<int> UpdateProductAsync(int id, ProductDto productDto) {

            #region Retrieval

            var companyId = currentService.CompanyId
                ?? throw new UnauthorizedAccessException("CompanyId not found");

            var branchId = currentService.BranchId
                ?? throw new UnauthorizedAccessException("BranchId not found");

            var product = await _unitOfWork.GetRepository<Product>()
                .GetAllAsIQueryable()
                .Include(p => p.Category)
                .Include(p => p.InventoryBatches)
                .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

            if (product is null)
                throw new Exception("Product not found");

            var inventory = await _unitOfWork.GetRepository<Inventory>()
                .GetAsync(i => i.ProductId == id && i.BranchId == branchId);

            if (inventory is null)
                throw new Exception("Inventory not found for this branch");

            #endregion

            #region Update

            // Product fields
            product.Name = productDto.Name;
            product.Description = productDto.Description;
            product.SKU = productDto.SKU;
            product.CostPrice = productDto.CostPrice;
            product.SellingPrice = productDto.SellingPrice;
            product.Barcode = productDto.Barcode;
            product.ReorderLevel = productDto.ReorderLevel;
            product.Status = productDto.Status;

            // Inventory field
            inventory.Quantity = productDto.Stock;
            SyncDefaultBranchBatch(product, branchId, productDto.Stock ?? 0, productDto);

            #endregion

            #region Category name change

            // Category — DB-level lookup, not in-memory
            if (string.IsNullOrWhiteSpace(productDto.Category))
                throw new BusinessRuleException("Category name cannot be empty");

            var categoryMatch = await _unitOfWork.GetRepository<Category>()
                    .GetAllAsIQueryable()
                    .Where(c => c.Name == productDto.Category && c.CompanyId == companyId)
                    .FirstOrDefaultAsync();

            if (categoryMatch is null) {
                var newCategory = new Category { Name = productDto.Category , CompanyId = companyId };
                await _unitOfWork.GetRepository<Category>().AddAsync(newCategory);
                product.Category = newCategory;
            }
            else {
                product.Category = categoryMatch;
            }

            #endregion

            _unitOfWork.GetRepository<Product>().UpdateAsync(product);
            await _unitOfWork.SaveChangesAsync();
            return product.Id;
        
        }

        public async Task<bool> DeletedProductAsync(int id) {

            #region Retrieval

            var companyId = currentService.CompanyId
                ?? throw new UnauthorizedAccessException("CompanyId not found");

            var branchId = currentService.BranchId
                ?? throw new UnauthorizedAccessException("BranchId not found");

            var product = await _unitOfWork.GetRepository<Product>()
                .GetAllAsIQueryable()
                .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

            if (product is null)
                throw new Exception("Product not found");

            var inventory = await _unitOfWork.GetRepository<Inventory>()
                .GetAsync(i => i.ProductId == id && i.BranchId == branchId);

            if (inventory is null)
                throw new Exception("Inventory not found for this branch");
            
            var forecast = await _unitOfWork.GetRepository<DemandForecast>()
                .GetAsync(d => d.ProductId == id && d.BranchId == branchId);

            var batches = await _unitOfWork.GetRepository<InventoryBatch>()
                .GetAllAsIQueryable()
                .Where(b => b.ProductId == id && b.BranchId == branchId)
                .ToListAsync();


            #endregion

            _unitOfWork.GetRepository<Inventory>().DeleteAsync(inventory);
            if (batches.Any())
            {
                _unitOfWork.GetRepository<InventoryBatch>().DeleteRangeAsync(batches);
            }
            if (forecast is not null)
            {
                _unitOfWork.GetRepository<DemandForecast>().DeleteAsync(forecast);
            }
            _unitOfWork.GetRepository<Product>().DeleteAsync(product);

            var count = await _unitOfWork.SaveChangesAsync();
            return count > 0;
        }

        private static string BuildDefaultBatchNumber(ProductDto productDto)
            => string.IsNullOrWhiteSpace(productDto.BatchNumber)
                ? $"DEFAULT-{DateTime.UtcNow:yyyyMMddHHmmss}"
                : productDto.BatchNumber.Trim();

        private static int GetBranchBatchQuantity(Product product, int branchId, int fallbackQuantity)
        {
            var branchBatches = product.InventoryBatches?
                .Where(b => b.BranchId == branchId)
                .ToList() ?? new List<InventoryBatch>();

            return branchBatches.Any()
                ? branchBatches.Sum(b => b.Quantity)
                : fallbackQuantity;
        }

        private static DateTime? GetNearestBranchExpiry(Product product, int branchId)
            => product.InventoryBatches?
                .Where(b => b.BranchId == branchId && b.Quantity > 0 && b.ExpiryDate.HasValue)
                .OrderBy(b => b.ExpiryDate)
                .Select(b => b.ExpiryDate)
                .FirstOrDefault();

        private static IEnumerable<InventoryBatchDto> MapBranchBatches(Product product, int branchId)
            => product.InventoryBatches?
                .Where(b => b.BranchId == branchId)
                .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
                .ThenBy(b => b.ReceivedAt)
                .Select(b => new InventoryBatchDto
                {
                    Id = b.Id,
                    BatchNumber = b.BatchNumber,
                    Quantity = b.Quantity,
                    ExpiryDate = b.ExpiryDate,
                    CostPrice = b.CostPrice,
                    ReceivedAt = b.ReceivedAt
                })
                .ToList() ?? new List<InventoryBatchDto>();

        private static void SyncDefaultBranchBatch(Product product, int branchId, int requestedQuantity, ProductDto productDto)
        {
            product.InventoryBatches ??= new List<InventoryBatch>();
            var branchBatches = product.InventoryBatches.Where(b => b.BranchId == branchId).ToList();
            var currentBatchQuantity = branchBatches.Sum(b => b.Quantity);
            var delta = requestedQuantity - currentBatchQuantity;

            if (!branchBatches.Any() && requestedQuantity > 0)
            {
                product.InventoryBatches.Add(new InventoryBatch
                {
                    ProductId = product.Id,
                    BranchId = branchId,
                    BatchNumber = BuildDefaultBatchNumber(productDto),
                    Quantity = requestedQuantity,
                    ExpiryDate = productDto.ExpiryDate,
                    CostPrice = productDto.CostPrice,
                    ReceivedAt = DateTime.UtcNow
                });
                return;
            }

            var targetBatch = branchBatches
                .OrderByDescending(b => b.ReceivedAt)
                .FirstOrDefault();

            if (targetBatch is null) return;

            targetBatch.Quantity = Math.Max(0, targetBatch.Quantity + delta);
            if (!string.IsNullOrWhiteSpace(productDto.BatchNumber))
                targetBatch.BatchNumber = productDto.BatchNumber.Trim();
            targetBatch.ExpiryDate = productDto.ExpiryDate;
            targetBatch.CostPrice = productDto.CostPrice;
        }

        public async Task<int> CsvUploadAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ValidationException("Please upload a valid CSV file.");

            int companyId = currentService.CompanyId ?? throw new UnauthorizedAccessException("No company assigned.");
            int branchId = currentService.BranchId ?? throw new UnauthorizedAccessException("No branch assigned.");

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                MissingFieldFound = null,
                HeaderValidated = null,
                PrepareHeaderForMatch = args => args.Header.ToLower()
            };

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, config);

            csv.Read();
            csv.ReadHeader();

            int rowIndex = 1;
            var validationErrors = new List<string>();

            var existingSkus = await _unitOfWork.GetRepository<Product>().GetAllAsIQueryable()
                .Where(p => p.CompanyId == companyId && !string.IsNullOrEmpty(p.SKU))
                .Select(p => p.SKU.ToLower())
                .ToListAsync();

            var existingBarcodes = await _unitOfWork.GetRepository<Product>().GetAllAsIQueryable()
                .Where(p => p.CompanyId == companyId && !string.IsNullOrEmpty(p.Barcode))
                .Select(p => p.Barcode.ToLower())
                .ToListAsync();

            var existingCategories = await _unitOfWork.GetRepository<Category>().GetAllAsIQueryable()
            .Where(c => c.CompanyId == companyId)
            .AsTracking()
            .ToDictionaryAsync(c => c.Name.ToLower(), c => c);

            var skusInCsv = new HashSet<string>();
            var barcodesInCsv = new HashSet<string>();

            var productsToInsert = new List<Product>();
            var inventoriesToInsert = new List<Inventory>();
            var inventoryBatchesToInsert = new List<InventoryBatch>();
            var nextProductSequence = await GetNextProductSequenceAsync(branchId);

            while (csv.Read())
            {
                rowIndex++;

                string name = csv.GetField<string>("name") ?? csv.GetField<string>("product name") ?? "";
                string sku = csv.GetField<string>("sku")?.Trim() ?? "N/A";
                string barcode = csv.GetField<string>("barcode")?.Trim() ?? "N/A";
                string costPriceStr = csv.GetField<string>("cost price") ?? "0";
                string sellingPriceStr = csv.GetField<string>("selling price") ?? "0";
                string expiryDateStr = csv.GetField<string>("batch expiry") ?? csv.GetField<string>("expiry date") ?? "";
                string batchNumber = csv.GetField<string>("batch number")?.Trim() ?? "";
                string description = csv.GetField<string>("description") ?? "N/A";
                string reorderLevelStr = csv.GetField<string>("reorder Level") ?? "0";
                string status = csv.GetField<string>("status") ?? "Active";
                string categoryName = csv.GetField<string>("category name") ?? "N/A";
                string quantityStr = csv.GetField<string>("quantity") ?? "0";

                #region Validation & Parsing

                if (string.IsNullOrWhiteSpace(name))
                {
                    validationErrors.Add($"Row {rowIndex}: 'Name' is required.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(sku))
                {
                    validationErrors.Add($"Row {rowIndex}: 'SKU' is required.");
                    continue;
                }

                string skuLower = sku.ToLower();
                if (existingSkus.Contains(skuLower))
                    validationErrors.Add($"Row {rowIndex}: SKU '{sku}' already exists in the system.");
                else if (skusInCsv.Contains(skuLower))
                    validationErrors.Add($"Row {rowIndex}: SKU '{sku}' is duplicated within the CSV file.");
                else
                    skusInCsv.Add(skuLower);

                if (!string.IsNullOrWhiteSpace(barcode))
                {
                    string barcodeLower = barcode.ToLower();
                    if (existingBarcodes.Contains(barcodeLower))
                        validationErrors.Add($"Row {rowIndex}: Barcode '{barcode}' already exists in the system.");
                    else if (barcodesInCsv.Contains(barcodeLower))
                        validationErrors.Add($"Row {rowIndex}: Barcode '{barcode}' is duplicated within the CSV file.");
                    else
                        barcodesInCsv.Add(barcodeLower);
                }


                if (!decimal.TryParse(costPriceStr, out decimal costPrice))
                    validationErrors.Add($"Row {rowIndex}: 'Cost Price' ({costPriceStr}) is not a valid number.");

                if (!decimal.TryParse(sellingPriceStr, out decimal sellingPrice))
                    validationErrors.Add($"Row {rowIndex}: 'Selling Price' ({sellingPriceStr}) is not a valid number.");
                
                if (!int.TryParse(quantityStr, out int quantity))
                    validationErrors.Add($"Row {rowIndex}: 'Quantity' ({quantityStr}) is not a valid number.");

                
                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    validationErrors.Add($"Row {rowIndex}: 'Category Name' is required.");
                    continue;
                }

                int reorderLevel = 0;
                if (!string.IsNullOrWhiteSpace(reorderLevelStr) && !int.TryParse(reorderLevelStr, out reorderLevel))
                    validationErrors.Add($"Row {rowIndex}: 'Reorder Level' ({reorderLevelStr}) must be a valid integer.");

                DateTime? batchExpiryDate = null;
                if (!string.IsNullOrWhiteSpace(expiryDateStr))
                {
                    if (!DateTime.TryParse(expiryDateStr, out var parsedExpiryDate))
                        validationErrors.Add($"Row {rowIndex}: 'Batch Expiry' ({expiryDateStr}) is not a valid date format.");
                    else
                    {
                        batchExpiryDate = parsedExpiryDate;
                    }
                }


                string categoryKey = categoryName.ToLower();
                if (!existingCategories.TryGetValue(categoryKey, out Category? categoryObj))
                {
                    categoryObj = new Category
                    {
                        Name = categoryName,
                        CompanyId = companyId
                    };
                    existingCategories[categoryKey] = categoryObj;
                }
                #endregion


                if (!validationErrors.Any(e => e.StartsWith($"Row {rowIndex}:")))
                {
                    var product = new Product
                    {
                        Name = name,
                        SKU = sku,
                        Barcode = string.IsNullOrWhiteSpace(barcode) ? "N/A" : barcode,
                        CostPrice = costPrice,
                        SellingPrice = sellingPrice,
                        Description = description,
                        ReorderLevel = reorderLevel,
                        Status = status,
                        Category = categoryObj,
                        CompanyId = companyId
                    };

                    productsToInsert.Add(product);

                    var inventory = new Inventory
                    {
                        Product = product,
                        BranchId = branchId,
                        ProductNumber = BuildProductNumber(branchId, nextProductSequence++),
                        Quantity = quantity 
                    };
                    inventoriesToInsert.Add(inventory);

                    if (quantity > 0)
                    {
                        inventoryBatchesToInsert.Add(new InventoryBatch
                        {
                            Product = product,
                            BranchId = branchId,
                            BatchNumber = string.IsNullOrWhiteSpace(batchNumber)
                                ? $"CSV-{sku}-{rowIndex}"
                                : batchNumber,
                            Quantity = quantity,
                            ExpiryDate = batchExpiryDate,
                            CostPrice = costPrice,
                            ReceivedAt = DateTime.UtcNow
                        });
                    }

                }
            }

            if (validationErrors.Any())
            {
                string detailedErrorMessage = string.Join(" | ", validationErrors);
                throw new ValidationException($"CSV Validation Failed: {detailedErrorMessage}");
            }

            if (productsToInsert.Any() || inventoriesToInsert.Any() || inventoryBatchesToInsert.Any())
            {
                await _unitOfWork.GetRepository<Product>().AddRangeAsync(productsToInsert);
                await _unitOfWork.GetRepository<Inventory>().AddRangeAsync(inventoriesToInsert);
                await _unitOfWork.GetRepository<InventoryBatch>().AddRangeAsync(inventoryBatchesToInsert);
                await _unitOfWork.SaveChangesAsync();
            }

            return productsToInsert.Count;
        }

        private async Task<string> GenerateProductNumberAsync(int branchId)
            => BuildProductNumber(branchId, await GetNextProductSequenceAsync(branchId));

        private async Task<int> GetNextProductSequenceAsync(int branchId)
        {
            var prefix = BuildNumberPrefix(branchId, "PRD");
            var productNumbers = await _unitOfWork.GetRepository<Inventory>()
                .GetAllAsIQueryable()
                .Where(i => i.BranchId == branchId && i.ProductNumber != null && i.ProductNumber.StartsWith(prefix))
                .Select(i => i.ProductNumber!)
                .ToListAsync();

            return productNumbers
                .Select(number => TryReadSequence(number, prefix))
                .DefaultIfEmpty(0)
                .Max() + 1;
        }

        private static string BuildProductNumber(int branchId, int sequence)
            => $"{BuildNumberPrefix(branchId, "PRD")}{Math.Max(sequence, 1):D4}";

        private static string BuildNumberPrefix(int branchId, string type)
            => $"B{branchId:D3}-{type}-";

        private static int TryReadSequence(string value, string prefix)
            => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(value[prefix.Length..], out var sequence)
                    ? sequence
                    : 0;

    }
}
