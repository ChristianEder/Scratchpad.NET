using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

}
