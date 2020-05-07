using Newtonsoft.Json;
using System;

namespace AlmeticaLauncher
{
    public class Configuration
    {
        [JsonProperty("ServerBaseAddress")]
        public Uri ServerBaseAddress;

        [JsonProperty("DefaultAccount")]
        public string DefaultAccount;

        [JsonProperty("DefaultPassword")]
        public string DefaultPassword;

        [JsonProperty("Language")]
        public string Language;
    }
}
