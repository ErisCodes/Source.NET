using Source.Common.MaterialSystem;

namespace Source.Common.Client;

/// <summary>
/// Exposes interfaces to the engine which allow the client to setup their own render targets
/// during the proper period of material system's init.
/// </summary>
public interface IClientRenderTargets
{
	/// <summary>
	/// Pass the material system interface to the client-- Their Material System singleton has not been created
	/// at the time they receive this call.
	/// </summary>
	void InitClientRenderTargets(IMaterialSystem materialSystem, IMaterialSystemHardwareConfig hardwareConfig);
	/// <summary>
	/// Call shutdown on every created refrence-- Clients keep track of this themselves
	/// and should add shutdown code to this function whenever they add a new render target.
	/// </summary>
	void ShutdownClientRenderTargets();
}
