using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ContentfulWebhookServer.Core
{
    public class WebhookHandler
    {
        public string ForTopic { get; set; }
        public string ForName { get; set; }

        public Func<WebhookEventArgs, WebhookHandlerLogEntry> Handler { get; set; }

        public string Name { get; set; }

        public WebhookHandler()
        {
           
        }

        public bool IsMatch(WebhookRequest request)
        {
            var matchesTopic = true;
            var matchesName = true;
            
            if(ForTopic != null && ForTopic != "*")
            {
                if(ForTopic != request.ContentfulTopic)
                {
                    matchesTopic = false;
                }
            }

            if(ForName != null && ForName != "*")
            {
                if(ForName != request.ContentfulWebhookName)
                {
                    matchesName = false;
                }
            }

            return matchesTopic && matchesName;

        }
    }
}
