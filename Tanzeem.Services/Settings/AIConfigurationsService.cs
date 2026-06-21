using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.CustomExceptions;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Settings;
using Tanzeem.Domain.Exceptions;
using Tanzeem.Services.Abstractions.AI;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Abstractions.Settings;
using Tanzeem.Shared.Dtos.Settings;

namespace Tanzeem.Services.Settings
{
    public class AIConfigurationsService(IUnitOfWork _unitOfWork, IDemandForecastingService _demandForecasting,
        ICurrentService _currentService) : IAIConfigService
    {
        public async Task<AIConfigurationsDto> CreateAIConfigurations(int branchId)
        {
            AIConfigurations aIConfigurations = new AIConfigurations()
            {
                BranchId = branchId,

                DemandForecasting = true,
                AutoCategorization = true,
            };

            await _unitOfWork.GetRepository<AIConfigurations>().AddAsync(aIConfigurations);
            int affectedRows = await _unitOfWork.SaveChangesAsync();

            if (affectedRows <= 0) {
                throw new DbUpdateFailedException("No update happened at Ai configuration - settings");
            }

            return new AIConfigurationsDto
            {
                AutoCategorization = aIConfigurations.AutoCategorization,
                DemandForecasting = aIConfigurations.DemandForecasting,
            };
        }

        public async Task<AIConfigurationsDto> GetIConfigurationsAsync()
        {
            //int branchId = 54;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 
            
            var aIConfigurations = await _unitOfWork.GetRepository<AIConfigurations>()
                .GetAllAsIQueryable()
                .FirstOrDefaultAsync( x => x.BranchId == branchId);

            if (aIConfigurations == null || aIConfigurations.BranchId != branchId)
            {
                throw new KeyNotFoundException("No ai settings with this branch! Try contact Tanzeem admins");
            }
            
            return new AIConfigurationsDto
            {
                AutoCategorization = aIConfigurations.AutoCategorization,
                DemandForecasting = aIConfigurations.DemandForecasting,
            };
        }

        public async Task<AIConfigurationsDto> UpdateIConfigurationsAsync(AIConfigurationsDto aIConfigurationsDto)
        {
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned");

            var aIConfigurations = await _unitOfWork.GetRepository<AIConfigurations>()
                .GetAllAsIQueryable()
                .FirstOrDefaultAsync(x => x.BranchId == branchId);

            if (aIConfigurations == null || aIConfigurations.BranchId != branchId)
            {
                throw new KeyNotFoundException("No ai settings with this branch! Try contact Tanzeem admins");
            }


            if(aIConfigurationsDto == null)
            {
                throw new ValidationException("Fill the fields,please");
            }

            bool pastOption = aIConfigurations.DemandForecasting;

            aIConfigurations.AutoCategorization = aIConfigurationsDto.AutoCategorization;
            aIConfigurations.DemandForecasting = aIConfigurationsDto.DemandForecasting;

            _unitOfWork.GetRepository<AIConfigurations>().UpdateAsync(aIConfigurations);

            int affectedRows = await _unitOfWork.SaveChangesAsync();

            if (affectedRows <= 0)
            {
                throw new DbUpdateFailedException("No update happened at Ai configuration - settings");
            }

            if (pastOption == false && aIConfigurationsDto.DemandForecasting == true)
            {
                Hangfire.BackgroundJob.Enqueue(() => _demandForecasting.UpdateForecastForBranchAsync(branchId));
            }

            return aIConfigurationsDto;

        }
    }
}
