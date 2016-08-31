# Contentful Webhook Server
This is a server framework for fielding Contentful webhook requests. This does NOT have to run in a dedicated website. It can be added to any existing ASP.NET website.


_**Note:** Until this message is removed, this code is very, very alpha._

Add the `Webhooks.Core` project to your website solution, and add a reference from your website project. (Alternately, you can add a reference to the compiled DLL.)

### Creating a URL Endpoint

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

### Writing Handlers

Once the dispatcher is mapped to a controller action, you are free to write "handlers" which will execute in response to webhook requests.

A handler is simply a static C# method of a specific signature.  You write the method, then "register" it with the dispatcher.

The method must:

1. Be public
2. Be static
3. Accept a single argument: a `WebhookEventArgs` object
4. Return a `WebhookHandlerLogEntry` object (or `null`)

For example:

    public static WebhookHandlerLogEntry DoSomething(WebhookEventArgs e)
    {
      return new WebhookHandlerLogEntry("Amazing things happened...");
    }

Whatever happens inside the handler is up to you.


### Matching Handlers to Webhooks

A Contentful webhook request passes two HTTP headers which describe what has happened.

For example:

    X-Contentful-Topic: [the name of the event]
    X-Contentful-Webhook-Name: [the user-supplied name of the webhook]

A handler can execute on a combination of these two values.

1. A specific Topic (and _any_ Name)
2. A specific Name (and _any_ Topic)
3. A combination of specific Topic and specific Name

The `WebhookDispatcher` maintains an internal collection of all handlers and the crieria under which each should execute. When a webhook request is received, the request is evaluated by each handler. The handler is executed if the request matches its specified criteria.

The actual collection is a `List<WebhookHandler>`. It can be visualized like this:

    [1] Name: "Webhook1"
        ForTopic: "ContentManagement.Entry.publish"
        ForName: "*"
        Handler: [a Func<WebhookEventArgs, WebhookHandlerLogEntry> delegate]

    [2] Name: "Webhook2"
        ForTopic: "ContentManagement.Entry.auto_save"
        ForName: "AutoSave for Blog Posts"
        Handler: [a Func<WebhookEventArgs, WebhookHandlerLogEntry> delegate]

    [etc]

The inbound `WebRequestBase` (from the controller) is converted to a `WebhookRequest` which is succesively passed into `WebhookHandler.IsMatch` for each item in the collection. Matching handlers are executed.

The specification of what combination of these values is required for a particular handler to execute is called "registering" a handler.  You "register" a handler to respond to one (or multiple) of the above scenarios. You do this in one of two ways...

#### 1. Manual Handler Registration

Call the static method `WebhookDispatcher.RegisterHandler`.  The arguments are:

1. **The name.** This is internal only, for logging and debugging.
2. **The webhook topic** for which this method should execute.  "*" is a wildcard for all.
3. **The webhook name** for which this method should execute.  "*" is a wildcard for all.
4. **The handler method** itself, as a `Func<WebhookEventArgs, WebhookHandlerLogEntry>` delegate

Example of a webhook handler that will fire on _any_ webhook request received from Contentful.

    WebhookDispatcher.RegisterHandler("MyWebhookHandler", "*", "*",
     (e) => {
	   // Do something here.
	   // "e" is a WebhookEventArgs object.
	   // The method must return a WebhookHandlerLogEntry (or null for no logging)
      }
    );

(**Note:** This will fire on any webhook request _received_. It's still up to you to configure Contentful to _send_ the webhooks you want, in response to specific events.  One pattern would be for Contentful to send a webhook on _all_ system events, then use various handlers to filter and process them. Some webhook requests wouldn't be processed at all and would simply pass through the system.  However, this would generate considerable traffic (especially from "auto\_save" events). A better pattern is to only send webhooks for events for which you _know_ handlers are waiting to execute.)

#### 2. Automatic Handler Registration

Alternately, you can write a method and decorate it with `WebhookBinding` attributes, like this:

    [WebhookBinding("ContentManagement.Entry.publish")]
    public static WebhookHandlerLogEntry DoSomething(WebhookEventArgs e)
    {
        // Do something here
    }

The `WebhookBinding` attribute takes a topic by default, with an option second argument for the name.

Bindings can be stacked. The same method will register once for every `WebhookBinding` provided:

    [WebhookBinding("ContentManagement.Entry.publish")]
    [WebhookBinding("ContentManagement.Entry.unpublish")]
    public static WebhookHandlerLogEntry DoSomething(WebhookEventArgs e)

Inside the handler method, the name/topic for which the handler is executing is accessible via the `ActiveHandler` property on the `WebhookEventArgs` object:

    e.ActiveHandler.ForTopic
    e.ActiveHandler.ForName

On application startup, the methods must be discovered for the dispatcher to automatically register them. The easiest way is to call the global auto-register method in `Application_Start`:

    WebhookDispatcher.AutoRegisterHandlers();

That will inspect all currently loaded assemblies in the AppDomain, and all unloaded assemblies in the `bin` folder (pass in an alternate path as a string, if you have another location).

You can also register methods individually using a `MethodInfo` object. The method will be inspected for `WebhookBinding` attributes.

    WebhookDispatcher.RegisterHandler(this.GetType().GetMethod("DoSomething"));

You can mass-register by `Type`. All methods in the type will be inspected as above.

    WebhookDispatcher.AutoRegisterHandlers(typeof(MyWebhookHandlerMethods));

Or by single assembly. All types in the assembly will be inspected as above.

    WebhookDispatcher.AutoRegisterHandlers(Assembly.GetExecutingAssembly());
 

### Logging

Contentful will store the response from the webhook request in its log. Each handler which executes in reponse to a webhook request can return its own log entry to be stored.

Handlers should return a `WebhookHandlerLogEntry` object.  These will be aggregated, and sent back as a JSON array, which Contentful will store as the `body` of the webhook response.

The `WebhookHandlerLogEntry` object has two properties:

1. **Source**: Where this log entry originated from. If left empty, this will populate with the registered name of the handler, or the Type.MethodName.  (It's generally expected that you'll let this auto-populate.)
2. **Message**: Whatever information you want to log about the handler.

The `Message` property can be set through the constructor:

    return new WebhookHandlerLogEntry("This handler did something");

A handler can return `null` if no logging of that handler is desired (if, for example, some internal logic causes the handler to exit without doing anything). Null log entries will be ignored.

### This Solution

This repository contains a single solution with multiple projects:

1. **Webhooks.Core:** This is the core assembly.  The `Webhooks.Core.dll` file should be referenced in your website.
2. **Website:** This is a simple website tester. It's a stripped down ASP.NET MVC project which hosts the server and should run directly from Visual Studio for testing. There are several binding examples in `global.asax.cs`
3. **Examples:** Some example webhook handlers are provided which demonstrate basic functionality. If these are referenced from the Website project, they will be auto-registered.

### To Do

* Unit tests
* Error handling/reporting
* Logging (perhaps just exposing events (below) would be enough, because the implementor could integrate that with their own logging infrastructure)
* Basic events: `OnWebhookRegistering/ed`, `OnWebhookExecuting/ed`, `OnWebhookRequestProcessing/ed`
* Key/value pairs on the `WebhookHandlerLogEntry` object, to allow setting of structured information
* Default meta capture for `WebhookHandlerLogEntry`; timestamp, elapsed time to execute
* Weighting/priority, in the event Handler X needs to execute before Handler Y
* Consistent settings access, so that shareable handlers (plugins?) can be written more easily
* Custom config for handler settings
* More handler registration logic: execute handler by type, by ID, etc.
* New example: SQL serialization
* Allow asynchronous execution of handlers?
* Consistent wrapping of data payload (I would rather not re-invent this wheel -- perhaps the Contentful .NET API already has this?)
* Debugging reports -- at the very least, a way to see a list of handlers that will run for a particular topic/name combination
* Easier handler access, so that handlers could expose logic as Lambdas, which can be reset with new logic from the "outside"