using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.DidExchange;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace WebAgent
{
    public class AgentDiscoveryMiddleware : IMiddleware
    {
        private readonly AgentOptions options;
        private readonly IProvisioningService provisioningService;
        private readonly IConnectionService connectionService;
        private readonly IAgentProvider agentProvider;

        public AgentDiscoveryMiddleware(
            IOptions<AgentOptions> options,
            IProvisioningService provisioningService,
            IConnectionService connectionService,
            IAgentProvider agentProvider)
        {
            this.options = options.Value;
            this.provisioningService = provisioningService;
            this.connectionService = connectionService;
            this.agentProvider = agentProvider;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var agentConfiguration = await GetConfigurationAsync();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsync(agentConfiguration.ToJson());
        }

        public async Task<ConnectionInvitationMessage> GetConfigurationAsync()
        {
            var agentContext = await agentProvider.GetContextAsync();
            var provisioningRecord = await provisioningService.GetProvisioningAsync(agentContext.Wallet);
            var connectionId = provisioningRecord.GetTag(SimpleWebAgentProvisioningService.MobileAppInvitationTagName);

            if (connectionId == null)
            {
                throw new Exception("This agent hasn't been provisioned as discovery agent");
            }
            var inviation = await connectionService.GetAsync(agentContext, connectionId);

            return inviation.GetTag(SimpleWebAgentProvisioningService.InvitationTagName)
                    .ToObject<ConnectionInvitationMessage>();
        }
    }
}
