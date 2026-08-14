using TodoApi.Models;

namespace TodoApi.Repositories;

public interface ITodoRepository
{
    IEnumerable<Todo> GetAll();
    Todo? GetById(Guid id);
    Todo Add(string title);
    Todo? Update(Guid id, string title, bool isDone);
    bool Delete(Guid id);
}
