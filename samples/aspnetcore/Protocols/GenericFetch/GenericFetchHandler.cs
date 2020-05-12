using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebAgent.Messages;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Decorators.Threading;
using Hyperledger.Aries.Models.Events;
using Hyperledger.Aries.Storage;

namespace WebAgent.Protocols.GenericFetch
{
    public class GenericFetchHandler : IMessageHandler
    {
        private readonly IGenericFetchService _genericFetchService;

        public GenericFetchHandler(IGenericFetchService genericFetchService)
        {
            _genericFetchService = genericFetchService;
        }

        /// <summary>
        /// Gets the supported message types.
        /// </summary>
        /// <value>
        /// The supported message types.
        /// </value>
        public IEnumerable<MessageType> SupportedMessageTypes => new[]
        {
            MessageType.FromUri(CustomMessageTypes.GenericFetchRequestMessageType),
            MessageType.FromUri(CustomMessageTypes.GenericFetchResponseMessageType)
        };

        /// <summary>
        /// Processes the agent message
        /// </summary>
        /// <param name="agentContext">The agent context.</param>
        /// <param name="messageContext">The agent message context.</param>
        public async Task<AgentMessage> ProcessAsync(IAgentContext agentContext, UnpackedMessageContext messageContext)
        {
            await Task.Yield();

            switch (messageContext.GetMessageType())
            {
                case CustomMessageTypes.GenericFetchRequestMessageType:
                    {
                        var message = messageContext.GetMessage<GenericFetchRequestMessage>();
                        var record = await _genericFetchService.ProcessRequestAsync(agentContext, message, messageContext.Connection);

                        messageContext.ContextRecord = record;
                        break;
                    }
                case CustomMessageTypes.GenericFetchResponseMessageType:
                    {
                        var message = messageContext.GetMessage<GenericFetchResponseMessage>();
                        var record = await _genericFetchService.ProcessResponseAsync(agentContext, message, messageContext.Connection);

                        messageContext.ContextRecord = record;
                        break;
                    }
            }
            return null;
        }
    }
}
