using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace ContentfulWebhookServer.Core
{
    public static partial class WebhookDispatcher
    {
        private static string EXECUTED_LOG_MESSAGE = "Executed";

        public static List<WebhookHandler> Handlers { get; set; }

        static WebhookDispatcher()
        {
            Handlers = new List<WebhookHandler>();
        }

        /// <summary>
        /// Compares the request to available handlers, executes matches, and returns log entires
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static List<WebhookHandlerLogEntry> Process(WebhookRequest request)
        {
            var log = new List<WebhookHandlerLogEntry>();
            var e = new WebhookEventArgs(request);

            foreach (var handler in Handlers.Where(x => x.IsMatch(request)))
            {
                e.ActiveHandler = handler;  // This will change each time through...

                var result = handler.Handler(e);

                if (result != null)
                {
                    result.Source = result.Source ?? handler.Name; // If they don't explicitly set a source, use the handler name
                    log.Add(result);
                }
            }

            return log;
        }

        /// <summary>
        /// Registers a single handler
        /// </summary>
        /// <param name="name">The name of the handler</param>
        /// <param name="forTopic">The topic signature for which this handler should execute</param>
        /// <param name="forName">The name signature for which this handler should execute</param>
        /// <param name="handler">The handler delegate</param>
        public static void RegisterHandler(string name, string forTopic, string forName, Func<WebhookEventArgs, WebhookHandlerLogEntry> handler)
        {
            forTopic = forTopic ?? "*";
            forName = forName ?? "*";

            Handlers.Add(new WebhookHandler()
            {
                Name = name,
                ForTopic = forTopic ?? "*",
                ForName = forName ?? "*",
                Handler = handler
            });
        }

        /// <summary>
        /// Registers the method provided as a handler
        /// </summary>
        /// <param name="method">The MethodInfo object which represents the handler. A delegate will be created from this method.</param>
        public static void RegisterHandler(MethodInfo method)
        {
            foreach (WebhookBindingAttribute binding in method.GetCustomAttributes(typeof(WebhookBindingAttribute), true))
            {
                var handler = (Func<WebhookEventArgs, WebhookHandlerLogEntry>)Delegate.CreateDelegate(typeof(Func<WebhookEventArgs, WebhookHandlerLogEntry>), method);
                var fullName = string.Concat(method.DeclaringType.Name, ".", method.Name);
                RegisterHandler(fullName, binding.Topic, binding.Name, handler);
            }
        }

        /// <summary>
        /// Iterates all methods in the type looking for those with WebhookBindingAttributes
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        public static void AutoRegisterHandlers(Type type)
        {
            foreach(var method in type.GetMethods().Where(x => x.GetCustomAttributes(typeof(WebhookBindingAttribute)).Any()))
            {
                RegisterHandler(method);
            }
        }

        /// <summary>
        /// Iterates all types in the assembly and autoloads by the type
        /// </summary>
        /// <param name="assembly">The assembly to inspect.</param>
        public static void AutoRegisterHandlers(Assembly assembly)
        {
            foreach(var type in assembly.GetTypes())
            {
                AutoRegisterHandlers(type);
            }
        }

        /// <summary>
        /// Iterates all currently loaded assemblies
        /// </summary>
        public static void AutoRegisterHandlers(string path = null)
        {
            var loaded = new List<string>();

            // We will always register from the loaded assemblies
            foreach(var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!loaded.Contains(loadedAssembly.GetName().FullName))
                {
                    AutoRegisterHandlers(loadedAssembly);
                    loaded.Add(loadedAssembly.GetName().FullName);
                }
            }

            // Then we will iterate all assemblies in provided path. If not path is provided, we will assume the bin
            path = path ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin");
            foreach (var dll in new DirectoryInfo(path).GetFiles("*.dll"))
            {
                if(dll.Name.StartsWith("System.") || dll.Name.StartsWith("Microsoft.") || dll.Name.StartsWith("mscorlib."))
                {
                    continue;
                }

                var assembly = Assembly.LoadFrom(dll.FullName);
                if (!loaded.Contains(assembly.GetName().FullName))
                {
                    AutoRegisterHandlers(assembly);
                    loaded.Add(assembly.GetName().FullName);
                }
            }
        }
    }
}
