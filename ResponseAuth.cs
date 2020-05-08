using Newtonsoft.Json;

namespace AlmeticaLauncher
{
    internal class ResponseAuth
    {
        [JsonProperty("ticket")] public string Ticket { get; set; }
    }
}