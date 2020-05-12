using System;
using Hyperledger.Aries.Agents;
using WebAgent.Messages;
using Newtonsoft.Json;

namespace WebAgent.Protocols.GenericFetch
{
    public class GenericFetchResponseMessage : AgentMessage
    {
        public GenericFetchResponseMessage()
        {
            Id = Guid.NewGuid().ToString();
            Type = CustomMessageTypes.GenericFetchResponseMessageType;
        }

        ///// <summary>
        ///// Gets or sets the type of fetch request.
        ///// </summary>
        ///// <value>
        ///// The fetch type. Oneof(USER_DETAIL, CONSENT, FABRIC_DATA).
        ///// </value>
        //[JsonProperty("fetch_type")]
        //public string FetchType { get; set; }

        /// <summary>
        /// Gets or sets the response of the fetch request.
        /// </summary>
        /// <value>
        /// The response of the fetch request.
        /// </value>
        [JsonProperty("fetch_response")]
        public string FetchResponse { get; set; }
    }
}
        