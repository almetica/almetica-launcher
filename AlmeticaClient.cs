using Google.Protobuf;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static AlmeticaLauncher.ServerList.Types;

namespace AlmeticaLauncher
{
    class AlmeticaClient
    {
        public AlmeticaClient(Uri serverBasePath)
        {
            ServerBasePath = serverBasePath;
        }

        private Uri ServerBasePath;
        public Uri ServerListUri => new Uri(this.ServerBasePath, "server/list");
        private Uri AuthUri => new Uri(this.ServerBasePath, "auth");

        public byte[] GetTicket(string accountName, string password)
        {
            return GetTicketAsync(accountName, password).GetAwaiter().GetResult();
        }

        public ServerList GetServerList()
        {
            return GetServerListAsync().GetAwaiter().GetResult();
        }

        private async Task<byte[]> GetTicketAsync(string accountName, string password)
        {
            using (var content = new FormUrlEncodedContent(new Dictionary<string, string> {
                {"accountname", accountName},
                {"password", password},
            }))
            {
                using HttpClient Client = new HttpClient();
                var response = await Client.PostAsync(AuthUri, content);
                var authenticationResponse = await response.Content.ReadAsStringAsync();
                var auth = JsonConvert.DeserializeObject<ResponseAuth>(authenticationResponse);
                return System.Convert.FromBase64String(auth.Ticket);
            }
        }

        private async Task<ServerList> GetServerListAsync()
        {
            using HttpClient Client = new HttpClient();
            var response = await Client.GetAsync(ServerListUri);
            var serverListResponse = await response.Content.ReadAsStringAsync();
            var server_list = JsonConvert.DeserializeObject<ResponseServerList>(serverListResponse);

            var game_compatible_server_list = server_list.Servers.Select(x => new Server() {
                Id = x.Id,
                Category = ByteString.CopyFrom(Encoding.Unicode.GetBytes(x.Category)),
                Rawname = ByteString.CopyFrom(Encoding.Unicode.GetBytes(x.Rawname)),
                Name = ByteString.CopyFrom(Encoding.Unicode.GetBytes(x.Name)),
                Crowdness = ByteString.CopyFrom(Encoding.Unicode.GetBytes(x.Crowdness)),
                Open = ByteString.CopyFrom(Encoding.Unicode.GetBytes(x.Open)),
                Ip = this.IpV4ToInt(x.Ip),
                Port = x.Port,
                Lang = x.Lang,
                Popup = ByteString.CopyFrom(Encoding.Unicode.GetBytes(x.Popup)),

            }).ToList();

            return new ServerList()
            {
                Servers = { game_compatible_server_list },
                LastPlayedId = 1,
                Unknwn = 0,
            };
        }

        private int IpV4ToInt(string s)
        {
            var address = IPAddress.Parse(s);
            var ipv4 = address.MapToIPv4();
            var address_bytes = ipv4.GetAddressBytes();
            
            if (BitConverter.IsLittleEndian)
                Array.Reverse(address_bytes);

            return BitConverter.ToInt32(address_bytes, 0);
        }

    }
}
