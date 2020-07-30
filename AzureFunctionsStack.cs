using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
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
                    // - not require explicit generic type definitions. https://github.com/dotnet/csharplang/issues/129
                    // - allow to use lambda functions (this will need a non-attribute driven way of configuring inputs)
                    CallbackFunction.From<HttpRequest, IAsyncCollector<CounterTableEntity>, CounterTableEntity, Task<string>>(Api.Count),
                    CallbackFunction.From<HttpRequest, Task<string>>(Api.Hello),
                    CallbackFunction.From<HttpRequest, IDurableEntityClient, ILogger, Task<string>>(Api.CountUsingDurableEntity),
                    CallbackFunction.From<IDurableEntityContext, ILogger, Task>(CounterDurableEntity.Run)
                },
                AppSettings = new InputMap<string>
                {
                    {"APPINSIGHTS_INSTRUMENTATIONKEY", insights.InstrumentationKey }
                }
            });
        }
    }

}
