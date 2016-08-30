# Contentful Webhook Server
This is a server framework for fielding Contentful webhook requests. This does NOT have to run in a dedicated website. It can be added to any existing ASP.NET website.


_**Note:** Until this message is removed, this code is very, very alpha._

To install:

Add the `Webhooks.Core` project to your website solution, and add a reference from your website project. (Alternately, you can add a reference to the compiled DLL.)

In any controller action that returns an `ActionResult`, add the following:

      var results = WebhookDispatcher.Process(new WebhookRequest(Request));
      return Json(results);

Add _the URL to this action_ as your webhook URL in Contentful.  For example, if you have this:

    public class WebhookController
    {
      public ActionResult Process()
      {
         [...]
      }
    }

You would use the URL: `http://mydomain.com/webhook/process`

This can be in any controller/action and should exist just fine in among your other controllers and actions. Remember, in the end, this is just an inbound HTTP request like anything else.

Authentication and filtering to Contentful IP ranges is _not_ handled by this library. That is left for you to implement and manage through provided options in the ASP.NET MVC stack.

Once the dispatcher is mapping to a controller action, you are free to register "handlers" to respond to inbound webhooks.  A handler is simply a C# method of a specific signature.  You write the method, then "register" it with the dispatcher.

A handler is a _static_ method of this signature:

    Func<WebhookEventArgs, WebhookHandlerLogEntry>

That is to say, it accepts a `WebhookEventArgs` object and returns a `WebhookHandlerLogEntry` object.  Whatever happens in between is up to you.

Once the handler is written, register it by one of two methods.

### Manual Handler Registration

Call the static method `WebhookDispatcher.RegisterHandler`.  The arguments are:

1. **The name.** This is internal only, for logging and debugging.
2. **The webhook topic** for which is method should execute.  "*" is a wildcard for all.
3. **The webhook name** topic for which is method should execute.  "*" is a wildcard for all.
4. **The handler method** itself, as a `Func<WebhookEventArgs, WebhookHandlerLogEntry>`

Example:

    WebhookDispatcher.RegisterHandler("Name", "*", "*"
     (e) => {
	   // Do something here.
	   // "e" is a WebhookEventArgs object.
	   // The method must return a WebhookHandlerLogEntry (or null for no logging)
      }
    );

### Auto Handler Registration

Alternately, you can write a method and bind it through the `WebhookBinding` attribute, like this:

    [WebhookBinding("ContentManagement.Entry.publish")]
    public static WebhookHandlerLogEntry DoSomething(WebhookEventArgs e)
    {
        // Do something here
    }

Before use, this method must be "discovered."  The easiest way is to call the global auto-register method in `Application_Start`:

    WebhookDispatcher.AutoRegisterHandlers();

That will inspect all currently loaded assemblies in the AppDomain, and all unloaded assemblies in the `bin` folder (pass in an alternate path as a string, if you have another location).

You can also register methods individually using a `MethodInfo` object. The method will be inspected for `WebhookBinding` attributes.

    WebhookDispatcher.RegisterHandler(this.GetType().GetMethod("DoSomething"));

You can mass-register by `Type`. All methods in the type will be inspected as above.

    WebhookDispatcher.AutoRegisterHandlers(typeof(MyWebhookHandlerMethods));

Or by single assembly. All types in the assembly will be inspected as above.

    WebhookDispatcher.AutoRegisterHandlers(Assembly.GetExecutingAssembly());
 

### Logging

Handlers should return a `WebhookHandlerLogEntry` object.  These will be aggregated, and sent back as a JSON array, which Contentful will store as the `body` of the webhook response.

A handler can return `null` if no logging of that handler is desired. Null log entries will be ignored.

### This Solution

This repository contains a single solution with multiple projects:

1. **Webhooks.Core:** This is the core assembly.  The `Webhooks.Core.dll` file should be referenced in your website.
2. **Website:** This is a simple website tester. It's a stripped down ASP.NET MVC project which hosts the server and should run directly from Visual Studio for testing. There are several binding examples in `global.asax.cs`
3. **Examples:** Some examples provided webhooks are provided. If these are referenced from the Website project, they will be auto-registered.