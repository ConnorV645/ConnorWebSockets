using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using ConnorWebSockets.Interfaces;

namespace ConnorWebSockets.Bases
{
    public class ConnectionManagerBase<T> : IConnectionManager<T> where T : IWebSocket
    {
        private readonly ConcurrentDictionary<string, T> sockets = new ConcurrentDictionary<string, T>();

        public T GetSocketById(string id)
        {
            sockets.TryGetValue(id, out var socket);
            return socket;
        }

        public ConcurrentDictionary<string, T> GetAll()
        {
            return sockets;
        }

        public string GetId(T socket)
        {
            return sockets.FirstOrDefault(x => x.Value.SocketId == socket.SocketId).Key;
        }

        public string AddSocket(T socket)
        {
            var key = CreateConnectionId();
            sockets.TryAdd(key, socket);
            return key;
        }

        public async Task RemoveSocket(string id)
        {
            sockets.TryRemove(id, out var socket);
            if (socket != null)
            {
                try
                {
                    await socket.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by the ConnectionManager", CancellationToken.None);
                }
                catch (Exception)
                {
                    // Handle Any Bad Handshake Closures
                }
            }
        }

        private static string CreateConnectionId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
