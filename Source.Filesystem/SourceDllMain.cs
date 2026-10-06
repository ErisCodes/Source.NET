global using static Source.Filesystem.SourceDllMain;
using Source.Common.Commands;
using Source.Common.Filesystem;

namespace Source.Filesystem;

[EngineComponent]
public static class SourceDllMain
{
	[Dependency] public static IFileSystem g_FullFileSystem = null!;
	[Dependency] public static ICommandLine CommandLine = null!;
	[Dependency] public static ICvar cvar { get; private set; } = null!;
	[Dependency(Required = false)] public static Source.Common.GarrysMod.IGet? get { get; private set; }
}
