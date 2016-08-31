# Contentful Webhook Server
This is a server framework for fielding Contentful webhook requests. This does NOT have to run in a dedicated website. It can be added to any existing ASP.NET website.


_**Note:** Until this message is removed, this code is very, very alpha._

To install:

Add the `Webhooks.Core` project to your website solution, and add a reference from your website project. (Alternately, you can add a reference to the compiled DLL.)

Create a new controller action that returns an `ActionResult`, and set the following as the body:

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

Once the dispatcher is mapped to a controller action, you are free to register "handlers" to respond to inbound webhooks.  A handler is simply a C# method of a specific signature.  You write the method, then "register" it with the dispatcher.

A handler is a _static_ method of this signature:

    Func<WebhookEventArgs, WebhookHandlerLogEntry>

That is to say, it accepts a `WebhookEventArgs` object and returns a `WebhookHandlerLogEntry` object.  Whatever happens in-between is up to you.

Once the handler is written, register it by one of two methods.

### Manual Handler Registration

Call the static method `WebhookDispatcher.RegisterHandler`.  The arguments are:

1. **The name.** This is internal only, for logging and debugging.
2. **The webhook topic** for which this method should execute.  "*" is a wildcard for all.
3. **The webhook name** for which this method should execute.  "*" is a wildcard for all.
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

the `WebhookBinding` attribute takes a topic by default, with an option second argument for the name.

Bindings can be stacked. The same method will register once for every `WebhookBinding` provided:

    [WebhookBinding("ContentManagement.Entry.publish")]
    [WebhookBinding("ContentManagement.Entry.unpublish")]
    public static WebhookHandlerLogEntry DoSomething(WebhookEventArgs e)

Inside the method, the name/topic for which the handler is executing is accessible via the `ActiveHandler` property on the `WebhookEventArgs` object:

    e.ActiveHandler.ContentfulTopic
    e.ActiveHandler.ContentfulName

Before use, this method must be "registered."  The easiest way is to call the global auto-register method in `Application_Start`:

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

A handler can return `null` if no logging of that handler is desired (if, for example, some internal logic causes the handler to exit without doing anything). Null log entries will be ignored.

### This Solution

This repository contains a single solution with multiple projects:

1. **Webhooks.Core:** This is the core assembly.  The `Webhooks.Core.dll` file should be referenced in your website.
2. **Website:** This is a simple website tester. It's a stripped down ASP.NET MVC project which hosts the server and should run directly from Visual Studio for testing. There are several binding examples in `global.asax.cs`
3. **Examples:** Some examples provided webhooks are provided. If these are referenced from the Website project, they will be auto-registered.

### To Do

* Error handling/reporting
* Logging
* Weighting/priority, in the event Handler X needs to execute before Handler Y
* Consistent settings access, so that shareable handlers (plugins?) can be written more easily
* New example: SQL serialization
* Consistent wrapping of data payload (I would rather not re-invent this wheel -- perhaps the Contentful .NET API already has this?)
* Debugging reports -- at the very least, a way to see a list of handlers that will run for a particular topic/name combination
* Easier handler access, so that handlers could expose logic as Lambdas, which can be reset with new logic from the "outside"