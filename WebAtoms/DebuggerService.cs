using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(WebAtoms.DebuggerService))]

namespace WebAtoms
{
    public class DebuggerMessage
    {
        [JsonProperty("type")]
        public string Command { get; set; }

        [JsonProperty("id")]
        public long ID { get; set; }

        [JsonProperty("payload")]
        public JObject Payload { get; set; }

        public static DebuggerMessage Parse(string json)
        {
            return JsonConvert.DeserializeObject<DebuggerMessage>(json);
        }

    }

    public class DebuggerService
    {


        private ClientWebSocket clientWebSocket;

        private System.Threading.CancellationTokenSource NewCancellationToken()
        {
            return new System.Threading.CancellationTokenSource();
        }

        public void Register(string url)
        {
            if (clientWebSocket != null)
            {
                Disconnect();
            }

            clientWebSocket = new ClientWebSocket();

            var nav = DependencyService.Get<NavigationService>();

            var uri = new UriBuilder(nav.GetLocation());
            uri.Scheme = "ws";
            uri.Path = url;
            AtomDevice.BeginInvokeOnMainThread(async () => {
            using (var t = NewCancellationToken()) {

                    await clientWebSocket.ConnectAsync(uri.Uri, t.Token);

                    await Task.Factory.StartNew(async () => await ReadMessagesAwait(clientWebSocket, t.Token));
                }
            });

        }

        public void OnMessage(DebuggerMessage message)
        {

            if(message.Command.EqualsIgnoreCase("refresh"))
            {
                var nav = DependencyService.Get<NavigationService>();
                nav.Refresh();
                return;
            }
        }

        public void SendMessage(DebuggerMessage message)
        {
            AtomDevice.BeginInvokeOnMainThread(async () => {
                var json = JsonConvert.SerializeObject(message);
                var byteMessage = Encoding.UTF8.GetBytes(json);
                var segment = new ArraySegment<byte>(byteMessage);
                using (var token = NewCancellationToken())
                {
                    await clientWebSocket.SendAsync(segment, WebSocketMessageType.Text, true, token.Token);
                }
            });
        }

        private async Task ReadMessagesAwait(ClientWebSocket client, System.Threading.CancellationToken token)
        {
            try
            {
                do {
                    var buffer = new byte[4096];
                    var message = new ArraySegment<byte>(buffer);
                    var result = await client.ReceiveAsync(message, token);
                    if (result.MessageType != WebSocketMessageType.Text)
                    {
                        break;
                    }
                    var messageBytes = message.Skip(message.Offset).Take(result.Count).ToArray();
                    string receivedMessage = Encoding.UTF8.GetString(messageBytes);
                    OnMessage(DebuggerMessage.Parse(receivedMessage));
                } while (true);
            }catch(Exception ex)
            {
                AtomDevice.Log(ex);
                Disconnect();
            }
        }

        public void Disconnect()
        {
            var c = this.clientWebSocket;
            if (c != null)
            {
                AtomDevice.BeginInvokeOnMainThread(async () =>
                {
                    using (var ct = new System.Threading.CancellationTokenSource())
                    {
                        await c.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client Closed", ct.Token);
                    }
                }, 
                () =>
                    {
                        c.Dispose();
                        return Task.CompletedTask;
                    });
            }
        }

    }
}
