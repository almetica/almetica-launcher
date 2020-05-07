using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;

namespace AlmeticaLauncher
{
    class ResponseServerList
    {
        [JsonProperty("servers")]
        public List<ResponseServerListEntry> Servers { get; set; }
    }

    class ResponseServerListEntry {
        [JsonProperty("id")]
        public Int32 Id { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("raw_name")]
        public string Rawname { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("crowdness")]
        public string Crowdness { get; set; }

        [JsonProperty("open")]
        public string Open { get; set; }

        [JsonProperty("ip")]
        public string Ip { get; set; }

        [JsonProperty("port")]
        public UInt16 Port { get; set; }

        [JsonProperty("lang")]
        public UInt16 Lang { get; set; }

        [JsonProperty("popup")]
        public string Popup { get; set; }
    }

}
