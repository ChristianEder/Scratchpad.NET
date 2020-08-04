using Pulumi.Azure.AppService.Inputs;
using Pulumi.Azure.Storage;

namespace Pulumi.AzureFunctions.Sdk
{
    public class ArchiveFunctionAppArgs : ResourceArgs
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
        /// The archive which will be used to deploy this Function App 
        /// </summary>
        [Input("archive", true, false)]
        public Input<AssetOrArchive> Archive { get; set; }

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
        /// The runtime version associated with the Function App. Defaults to `~1`.
        /// </summary>
        [Input("version", false, false)]
        public Input<string> Version { get; set; }
    }

}
