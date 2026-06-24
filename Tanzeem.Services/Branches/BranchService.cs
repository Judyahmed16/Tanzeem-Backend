using Microsoft.EntityFrameworkCore;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Enums;
using Tanzeem.Domain.Exceptions;
using Tanzeem.Services.Abstractions.Branches;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Shared.Dtos.Branches;

namespace Tanzeem.Services.Branches {
    public class BranchService(
        IUnitOfWork _unitOfWork,
        ICurrentService currentService) : IBranchService {

        // TODO: Part of inactive branch enforcement feature — revisit when
        // company/branch inactivity is fully implemented across auth and CurrentService.
        public async Task<BranchDto> GetBranchAsync(int branchId) {

            var branch = await _unitOfWork.GetRepository<Branch>().GetByIdAsync(branchId);

            if (branch is null)
                throw new Exception("Branch not found.");
            if (branch.Status != BranchStatus.Active)
                throw new BusinessRuleException("Branch is not active.");

            return MapToBranchDto(branch);
        }

        public async Task<BranchDto> GetCurrentBranchAsync() {

            var branchId = currentService.BranchId
                ?? throw new UnauthorizedAccessException("No branch assigned.");
            var companyId = currentService.CompanyId;

            var branch = await _unitOfWork.GetRepository<Branch>()
                .GetAllAsIQueryable()
                .FirstOrDefaultAsync(b => b.Id == branchId && (!companyId.HasValue || b.CompanyId == companyId.Value));

            if (branch is null)
                throw new Exception("Branch not found.");
            if (branch.Status != BranchStatus.Active)
                throw new BusinessRuleException("Branch is not active.");

            return MapToBranchDto(branch);
        }

        public async Task<List<BranchDto>> GetCompanyBranchesAsync() {

            var companyId = currentService.CompanyId;

            var branches = await _unitOfWork.GetRepository<Branch>()
                .GetAllAsIQueryable()
                .Where(b => b.CompanyId == companyId)
                .ToListAsync();

            return branches.Select(MapToBranchDto).ToList();
        }

        // TODO: Filter out inactive branches once inactive branch enforcement
        // feature is implemented (greyed-out UI + login restriction).
        public async Task<List<BranchesMenuDto>> GetBranchesList() {

            var companyId = currentService.CompanyId;

            var branches = await _unitOfWork.GetRepository<Branch>()
                .GetAllAsIQueryable()
                .Where(b => b.CompanyId == companyId)
                .ToListAsync();

            return branches.Select(b => new BranchesMenuDto {
                Id = b.Id,
                BranchName = b.Name,
                Location = b.Location ?? "Null"
            }).ToList();
        }
    
        public async Task<int> UpdateBranchAsync(int branchId, BranchDto branchDto) {

            var branch = await _unitOfWork.GetRepository<Branch>().GetByIdAsync(branchId);

            if (branch is null)
                throw new Exception("Branch not found.");

            branch.Name = branchDto.Name;
            branch.Location = branchDto.Location;
            branch.PhoneNumber = branchDto.PhoneNumber;
            branch.Email = branchDto.Email;
            branch.Status = Enum.Parse<BranchStatus>(branchDto.Status);

            await _unitOfWork.SaveChangesAsync();
            return branch.Id;
        }

        public async Task<bool> DeleteBranchAsync(int branchId) {

            var branch = await _unitOfWork.GetRepository<Branch>().GetByIdAsync(branchId);

            if (branch is null)
                throw new Exception("Branch not found.");

            branch.Status = BranchStatus.Closed;
            return await _unitOfWork.SaveChangesAsync() > 0;
        }
        
        public async Task<int> CreateNewBranchAsync(BranchDto branchDto, int adminId, int companyId) {

            var branch = new Branch {
                Name = branchDto.Name,
                Location = branchDto.Location,
                PhoneNumber = branchDto.PhoneNumber,
                Email = branchDto.Email,
                CreatedAt = DateTime.UtcNow,
                Status = BranchStatus.Active,
                CompanyId = companyId,
                BURelations = new List<BranchUserRelationship>()
            };

            bool isFirstBranch = !await _unitOfWork.GetRepository<BranchUserRelationship>()
                .GetAllAsIQueryable()
                .AnyAsync(r => r.UserId == adminId);

            branch.BURelations.Add(new BranchUserRelationship {
                UserId = adminId,
                IsPrimary = isFirstBranch
            });

            await _unitOfWork.GetRepository<Branch>().AddAsync(branch);
            await _unitOfWork.SaveChangesAsync();

            return branch.Id;
        }

        private static BranchDto MapToBranchDto(Branch branch) => new() {
            Id = branch.Id,
            Name = branch.Name,
            Location = branch.Location ?? "Null",
            PhoneNumber = branch.PhoneNumber ?? "Null",
            Email = branch.Email ?? "Null",
            CreatedAt = branch.CreatedAt,
            Status = branch.Status.ToString()
        };
    }
}
