using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Shared.Dtos.Suppliers
{
    public class SupplierResponseDto
    {
        public int Id { get; set; }
        public string SupplierName {  get; set; }
        public string SupplierNumber{  get; set; }
        public decimal onTimePercentage { get; set; }
        public double LeadTime { get; set; }    
        public string Badge { get; set; }
        public string Email { get; set; }
        public string PhoneNumberOne { get; set; }
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
