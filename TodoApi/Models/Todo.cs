namespace TodoApi.Models;

public record Todo(Guid Id, string Title, bool IsDone, DateTimeOffset CreatedAt);
