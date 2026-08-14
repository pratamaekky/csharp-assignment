using System.Collections.Concurrent;
using TodoApi.Models;

namespace TodoApi.Repositories;

public class InMemoryTodoRepository : ITodoRepository
{
    private readonly ConcurrentDictionary<Guid, Todo> _todos = new();

    public IEnumerable<Todo> GetAll() => _todos.Values.OrderBy(t => t.CreatedAt);

    public Todo? GetById(Guid id) => _todos.TryGetValue(id, out var todo) ? todo : null;

    public Todo Add(string title)
    {
        var todo = new Todo(Guid.NewGuid(), title, false, DateTimeOffset.UtcNow);
        _todos[todo.Id] = todo;
        return todo;
    }

    public Todo? Update(Guid id, string title, bool isDone)
    {
        if (!_todos.TryGetValue(id, out var existing)) return null;
        var updated = existing with { Title = title, IsDone = isDone };
        _todos[id] = updated;
        return updated;
    }

    public bool Delete(Guid id) => _todos.TryRemove(id, out _);
}
