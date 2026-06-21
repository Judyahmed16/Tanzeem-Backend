using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.CustomExceptions;
using Tanzeem.Domain.Entities.DeliveryIssues;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Suppliers;
using Tanzeem.Domain.Enums;
using Tanzeem.Domain.Exceptions;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Abstractions.DeliveryIssues;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.Delivery_Issue;
using Tanzeem.Shared.Dtos.Orders;

namespace Tanzeem.Services.DeliveryIssues
{
    public class DeliveryIssuesService(IUnitOfWork _unitOfWork,ICurrentService _currentService) : IDeliveryIssuesService
    {
        public async Task<int> CreateDeliveryIssue(OrderConfirmDto orderConfirmDto)
        {
            //int branchId = 2;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 
            
            var order = await _unitOfWork.GetRepository<Order>()
                .GetByIdAsQueryable(orderConfirmDto.OrderId)
                .Where(order => order.BranchId == branchId)
                .Include(x => x.Supplier)
                .Include(x => x.Items)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                throw new KeyNotFoundException("no order found with this id");
            }

            int itemsAffectedCount = 0;
            int totalDiscrepancy = 0;
            var deliveryIssueItemsList = new List<DeliveryIssueItem>();

            foreach (var itemConfirm in orderConfirmDto.ItemsConfirmDtos)
            {
                var validIssues = itemConfirm.ItemsIssueDtos.Where(issue => issue.Quantity > 0).ToList();

                if (validIssues.Any())
                {
                    itemsAffectedCount++;

                    var originalOrderItem = order.Items.FirstOrDefault(i => i.ProductId == itemConfirm.ProductId);

                    if (originalOrderItem != null)
                    {
                        foreach (var issue in validIssues)
                        {
                            totalDiscrepancy += issue.Quantity;

                            deliveryIssueItemsList.Add(new DeliveryIssueItem
                            {
                                OrderItemId = originalOrderItem.Id, 
                                IssueType = issue.IssueType,
                                Quantity = issue.Quantity,
                                Notes = itemConfirm.Notes 
                            });
                        }
                    }
                }
            }
            if (!deliveryIssueItemsList.Any())
            {
                return 0;
            }
            var lastDeliveryIssue = await _unitOfWork.GetRepository<DeliveryIssue>().GetAllAsIQueryable()
                .Where(d => d.BranchId == branchId)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastDeliveryIssue != null && !string.IsNullOrWhiteSpace(lastDeliveryIssue.DeliveryIssueNumber))
            {
                string[] numberParts = lastDeliveryIssue.DeliveryIssueNumber.Split('-');
                if (numberParts.Length > 0 && int.TryParse(numberParts.Last(), out int lastSeq))
                {
                    nextNumber = lastSeq + 1;
                }
            }

            string generatedIssueNumber = $"ISS-{nextNumber:D4}";
            DeliveryIssue deliveryIssue = new DeliveryIssue()
            {
                BranchId = order.BranchId,
                DeliveryIssueNumber = generatedIssueNumber,
                OrderId = orderConfirmDto.OrderId,
                RecieveDate = orderConfirmDto.RecievedDate ?? DateTime.UtcNow,
                SupplierId = order!.SupplierId ?? 0,
                SupplierName = order.SupplierName,
                ItemsAffected = itemsAffectedCount,
                Discrepancy = totalDiscrepancy,
                DeliveryIssueItem = deliveryIssueItemsList, //items
                Notes = orderConfirmDto.Notes,
            };

            await _unitOfWork.GetRepository<DeliveryIssue>().AddAsync(deliveryIssue);
            int affectedRows = await _unitOfWork.SaveChangesAsync();

            if (affectedRows <= 0)
            {
                throw new DbUpdateFailedException("failed to create issues");
            }
            return deliveryIssue.Id;
        }

        public async Task<PaginationResponseDto<DeliveryIssueDto>> GetAllDeliveryIssues(int page, int pageSize, DeliveryIssuesSort? sortId = null, string? searchTerm = null)
        {
            //int branchId = 2;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 
            
            if (page <= 0) page = 1;

            const int maxPageSize = 20;

            if (pageSize > maxPageSize) pageSize = maxPageSize;

            var query = _unitOfWork.GetRepository<DeliveryIssue>()
                .GetAllAsIQueryable()
                .Where(x => x.BranchId == branchId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var cleanSearch = searchTerm.Trim().ToLower();

                bool isNumber = int.TryParse(cleanSearch, out int searchNumber);
                bool isDate = DateTime.TryParse(searchTerm, out DateTime searchDate);

                query = query.Where(issue =>
                        issue.SupplierName.Contains(cleanSearch) ||
                        issue.DeliveryIssueNumber.Contains(cleanSearch) ||
                        (issue.Notes != null && issue.Notes.Contains(cleanSearch)) ||
                        (isNumber && (issue.Id == searchNumber || issue.SupplierId == searchNumber 
                        || issue.OrderId == searchNumber || issue.Discrepancy == searchNumber || issue.ItemsAffected == searchNumber)) ||

                        (isDate && (issue.RecieveDate == searchDate))
                      
                );

            }

            var totalCount = await query.CountAsync();

            if (sortId.HasValue)
            {
                switch (sortId.Value)
                {
                    case DeliveryIssuesSort.HighItemsAffected:
                        query = query.OrderByDescending(x => x.ItemsAffected);
                        break;
                    case DeliveryIssuesSort.LowItemsAffected:
                        query = query.OrderBy(x => x.ItemsAffected);
                        break;
                    case DeliveryIssuesSort.NearDate:
                        query = query.OrderByDescending(x => x.RecieveDate);
                        break;
                    case DeliveryIssuesSort.FarDate:
                        query = query.OrderBy(x => x.RecieveDate);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(x => x.RecieveDate);
            }

            var deliveryIssues = await query
                .Include(d => d.Supplier)
                .Include(d => d.DeliveryIssueItem)
                    .ThenInclude(issue => issue.OrderItem)
                    .ThenInclude(orderItem => orderItem.Product)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var deliveryIssuesDtos = deliveryIssues
                .Select(d => new DeliveryIssueDto
                {
                    Id = d.Id,
                    StringId = d.DeliveryIssueNumber,
                    ItemsAffected = d.ItemsAffected,
                    Discrepancy = d.Discrepancy,
                    Notes = d.Notes,
                    OrderId = d.OrderId,
                    RecievedDate = d.RecieveDate,
                    SupplierName = d.SupplierName,
                    SupplierId = d.SupplierId ?? 0,
                    SupplierEmail = d.Supplier?.Email ?? "",
                    SupplierPhone = d.Supplier?.PhoneNumberOne ?? "",
                    Items = d.DeliveryIssueItem
                        .GroupBy(issue => issue.OrderItemId)
                        .Select(group =>
                        {
                            var orderItem = group.FirstOrDefault()?.OrderItem;
                            return new DeliveryIssueItemDto
                            {
                                ProductId = orderItem?.ProductId ?? 0,
                                ProductName = orderItem?.Product?.Name ?? "Unknown",
                                SKU = orderItem?.Product?.SKU ?? "N/A",
                                OrderedQuantity = orderItem?.Quantity ?? 0,
                                Issues = group.Select(issueDetails => new ItemIssuesDto
                                {
                                    IssueType = issueDetails.IssueType,
                                    Quantity = issueDetails.Quantity
                                }).ToList()
                            };
                        }).ToList()
                })
                .ToList();


            #region old code

                        //            .Include(x => x.Supplier)
                        //.Include(x => x.Order)
                        //.ThenInclude(o => o.Items)
                        //.ThenInclude(i => i.Product)
                        //.Include(x => x.DeliveryIssueItem) //items
            //var deliveryIssuesDtos = issuesFromDb.Select(d => new DeliveryIssueDto
            //{
            //    Id = d.Id,
            //    StringId = $"ISS-{d.Id:D4}",
            //    ItemsAffected = d.ItemsAffected,
            //    Discrepancy = d.Discrepancy,
            //    Notes = d.Notes,

            //    OrderId = d.OrderId,
            //    RecievedDate = d.RecieveDate,

            //    SupplierName = d.SupplierName,
            //    SupplierId = d.SupplierId,
            //    SupplierEmail = d.Supplier?.Email ?? "",
            //    SupplierPhone = d.Supplier?.PhoneNumberOne ?? "",

            //    Items = d.DeliveryIssueItem.GroupBy(issue => issue.OrderItemId)
            //    .Select(group =>
            //    {
            //        var originalOrderItem = d?.Order?.Items?.FirstOrDefault(oi => oi.Id == group.Key);

            //        return new DeliveryIssueItemDto
            //        {
            //            ProductId = originalOrderItem?.ProductId ?? 0,
            //            ProductName = originalOrderItem?.Product.Name ?? "Un-known",
            //            SKU = originalOrderItem?.Product.SKU ?? "N/A",
            //            OrderedQuantity = originalOrderItem?.Quantity ?? 0,


            //            Issues = group.Select(issueDetails => new ItemIssuesDto
            //            {
            //                IssueType = issueDetails.IssueType,
            //                Quantity = issueDetails.Quantity
            //            }).ToList()
            //        };
            //    }).ToList() //end of big select

            //    }).ToListAsync();
            #endregion
            return new PaginationResponseDto<DeliveryIssueDto>
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = deliveryIssuesDtos
            };
        }
        
        public async Task<DeliveryIssueDto> GetDeliveryIssueById(int id)
        {
            //int branchId = 2;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 

            var deliveryIssue = await _unitOfWork.GetRepository<DeliveryIssue>()
                .GetByIdAsQueryable(id)
                .Where(x => x.BranchId == branchId) 
                .Include(x => x.Supplier)
                    .Include(x => x.Order)
                    .Include(x => x.DeliveryIssueItem)//items
                    .ThenInclude(x => x.OrderItem)
                    .ThenInclude(x => x.Product)
                    .FirstOrDefaultAsync();

            if (deliveryIssue == null || deliveryIssue.BranchId != branchId)
            {
                throw new KeyNotFoundException("No delivery issue found with this id");
            }

            var deliveryItemsIssues = deliveryIssue.DeliveryIssueItem
                .GroupBy(issue => issue.OrderItemId)
                .Select(group =>
                {
                var originalOrderItem = group.First().OrderItem;

                return new DeliveryIssueItemDto
                {
                ProductId = originalOrderItem?.ProductId ?? 0,
                ProductName = originalOrderItem?.Product?.Name ?? "Unknown Product",
                SKU = originalOrderItem?.Product?.SKU ?? "N/A",
                OrderedQuantity = originalOrderItem?.Quantity ?? 0,

                Issues = group.Select(issueDetails => new ItemIssuesDto
                {
                    IssueType = issueDetails.IssueType,
                    Quantity = issueDetails.Quantity
                }).ToList()
                };
            }).ToList();

            return new DeliveryIssueDto
            {
               Id = deliveryIssue.Id,
               StringId = deliveryIssue.DeliveryIssueNumber, 
               ItemsAffected = deliveryIssue.ItemsAffected,
               Discrepancy = deliveryIssue.Discrepancy,
               Notes = deliveryIssue.Notes,

               OrderId = deliveryIssue.OrderId,
               RecievedDate = deliveryIssue.RecieveDate,

               SupplierId = deliveryIssue.SupplierId ?? 0,
               SupplierName = deliveryIssue.SupplierName,
               SupplierEmail = deliveryIssue.Supplier?.Email ?? "",
               SupplierPhone = deliveryIssue.Supplier?.PhoneNumberOne ?? "",

               Items = deliveryItemsIssues

            };


        }
        

    }
}
