using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Pulumi.Azure.Core;
using Pulumi.AzureFunctions.Sdk;
using System.Threading.Tasks;

namespace Pulumi.AzureFunctions
{
    public class AzureFunctionsStack : Stack
    {
        public AzureFunctionsStack()
        {
            var resourceGroup = new ResourceGroup("ched-funcy", new ResourceGroupArgs
            {
                Location = "WestEurope"
            });

            var insights = new Azure.AppInsights.Insights("ched-funcy", new Azure.AppInsights.InsightsArgs
            {
                ResourceGroupName = resourceGroup.Name,
                ApplicationType = "web"
            });

            var functionApp = new CallbackFunctionApp("ched-funcy", new CallbackFunctionAppArgs
            {
                ResourceGroupName = resourceGroup.Name,
                Functions = new CallbackFunction[]
                {
                    // TODO: find ways to
                    // - not require explicit generic type definitions 
                    // - allow to use lambda functions (this will need a non-attribute driven way of configuring inputs)
                    CallbackFunction.From<HttpRequest, IAsyncCollector<Counter>, Counter, Task<string>>(Api.Count),
                    CallbackFunction.From<HttpRequest, Task<string>>(Api.Hello)
                },
                AppSettings = new InputMap<string>
                {
                    {"APPINSIGHTS_INSTRUMENTATIONKEY", insights.InstrumentationKey }
                }
            });
        }
    }

}
