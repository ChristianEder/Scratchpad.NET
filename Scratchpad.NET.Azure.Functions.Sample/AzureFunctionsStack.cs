using Pulumi;
using Pulumi.Azure.AppInsights;
using Pulumi.Azure.Core;

namespace Scratchpad.NET.Azure.Functions.Sample
{
    public class AzureFunctionsStack : Stack
    {
        public AzureFunctionsStack()
        {
            var resourceGroup = new ResourceGroup("ched-funcy", new ResourceGroupArgs
            {
                Location = "WestEurope"
            });

            var insights = new Insights("ched-funcy", new InsightsArgs
            {
                ResourceGroupName = resourceGroup.Name,
                ApplicationType = "web"
            });

            var functionApp = new CallbackFunctionApp("ched-funcy", new CallbackFunctionAppArgs
            {
                ResourceGroupName = resourceGroup.Name,
                Functions = CallbackFunction.FromAssembly(typeof(Api).Assembly),
                AppSettings = new InputMap<string>
                {
                    {"APPINSIGHTS_INSTRUMENTATIONKEY", insights.InstrumentationKey }
                }
            });
        }
    }

}
