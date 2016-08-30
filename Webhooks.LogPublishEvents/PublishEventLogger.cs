using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContentfulWebhookServer.Core;
using System.IO;
using System.Web;

namespace Webhooks.LogPublishEvents
{
    public static class PublishEventLogger
    {
        private static string LOG_PATH = "App_Data/publish.txt";

        [WebhookBinding("ContentManagement.Entry.publish")]
        [WebhookBinding("ContentManagement.Entry.unpublish")]
        public static WebhookHandlerLogEntry LogPublishEvent(WebhookEventArgs e)
        {
            var path = LOG_PATH;
            if(!Path.IsPathRooted(path))
            {
                path = HttpContext.Current.Server.MapPath(path);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            File.AppendAllLines(path, new[] { GetLogEntry(e) });

            return null;
        }

        private static string GetLogEntry(WebhookEventArgs e)
        {
            return string.Concat(
                DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"),
                "\t",
                e.ActiveHandler.ForTopic.EndsWith(".publish") ? "Published" : "Unpublished",
                "\t",
                e.Request.ContentId);
        }
    }
}
