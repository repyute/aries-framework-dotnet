using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using WebAgent.Models;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.DidExchange;

namespace WebAgent.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly IWalletService _walletService;
        private readonly IProvisioningService _provisioningService;
        private readonly IConnectionService _connectionService;
        private readonly AgentOptions _walletOptions;

        private readonly ISchemaService _schemaService;
        private readonly IAgentProvider _agentContextProvider;

        public HomeController(
            IWalletService walletService,
            IProvisioningService provisioningService,
            IConnectionService connectionService,
            ISchemaService schemaService,
            IAgentProvider agentContextProvider,
            ILogger<HomeController> logger,
            IOptions<AgentOptions> walletOptions)
        {
            _walletService = walletService;
            _provisioningService = provisioningService;
            _walletOptions = walletOptions.Value;
            _schemaService = schemaService;
            _agentContextProvider = agentContextProvider;
            _connectionService = connectionService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var wallet = await _walletService.GetWalletAsync(
                _walletOptions.WalletConfiguration,
                _walletOptions.WalletCredentials);

            var provisioning = await _provisioningService.GetProvisioningAsync(wallet);

            var connectionId = provisioning.GetTag(SimpleWebAgentProvisioningService.MobileAppInvitationTagName);
            var agentContext = await _agentContextProvider.GetContextAsync();
            if (connectionId == null)
            {
                throw new Exception("This agent hasn't been provisioned with multiparty invite");
            }
            var inviation = await _connectionService.GetAsync(agentContext, connectionId);
            string invitationString = inviation.GetTag(SimpleWebAgentProvisioningService.InvitationTagName);
            var inviationBytes = System.Text.Encoding.UTF8.GetBytes(invitationString);
            ViewData["Invitation"] = $"{provisioning.Endpoint.Uri}?c_i={System.Convert.ToBase64String(inviationBytes)}";

            return View(provisioning);
        }
        
        public async Task<IActionResult> CreateSchema()
        {
            var context = await _agentContextProvider.GetContextAsync();

            var record = await _provisioningService.GetProvisioningAsync(context.Wallet);

            var schemaName = $"Rent-Credential";
            var schemaVersion = "1.0";
            var schemaAttrNames = new[] {"Name", "Rented"};
            var schemaId = await _schemaService.CreateSchemaAsync(context, record.Endpoint.Did,
                schemaName, schemaVersion, schemaAttrNames);

            await Task.Delay(TimeSpan.FromSeconds(2));
            
            string credId = await _schemaService.CreateCredentialDefinitionAsync(context, schemaId,
                record.Endpoint.Did, "Tag", false, 100, new Uri("http://mock/tails"));

            _logger.LogInformation("CREDDEFID ++++++++++++ {0}", credId);
            return RedirectToAction("Index");
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
