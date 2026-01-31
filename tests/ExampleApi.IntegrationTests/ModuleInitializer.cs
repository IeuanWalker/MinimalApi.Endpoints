using System.Runtime.CompilerServices;

namespace ExampleApi.IntegrationTests;

public static class ModuleInitializer
{
	[ModuleInitializer]
	public static void Init()
	{
		VerifyHttp.Initialize();
	}
}

public sealed class VerifyChecksTests
{
	[Fact]
	public Task Run() => VerifyChecks.Run();
}
