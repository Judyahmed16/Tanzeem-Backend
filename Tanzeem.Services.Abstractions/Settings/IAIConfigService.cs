using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Settings;
using Tanzeem.Shared.Dtos.Settings;

namespace Tanzeem.Services.Abstractions.Settings
{
    public interface IAIConfigService
    {
        public Task<AIConfigurationsDto> CreateAIConfigurations(int branchId);
        public Task<AIConfigurationsDto> GetIConfigurationsAsync();
        public Task<AIConfigurationsDto> UpdateIConfigurationsAsync(AIConfigurationsDto aIConfigurationsDto);
    }
}
