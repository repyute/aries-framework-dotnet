using System;
using Hyperledger.Aries.Agents;
using WebAgent.Messages;
using WebAgent.Protocols.BasicMessage;
using WebAgent.Protocols.GenericFetch;

namespace WebAgent
{
    public class SimpleWebAgent : AgentBase
    {
        public SimpleWebAgent(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        protected override void ConfigureHandlers()
        {
            AddConnectionHandler();
            AddForwardHandler();
            AddHandler<BasicMessageHandler>();
            AddHandler<TrustPingMessageHandler>();
            AddHandler<GenericFetchHandler>();
            AddDiscoveryHandler();
            AddTrustPingHandler();
            AddCredentialHandler();
            AddProofHandler();
        }
    }
}