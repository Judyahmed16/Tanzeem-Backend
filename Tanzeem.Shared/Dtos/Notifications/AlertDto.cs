using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Shared.Dtos.Notifications
{
    public class AlertDto
    {
        public string AlertTitle { get; set; }
        public string AlertDescription { get; set; }
        public string AlertSubTitle { get; set; }
        public NotificationType Type { get; set; }
        public string Priority { get; set; }
        public int ProductId { get; set; }

    }
}
