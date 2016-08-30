using ContentfulWebhookServer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Website
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            
            // An example of manually registering a webhook handler that will execute on everything, all the time.
            // This handler will apply a log message indicating that the webhook was processed by this server.
            WebhookDispatcher.RegisterHandler(

                // 1. The name (internal; just for logging and debugging; if not provided, will auto-name from the reflected method name)
                "GlobalReceiptAcknowledgement",

                // 2. The webhook topic on which to execute ("*" is wildcard for everything)
                "*",

                // 3. The webhook name on which to execute
                "*",

                // 4. The handler itself. It takes in a WebhookEventArgs object, and returns a WebhookHandlerLogEntry
                (e) => {
                    var message = string.Format("Received by Contentful Webhook Server; IP: {0}; Timestamp: {1}",
                        HttpContext.Current.Request.ServerVariables["LOCAL_ADDR"],
                        DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
                    );

                    return new WebhookHandlerLogEntry(message);
                }

            );

            // Multiple handlers can fire on a single request. Log messages will be aggregate and be sent back as a group.

            // You can also register from a method, type, or assembly. These will be inspected for methods with WebhookBinding attributes.
            // WebhookDispatcher.RegisterHandler(typeof(MyClass).GetMethod("MyMethodName"))
            // WebhookDispatcher.AutoRegisterHandlers(typeof(MyClass))
            // WebhookDispatcher.AutoRegisterHandlers(Assembly.GetExecutingAssembly())

            // ...or just auto-register everything. This will:
            // (1) inspect all loaded assemblies for bound handlers
            // (2) inspect anything in a passed-in path (default: "bin") for DLL files that are not loaded into the AppDomain
            WebhookDispatcher.AutoRegisterHandlers();
        }
    }

}
