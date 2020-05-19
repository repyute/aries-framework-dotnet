using Newtonsoft.Json;
namespace WebAgent.Services.Models
{
    public class FabricPayload
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("user_id")]
        public string UserId { get; set; }
    }
}
