using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ConnorWebSockets.Interfaces
{
    public interface IConnectionManager<T>
    {
        T GetSocketById(string id);
        ConcurrentDictionary<string, T> GetAll();
        string GetId(T socket);
        string AddSocket(T socket);
        Task RemoveSocket(string id);
    }
}
