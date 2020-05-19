﻿using System;
using System.Globalization;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Models.Events;
using Hyperledger.Aries.Storage;
using Hyperledger.Aries.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using WebAgent.Messages;
using WebAgent.Models;
using WebAgent.Protocols.BasicMessage;
using Hyperledger.Aries.Extensions;
using WebAgent.Protocols.GenericFetch;

namespace WebAgent.Controllers
{
    public class ConnectionsController : Controller
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IConnectionService _connectionService;
        private readonly IWalletService _walletService;
        private readonly IWalletRecordService _recordService;
        private readonly IProvisioningService _provisioningService;
        private readonly IAgentProvider _agentContextProvider;
        private readonly IMessageService _messageService;
        private readonly AgentOptions _walletOptions;
        private readonly ISchemaService _schemaService;
        private readonly IGenericFetchService _genericFetchService;

        private readonly ICredentialService _credentialService;
        public ConnectionsController(
            IEventAggregator eventAggregator,
            IConnectionService connectionService, 
            IWalletService walletService, 
            IWalletRecordService recordService,
            IProvisioningService provisioningService,
            ISchemaService schemaService,
            IAgentProvider agentContextProvider,
            IMessageService messageService,
            ICredentialService credentialService,
            IGenericFetchService genericFetchService,
            IOptions<AgentOptions> walletOptions)
        {
            _eventAggregator = eventAggregator;
            _connectionService = connectionService;
            _walletService = walletService;
            _recordService = recordService;
            _provisioningService = provisioningService;
            _agentContextProvider = agentContextProvider;
            _schemaService = schemaService;
            _messageService = messageService;
            _walletOptions = walletOptions.Value;
            _credentialService = credentialService;
            _genericFetchService = genericFetchService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var context = await _agentContextProvider.GetContextAsync();

            return View(new ConnectionsViewModel
            {
                Connections = await _connectionService.ListAsync(context)
            });
        }

        [HttpGet]
        public async Task<IActionResult> CreateInvitation()
        {
            var context = await _agentContextProvider.GetContextAsync();

            var (invitation, _) = await _connectionService.CreateInvitationAsync(context, new InviteConfiguration { AutoAcceptConnection = true });
            ViewData["Invitation"] = $"{(await _provisioningService.GetProvisioningAsync(context.Wallet)).Endpoint.Uri}?c_i={EncodeInvitation(invitation)}";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AcceptInvitation(AcceptConnectionViewModel model)
        {
            var context = await _agentContextProvider.GetContextAsync();

            var invite = MessageUtils.DecodeMessageFromUrlFormat<ConnectionInvitationMessage>(model.InvitationDetails);
            var (request, record) = await _connectionService.CreateRequestAsync(context, invite);
            await _messageService.SendAsync(context.Wallet, request, record);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ViewInvitation(AcceptConnectionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Redirect(Request.Headers["Referer"].ToString());
            }

            ViewData["InvitationDetails"] = model.InvitationDetails;

            var invite = MessageUtils.DecodeMessageFromUrlFormat<ConnectionInvitationMessage>(model.InvitationDetails);

            return View(invite);
        }

        [HttpPost]
        public async Task<IActionResult> SendTrustPing(string connectionId)
        {
            var context = await _agentContextProvider.GetContextAsync();
            var connection = await _connectionService.GetAsync(context, connectionId);
            var message = new TrustPingMessage
            {
                ResponseRequested = true,
                Comment = "Hello"
            };

            var slim = new SemaphoreSlim(0, 1);
            var success = false;

            using (var subscription = _eventAggregator.GetEventByType<ServiceMessageProcessingEvent>()
                .Where(_ => _.MessageType == CustomMessageTypes.TrustPingResponseMessageType)
                .Subscribe(_ => { success = true; slim.Release(); }))
            {
                await _messageService.SendAsync(context.Wallet, message, connection);

                await slim.WaitAsync(TimeSpan.FromSeconds(5));

                return RedirectToAction("Details", new { id = connectionId, trustPingSuccess = success });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id, bool? trustPingSuccess = null)
        {
            var context = new DefaultAgentContext
            {
                Wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials)
            };

            var model = new ConnectionDetailsViewModel
            {
                Connection = await _connectionService.GetAsync(context, id),
                Messages = await _recordService.SearchAsync<BasicMessageRecord>(context.Wallet,
                    SearchQuery.Equal(nameof(BasicMessageRecord.ConnectionId), id), null, 10),
                FetchRecords = await _recordService.SearchAsync<GenericFetchRecord>(context.Wallet,
                    SearchQuery.Equal(nameof(GenericFetchRecord.ConnectionId), id), null, 10),
                TrustPingSuccess = trustPingSuccess
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string connectionId, string text)
        {
            if (string.IsNullOrEmpty(text)) return RedirectToAction("Details", new { id = connectionId });

            var context = new DefaultAgentContext
            {
                Wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials)
            };

            var sentTime = DateTime.UtcNow;
            var messageRecord = new BasicMessageRecord
            {
                Id = Guid.NewGuid().ToString(),
                Direction = MessageDirection.Outgoing,
                Text = text,
                SentTime = sentTime,
                ConnectionId = connectionId
            };
            var message = new BasicMessage
            {
                Content = text,
                SentTime = sentTime.ToString("s", CultureInfo.InvariantCulture)
            };
            var connection = await _connectionService.GetAsync(context, connectionId);

            // Save the outgoing message to the local wallet for chat history purposes
            await _recordService.AddAsync(context.Wallet, messageRecord);

            // Send an agent message using the secure connection
            await _messageService.SendAsync(context.Wallet, message, connection);

            return RedirectToAction("Details", new {id = connectionId});
        }

        [HttpPost]
        public async Task<IActionResult> SendOffer(string connectionId, string text)
        {
            var context = new AgentContext
            {
                Wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials)
            };            
            var connection = await _connectionService.GetAsync(context, connectionId);
            var record = await _provisioningService.GetProvisioningAsync(context.Wallet);


            (var offer, var issuerCredentialRecord) = await _credentialService.CreateOfferAsync(
                agentContext: context,
                config: new OfferConfiguration
                {
                    IssuerDid = record.Endpoint.Did,
                    CredentialDefinitionId = text,
                    CredentialAttributeValues = new List<CredentialPreviewAttribute>
                        {
                            new CredentialPreviewAttribute("Name", "Ayushman"),
                            new CredentialPreviewAttribute("Rented", "Yes")
                        },
                },
                connectionId: connectionId);
            await _messageService.SendAsync(context.Wallet, offer, connection);

            return RedirectToAction("Details", new {id = connectionId});
        }

        [HttpPost]
        public async Task<IActionResult> SendCredential(string connectionId)
        {
            var context = new AgentContext
            {
                Wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials)
            };            
            var connection = await _connectionService.GetAsync(context, connectionId);
            var record = await _provisioningService.GetProvisioningAsync(context.Wallet);

            var requests = await _credentialService.ListRequestsAsync(context);
            foreach( var item in requests) {
                (CredentialIssueMessage cred, _) = await _credentialService.CreateCredentialAsync(
                agentContext: context,
                credentialId: item.Id);
                await _messageService.SendAsync(context.Wallet, cred, connection);
            }

            return RedirectToAction("Details", new {id = connectionId});
        }        

        [HttpPost]
        public async Task<IActionResult> SendConsentRequest(string connectionId)
        {
            var context = new AgentContext
            {
                Wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials)
            };
            var connection = await _connectionService.GetAsync(context, connectionId);
            var record = await _provisioningService.GetProvisioningAsync(context.Wallet);

            (var genericFetchRequest, var genericFetchRecord) = await _genericFetchService.CreateRequestAsync(
                agentContext: context,
                fetchRequest: new GenericFetchRequest
                {
                    Type = "FB_CONSENT_GET",
                    Payload = ""
                },
                connectionId: connectionId);

            await _messageService.SendAsync(context.Wallet, genericFetchRequest, connection);
            return RedirectToAction("Details", new { id = connectionId });
        }

        [HttpPost]
        public async Task<IActionResult> SendGenericRequest(string connectionId)
        {
            var context = new AgentContext
            {
                Wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials)
            };
            var connection = await _connectionService.GetAsync(context, connectionId);
            var record = await _provisioningService.GetProvisioningAsync(context.Wallet);

            (var genericFetchRequest, var genericFetchRecord) = await _genericFetchService.CreateRequestAsync(
                agentContext: context,
                fetchRequest: new GenericFetchRequest
                {
                    Type = "GG",
                    Payload = "GG"
                },
                connectionId: connectionId);

            await _messageService.SendAsync(context.Wallet, genericFetchRequest, connection);

            return RedirectToAction("Details", new { id = connectionId });
        }


        [HttpPost]
        public async Task<IActionResult> SendGenericResponse(string connectionId)
        {
            var context = new AgentContext
            {
                Wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration,
                    _walletOptions.WalletCredentials)
            };
            var connection = await _connectionService.GetAsync(context, connectionId);
            var record = await _provisioningService.GetProvisioningAsync(context.Wallet);

            var requests = await _genericFetchService.ListAsync(context);
            foreach( var item in requests) {
                (GenericFetchResponseMessage resp, _) = await _genericFetchService.CreateResponseAsync(
                agentContext: context,
                genericFetchRecordId: item.Id);
                await _messageService.SendAsync(context.Wallet, resp, connection);
            }

            return RedirectToAction("Details", new {id = connectionId});
        }




        [HttpPost]
        public IActionResult LaunchApp(LaunchAppViewModel model)
        {
            return Redirect($"{model.UriSchema}{Uri.EscapeDataString(model.InvitationDetails)}");
        }

        /// <summary>
        /// Encodes the invitation to a base64 string which can be presented to the user as QR code or a deep link Url
        /// </summary>
        /// <returns>The invitation.</returns>
        /// <param name="invitation">Invitation.</param>
        public string EncodeInvitation(ConnectionInvitationMessage invitation)
        {
            return invitation.ToJson().ToBase64();
        }

        /// <summary>
        /// Decodes the invitation from base64 to strongly typed object
        /// </summary>
        /// <returns>The invitation.</returns>
        /// <param name="invitation">Invitation.</param>
        public ConnectionInvitationMessage DecodeInvitation(string invitation)
        {
            return JsonConvert.DeserializeObject<ConnectionInvitationMessage>(Encoding.UTF8.GetString(Convert.FromBase64String(invitation)));
        }
    }
}
