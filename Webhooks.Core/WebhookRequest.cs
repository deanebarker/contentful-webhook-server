using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using System.Web;

namespace ContentfulWebhookServer.Core
{
    public class WebhookRequest
    {
        public WebhookRequest(HttpRequestBase request)
        {
            this.request = request;
        }

        private HttpRequestBase request;

        public string ContentfulTopic
        {
            get
            {
                return request.Headers["X-Contentful-Topic"];
            }
        }

        public string ContentfulWebhookName
        {
            get
            {
                return request.Headers["X-Contentful-Webhook-Name"];
            }
        }

        private string body;
        public string Body
        {
            get
            {
                if (body == null)
                {
                    using (Stream receiveStream = request.InputStream)
                    {
                        using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                        {
                            body = readStream.ReadToEnd();
                        }
                    }
                }
                return body;
            }
        }

        private JObject parsedBody;
        public JObject ParsedBody
        {
            get
            {
                if(parsedBody == null)
                {
                    parsedBody = JObject.Parse(Body);
                }
                return parsedBody;
            }
        }

        public string ContentId
        {
            get
            {
                return (string)ParsedBody["sys"]["id"];
            }
        }
    }
}
