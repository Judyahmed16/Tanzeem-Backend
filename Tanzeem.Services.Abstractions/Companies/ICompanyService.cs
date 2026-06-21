using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Shared.Dtos.Companies;

namespace Tanzeem.Services.Abstractions.Companies {
    public interface ICompanyService {

        Task<CompanyDto> GetCompanyAsync();
        Task<int> UpdateCompanyAsync(CompanyDto companyDto);
        Task<bool> DeleteCompanyAsync();
        Task<int> CreateNewCompanyAsync(CompanyDto companyDto, int adminId);



    }
}
