using System.Net.WebSockets;
using System.Text;

namespace ChatWs.WebSockets;

public class ChatWebSocketHandler
{
    private const int MaxMessageBytes = 64 * 1024;

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
                using var messageStream = new MemoryStream();
                WebSocketReceiveResult result;
                var tooBig = false;

                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        return;
                    }

                    if (messageStream.Length + result.Count > MaxMessageBytes)
                    {
                        tooBig = true;
                        break;
                    }

                    messageStream.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                if (tooBig)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, $"Message exceeds {MaxMessageBytes} bytes", CancellationToken.None);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    _logger.LogWarning("Client {ConnectionId} sent a binary frame; ignoring (text only)", id);
                    continue;
                }

                var message = Encoding.UTF8.GetString(messageStream.ToArray());
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
