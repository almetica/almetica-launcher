using System.Collections.Generic;
using Newtonsoft.Json;

namespace AlmeticaLauncher
{
    internal class ResponseServerList
    {
        [JsonProperty("servers")] public List<ResponseServerListEntry> Servers { get; set; }
    }

    internal class ResponseServerListEntry
    {
        [JsonProperty("id")] public int Id { get; set; }

        [JsonProperty("category")] public string Category { get; set; }

        [JsonProperty("raw_name")] public string Rawname { get; set; }

        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("crowdness")] public string Crowdness { get; set; }

        [JsonProperty("open")] public string Open { get; set; }

        [JsonProperty("ip")] public string Ip { get; set; }

        [JsonProperty("port")] public ushort Port { get; set; }

        [JsonProperty("lang")] public ushort Lang { get; set; }

        [JsonProperty("popup")] public string Popup { get; set; }
    }
}