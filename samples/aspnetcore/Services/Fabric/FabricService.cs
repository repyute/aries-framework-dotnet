using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WebAgent.Services.Models;

namespace WebAgent.Services.Fabric
{
    public class FabricService
    {
        private readonly IProvisioningService _provisioningService;
        private readonly IAgentProvider _agentProvider;
        private readonly ILogger<FabricService> Logger;
        private const string _tokenTag = "JSESSIONID";
        private Cookie _cookie;

        protected readonly int FabricServiceLoginLoggingEventId = 10000;

        public FabricService
        (
            IProvisioningService provisioningService,
            IAgentProvider agentProvider,
            ILogger<FabricService> logger
        )
        {
            _provisioningService = provisioningService;
            _agentProvider = agentProvider;
            Logger = logger;
            Task.Run(async () => await Login());
        }

        public async Task Login()
        {
            var agentContext = await _agentProvider.GetContextAsync();
            var provsioningRecord = await _provisioningService.GetProvisioningAsync(agentContext.Wallet);
            if (provsioningRecord.Owner.Name == "Nestaway")
            {
                var baseAddress = "http://nestaway0.qa.repyute.com/apis/login";
                var cookieContainer = new CookieContainer();
                var uri = new Uri(baseAddress);
                using (var httpClientHandler = new HttpClientHandler
                {
                    CookieContainer = cookieContainer
                })
                {
                    using (var httpClient = new HttpClient(httpClientHandler))
                    {
                        var formContent = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("username", "user1"),
                            new KeyValuePair<string, string>("password", "user1Pass"),
                            new KeyValuePair<string, string>("submit", "submit")
                        });
                        await httpClient.PostAsync(uri, formContent);
                        _cookie = cookieContainer.GetCookies(uri).Cast<Cookie>().FirstOrDefault(x => x.Name == _tokenTag);
                    }
                }
            }
        }

        public async Task<string> GetData(string payloadStr)
        {
            var agentContext = await _agentProvider.GetContextAsync();
            var provsioningRecord = await _provisioningService.GetProvisioningAsync(agentContext.Wallet);
            if (provsioningRecord.Owner.Name == "Nestaway")
            {
                var payload = JsonConvert.DeserializeObject<FabricPayload>(payloadStr);
                var baseAddress = $"http://nestaway0.qa.repyute.com/apis/transaction/{payload.Type}?tenant_id={payload.UserId}";
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Cookie", string.Format("{0}={1}", _tokenTag, _cookie?.Value));
                    var result = await client.GetAsync(baseAddress);
                    var resultString = await result.Content.ReadAsStringAsync();
                    return resultString;
                }
            }
            return "Agent not configured for the operation";
        }
    }
}
