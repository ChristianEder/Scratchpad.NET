using Pulumi;
using System.Threading.Tasks;

namespace Scratchpad.NET.Azure.Functions.Sample
{

    class Program
    {
        static Task<int> Main() => Deployment.RunAsync<AzureFunctionsStack>();
    }
}
