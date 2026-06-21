using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Branches {
    public class BranchDto {

        public int Id { get; set; }

        public string Name { get; set; }

        public string Location { get; set; }

        public string PhoneNumber { get; set; }

        public string Email { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Status { get; set; }

    }
}
