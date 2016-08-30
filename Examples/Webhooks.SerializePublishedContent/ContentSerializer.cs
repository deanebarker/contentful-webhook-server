using ContentfulWebhookServer.Core;
using System.IO;
using System.Web;

namespace Webhooks.SerializePublishedContent
{
    public static class ContentSerializer
    {
        private static string ARCHIVE_DIRECTORY_PATH = "App_Data/contentful-content";
        private static string FILE_EXTENSION = "json";

        [WebhookBinding("ContentManagement.Entry.publish")]
        public static WebhookHandlerLogEntry SerializePublishedContent(WebhookEventArgs e)
        {
            // Establish the archive directory
            var path = ARCHIVE_DIRECTORY_PATH;
            if(!Path.IsPathRooted(path))
            {
                path = HttpContext.Current.Server.MapPath(path);
            }
            Directory.CreateDirectory(path);

            // The actual path to the file will be the content ID
            var fullPath = Path.Combine(path, string.Concat(e.Request.ContentId, ".", FILE_EXTENSION));

            File.WriteAllText(fullPath, e.Request.Body);

            return new WebhookHandlerLogEntry("Content serialized to " + fullPath);
        }
    }
}
