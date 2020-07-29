using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;

namespace Pulumi.AzureFunctions
{
    public static class Api
    {
        [FunctionName("count")]
        public static async Task<string> Count(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "count")] HttpRequest req,
            [Table("counter")] IAsyncCollector<Counter> counter,
            [Table("counter", "singleton", "singleton")] Counter current)
        {
            current = current ?? new Counter { RowKey = "singleton", PartitionKey = "singleton", CounterValue = 0 };
            current.CounterValue += 1;
            await counter.AddAsync(current);

            return "Hello, # " + current.CounterValue;
        }

        [FunctionName("hello")]
        public static async Task<string> Hello(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hello")] HttpRequest req)
        {
            return "Hello!";
        }
    }

    public class Counter
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public int CounterValue { get; set; }
        public string ETag { get; } = "*";
    }
}
