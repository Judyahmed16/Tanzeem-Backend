using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Users;

namespace Tanzeem.Domain.Entities.Branches {
    public class BranchUserRelationship {
    
        public int Id { get; set; }

        public bool IsPrimary { get; set; }

        #region Relationships

        #endregion
        public int UserId { get; set; }
        
        public int BranchId { get; set; }

        #region Navigation

        #endregion
        public User User { get; set; }

        public Branch Branch { get; set; }

    }
    
}
