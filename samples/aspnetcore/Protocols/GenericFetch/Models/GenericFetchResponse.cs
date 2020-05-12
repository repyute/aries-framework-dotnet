using Newtonsoft.Json;
namespace WebAgent.Protocols.GenericFetch
{
    public class GenericFetchResponse
    {
        /// <summary>
        /// Gets or sets the payload.
        /// </summary>
        /// <value>The version.</value>
        [JsonProperty("response")]
        public string Response { get; set; }
    }
}
