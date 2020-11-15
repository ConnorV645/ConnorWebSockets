using System;
using System.Net.WebSockets;

namespace ConnorWebSockets.Interfaces
{
    public interface IWebSocket
    {
        WebSocket Socket { get; set; }
        Guid SocketId { get; set; }
        bool IsAuthorized { get; set; }
    }
}
