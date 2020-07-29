using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Hosting;
using Newtonsoft.Json;
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
                {"host.json",  new StringAsset("{\"version\": \"2.0\", \"logging\": { \"applicationInsights\": { \"samplingExcludedTypes\": \"Request\", \"samplingSettings\": { \"isEnabled\": true } } } }")},
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

            // TODO: support all trigger types. 
            //       At least other bindings (in&out) seem to work out of the box 
            //       with attributes even if they are not included in the function.json

            foreach (var parameter in callback.GetParameters())
            {
                var httpBinding = parameter.GetCustomAttribute<HttpTriggerAttribute>();
                if (httpBinding != null)
                {
                    bindings.Add(new
                    {
                        type = "httpTrigger",
                        direction = "in",
                        route = httpBinding.Route,
                        methods = httpBinding.Methods,
                        authLevel = httpBinding.AuthLevel.ToString().ToLowerInvariant(),
                        name = parameter.Name
                    });
                }
            }

            return bindings;
        }
    }

    public class CallbackFunctionAppArgs : ArchiveFunctionAppArgs
    {
        // Summary:
        //     The backend storage account which will be used by this Function App (such
        //     as the dashboard, logs, and to deploy the function to).
        [Input("storageAccount", true, false)]
        public Input<Account>? StorageAccount { get; set; }

        // Summary:
        //     The backend storage container which will be used to deploy this Function App 
        [Input("storageAccountContainer", true, false)]
        public Input<Container>? StorageContainer { get; set; }

        //
        // Summary:
        //     A `site_config` object as defined below.
        [Input("siteConfig", false, false)]
        public Input<FunctionAppSiteConfigArgs>? SiteConfig { get; set; }
        //
        // Summary:
        //     The name of the resource group in which to create the Function App.
        [Input("resourceGroupName", true, false)]
        public Input<string> ResourceGroupName { get; set; }
        //
        // Summary:
        //     A string indicating the Operating System type for this function app.
        [Input("osType", false, false)]
        public Input<string>? OsType { get; set; }
        //
        // Summary:
        //     Specifies the name of the Function App. Changing this forces a new resource to
        //     be created.
        [Input("name", false, false)]
        public Input<string>? Name { get; set; }
        //
        // Summary:
        //     Specifies the supported Azure location where the resource exists. Changing this
        //     forces a new resource to be created.
        [Input("location", false, false)]
        public Input<string>? Location { get; set; }
        //
        // Summary:
        //     A mapping of tags to assign to the resource.
        public InputMap<string> Tags { get; set; }
        //
        // Summary:
        //     An `identity` block as defined below.
        [Input("identity", false, false)]
        public Input<FunctionAppIdentityArgs>? Identity { get; set; }
        //
        // Summary:
        //     Is the Function App enabled?
        [Input("enabled", false, false)]
        public Input<bool>? Enabled { get; set; }
        //
        // Summary:
        //     Should the built-in logging of this Function App be enabled? Defaults to `true`.
        [Input("enableBuiltinLogging", false, false)]
        public Input<bool>? EnableBuiltinLogging { get; set; }
        //
        // Summary:
        //     The amount of memory in gigabyte-seconds that your application is allowed to
        //     consume per day. Setting this value only affects function apps under the consumption
        //     plan. Defaults to `0`.
        [Input("dailyMemoryTimeQuota", false, false)]
        public Input<int>? DailyMemoryTimeQuota { get; set; }
        //
        // Summary:
        //     An `connection_string` block as defined below.
        public InputList<FunctionAppConnectionStringArgs> ConnectionStrings { get; set; }
        //
        // Summary:
        //     Should the Function App send session affinity cookies, which route client requests
        //     in the same session to the same instance?
        [Input("clientAffinityEnabled", false, false)]
        public Input<bool>? ClientAffinityEnabled { get; set; }
        //
        // Summary:
        //     A `auth_settings` block as defined below.
        [Input("authSettings", false, false)]
        public Input<FunctionAppAuthSettingsArgs>? AuthSettings { get; set; }
        //
        // Summary:
        //     A map of key-value pairs for [App Settings](https://docs.microsoft.com/en-us/azure/azure-functions/functions-app-settings)
        //     and custom values.
        public InputMap<string> AppSettings { get; set; }
        //
        // Summary:
        //     The ID of the App Service Plan within which to create this Function App.
        [Input("appServicePlanId", true, false)]
        public Input<string> AppServicePlanId { get; set; }
        //
        // Summary:
        //     Can the Function App only be accessed via HTTPS? Defaults to `false`.
        [Input("httpsOnly", false, false)]
        public Input<bool>? HttpsOnly { get; set; }
        //
        // Summary:
        //     The runtime version associated with the Function App. Defaults to `~1`.
        [Input("version", false, false)]
        public Input<string>? Version { get; set; }

        // Summary:
        //     The functions to be deployed to this function app
        public CallbackFunction[] Functions { get; set; }
    }

    public class CallbackFunction
    {
        private CallbackFunction(MethodInfo callback, string name)
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

    }

}
