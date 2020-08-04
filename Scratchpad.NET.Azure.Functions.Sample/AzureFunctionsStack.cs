using Pulumi.Azure.Core;
using Pulumi.AzureFunctions.Sdk;

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
                Functions = CallbackFunction.FromAssembly(typeof(Api).Assembly),
                AppSettings = new InputMap<string>
                {
                    {"APPINSIGHTS_INSTRUMENTATIONKEY", insights.InstrumentationKey }
                }
            });
        }
    }

}
