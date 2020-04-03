using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Storage;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries;
using Hyperledger.Aries.Utils;
using Hyperledger.Aries.Routing;
using EdgeConsoleClient.Protocols.BasicMessage;

namespace EdgeConsoleClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));

            var host = CreateHostBuilder("Edge").Build();
            
            var inviteUrl = "http://10.0.0.11:7000?c_i=eyJsYWJlbCI6IkNvbXBldGVudCBNaXJ6YWtoYW5pIiwiaW1hZ2VVcmwiOm51bGwsInNlcnZpY2VFbmRwb2ludCI6Imh0dHA6Ly8xMC4wLjAuMTE6NzAwMCIsInJvdXRpbmdLZXlzIjpbIkNoSjFGNnpDVFhvclN6bkx6c1ptVlB4aDJVa29VWUo4Y0YzUG04d2JzUlZ2Il0sInJlY2lwaWVudEtleXMiOlsiNU1TdGtqVVdIM2R4RXBDOXNYS1BzRXBSdmFNd0RuUFBCZ1JudWFVdVRrc0YiXSwiQGlkIjoiZWFlMDBjMjUtOWUxZC00ZGI4LTk1Y2UtN2NjZDJmM2M5ZTk5IiwiQHR5cGUiOiJkaWQ6c292OkJ6Q2JzTlloTXJqSGlxWkRUVUFTSGc7c3BlYy9jb25uZWN0aW9ucy8xLjAvaW52aXRhdGlvbiJ9";
            var invitation = MessageUtils.DecodeMessageFromUrlFormat<ConnectionInvitationMessage>(inviteUrl);

            await host.StartAsync();

            var context = await host.Services.GetRequiredService<IAgentProvider>().GetContextAsync();

            var (request, record) = await host.Services.GetRequiredService<IConnectionService>().CreateRequestAsync(context, invitation);
            await host.Services.GetRequiredService<IMessageService>().SendAsync(context.Wallet, request, record);
            Console.WriteLine(record);
            
            await host.Services.GetRequiredService<IEdgeClientService>().FetchInboxAsync(context);
            record = await host.Services.GetRequiredService<IConnectionService>().GetAsync(context, record.Id);
            Console.WriteLine(record);
            
            while(true) {
                await Task.Delay(TimeSpan.FromSeconds(5));
                try
                {
                    await host.Services.GetRequiredService<IEdgeClientService>().FetchInboxAsync(context);
                    var msgs = await host.Services.GetRequiredService<IWalletRecordService>().SearchAsync<BasicMessageRecord>(context.Wallet,
                    SearchQuery.Equal(nameof(BasicMessageRecord.ConnectionId), record.Id), null, 10);
                    Console.WriteLine("Messages..........");
                    foreach( var item in msgs) {
                        Console.WriteLine(item.Text);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            Console.WriteLine("Exiting...");
        }

        public static IHostBuilder CreateHostBuilder(string walletId) =>
            Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddAriesFramework(builder =>
                    {
                        builder.RegisterEdgeAgent(options =>
                        {
                            options.EndpointUri = "http://10.0.0.12:5000";
                            options.WalletConfiguration.Id = walletId;
                        });
                    });
                    services.AddSingleton<BasicMessageHandler>();
                });
    }
}
