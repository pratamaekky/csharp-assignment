using TodoApi.Repositories;
using Xunit;

namespace TodoApi.Tests;

public class InMemoryTodoRepositoryTests
{
    [Fact]
    public void Add_ThenGetById_ReturnsSameItem()
    {
        var repo = new InMemoryTodoRepository();

        var added = repo.Add("Buy milk");
        var fetched = repo.GetById(added.Id);

        Assert.NotNull(fetched);
        Assert.Equal(added.Id, fetched!.Id);
        Assert.Equal("Buy milk", fetched.Title);
        Assert.False(fetched.IsDone);
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        var repo = new InMemoryTodoRepository();

        var result = repo.GetById(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public void Update_UnknownId_ReturnsNull()
    {
        var repo = new InMemoryTodoRepository();

        var result = repo.Update(Guid.NewGuid(), "Doesn't exist", true);

        Assert.Null(result);
    }

    [Fact]
    public void Update_KnownId_ReturnsUpdatedItemAndPersists()
    {
        var repo = new InMemoryTodoRepository();
        var added = repo.Add("Buy milk");

        var updated = repo.Update(added.Id, "Buy oat milk", true);
        var fetched = repo.GetById(added.Id);

        Assert.NotNull(updated);
        Assert.Equal("Buy oat milk", updated!.Title);
        Assert.True(updated.IsDone);
        Assert.Equal(updated, fetched);
    }

    [Fact]
    public void Delete_KnownId_RemovesItAndSecondDeleteReturnsFalse()
    {
        var repo = new InMemoryTodoRepository();
        var added = repo.Add("Buy milk");

        var firstDelete = repo.Delete(added.Id);
        var secondDelete = repo.Delete(added.Id);

        Assert.True(firstDelete);
        Assert.False(secondDelete);
        Assert.DoesNotContain(repo.GetAll(), t => t.Id == added.Id);
    }
}
