using System;
using Newtonsoft.Json;

namespace AlmeticaLauncher
{
    public class Configuration
    {
        [JsonProperty("DefaultAccount")] public string DefaultAccount;

        [JsonProperty("DefaultPassword")] public string DefaultPassword;

        [JsonProperty("Language")] public string Language;

        [JsonProperty("ServerBaseAddress")] public Uri ServerBaseAddress;
    }
}