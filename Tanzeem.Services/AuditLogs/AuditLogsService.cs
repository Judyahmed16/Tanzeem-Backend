using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.AuditLogs;
using Tanzeem.Services.Abstractions.AuditLogs;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.AuditLogs;

namespace Tanzeem.Services.AuditLogs
{
    public class AuditLogsService(IUnitOfWork unitOfWork , ICurrentService currentService) : IAuditLogsService
    {
        public async Task<PaginationResponseDto<AuditLogsDto>> ViewAllAudits(int page, int pageSize)
        {
            int BranchId = currentService.BranchId ?? throw new UnauthorizedAccessException();
            if (page <= 0) page = 1;

            const int maxPageSize = 20;

            if (pageSize > maxPageSize) pageSize = maxPageSize;

            var query = unitOfWork.GetRepository<AuditTrial>().GetAllAsIQueryable().Include(x => x.User)
                .Where(x => x.BranchId == BranchId).OrderByDescending(x => x.CreatedAt);

            int count = await query.CountAsync();

            var logs = query.Select(a => new AuditLogsDto
            {
                Action = a.Action,
                EntityName = a.EntityName,
                CreatedAt = a.CreatedAt,
                EntityPrimaryKey = a.EntityPrimaryKey,
                Id = a.Id,
                NewValue = a.NewValue,
                OldValue = a.OldValue,
                UserId = a.UserId ?? 0,
                UserName = a.User.Name ?? "System",
                
            });

            var data = await logs
                .OrderByDescending(a => a.CreatedAt)
                .ThenByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize).ToListAsync();

            return new PaginationResponseDto<AuditLogsDto>
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = count,
                Data = data
            };
        }

        public async Task<PaginationResponseDto<AuditLogsDto>> ViewAuditsPerProfile(int page, int pageSize)
        {
            int userId = currentService.UserId ?? throw new UnauthorizedAccessException();
            
            if (page <= 0) page = 1;

            const int maxPageSize = 20;

            if (pageSize > maxPageSize) pageSize = maxPageSize;

            var query = unitOfWork.GetRepository<AuditTrial>().GetAllAsIQueryable().Include(x => x.User)
                .Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt);

            int count = await query.CountAsync();

            var logs = query.Select(a => new AuditLogsDto
            {
                Action = a.Action,
                EntityName = a.EntityName,
                CreatedAt = a.CreatedAt,
                EntityPrimaryKey = a.EntityPrimaryKey,
                Id = a.Id,
                NewValue = a.NewValue,
                OldValue = a.OldValue,
                UserId = a.UserId ?? 0,
                UserName = a.User.Name ?? "System",

            });

            var data = await logs
                .OrderByDescending(a => a.CreatedAt)
                .ThenByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize).ToListAsync();

            return new PaginationResponseDto<AuditLogsDto>
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = count,
                Data = data
            };
        }
    }
}
