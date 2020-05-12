using System;
using Hyperledger.Aries.Storage;
using Newtonsoft.Json;

namespace WebAgent.Protocols.GenericFetch
{
    /// <summary>
    /// Represents a fetch response record in the user's wallet
    /// </summary>
    /// <seealso cref="AgentFramework.Core.Models.Records.RecordBase" />
    public class GenericFetchRecord : RecordBase
    {
        public override string TypeName => "WebAgent.GenericFetchRecord";

        [JsonIgnore]
        public string ConnectionId
        {
            get => Get();
            set => Set(value);
        }

        public string Type { get; set; }

        public string Payload { get; set; }

        public string Response { get; set; }
    }
}