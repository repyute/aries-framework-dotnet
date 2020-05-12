using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Decorators;
using Hyperledger.Aries.Decorators.Attachments;
using Hyperledger.Aries.Decorators.Threading;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Models.Events;
using Hyperledger.Aries.Utils;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.PoolApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Storage;
using Hyperledger.Aries.Decorators.Service;
using System.Diagnostics;
using Hyperledger.Aries;

namespace WebAgent.Protocols.GenericFetch
{
    public class GenericFetchService : IGenericFetchService
    {
        protected readonly IConnectionService ConnectionService;

        protected readonly IWalletRecordService RecordService;

        protected readonly IEventAggregator EventAggregator;

        protected readonly ILogger<GenericFetchService> Logger;

        protected readonly IMessageService MessageService;

        protected readonly int GenericFetchCreateRequestLoggingEventId = 10000;

        public GenericFetchService(
            IConnectionService connectionService,
            IEventAggregator eventAggregator,
            IWalletRecordService recordService,
            IMessageService messageService,
            ILogger<GenericFetchService> logger)
        {
            EventAggregator = eventAggregator;
            ConnectionService = connectionService;
            MessageService = messageService;
            RecordService = recordService;
            Logger = logger;
        }

        public async Task<(GenericFetchRequestMessage, GenericFetchRecord)> CreateRequestAsync(IAgentContext agentContext, GenericFetchRequest fetchRequest, string connectionId)
        {
            Logger.LogInformation(GenericFetchCreateRequestLoggingEventId, "ConnectionId {0}", connectionId);
            if (fetchRequest == null)
            {
                throw new ArgumentNullException(nameof(fetchRequest), "You must provide fetch request");
            }
            if (connectionId != null)
            {
                var connection = await ConnectionService.GetAsync(agentContext, connectionId);

                if (connection.State != ConnectionState.Connected)
                    throw new AriesFrameworkException(ErrorCode.RecordInInvalidState,
                        $"Connection state was invalid. Expected '{ConnectionState.Connected}', found '{connection.State}'");
            }

            var threadId = Guid.NewGuid().ToString();
            var fetchRecord = new GenericFetchRecord
            {
                Id = Guid.NewGuid().ToString(),
                ConnectionId = connectionId,
                Type = fetchRequest.Type,
                Payload = fetchRequest.Payload
            };
            fetchRecord.SetTag(TagConstants.Role, TagConstants.Requestor);
            fetchRecord.SetTag(TagConstants.LastThreadId, threadId);

            await RecordService.AddAsync(agentContext.Wallet, fetchRecord);

            var message = new GenericFetchRequestMessage
            {
                Id = threadId,
                FetchType = fetchRequest.Type,
                FetchPayload = fetchRequest.Payload
            };

            message.ThreadFrom(threadId);
            return (message, fetchRecord);
        }

        public async Task<(GenericFetchResponseMessage, GenericFetchRecord)> CreateResponseAsync(IAgentContext agentContext, string genericFetchRecordId)
        {
            var fetchRecord = await GetAsync(agentContext, genericFetchRecordId);

            // Some state validation that fetchrecord is ready to be consumed

            // Processing request goes here
            string fetch_response = "fixed response for now.";

            // Update local credential record with new info
            fetchRecord.Response = fetch_response;

            // Go deep into trigger and check if it is required heres
            await RecordService.UpdateAsync(agentContext.Wallet, fetchRecord);

            var threadId = fetchRecord.GetTag(TagConstants.LastThreadId);
            var response = new GenericFetchResponseMessage
            {
                FetchResponse = fetch_response
            };

            response.ThreadFrom(threadId);
            return (response, fetchRecord);
        }

        public async Task<GenericFetchRecord> GetAsync(IAgentContext agentContext, string genericFetchRecordId)
        {
            var record = await RecordService.GetAsync<GenericFetchRecord>(agentContext.Wallet, genericFetchRecordId);

            if (record == null)
                throw new AriesFrameworkException(ErrorCode.RecordNotFound, "Fetchs record not found");

            return record;
        }

        public virtual Task<List<GenericFetchRecord>> ListAsync(IAgentContext agentContext, ISearchQuery query = null, int count = 100) =>
                    RecordService.SearchAsync<GenericFetchRecord>(agentContext.Wallet, query, null, count);

        public async Task<GenericFetchRecord> ProcessRequestAsync(IAgentContext agentContext, GenericFetchRequestMessage fetchRequest, ConnectionRecord connection)
        {
            // Add basic validation

            // Write fetch record to local wallet
            var fetchRecord = new GenericFetchRecord
            {
                Id = Guid.NewGuid().ToString(),
                ConnectionId = connection?.Id,
                Type = fetchRequest.FetchType,
                Payload = fetchRequest.FetchPayload,
            };
            fetchRecord.SetTag(TagConstants.LastThreadId, fetchRequest.GetThreadId());
            fetchRecord.SetTag(TagConstants.Role, TagConstants.Issuer);

            await RecordService.AddAsync(agentContext.Wallet, fetchRecord);

            EventAggregator.Publish(new ServiceMessageProcessingEvent
            {
                RecordId = fetchRecord.Id,
                MessageType = fetchRequest.Type,
                ThreadId = fetchRequest.GetThreadId()
            });

            return fetchRecord;
        }

        public async Task<GenericFetchRecord> ProcessResponseAsync(IAgentContext agentContext, GenericFetchResponseMessage fetchResponse, ConnectionRecord connection)
        {
            // Add basic validation for fetchReponse

            var fetchRecord = await this.GetByThreadIdAsync(agentContext, fetchResponse.GetThreadId());

            // Basic validation for fetchRecord

            fetchRecord.Response = fetchResponse.FetchResponse;
            await RecordService.UpdateAsync(agentContext.Wallet, fetchRecord);

            EventAggregator.Publish(new ServiceMessageProcessingEvent
            {
                RecordId = fetchRecord.Id,
                MessageType = fetchResponse.Type,
                ThreadId = fetchResponse.GetThreadId()
            });

            return fetchRecord;
        }
    }
}
