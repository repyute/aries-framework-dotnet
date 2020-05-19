using System;
using System.IO;
using Hyperledger.Aries.Storage;
using Jdenticon.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebAgent.Messages;
using WebAgent.Protocols.BasicMessage;
using WebAgent.Utils;
using WebAgent.Protocols.GenericFetch;
using WebAgent.Services.Fabric;

namespace WebAgent
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddLogging();

            // Register agent framework dependency services and handlers
            services.AddAriesFramework(builder =>
            {
                builder.RegisterAgent<SimpleWebAgent>(c =>
                {
                    c.AgentName = Environment.GetEnvironmentVariable("AGENT_NAME") ?? NameGenerator.GetRandomName();
                    c.EndpointUri = Environment.GetEnvironmentVariable("ENDPOINT_HOST") ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
                    c.WalletConfiguration = new WalletConfiguration { Id = "WebAgentWallet" };
                    c.WalletCredentials = new WalletCredentials { Key = "MyWalletKey" };
                    c.AgentKeySeed = "000000000000000000000000Steward1";
                    c.GenesisFilename = Path.GetFullPath("pool_genesis.txn");
                    c.PoolName = "TestPool";
                });
            });

            // Register custom handlers with DI pipeline
            services.AddSingleton<BasicMessageHandler>();
            services.AddSingleton<TrustPingMessageHandler>();
            services.AddSingleton<IGenericFetchService, GenericFetchService>();
            services.AddSingleton<GenericFetchHandler>();
            services.AddHostedService<SimpleWebAgentProvisioningService>();
            services.AddSingleton<AgentDiscoveryMiddleware>();
            services.AddSingleton<FabricService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            // Register agent middleware
            app.UseAriesFramework();
            app.MapWhen(
                context => context.Request.Path.ToString().Contains(".well-known/agent-configuration"),
                builder => builder.UseMiddleware<AgentDiscoveryMiddleware>());

            // fun identicons
            app.UseJdenticon();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
