using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Enums;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.Delivery_Issue;
using Tanzeem.Shared.Dtos.Orders;

namespace Tanzeem.Services.Abstractions.DeliveryIssues
{
    public interface IDeliveryIssuesService
    {
        public Task<int> CreateDeliveryIssue(OrderConfirmDto orderConfirmDto);
        public Task<PaginationResponseDto<DeliveryIssueDto>> GetAllDeliveryIssues(int page, int pageSize, DeliveryIssuesSort? sortId = null, string? searchTerm = null);
        public Task<DeliveryIssueDto> GetDeliveryIssueById(int id);
    }
}
