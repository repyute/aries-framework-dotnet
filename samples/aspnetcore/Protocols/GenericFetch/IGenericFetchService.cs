using System;
using System.Threading.Tasks;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Storage;
using System.Collections.Generic;

namespace WebAgent.Protocols.GenericFetch
{
    public interface IGenericFetchService
    {
        Task<GenericFetchRecord> GetAsync(IAgentContext agentContext, string genericFetchRecordId);

        Task<(GenericFetchRequestMessage, GenericFetchRecord)> CreateRequestAsync(IAgentContext agentContext,
            GenericFetchRequest fetchRequest, string connectionId);

        Task<GenericFetchRecord> ProcessRequestAsync(IAgentContext agentContext, GenericFetchRequestMessage fetchRequest, ConnectionRecord connection);

        Task<GenericFetchRecord> ProcessResponseAsync(IAgentContext agentContext, GenericFetchResponseMessage fetchResponse, ConnectionRecord connection);

        Task<(GenericFetchResponseMessage, GenericFetchRecord)> CreateResponseAsync(IAgentContext agentContext, string genericFetchRecordId);

        Task<List<GenericFetchRecord>> ListAsync(IAgentContext agentContext, ISearchQuery query = null, int count = 100);
    }
}
