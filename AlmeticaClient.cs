using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Newtonsoft.Json;
using static AlmeticaLauncher.ServerList.Types;

namespace AlmeticaLauncher
{
    internal class AlmeticaClient
    {
        private readonly Uri _serverBasePath;

        public AlmeticaClient(Uri serverBasePath)
        {
            _serverBasePath = serverBasePath;
        }

        Uri ServerListUri => new Uri(_serverBasePath, "server/list");
        Uri AuthUri => new Uri(_serverBasePath, "auth");

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
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"accountname", accountName},
                {"password", password}
            });
            using var client = new HttpClient();
            var response = await client.PostAsync(AuthUri, content);
            var authenticationResponse = await response.Content.ReadAsStringAsync();
            var auth = JsonConvert.DeserializeObject<ResponseAuth>(authenticationResponse);
            return Convert.FromBase64String(auth.Ticket);
        }

        private async Task<ServerList> GetServerListAsync()
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(ServerListUri);
            var serverListResponse = await response.Content.ReadAsStringAsync();
            var serverList = JsonConvert.DeserializeObject<ResponseServerList>(serverListResponse);

            var gameCompatibleServerList = serverList.Servers.Select(x => new Server
            {
                Id = x.Id,
                Category = ByteString.CopyFrom(Encoding.Unicode.GetBytes(x.Category)),
                Rawname = ByteString.CopyFrom(Encoding.Unicode.GetBytes(x.Rawname)),
                Name = ByteString.CopyFrom(Encoding.Unicode.GetBytes(x.Name)),
                Crowdness = ByteString.CopyFrom(Encoding.Unicode.GetBytes(x.Crowdness)),
                Open = ByteString.CopyFrom(Encoding.Unicode.GetBytes(x.Open)),
                Ip = IpV4ToInt(x.Ip),
                Port = x.Port,
                Lang = x.Lang,
                Popup = ByteString.CopyFrom(Encoding.Unicode.GetBytes(x.Popup))
            }).ToList();

            return new ServerList
            {
                Servers = {gameCompatibleServerList},
                LastPlayedId = 1,
                Unknwn = 0
            };
        }

        private int IpV4ToInt(string s)
        {
            var address = IPAddress.Parse(s);
            var ipv4 = address.MapToIPv4();
            var addressBytes = ipv4.GetAddressBytes();

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(addressBytes);
            }

            return BitConverter.ToInt32(addressBytes, 0);
        }
    }
}