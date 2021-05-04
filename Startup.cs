using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace websocket_server
{
    public class WebSocketMessage
    {
        public string id;
        public string type;
        public string data;
    }

    public class Startup
    {
        public static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .Build()
                .Run();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new Dictionary<string, List<WebSocket>>());
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseWebSockets();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/webrtc-signaling")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        var sockets = context.RequestServices.GetRequiredService<Dictionary<string, List<WebSocket>>>();

                        var buffer = new byte[1024 * 4];
                        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                        var id = "";

                        while (!result.CloseStatus.HasValue)
                        {
                            var text = System.Text.Encoding.UTF8.GetString(new ArraySegment<byte>(buffer, 0, result.Count));
                            var message = Newtonsoft.Json.JsonConvert.DeserializeObject<WebSocketMessage>(text);

                            id = message.id;

                            await HandleMessage(sockets, webSocket, message);

                            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        }

                        if (sockets.ContainsKey(id))
                        {
                            sockets[id].Remove(webSocket);

                            if (sockets[id].Count == 0)
                                sockets.Remove(id);
                        }

                        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                    }
                    else
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                }
                else
                {
                    await next();
                }
            });
        }

        private async Task HandleMessage(Dictionary<string, List<WebSocket>> socketsIds, WebSocket webSocket, WebSocketMessage message)
        {
            if (message.type == "connect")
            {
                await Connect(socketsIds, webSocket, message);
            }
            if (message.type == "offer")
            {
                await HandleOffer(socketsIds, message);
            }
            if (message.type == "answer")
            {
                await HandleAnswer(socketsIds, message);
            }
        }

        private List<WebSocket> GetSockets(Dictionary<string, List<WebSocket>> socketsIds, string id)
        {
            if (!socketsIds.TryGetValue(id, out var sockets))
            {
                sockets = new List<WebSocket>();
                socketsIds.Add(id, sockets);
            }

            return sockets;
        }

        private async Task Connect(Dictionary<string, List<WebSocket>> socketsIds, WebSocket webSocket, WebSocketMessage message)
        {
            var sockets = GetSockets(socketsIds, message.id);

            sockets.Add(webSocket);

            if (sockets.Count < 2)
                return;

            var socket = sockets.First();

            var payload = new
            {
                type = "make_offer",
                data = "",
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);

            await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task HandleOffer(Dictionary<string, List<WebSocket>> socketsIds, WebSocketMessage message)
        {
            var sockets = GetSockets(socketsIds, message.id);

            if (sockets.Count < 2)
                return;

            var host = sockets.Last();

            var payload = new
            {
                type = "make_answer",
                data = message.data,
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);

            await host.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task HandleAnswer(Dictionary<string, List<WebSocket>> socketsIds, WebSocketMessage message)
        {
            var sockets = GetSockets(socketsIds, message.id);

            if (sockets.Count < 2)
                return;

            var socket = sockets.First();

            var payload = new
            {
                type = "accept_answer",
                data = message.data,
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);

            await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
