using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentfulWebhookServer.Core
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class WebhookBindingAttribute : Attribute
    {
        public string Name { get; set; }
        public string Topic { get; set; }

        public WebhookBindingAttribute(string topic, string name = "*")
        {
            Name = name;
            Topic = topic;
        }
    }
}
