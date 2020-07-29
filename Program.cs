using System.Threading.Tasks;

namespace Pulumi.AzureFunctions
{

    class Program
    {
        static Task<int> Main() => Deployment.RunAsync<AzureFunctionsStack>();
    }
}
