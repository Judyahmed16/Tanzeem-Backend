using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Shared.Dtos.Suppliers
{
    public class SupplierRequestDto
    {
        public string SupplierName { get; set; }
        public string Email { get; set; }

        [RegularExpression(@"^\+?[0-9\s\-]+$", ErrorMessage = "Phone number is invalid.")]
        public string PhoneNumberOne { get; set; }
        [RegularExpression(@"^\+?[0-9\s\-]+$", ErrorMessage = "Phone number is invalid.")]
        public string? PhoneNumberTwo { get; set; }

        public string Street { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string? WebsiteURL { get; set; }
        public string? ContactPersonName { get; set; } //can be null
        public string? Tax_Id { get; set; }
        public string? Notes { get; set; }

        public SupplierStatus SupplierStatus { get; set; }
    }
}
