using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using ConnorWebSockets.Bases;
using Microsoft.AspNetCore.Http;

namespace ConnorWebSockets.Middleware
{
    public class SocketMiddleware<Y, T> where Y : WebSocketHandlerBase<T> where T : WebSocketBase
    {
        private readonly RequestDelegate next;
        private readonly Y webSocketHandler;

        public SocketMiddleware(RequestDelegate next, Y webSocketHandler)
        {
            this.next = next;
            this.webSocketHandler = webSocketHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                var socket = await context.WebSockets.AcceptWebSocketAsync();
                var socketType = (T)Activator.CreateInstance(typeof(T), socket);
                await webSocketHandler.OnConnected(socketType);

                await Receive(socketType, async (result, buffer) =>
                {
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        await webSocketHandler.ReceiveAsync(socketType, result, buffer);
                        return;
                    }

                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocketHandler.OnDisconnected(socketType);
                        return;
                    }

                });

                // no next for web sockets
            }
            catch (WebSocketException)
            {
                // Most Likely a Bad close
            }
        }

        private async Task Receive(T socketType, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 4];

            while (socketType.Socket.State == WebSocketState.Open)
            {
                var result = await socketType.Socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer),
                                                       cancellationToken: CancellationToken.None);

                handleMessage(result, buffer);
            }
        }
    }
}
