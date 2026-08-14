using ChatWs.WebSockets;
using Xunit;

namespace ChatWs.Tests;

public class ConnectionManagerTests
{
    [Fact]
    public void Add_ReturnsUsableId()
    {
        var manager = new ConnectionManager();
        var socket = new FakeWebSocket();

        var id = manager.Add(socket);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Remove_DeregistersConnection_BroadcastNoLongerTargetsIt()
    {
        var manager = new ConnectionManager();
        var socket = new FakeWebSocket();
        var id = manager.Add(socket);

        manager.Remove(id);
        await manager.BroadcastAsync(Guid.NewGuid(), "hello");

        Assert.Empty(socket.SentMessages);
    }

    [Fact]
    public async Task BroadcastAsync_SendsToAllExceptSender()
    {
        var manager = new ConnectionManager();
        var sender = new FakeWebSocket();
        var other1 = new FakeWebSocket();
        var other2 = new FakeWebSocket();
        var senderId = manager.Add(sender);
        manager.Add(other1);
        manager.Add(other2);

        await manager.BroadcastAsync(senderId, "hello");

        Assert.Empty(sender.SentMessages);
        Assert.Equal(new[] { "hello" }, other1.SentMessages);
        Assert.Equal(new[] { "hello" }, other2.SentMessages);
    }
}
