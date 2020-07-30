using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pulumi.Azure.AppService.Inputs;
using Pulumi.Azure.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Pulumi.AzureFunctions.Sdk
{
    public class CallbackFunctionApp : ArchiveFunctionApp
    {
        public CallbackFunctionApp(string name, CallbackFunctionAppArgs args, CustomResourceOptions options = null) : base(name, Map(args), options)
        {
        }

        private static ArchiveFunctionAppArgs Map(CallbackFunctionAppArgs args)
        {
            var mapped = new ArchiveFunctionAppArgs
            {
                StorageAccount = args.StorageAccount,
                StorageContainer = args.StorageContainer,
                SiteConfig = args.SiteConfig,
                ResourceGroupName = args.ResourceGroupName,
                OsType = args.OsType,
                Name = args.Name,
                Location = args.Location,
                Tags = args.Tags,
                Identity = args.Identity,
                Enabled = args.Enabled,
                EnableBuiltinLogging = args.EnableBuiltinLogging,
                DailyMemoryTimeQuota = args.DailyMemoryTimeQuota,
                ConnectionStrings = args.ConnectionStrings,
                ClientAffinityEnabled = args.ClientAffinityEnabled,
                AuthSettings = args.AuthSettings,
                AppSettings = args.AppSettings,
                AppServicePlanId = args.AppServicePlanId,
                HttpsOnly = args.HttpsOnly,
                Version = args.Version ?? "~3"
            };


            var archive = new Dictionary<string, AssetOrArchive>
            {
                {"host.json",  new StringAsset(
                    args.HostJson != null
                    ? JsonConvert.SerializeObject(args.HostJson)
                    : "{\"version\": \"2.0\", \"logging\": { \"applicationInsights\": { \"samplingExcludedTypes\": \"Request\", \"samplingSettings\": { \"isEnabled\": true } } } }")},
            };


            foreach (var function in args.Functions)
            {
                archive.Add(function.Name + "/function.json", new StringAsset(BuildFunctionJson(function)));

                // TODO: find better way to identify all required assemblies
                foreach (var assembly in Directory.GetFiles(new FileInfo(function.Callback.DeclaringType.Assembly.Location).DirectoryName, "*.dll"))
                {
                    var key = "bin/" + new FileInfo(assembly).Name;
                    if (!archive.ContainsKey(key))
                    {
                        archive.Add(key, new FileAsset(assembly));
                    }
                }
            }

            archive.Add("bin/extensions.json", new StringAsset(BuildExtensionsJson(args)));

            mapped.Archive = new AssetArchive(archive);

            if (mapped.AppSettings == null)
            {
                mapped.AppSettings = new InputMap<string>();
            }

            mapped.AppSettings.Add("FUNCTIONS_EXTENSION_VERSION", "~3");
            mapped.AppSettings.Add("FUNCTIONS_WORKER_RUNTIME", "dotnet");

            return mapped;
        }

        private static string BuildExtensionsJson(CallbackFunctionAppArgs args)
        {
            return JsonConvert.SerializeObject(new
            {
                extensions = args.Functions.SelectMany(GetExtensionsUsedInFunction)
                .Distinct()
                .Where(t => t != typeof(HttpWebJobsStartup))
                .Select(e => new
                {
                    name = e.Name.Replace("WebJobsStartup", ""),
                    typeName = e.AssemblyQualifiedName
                })
                .ToArray()
            });
        }

        private static IEnumerable<Type> GetExtensionsUsedInFunction(CallbackFunction f)
        {
            return f.Callback
                .GetParameters()
                .SelectMany(p => p.GetCustomAttributes())
                .SelectMany(a => a.GetType().Assembly.GetTypes().Where(t => t.GetInterface(nameof(IWebJobsStartup)) != null));
        }

        private static string BuildFunctionJson(CallbackFunction function)
        {
            return JsonConvert.SerializeObject(new
            {
                configurationSource = "attributes",
                bindings = BuildBindingDefinition(function.Callback),
                disabled = false,
                scriptFile = "../bin/" + new FileInfo(function.Callback.DeclaringType.Assembly.Location).Name,
                entryPoint = function.Callback.DeclaringType.FullName + "." + function.Callback.Name
            });
        }

        private static List<object> BuildBindingDefinition(MethodInfo callback)
        {
            var bindings = new List<object>();

            // TODO: test with all trigger types.

            foreach (var parameter in callback.GetParameters())
            {
                var bindingDefinition = BuildBindingDefinition(parameter);
                if(bindingDefinition != null)
                {
                    bindings.Add(bindingDefinition);
                }
            }

            return bindings;
        }

        private static object BuildBindingDefinition(ParameterInfo parameter)
        {
            var httpTrigger = parameter.GetCustomAttribute<HttpTriggerAttribute>();
            if (httpTrigger != null)
            {
                return new
                {
                    type = "httpTrigger",
                    direction = "in",
                    route = httpTrigger.Route,
                    methods = httpTrigger.Methods,
                    authLevel = httpTrigger.AuthLevel.ToString().ToLowerInvariant(),
                    name = parameter.Name
                };
            }

            var otherTrigger = parameter.GetCustomAttributes().SingleOrDefault(a => a.GetType().Name.EndsWith("TriggerAttribute"));
            if (otherTrigger != null)
            {
                return BuildTriggerBindingDefinition(parameter, otherTrigger, otherTrigger.GetType().GetProperties().Where(p => p.DeclaringType == otherTrigger.GetType()).Select(p => p.Name).ToArray());
            }

            return null;
        }

        private static JObject BuildTriggerBindingDefinition(ParameterInfo parameter, Attribute attribute, params string[] properties)
        {
            var bindingDefinition = new JObject
            {
                ["type"] = PascalCase(attribute.GetType().Name.Replace("Attribute", "")),
                ["name"] = parameter.Name,
                ["direction"] = "in"
            };

            foreach (var prop in properties)
            {
                var value = attribute.GetType().GetProperty(prop).GetValue(attribute);
                if(value != null)
                {
                    bindingDefinition[PascalCase(prop)] = JToken.FromObject(value);
                }
            }
            return bindingDefinition;
        }

        private static string PascalCase(string s)
        {
            return s.Substring(0, 1).ToLowerInvariant() + s.Substring(1);
        }
    }

    public class CallbackFunctionAppArgs : ResourceArgs
    {
        /// <summary>
        /// The backend storage account which will be used by this Function App (such
        /// as the dashboard, logs, and to deploy the function to).
        /// </summary>
        [Input("storageAccount", true, false)]
        public Input<Account> StorageAccount { get; set; }

        /// <summary>
        /// The backend storage container which will be used to deploy this Function App 
        /// </summary>
        [Input("storageAccountContainer", true, false)]
        public Input<Container> StorageContainer { get; set; }

        /// <summary>
        /// A `site_config` object as defined below.
        /// </summary>
        [Input("siteConfig", false, false)]
        public Input<FunctionAppSiteConfigArgs> SiteConfig { get; set; }

        /// <summary>
        /// The name of the resource group in which to create the Function App.
        [Input("resourceGroupName", true, false)]
        public Input<string> ResourceGroupName { get; set; }

        /// <summary>
        /// A string indicating the Operating System type for this function app.
        /// </summary>
        [Input("osType", false, false)]
        public Input<string> OsType { get; set; }

        /// <summary>
        /// Specifies the name of the Function App. Changing this forces a new resource to
        /// be created.
        /// </summary>
        [Input("name", false, false)]
        public Input<string> Name { get; set; }

        /// <summary>
        /// Specifies the supported Azure location where the resource exists. Changing this
        /// forces a new resource to be created.
        /// </summary>
        [Input("location", false, false)]
        public Input<string> Location { get; set; }

        /// <summary>
        /// A mapping of tags to assign to the resource.
        /// </summary>
        public InputMap<string> Tags { get; set; }

        /// <summary>
        /// An `identity` block as defined below.
        /// </summary>
        [Input("identity", false, false)]
        public Input<FunctionAppIdentityArgs> Identity { get; set; }

        /// <summary>
        /// Is the Function App enabled?
        /// </summary>
        [Input("enabled", false, false)]
        public Input<bool> Enabled { get; set; }

        /// <summary>
        /// Should the built-in logging of this Function App be enabled? Defaults to `true`.
        /// </summary>
        [Input("enableBuiltinLogging", false, false)]
        public Input<bool> EnableBuiltinLogging { get; set; }

        /// <summary>
        /// The amount of memory in gigabyte-seconds that your application is allowed to
        /// consume per day. Setting this value only affects function apps under the consumption
        /// plan. Defaults to `0`.
        /// </summary>
        [Input("dailyMemoryTimeQuota", false, false)]
        public Input<int> DailyMemoryTimeQuota { get; set; }

        /// <summary>
        /// An `connection_string` block as defined below.
        public InputList<FunctionAppConnectionStringArgs> ConnectionStrings { get; set; }

        /// <summary>
        /// Should the Function App send session affinity cookies, which route client requests
        /// in the same session to the same instance?
        /// </summary>
        [Input("clientAffinityEnabled", false, false)]
        public Input<bool> ClientAffinityEnabled { get; set; }

        /// <summary>
        /// A `auth_settings` block as defined below.
        /// </summary>
        [Input("authSettings", false, false)]
        public Input<FunctionAppAuthSettingsArgs> AuthSettings { get; set; }

        /// <summary>
        /// A map of key-value pairs for [App Settings](https://docs.microsoft.com/en-us/azure/azure-functions/functions-app-settings)
        /// and custom values.
        /// </summary>
        public InputMap<string> AppSettings { get; set; }

        /// <summary>
        /// The ID of the App Service Plan within which to create this Function App.
        /// </summary>
        [Input("appServicePlanId", true, false)]
        public Input<string> AppServicePlanId { get; set; }

        /// <summary>
        /// Can the Function App only be accessed via HTTPS? Defaults to `false`.
        /// </summary>
        [Input("httpsOnly", false, false)]
        public Input<bool> HttpsOnly { get; set; }

        /// <summary>
        /// The runtime version associated with the Function App. Defaults to `~3`.
        /// </summary>
        [Input("version", false, false)]
        public Input<string> Version { get; set; }

        /// <summary>
        /// The functions to be deployed to this function app
        /// </summary>
        public CallbackFunction[] Functions { get; set; }

        /// <summary>
        /// (Optional) the contents of the host.json file to be generated. The object provided will be JSON serialized.
        /// </summary>
        public object HostJson { get; set; }
    }

    public class CallbackFunction
    {
        public CallbackFunction(MethodInfo callback, string name = null)
        {
            Callback = callback;
            Name = string.IsNullOrEmpty(name) ? callback.GetCustomAttribute<FunctionNameAttribute>().Name : name;
        }

        internal MethodInfo Callback { get; private set; }
        internal string Name { get; private set; }

        public static CallbackFunction From<T1>(Func<T1> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2>(Func<T1, T2> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3>(Func<T1, T2, T3> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4>(Func<T1, T2, T3, T4> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4, T5, T6, T7, T8>(Func<T1, T2, T3, T4, T5, T6, T7, T8> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1>(Action<T1> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2>(Action<T1, T2> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3>(Action<T1, T2, T3> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4>(Action<T1, T2, T3, T4> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

    }

}
