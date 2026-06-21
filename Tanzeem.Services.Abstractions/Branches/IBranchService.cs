using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Shared.Dtos.Branches;

namespace Tanzeem.Services.Abstractions.Branches {
    public interface IBranchService {

        Task<BranchDto> GetBranchAsync(int branchId);

        Task<List<BranchDto>> GetCompanyBranchesAsync();
        
        Task<List<BranchesMenuDto>> GetBranchesList();

        Task<int> CreateNewBranchAsync(BranchDto branchDto, int adminId, int companyId);

        Task<int> UpdateBranchAsync(int branchId, BranchDto branchDto);

        Task<bool> DeleteBranchAsync(int branchId);


    }

}

