using Microsoft.EntityFrameworkCore;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.CustomExceptions;
using Tanzeem.Domain.Entities.Settings;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Abstractions.Settings;
using Tanzeem.Shared.Dtos.Settings;

namespace Tanzeem.Services.Settings
{
    public class AlertConfigurationsService(IUnitOfWork _unitOfWork,ICurrentService _currentService) : IAlertConfigurationsService
    {
        public async Task<int> CreateDefaultAlertsConfigurationsAsync(int branchId)
        {
            AlertConfigurations alertConfigurations = new AlertConfigurations()
            {
                BranchId = branchId,

                DaysBeforeExpiry = 90,
                DaysWithoutMovement = 90,
                
                IsActive_DeadAlert = true,
                IsActive_ExpiryAlert = true,
                IsActive_LowAlert = true,
                IsActive_NewOrderAlert = true,
                IsActive_OrderUpdateAlert = true,
                IsActive_OutAlert = true,
                LowStockThreshold = 5,
                
                IsActive_InAppNotifiation = true,
                IsActive_EmailNotifiation = true,
                
            };

            await _unitOfWork.GetRepository<AlertConfigurations>().AddAsync(alertConfigurations);
            int affectedRows = await _unitOfWork.SaveChangesAsync();

            if (affectedRows <= 0)
            {
                throw new DbUpdateFailedException("no default Alert Configurations created");
            }
            return alertConfigurations.BranchId;
        }

        public async Task<AlertConfigurationsDto> GetAlertConfigurations()
        {
            //int branchId = 1;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 

            var alert = await _unitOfWork.GetRepository<AlertConfigurations>().GetAllAsIQueryable()
                .FirstOrDefaultAsync(x => x.BranchId == branchId);

            if (alert == null || alert.BranchId != branchId)
            {
                throw new KeyNotFoundException("No settings found");
            }
            
            AlertConfigurationsDto alertConfigurationsDto = new AlertConfigurationsDto()
            {
                DaysBeforeExpiry = alert.DaysBeforeExpiry,
                DaysWithoutMovement = alert.DaysWithoutMovement,
                
                IsActive_DeadAlert = alert.IsActive_DeadAlert,
                IsActive_ExpiryAlert = alert.IsActive_ExpiryAlert,
                IsActive_LowAlert = alert.IsActive_LowAlert,
                IsActive_OutAlert = alert.IsActive_OutAlert,
                LowStockThreshold = alert.LowStockThreshold,
                IsActive_NewOrderAlert = alert.IsActive_NewOrderAlert,
                IsActive_OrderUpdateAlert = alert.IsActive_OrderUpdateAlert,

                IsActive_InAppNotifiation = alert.IsActive_InAppNotifiation,
                IsActive_EmailNotifiation = alert.IsActive_EmailNotifiation,
                
            };
            return alertConfigurationsDto;
        }

        public async Task<AlertConfigurationsDto> UpdateAlertConfigurations(AlertConfigurationsDto alertConfigurationsDto)
        {
            //int branchId = 1;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 

            var alert = await _unitOfWork.GetRepository<AlertConfigurations>().GetAllAsIQueryable()
                .FirstOrDefaultAsync(x => x.BranchId == branchId);

            if (alert is null || alert.BranchId != branchId)
            {
                throw new KeyNotFoundException("no alert settings found");
            }

            alert.DaysBeforeExpiry = alertConfigurationsDto.DaysBeforeExpiry;
            alert.DaysWithoutMovement = alertConfigurationsDto.DaysWithoutMovement;
            alert.IsActive_DeadAlert = alertConfigurationsDto.IsActive_DeadAlert;
            alert.IsActive_ExpiryAlert = alertConfigurationsDto.IsActive_ExpiryAlert;
            alert.IsActive_LowAlert = alertConfigurationsDto.IsActive_LowAlert;
            alert.IsActive_OutAlert = alertConfigurationsDto.IsActive_OutAlert;
            alert.IsActive_NewOrderAlert = alertConfigurationsDto.IsActive_NewOrderAlert;
            alert.IsActive_OrderUpdateAlert = alertConfigurationsDto.IsActive_OrderUpdateAlert;

            alert.IsActive_EmailNotifiation = alertConfigurationsDto.IsActive_EmailNotifiation;
            alert.IsActive_InAppNotifiation = alertConfigurationsDto.IsActive_InAppNotifiation;

            alert.LowStockThreshold = alertConfigurationsDto.LowStockThreshold;


             _unitOfWork.GetRepository<AlertConfigurations>().UpdateAsync(alert);
            await _unitOfWork.SaveChangesAsync();

            return alertConfigurationsDto;
        }
    }
}
