using ExampleApi.Data;
using Shouldly;

namespace ExampleApi.Tests;

public class InMemoryTodoStoreTests
{
	[Fact]
	public async Task GetAllAsync_ReturnsInitialSeedData()
	{
		// Arrange
		InMemoryTodoStore store = new();

		// Act
		IEnumerable<Todo> todos = await store.GetAllAsync(CancellationToken.None);

		// Assert
		todos.Count().ShouldBe(3);
	}

	[Fact]
	public async Task GetByIdAsync_WithValidId_ReturnsTodo()
	{
		// Arrange
		InMemoryTodoStore store = new();

		// Act
		Todo? todo = await store.GetByIdAsync(1, CancellationToken.None);

		// Assert
		todo.ShouldNotBeNull();
		todo.Id.ShouldBe(1);
	}

	[Fact]
	public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
	{
		// Arrange
		InMemoryTodoStore store = new();

		// Act
		Todo? todo = await store.GetByIdAsync(999, CancellationToken.None);

		// Assert
		todo.ShouldBeNull();
	}

	[Fact]
	public async Task CreateAsync_AddsNewTodo()
	{
		// Arrange
		InMemoryTodoStore store = new();
		Todo newTodo = new()
		{
			Title = "New Todo",
			Description = "New Description",
			IsCompleted = false
		};

		// Act
		Todo created = await store.CreateAsync(newTodo, CancellationToken.None);

		// Assert
		created.Id.ShouldBeGreaterThan(0);
		created.Title.ShouldBe("New Todo");
		created.CreatedAt.ShouldBe(DateTime.UtcNow, TimeSpan.FromSeconds(1));
	}

	[Fact]
	public async Task UpdateAsync_WithValidId_UpdatesTodo()
	{
		// Arrange
		InMemoryTodoStore store = new();
		Todo updatedTodo = new()
		{
			Title = "Updated Title",
			Description = "Updated Description",
			IsCompleted = true
		};

		// Act
		Todo? result = await store.UpdateAsync(1, updatedTodo, CancellationToken.None);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.Title.ShouldBe("Updated Title");
		result.UpdatedAt.ShouldNotBeNull();
	}

	[Fact]
	public async Task UpdateAsync_WithInvalidId_ReturnsNull()
	{
		// Arrange
		InMemoryTodoStore store = new();
		Todo updatedTodo = new()
		{
			Title = "Updated Title",
			Description = "Updated Description",
			IsCompleted = true
		};

		// Act
		Todo? result = await store.UpdateAsync(999, updatedTodo, CancellationToken.None);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task DeleteAsync_WithValidId_ReturnsTrue()
	{
		// Arrange
		InMemoryTodoStore store = new();

		// Act
		bool result = await store.DeleteAsync(1, CancellationToken.None);

		// Assert
		result.ShouldBeTrue();

		// Verify deletion
		Todo? deletedTodo = await store.GetByIdAsync(1, CancellationToken.None);
		deletedTodo.ShouldBeNull();
	}

	[Fact]
	public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
	{
		// Arrange
		InMemoryTodoStore store = new();

		// Act
		bool result = await store.DeleteAsync(999, CancellationToken.None);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task PatchAsync_WithValidId_AppliesPatch()
	{
		// Arrange
		InMemoryTodoStore store = new();

		// Act
		Todo? result = await store.PatchAsync(1, todo => todo.IsCompleted = true, CancellationToken.None);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.IsCompleted.ShouldBeTrue();
		result.UpdatedAt.ShouldNotBeNull();
	}

	[Fact]
	public async Task PatchAsync_WithInvalidId_ReturnsNull()
	{
		// Arrange
		InMemoryTodoStore store = new();

		// Act
		Todo? result = await store.PatchAsync(999, todo => todo.IsCompleted = true, CancellationToken.None);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task CreateAsync_AssignsSequentialIds()
	{
		// Arrange
		InMemoryTodoStore store = new();
		Todo todo1 = new() { Title = "Todo 1", Description = "Description 1", IsCompleted = false };
		Todo todo2 = new() { Title = "Todo 2", Description = "Description 2", IsCompleted = false };

		// Act
		Todo created1 = await store.CreateAsync(todo1, CancellationToken.None);
		Todo created2 = await store.CreateAsync(todo2, CancellationToken.None);

		// Assert
		created1.Id.ShouldBe(4); // After 3 seed items
		created2.Id.ShouldBe(5);
	}

	[Fact]
	public async Task CreateAsync_SetsCreatedAtAndNullUpdatedAt()
	{
		// Arrange
		InMemoryTodoStore store = new();
		Todo newTodo = new()
		{
			Title = "Test Todo",
			Description = "Test Description",
			IsCompleted = false
		};

		// Act
		Todo created = await store.CreateAsync(newTodo, CancellationToken.None);

		// Assert
		created.CreatedAt.ShouldBe(DateTime.UtcNow, TimeSpan.FromSeconds(1));
		created.UpdatedAt.ShouldBeNull();
	}

	[Fact]
	public async Task PatchAsync_PreservesUnmodifiedProperties()
	{
		// Arrange
		InMemoryTodoStore store = new();
		Todo? original = await store.GetByIdAsync(1, CancellationToken.None);

		// Act
		Todo? result = await store.PatchAsync(1, todo => todo.IsCompleted = true, CancellationToken.None);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(original!.Id);
		result.Title.ShouldBe(original.Title);
		result.Description.ShouldBe(original.Description);
		result.CreatedAt.ShouldBe(original.CreatedAt);
		result.IsCompleted.ShouldBeTrue(); // Only this should change
		result.UpdatedAt.ShouldNotBeNull();
	}

	[Fact]
	public async Task PatchAsync_CanModifyMultipleProperties()
	{
		// Arrange
		InMemoryTodoStore store = new();

		// Act
		Todo? result = await store.PatchAsync(1, todo =>
		{
			todo.Title = "Patched Title";
			todo.Description = "Patched Description";
			todo.IsCompleted = true;
		}, CancellationToken.None);

		// Assert
		result.ShouldNotBeNull();
		result.Title.ShouldBe("Patched Title");
		result.Description.ShouldBe("Patched Description");
		result.IsCompleted.ShouldBeTrue();
	}

	[Fact]
	public async Task GetAllAsync_ReturnsOrderedById()
	{
		// Arrange
		InMemoryTodoStore store = new();

		// Act
		IEnumerable<Todo> todos = await store.GetAllAsync(CancellationToken.None);

		// Assert
		List<Todo> todoList = todos.ToList();
		todoList.Count.ShouldBe(3);
		todoList[0].Id.ShouldBe(1);
		todoList[1].Id.ShouldBe(2);
		todoList[2].Id.ShouldBe(3);
	}

	[Fact]
	public async Task DeleteAsync_RemovedTodoNotInGetAll()
	{
		// Arrange
		InMemoryTodoStore store = new();

		// Act
		bool deleted = await store.DeleteAsync(2, CancellationToken.None);
		IEnumerable<Todo> remaining = await store.GetAllAsync(CancellationToken.None);

		// Assert
		deleted.ShouldBeTrue();
		remaining.Count().ShouldBe(2);
		remaining.Any(t => t.Id == 2).ShouldBeFalse();
	}

	[Fact]
	public async Task CreateAsync_AfterDelete_ReusesIdSpace()
	{
		// Arrange
		InMemoryTodoStore store = new();
		await store.DeleteAsync(1, CancellationToken.None);
		await store.DeleteAsync(2, CancellationToken.None);
		await store.DeleteAsync(3, CancellationToken.None);

		Todo newTodo = new()
		{
			Title = "New After Delete",
			Description = "Description",
			IsCompleted = false
		};

		// Act
		Todo created = await store.CreateAsync(newTodo, CancellationToken.None);

		// Assert
		created.Id.ShouldBe(4); // ID counter continues, doesn't reset
	}
}
