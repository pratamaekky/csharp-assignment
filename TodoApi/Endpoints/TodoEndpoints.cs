using TodoApi.Models;
using TodoApi.Repositories;

namespace TodoApi.Endpoints;

public static class TodoEndpoints
{
    public const int MaxTitleLength = 200;

    public static void MapTodoEndpoints(this WebApplication app)
    {
        app.MapGet("/todos", (ITodoRepository repo) => Results.Ok(repo.GetAll()));

        app.MapGet("/todos/{id:guid}", (Guid id, ITodoRepository repo) =>
        {
            var todo = repo.GetById(id);
            return todo is not null ? Results.Ok(todo) : Results.NotFound();
        });

        app.MapPost("/todos", (CreateTodoRequest request, ITodoRepository repo) =>
        {
            var title = request.Title?.Trim();
            if (string.IsNullOrEmpty(title) || title.Length > MaxTitleLength)
            {
                return Results.BadRequest(new { error = $"Title is required and must be 1-{MaxTitleLength} characters." });
            }

            var todo = repo.Add(title);
            return Results.Created($"/todos/{todo.Id}", todo);
        });

        app.MapPut("/todos/{id:guid}", (Guid id, UpdateTodoRequest request, ITodoRepository repo) =>
        {
            var title = request.Title?.Trim();
            if (string.IsNullOrEmpty(title) || title.Length > MaxTitleLength)
            {
                return Results.BadRequest(new { error = $"Title is required and must be 1-{MaxTitleLength} characters." });
            }

            var updated = repo.Update(id, title, request.IsDone);
            return updated is not null ? Results.Ok(updated) : Results.NotFound();
        });

        app.MapDelete("/todos/{id:guid}", (Guid id, ITodoRepository repo) =>
            repo.Delete(id) ? Results.NoContent() : Results.NotFound());
    }
}
