using ExampleApi.Models;
using ExampleApi.Services;
using FluentAssertions;

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
		todos.Should().HaveCount(3);
	}

	[Fact]
	public async Task GetByIdAsync_WithValidId_ReturnsTodo()
	{
		// Arrange
		InMemoryTodoStore store = new();

		// Act
		Todo? todo = await store.GetByIdAsync(1, CancellationToken.None);

		// Assert
		todo.Should().NotBeNull();
		todo!.Id.Should().Be(1);
	}

	[Fact]
	public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
	{
		// Arrange
		InMemoryTodoStore store = new();

		// Act
		Todo? todo = await store.GetByIdAsync(999, CancellationToken.None);

		// Assert
		todo.Should().BeNull();
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
		created.Id.Should().BeGreaterThan(0);
		created.Title.Should().Be("New Todo");
		created.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
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
		result.Should().NotBeNull();
		result!.Id.Should().Be(1);
		result.Title.Should().Be("Updated Title");
		result.UpdatedAt.Should().NotBeNull();
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
		result.Should().BeNull();
	}

	[Fact]
	public async Task DeleteAsync_WithValidId_ReturnsTrue()
	{
		// Arrange
		InMemoryTodoStore store = new();

		// Act
		bool result = await store.DeleteAsync(1, CancellationToken.None);

		// Assert
		result.Should().BeTrue();
		
		// Verify deletion
		Todo? deletedTodo = await store.GetByIdAsync(1, CancellationToken.None);
		deletedTodo.Should().BeNull();
	}

	[Fact]
	public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
	{
		// Arrange
		InMemoryTodoStore store = new();

		// Act
		bool result = await store.DeleteAsync(999, CancellationToken.None);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task PatchAsync_WithValidId_AppliesPatch()
	{
		// Arrange
		InMemoryTodoStore store = new();

		// Act
		Todo? result = await store.PatchAsync(1, todo =>
		{
			todo.IsCompleted = true;
		}, CancellationToken.None);

		// Assert
		result.Should().NotBeNull();
		result!.Id.Should().Be(1);
		result.IsCompleted.Should().BeTrue();
		result.UpdatedAt.Should().NotBeNull();
	}

	[Fact]
	public async Task PatchAsync_WithInvalidId_ReturnsNull()
	{
		// Arrange
		InMemoryTodoStore store = new();

		// Act
		Todo? result = await store.PatchAsync(999, todo =>
		{
			todo.IsCompleted = true;
		}, CancellationToken.None);

		// Assert
		result.Should().BeNull();
	}
}
