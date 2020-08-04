using Pulumi.Azure.AppService;
using Pulumi.Azure.AppService.Inputs;
using Pulumi.Azure.Storage;
using System.Text.RegularExpressions;

namespace Pulumi.AzureFunctions.Sdk
{

    public class ArchiveFunctionApp : FunctionApp
    {
        public ArchiveFunctionApp(string name, ArchiveFunctionAppArgs args, CustomResourceOptions options = null) : base(name, Map(name, args), options)
        {
        }

        private static FunctionAppArgs Map(string name, ArchiveFunctionAppArgs args)
        {
            if(args.AppServicePlanId == null)
            {
                var plan = new Plan(name, new PlanArgs
                {
                    ResourceGroupName = args.ResourceGroupName,
                    Kind = "FunctionApp",
                    Sku = new PlanSkuArgs
                    {
                        Tier = "Dynamic",
                        Size = "Y1"
                    }
                });
                args.AppServicePlanId = plan.Id;
            }

            if (args.StorageAccount == null)
            {
                args.StorageAccount = new Account(MakeSafeStorageAccountName(name), new AccountArgs
                {
                    ResourceGroupName = args.ResourceGroupName,
                    AccountReplicationType = "LRS",
                    AccountTier = "Standard",
                    AccountKind = "StorageV2"
                });
            }

            if(args.StorageContainer == null)
            {
                args.StorageContainer = new Container(MakeSafeStorageContainerName(name), new ContainerArgs
                {
                    StorageAccountName = args.StorageAccount.Apply(a => a.Name),
                    ContainerAccessType = "private"
                });
            }

            var mapped = new FunctionAppArgs
            {
                StorageAccountName = args.StorageAccount.Apply(a => a.Name),
                StorageAccountAccessKey = args.StorageAccount.Apply(a => a.PrimaryAccessKey),
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
                Version = args.Version
            };

            var blob = new Blob(name, new BlobArgs
            {
                StorageAccountName = args.StorageAccount.Apply(a => a.Name),
                StorageContainerName = args.StorageContainer.Apply(a => a.Name),
                Source = args.Archive.Apply(a => (AssetOrArchive)a),
                Type = "Block"
            });

            if (mapped.AppSettings == null)
            {
                mapped.AppSettings = new InputMap<string>();
            }

            mapped.AppSettings.Add("WEBSITE_RUN_FROM_PACKAGE", GetSignedBlobUrl(blob, args.StorageAccount));
            return mapped;
        }

        private static string MakeSafeStorageAccountName(string name)
        {
            var regex = new Regex("[^a-zA-Z0-9]");
            var replaced = regex.Replace(name, "").ToLowerInvariant();
            return TrimLength(replaced, 24 - 8);
        }

        private static string MakeSafeStorageContainerName(string name)
        {
            var regex = new Regex("[^a-zA-Z0-9-]");
            var replaced = regex.Replace(name, "").ToLowerInvariant();
            return TrimLength(replaced, 63 - 8);
        }

        private static string TrimLength(string name, int length)
        {
            if(name.Length < length)
            {
                return name;
            }

            return name.Substring(0, length);
        }

        private static Output<string> GetSignedBlobUrl(Blob blob, Input<Account> storage)
        {
            const string signatureExpiration = "2100-01-01";

            var url = Output.All(new[] { storage.Apply(s => s.Name), storage.Apply(s => s.PrimaryConnectionString), blob.StorageContainerName, blob.Name })
            .Apply(async (parameters) =>
            {
                var accountName = parameters[0];
                var connectionString = parameters[1];
                var containerName = parameters[2];
                var blobName = parameters[3];

                var sas = await GetAccountBlobContainerSAS.InvokeAsync(new GetAccountBlobContainerSASArgs
                {
                    ConnectionString = connectionString,
                    ContainerName = containerName,
                    Start = "2020-07-20",
                    Expiry = signatureExpiration,
                    Permissions = new Azure.Storage.Inputs.GetAccountBlobContainerSASPermissionsArgs
                    {
                        Read = true,
                        Write = false,
                        Delete = false,
                        List = false,
                        Add = false,
                        Create = false
                    }
                });
                return $"https://{accountName}.blob.core.windows.net/{containerName}/{blobName}{sas.Sas}";
            });

            return url;

        }
    }
}
