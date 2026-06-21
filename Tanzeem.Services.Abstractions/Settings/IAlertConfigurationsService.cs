using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Settings;
using Tanzeem.Shared.Dtos.Settings;


namespace Tanzeem.Services.Abstractions.Settings
{
    public interface IAlertConfigurationsService
    {
        Task<int> CreateDefaultAlertsConfigurationsAsync(int branchId);

        Task<AlertConfigurationsDto> GetAlertConfigurations();

        Task<AlertConfigurationsDto> UpdateAlertConfigurations(AlertConfigurationsDto alertConfigurationsDto);
    }
}
