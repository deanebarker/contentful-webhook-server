using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentfulWebhookServer.Core
{
    public class WebhookHandlerLogEntry
    {
        public string Source { get; set; }
        public string Message { get; set; }

        public WebhookHandlerLogEntry(string message)
        {
            Message = message;
        }
    }
}
