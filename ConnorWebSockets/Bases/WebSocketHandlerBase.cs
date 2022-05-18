using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConnorWebSockets.Interfaces;
using Microsoft.Extensions.Logging;

namespace ConnorWebSockets.Bases
{
    public abstract class WebSocketHandlerBase<T> where T : IWebSocket
    {
        public IConnectionManager<T> WebSocketConnectionManager { get; set; }
        protected ILogger Logger { get; set; }

        public WebSocketHandlerBase(IConnectionManager<T> webSocketConnectionManager, ILogger logger)
        {
            WebSocketConnectionManager = webSocketConnectionManager;
            Logger = logger;
        }

        public virtual async Task<string> OnConnected(T socket)
        {
            return WebSocketConnectionManager.AddSocket(socket);
        }

        public virtual async Task OnDisconnected(T socket)
        {
            await WebSocketConnectionManager.RemoveSocket(WebSocketConnectionManager.GetId(socket));
        }

        public async Task SendMessageAsync(T socket, string message)
        {
            if (socket.Socket.State != WebSocketState.Open)
            {
                return;
            }

            await socket.Socket.SendAsync(buffer: new ArraySegment<byte>(array: Encoding.ASCII.GetBytes(message),
                                                                  offset: 0,
                                                                  count: message.Length),
                                   messageType: WebSocketMessageType.Text,
                                   endOfMessage: true,
                                   cancellationToken: CancellationToken.None);
        }

        public async Task SendMessageAsync(string socketId, string message)
        {
            await SendMessageAsync(WebSocketConnectionManager.GetSocketById(socketId), message);
        }

        public async Task SendMessageToAllAsync(string message)
        {
            foreach (var pair in WebSocketConnectionManager.GetAll())
            {
                if (pair.Value.Socket.State == WebSocketState.Open)
                {
                    await SendMessageAsync(pair.Value, message);
                }
            }
        }

        public abstract Task ReceiveAsync(T socket, WebSocketReceiveResult result, byte[] buffer);
        public abstract Task ReceiveBinaryAsync(T socket, WebSocketReceiveResult result, byte[] buffer);
    }
}
