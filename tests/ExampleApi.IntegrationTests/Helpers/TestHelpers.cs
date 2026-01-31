namespace ExampleApi.IntegrationTests.Helpers;

/// <summary>
/// Helper methods for integration tests
/// </summary>
public static class TestHelpers
{
	/// <summary>
	/// Generates random test data for titles
	/// </summary>
	public static string GenerateRandomTitle(int length = 10)
	{
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ";
		Random random = new();
		return new string([.. Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)])]).Trim();
	}

	/// <summary>
	/// Creates test data
	/// </summary>
	public static Data.Todo CreateTestTodo(string? title = null, string? description = null, bool isCompleted = false)
	{
		return new Data.Todo
		{
			Title = title ?? GenerateRandomTitle(),
			Description = description ?? "Test description",
			IsCompleted = isCompleted,
			CreatedAt = DateTime.UtcNow
		};
	}

	/// <summary>
	/// Waits for a condition to be met with timeout
	/// </summary>
	public static async Task WaitForConditionAsync(Func<Task<bool>> condition, TimeSpan timeout, TimeSpan? interval = null)
	{
		interval ??= TimeSpan.FromMilliseconds(100);
		DateTime endTime = DateTime.UtcNow.Add(timeout);

		while (DateTime.UtcNow < endTime)
		{
			if (await condition())
			{
				return;
			}

			await Task.Delay(interval.Value);
		}

		throw new TimeoutException($"Condition was not met within {timeout}");
	}
}
