using ExampleApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleApi.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for ExampleApi integration tests.
/// This sets up the test server and allows for dependency overrides.
/// </summary>
public class ExampleApiWebApplicationFactory : WebApplicationFactory<Program>
{
	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.ConfigureServices(services =>
		{
			// Remove the existing ITodoStore registration
			ServiceDescriptor? descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITodoStore));
			if (descriptor is not null)
			{
				services.Remove(descriptor);
			}

			// Add a test-specific ITodoStore implementation
			services.AddSingleton<ITodoStore, TestTodoStore>();
		});

		builder.UseEnvironment("Testing");
	}
}

/// <summary>
/// Test implementation of ITodoStore for integration tests.
/// Uses in-memory storage but allows for test-specific behavior.
/// </summary>
public class TestTodoStore : ITodoStore
{
	readonly Dictionary<int, Todo> _todos = [];
	int _nextId = 1;

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

		_todos[todo.Id] = todo;
		return Task.FromResult(todo);
	}

	public Task<Todo?> UpdateAsync(int id, Todo todo, CancellationToken ct)
	{
		if (!_todos.ContainsKey(id))
		{
			return Task.FromResult<Todo?>(null);
		}

		todo.Id = id;
		todo.UpdatedAt = DateTime.UtcNow;
		_todos[id] = todo;
		return Task.FromResult<Todo?>(todo);
	}

	public Task<Todo?> PatchAsync(int id, Action<Todo> patchAction, CancellationToken ct)
	{
		if (!_todos.TryGetValue(id, out Todo? existingTodo))
		{
			return Task.FromResult<Todo?>(null);
		}

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
		_todos[id] = updatedTodo;
		return Task.FromResult<Todo?>(updatedTodo);
	}

	public Task<bool> DeleteAsync(int id, CancellationToken ct)
	{
		return Task.FromResult(_todos.Remove(id));
	}

	/// <summary>
	/// Test helper method to seed data for tests
	/// </summary>
	public void SeedData(params Todo[] todos)
	{
		foreach (Todo todo in todos)
		{
			todo.Id = _nextId++;
			todo.CreatedAt = DateTime.UtcNow;
			_todos[todo.Id] = todo;
		}
	}

	/// <summary>
	/// Test helper method to clear all data
	/// </summary>
	public void Clear()
	{
		_todos.Clear();
		_nextId = 1;
	}
}
