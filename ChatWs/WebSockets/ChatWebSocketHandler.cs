using System.Net.WebSockets;
using System.Text;

namespace ChatWs.WebSockets;

public class ChatWebSocketHandler
{
    private readonly ConnectionManager _connectionManager;
    private readonly ILogger<ChatWebSocketHandler> _logger;

    public ChatWebSocketHandler(ConnectionManager connectionManager, ILogger<ChatWebSocketHandler> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public async Task HandleAsync(WebSocket socket)
    {
        var id = _connectionManager.Add(socket);
        _logger.LogInformation("Client {ConnectionId} connected", id);
        var buffer = new byte[4096];

        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await _connectionManager.BroadcastAsync(id, message);
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogInformation(ex, "Client {ConnectionId} disconnected uncleanly", id);
        }
        finally
        {
            _connectionManager.Remove(id);
            _logger.LogInformation("Client {ConnectionId} removed", id);
        }
    }
}
