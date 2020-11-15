using System;
using System.Net.WebSockets;
using ConnorWebSockets.Interfaces;

namespace ConnorWebSockets.Bases
{
    public abstract class WebSocketBase : IWebSocket
    {
        public WebSocket Socket { get; set; }
        public Guid SocketId { get; set; }
        public bool IsAuthorized { get; set; }

        public WebSocketBase(WebSocket socket)
        {

            IsAuthorized = false;
            SocketId = Guid.NewGuid();
            Socket = socket;
        }
    }
}
