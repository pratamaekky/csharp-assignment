using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace ChatWs.WebSockets;

public class ConnectionManager
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _connections = new();

    public Guid Add(WebSocket socket)
    {
        var id = Guid.NewGuid();
        _connections[id] = socket;
        return id;
    }

    public void Remove(Guid id) => _connections.TryRemove(id, out _);

    public async Task BroadcastAsync(Guid senderId, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);

        foreach (var (id, socket) in _connections)
        {
            if (id == senderId) continue;
            if (socket.State != WebSocketState.Open) continue;

            try
            {
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception)
            {
                // Best-effort: one dead client must not stop the broadcast to the others.
            }
        }
    }
}
