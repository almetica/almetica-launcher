using Newtonsoft.Json;

namespace AlmeticaLauncher
{
    class ResponseAuth
    { 
        [JsonProperty("ticket")]
        public string Ticket { get; set; }
    }
}
