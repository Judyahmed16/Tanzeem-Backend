using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Users {
    public class AdminSignUpDto {
    
        public string Name { get; set; }
    
        public string Email { get; set; }
        
        public string Password { get; set; }

        public string PhoneNumber { get; set; }

    }
}
