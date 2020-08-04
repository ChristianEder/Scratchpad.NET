using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Scratchpad.NET.Azure.Functions.Sample
{
    public static class Api
    {
        [FunctionName("count")]
        public static async Task<string> Count(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "count")] HttpRequest req,
            [Table("counter")] IAsyncCollector<CounterTableEntity> counter,
            [Table("counter", "singleton", "singleton")] CounterTableEntity current)
        {
            current = current ?? new CounterTableEntity { RowKey = "singleton", PartitionKey = "singleton", CounterValue = 0 };
            current.CounterValue += 1;
            await counter.AddAsync(current);

            return "Hello, # " + current.CounterValue;
        }

        [FunctionName("count-durable")]
        public static async Task<string> CountUsingDurableEntity(
          [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "count-durable")] HttpRequest req,
          [DurableClient] IDurableEntityClient client, ILogger logger)
        {
            logger.LogInformation("CountUsingDurableEntity started");
            var id = new EntityId(nameof(CounterDurableEntity), "singleton");
            var state = await client.ReadEntityStateAsync<CounterDurableEntity>(id);

            await client.SignalEntityAsync(id, nameof(CounterDurableEntity.Inc));

            var count = 1;
            if (state.EntityExists)
            {
                logger.LogInformation("CountUsingDurableEntity entity exists: " + JsonConvert.SerializeObject(state.EntityState));
                count = state.EntityState.CurrentValue + 1;
            }
            else
            {
                logger.LogInformation("CountUsingDurableEntity entity does not exist");
            }

            return "Hello from durable entity, # " + count + "(entity found: " + state.EntityExists + ")";
        }

        [FunctionName("hello")]
        public static async Task<string> Hello(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hello")] HttpRequest req)
        {
            return "Hello new!";
        }
    }

    public class CounterTableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public int CounterValue { get; set; }
        public string ETag { get; } = "*";
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class CounterDurableEntity
    {
        [JsonProperty("value")]
        public int CurrentValue { get; set; }

        public void Inc() => CurrentValue += 1;

        public void Reset() => CurrentValue = 0;

        public int Get() => CurrentValue;

        [FunctionName(nameof(CounterDurableEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx, ILogger logger)
        {
            logger.LogInformation("CounterDurableEntity running " + ctx.EntityName + "." + ctx.EntityKey + "." + ctx.OperationName);
            return ctx.DispatchAsync<CounterDurableEntity>();
        }
    }
}
