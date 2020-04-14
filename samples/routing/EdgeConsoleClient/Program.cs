using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Storage;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries;
using Hyperledger.Aries.Utils;
using Hyperledger.Aries.Routing;
using EdgeConsoleClient.Protocols.BasicMessage;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Extensions;
namespace EdgeConsoleClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder("Edge").Build();
            await host.StartAsync();
            var context = await host.Services.GetRequiredService<IAgentProvider>().GetContextAsync();
            ConnectionRequestMessage request;
            ConnectionRecord record = null;
            
            while(true) {
                var options = new List<string>() { "1", "2", "3", "4", "0" };
                Console.Write(@"
                    Choose your option.
                    1) Request Invitation
                    2) Get basic messages
                    3) List and request offer
                    4) List Credentials
                    0) Exit
                ");
                var option = Console.ReadLine();
                if (options.Contains(option)) {
                    switch(option) 
                    {
                    case "0":
                        Console.WriteLine("Exiting....");
                        break;
                    case "1":
                        Console.WriteLine("Enter Invite");
                        var invite = Console.ReadLine();
                        var invitation = MessageUtils.DecodeMessageFromUrlFormat<ConnectionInvitationMessage>(invite);

                        (request, record) = await host.Services.GetRequiredService<IConnectionService>().CreateRequestAsync(context, invitation);
                        await host.Services.GetRequiredService<IMessageService>().SendAsync(context.Wallet, request, record);
                        Console.WriteLine(record);
                        
                        await host.Services.GetRequiredService<IEdgeClientService>().FetchInboxAsync(context);
                        record = await host.Services.GetRequiredService<IConnectionService>().GetAsync(context, record.Id);
                        Console.WriteLine(record);
                        break;

                    case "2":
                        try
                        {
                            await host.Services.GetRequiredService<IEdgeClientService>().FetchInboxAsync(context);
                            var msgs = await host.Services.GetRequiredService<IWalletRecordService>().SearchAsync<BasicMessageRecord>(context.Wallet,
                            SearchQuery.Equal(nameof(BasicMessageRecord.ConnectionId), record.Id), null, 10);
                            Console.WriteLine("Basic Messages..........");
                            foreach( var item in msgs) {
                                Console.WriteLine(item.Text);
                            }
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        break;

                    case "3":
                        try
                        {
                            await host.Services.GetRequiredService<IEdgeClientService>().FetchInboxAsync(context);
                            var offers = await host.Services.GetRequiredService<ICredentialService>().ListOffersAsync(context);
                            Console.WriteLine("Credential Offers..........");
                            foreach( var item in offers) {
                                Console.WriteLine(item);
                                (var requestCred, var holderCredentialRecord) = await host.Services.GetRequiredService<ICredentialService>().CreateRequestAsync(context, item.Id);
                                await host.Services.GetRequiredService<IMessageService>().SendAsync(context.Wallet, requestCred, record);
                            }
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        break;

                    case "4":
                        try
                        {
                            await host.Services.GetRequiredService<IEdgeClientService>().FetchInboxAsync(context);
                            var creds = await host.Services.GetRequiredService<ICredentialService>().ListIssuedCredentialsAsync(context);
                            Console.WriteLine("Credentials..........");
                            foreach( var item in creds) {
                                foreach( var credattr in item.CredentialAttributesValues) {
                                    Console.WriteLine("{0} {1}", credattr.Name, credattr.Value);
                                }
                            }
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        break;
                    default:
                        break;
                    }
                    if (option == "0") break;
                } else {
                    Console.WriteLine("Wrong option try again.");
                }
            }
        }

        public static IHostBuilder CreateHostBuilder(string walletId) =>
            Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddAriesFramework(builder =>
                    {
                        builder.RegisterEdgeAgent(options =>
                        {
                            options.PoolName = "Test2Pool";
                            options.GenesisFilename = Path.GetFullPath("pool_genesis.txn");
                            options.EndpointUri = "http://dotnet.repyute.com:5000";
                            options.WalletConfiguration.Id = walletId;
                        });
                    });
                    services.AddSingleton<BasicMessageHandler>();
                });
    }
}
