using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Utils
{
    public static class DateUtils
    {
        public static DateFilterResult ApplyDateRange(string rangeType)
        {
            var today = DateTime.Today;
            DateTime? selectedStartDate;
            DateTime? selectedEndDate;

            switch (rangeType)
            {
                case "today":
                    selectedStartDate = today;
                    selectedEndDate = today;
                    break;
                case "yesterday":
                    var yesterday = today.AddDays(-1);
                    selectedStartDate = yesterday;
                    selectedEndDate = yesterday;
                    break;
                case "this_week":
                    var dayOffset = today.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)today.DayOfWeek - 1;
                    selectedStartDate = today.AddDays(-dayOffset);
                    selectedEndDate = today;
                    break;
                case "last_week":
                    var dayOffset2 = today.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)today.DayOfWeek - 1;
                    selectedStartDate = today.AddDays(-dayOffset2 - 7);
                    selectedEndDate = selectedStartDate.Value.AddDays(6);
                    break;
                case "this_month":
                    var monthStart = new DateTime(today.Year, today.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                    selectedStartDate = monthStart;
                    selectedEndDate = monthEnd;
                    break;
                case "last_month":
                    var lastMonthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                    var lastMonthEndStart = lastMonthStart.AddMonths(1).AddDays(-1);
                    selectedStartDate = lastMonthStart;
                    selectedEndDate = lastMonthEndStart;
                    break;
                case "this_year":
                    var yearStart = new DateTime(today.Year, 1, 1);
                    var yearEnd = today;
                    selectedStartDate = yearStart;
                    selectedEndDate = yearEnd;
                    break;
                case "last_year":
                    var lastYearStart = new DateTime(today.Year, 1, 1).AddYears(-1);
                    var lastYearEnd = new DateTime(today.Year, 12, 31).AddYears(-1);
                    selectedStartDate = lastYearStart;
                    selectedEndDate = lastYearEnd;
                    break;
                case "all":
                default:
                    selectedStartDate = null;
                    selectedEndDate = null;
                    break;
            }
            return new DateFilterResult
            {
                SelectedStartDate = selectedStartDate,
                SelectedEndDate = selectedEndDate
            };
        }
    }

    public class DateFilterResult
    {
        public DateTime? SelectedStartDate;
        public DateTime? SelectedEndDate;
    }
}
