using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Shared.Dtos.Delivery_Issue
{
    public class ItemIssuesDto
    {
        public IssueType IssueType { get; set; }
        public int Quantity { get; set; }
    }
}
