using System.Collections.Concurrent;
using ExampleApi.Models;

namespace ExampleApi.Services;

public interface ITodoStore
{
    Task<IEnumerable<Todo>> GetAllAsync(CancellationToken ct);
    Task<Todo?> GetByIdAsync(int id, CancellationToken ct);
    Task<Todo> CreateAsync(Todo todo, CancellationToken ct);
    Task<Todo?> UpdateAsync(int id, Todo todo, CancellationToken ct);
    Task<Todo?> PatchAsync(int id, Action<Todo> patchAction, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}

public class InMemoryTodoStore : ITodoStore
{
    private readonly ConcurrentDictionary<int, Todo> _todos = new();
    private int _nextId = 1;

    public InMemoryTodoStore()
    {
        // Seed with some initial data
        Todo[] initialTodos =
        [
            new Todo { Id = _nextId++, Title = "Learn ASP.NET Core", Description = "Study minimal APIs", IsCompleted = false, CreatedAt = DateTime.UtcNow },
            new Todo { Id = _nextId++, Title = "Build a Todo API", Description = "Create CRUD endpoints", IsCompleted = true, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Todo { Id = _nextId++, Title = "Write tests", Description = "Add unit tests for the API", IsCompleted = false, CreatedAt = DateTime.UtcNow.AddHours(-2) }
        ];

        foreach(Todo? todo in initialTodos)
        {
            _todos.TryAdd(todo.Id, todo);
        }
    }

    public Task<IEnumerable<Todo>> GetAllAsync(CancellationToken ct)
    {
        return Task.FromResult(_todos.Values.OrderBy(t => t.Id).AsEnumerable());
    }

    public Task<Todo?> GetByIdAsync(int id, CancellationToken ct)
    {
        _todos.TryGetValue(id, out Todo? todo);
        return Task.FromResult(todo);
    }

    public Task<Todo> CreateAsync(Todo todo, CancellationToken ct)
    {
        todo.Id = _nextId++;
        todo.CreatedAt = DateTime.UtcNow;
        todo.UpdatedAt = null;

        _todos.TryAdd(todo.Id, todo);
        return Task.FromResult(todo);
    }

    public Task<Todo?> UpdateAsync(int id, Todo todo, CancellationToken ct)
    {
        if(!_todos.ContainsKey(id))
            return Task.FromResult<Todo?>(null);

        todo.Id = id;
        todo.UpdatedAt = DateTime.UtcNow;

        _todos.TryUpdate(id, todo, _todos[id]);
        return Task.FromResult<Todo?>(todo);
    }

    public Task<Todo?> PatchAsync(int id, Action<Todo> patchAction, CancellationToken ct)
    {
        if(!_todos.TryGetValue(id, out Todo? existingTodo))
            return Task.FromResult<Todo?>(null);

        Todo updatedTodo = new()
        {
            Id = existingTodo.Id,
            Title = existingTodo.Title,
            Description = existingTodo.Description,
            IsCompleted = existingTodo.IsCompleted,
            CreatedAt = existingTodo.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        patchAction(updatedTodo);

        _todos.TryUpdate(id, updatedTodo, existingTodo);
        return Task.FromResult<Todo?>(updatedTodo);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        return Task.FromResult(_todos.TryRemove(id, out _));
    }
}