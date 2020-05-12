using System;
using Hyperledger.Aries.Agents;
using WebAgent.Messages;
using Newtonsoft.Json;

namespace WebAgent.Protocols.GenericFetch
{
    public class GenericFetchRequestMessage : AgentMessage
    {
        public GenericFetchRequestMessage()
        {
            Id = Guid.NewGuid().ToString();
            Type = CustomMessageTypes.GenericFetchRequestMessageType;
        }

        /// <summary>
        /// Gets or sets the type of fetch request.
        /// </summary>
        /// <value>
        /// The fetch type. Oneof(USER_DETAIL, CONSENT, FABRIC_DATA).
        /// </value>
        [JsonProperty("fetch_type")]
        public string FetchType { get; set; }

        /// <summary>
        /// Gets or sets the payload of the fetch request.
        /// </summary>
        /// <value>
        /// The payload of the fetch request.
        /// </value>
        [JsonProperty("fetch_payload")]
        public string FetchPayload { get; set; }
    }
}
        