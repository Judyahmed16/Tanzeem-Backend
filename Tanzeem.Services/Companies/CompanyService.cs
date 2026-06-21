using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Domain.Exceptions;
using Tanzeem.Services.Abstractions.Companies;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Current;
using Tanzeem.Shared.Dtos.Companies;

namespace Tanzeem.Services.Companies {
    public class CompanyService(IUnitOfWork _unitOfWork,
        ICurrentService currentService) : ICompanyService {

        public async Task<CompanyDto> GetCompanyAsync() {

            var companyId = currentService.CompanyId
                ?? throw new InvalidOperationException("Company context missing from token.");

            var company = await _unitOfWork.GetRepository<Company>().GetByIdAsync(companyId);

            if (company is null)
                throw new BusinessRuleException("Company not found.");

            return new CompanyDto {
                Name = company.Name,
                Field = company.Field,
                Email = company.Email,
                Phone = company.Phone
            };
        }

        public async Task<int> UpdateCompanyAsync(CompanyDto companyDto) {

            var companyId = currentService.CompanyId
                ?? throw new InvalidOperationException("Company context missing from token.");

            var company = await _unitOfWork.GetRepository<Company>().GetByIdAsync(companyId);

            if (company is null)
                throw new BusinessRuleException("Company not found.");

            company.Name = companyDto.Name;
            company.Field = companyDto.Field;
            company.Email = companyDto.Email;
            company.Phone = companyDto.Phone;

            await _unitOfWork.SaveChangesAsync();
            return company.Id;
        }

        public async Task<bool> DeleteCompanyAsync() {

            var companyId = currentService.CompanyId
                ?? throw new InvalidOperationException("Company context missing from token.");

            var company = await _unitOfWork.GetRepository<Company>().GetByIdAsync(companyId);

            if (company is null)
                throw new BusinessRuleException("Company not found.");

            company.IsActive = false;
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<int> CreateNewCompanyAsync(CompanyDto companyDto, int adminId) {

            var admin = await _unitOfWork.GetRepository<User>().GetAsync(u => u.Id == adminId);

            if (admin is null)
                throw new BusinessRuleException("Admin not found.");

            #region Mapping
            var company = new Company {
                Name = companyDto.Name,
                Field = companyDto.Field,
                Email = companyDto.Email,
                Phone = companyDto.Phone,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                StripeCustomerId = string.IsNullOrWhiteSpace(companyDto.StripeCustomerId) ? null : companyDto.StripeCustomerId.Trim(),
            };

            admin.Company = company;
            #endregion

            await _unitOfWork.GetRepository<Company>().AddAsync(company);
            var count = await _unitOfWork.SaveChangesAsync();

            return company.Id;
        }



    }
}
