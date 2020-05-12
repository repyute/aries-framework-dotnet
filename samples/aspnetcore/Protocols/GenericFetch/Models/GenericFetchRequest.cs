using Newtonsoft.Json;
namespace WebAgent.Protocols.GenericFetch
{
    public class GenericFetchRequest
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The name.</value>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the payload.
        /// </summary>
        /// <value>The version.</value>
        [JsonProperty("payload")]
        public string Payload { get; set; }
    }
}
