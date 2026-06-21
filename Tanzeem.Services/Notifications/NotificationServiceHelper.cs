using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Services.Notifications
{
    public static class NotificationServiceHelper
    {
        public static string GenerateSinceDate(DateTime createdAt)
        {        
            int monthsApart = (DateTime.Now.Year - createdAt.Year) * 12 + DateTime.Now.Month - createdAt.Month;

            if (monthsApart > 12)
            {
                int years = monthsApart / 12;
                int months = monthsApart % 12;

                if (months == 0)
                {
                    return $"{years} year(s)";
                }
                else
                {
                    return $"{years} year(s) and {months} month(s)";
                }
            }
            else if (monthsApart <= 12 & monthsApart > 0)
            {
                return $"{monthsApart} month(s)";
            }
            else
            {
                TimeSpan span = DateTime.Now - createdAt;
                int days = (int)span.TotalDays;

                if (days <= 0) return "Today";
                return $"{days} day(s)";
            }

                
        }
    }
}
