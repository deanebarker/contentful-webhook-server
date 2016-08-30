using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentfulWebhookServer.Core
{
    public class WebhookEventArgs : EventArgs
    {
        public WebhookRequest Request { get; set; }

        public WebhookHandler ActiveHandler { get; set; }

        public WebhookEventArgs(WebhookRequest request)
        {
            Request = request;
        }
    }
}
