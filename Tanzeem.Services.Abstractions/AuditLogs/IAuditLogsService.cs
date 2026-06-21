using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.AuditLogs;

namespace Tanzeem.Services.Abstractions.AuditLogs
{
    public interface IAuditLogsService
    {
        public Task<PaginationResponseDto<AuditLogsDto>> ViewAllAudits(int page, int pageSize);
        public Task<PaginationResponseDto<AuditLogsDto>> ViewAuditsPerProfile(int page, int pageSize);
    }
}
