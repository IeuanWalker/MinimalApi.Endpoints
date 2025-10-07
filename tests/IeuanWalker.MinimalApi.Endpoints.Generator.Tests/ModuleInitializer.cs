using System.Runtime.CompilerServices;
using VerifyTests;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests;

public static class ModuleInitializer
{
	[ModuleInitializer]
	public static void Init()
	{
		// Initialize Verify settings for source generators
		VerifySourceGenerators.Initialize();
		
		// Configure Verify to use deterministic GUIDs for snapshots
		VerifierSettings.UniqueForRuntimeAndVersion();
	}
}
