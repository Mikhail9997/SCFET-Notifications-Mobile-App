using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models.Channel
{
    public class SendMessageRequest
    {
        public string Content { get; set; } = string.Empty;
        public Guid? ReplyToMessageId { get; set; }
        public FileResult? Image { get; set; }
    }

}
