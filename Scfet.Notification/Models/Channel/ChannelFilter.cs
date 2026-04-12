using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models.Channel
{
    public class ChannelFilter : BaseFilter
    {
        public string? SearchTerm { get; set; }
        public ChannelSortBy SortBy { get; set; } = ChannelSortBy.CreatedAt;
    }

    public enum ChannelSortBy
    {
        CreatedAt,
        Title,
        Name,
        MembersCount,
        Status
    }
}
