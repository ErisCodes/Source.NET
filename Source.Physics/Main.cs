global using static Source.Physics.SourceDllMain;

using Microsoft.Extensions.DependencyInjection;

using Source.Common.Physics;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static Box3D.Box3D;

namespace Source.Physics;


public static class SourceDllMain
{
	[Dependency] public static IPhysicsSurfaceProps physprops { get; private set; } = null!;
	[Dependency] public static IPhysicsCollision physcollision { get; private set; } = null!;
}

internal sealed class PhysicsCollisionSet : IPhysicsCollisionSet
{
	readonly uint[] Bits = new uint[32];

	public void EnableCollisions(int index0, int index1) {
		Bits[index0] |= 1u << index1;
		Bits[index1] |= 1u << index0;
	}

	public void DisableCollisions(int index0, int index1) {
		Bits[index0] &= ~(1u << index1);
		Bits[index1] &= ~(1u << index0);
	}

	public bool ShouldCollide(int index0, int index1) => (Bits[index0] & (1u << index1)) != 0;
}

public unsafe class PhysicsInterface : IPhysics
{
	readonly List<IPhysicsEnvironment> Environments = [];
	readonly Dictionary<uint, PhysicsCollisionSet> CollisionSets = [];

	public static void DLLInit(IServiceCollection services) {
		services.AddSingleton<IPhysicsCollision, PhysicsCollide>();
		services.AddSingleton<IPhysicsSurfaceProps, PhysicsSurfaceProps>();

		b3SetAssertFcn(&OnAssert);
		b3SetLogFcn(&OnLog);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	static int OnAssert(sbyte* condition, sbyte* fileName, int lineNumber) {
		Warning($"Box3D assert: {Marshal.PtrToStringUTF8((nint)condition)} ({Marshal.PtrToStringUTF8((nint)fileName)}:{lineNumber})\n");
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	static void OnLog(sbyte* message) => Msg(Marshal.PtrToStringUTF8((nint)message));

	public IPhysicsEnvironment CreateEnvironment() {
		IPhysicsEnvironment environment = CreatePhysicsEnvironment();
		Environments.Add(environment);
		return environment;
	}

	public void DestroyEnvironment(IPhysicsEnvironment? env) {
		if (env == null)
			return;

		Environments.Remove(env);
		(env as PhysicsEnvironment)?.Destroy();
	}

	public IPhysicsEnvironment? GetActiveEnvironmentByIndex(int index) {
		if (index < 0 || index >= Environments.Count)
			return null;

		return Environments[index];
	}

	public IPhysicsObjectPairHash CreateObjectPairHash() => PhysicsEnvironmentGlobals.CreateObjectPairHash();

	public void DestroyObjectPairHash(IPhysicsObjectPairHash hash) { }

	public IPhysicsCollisionSet FindOrCreateCollisionSet(uint id, int maxElementCount) {
		if (maxElementCount > 32)
			return null!;

		if (CollisionSets.TryGetValue(id, out PhysicsCollisionSet? set))
			return set;

		set = new PhysicsCollisionSet();
		CollisionSets[id] = set;
		return set;
	}

	public IPhysicsCollisionSet FindCollisionSet(uint id) => CollisionSets.TryGetValue(id, out PhysicsCollisionSet? set) ? set : null!;

	public void DestroyAllCollisionSets() => CollisionSets.Clear();
}
