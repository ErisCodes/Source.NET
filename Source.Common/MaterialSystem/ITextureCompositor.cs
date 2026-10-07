using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common.MaterialSystem;

public enum CompositeResolveStatus
{
	Idle,
	Scheduled,
	PendingTextureLoads,
	PendingComposites,
	Error,
	Complete
}

[Flags]
public enum TextureCompositeCreateFlags
{
	Force = 0x00000001,
	NoCompression = 0x00000002,
	NoMipmaps = 0x00000004,
	VerifySchemaOnly = 0x00000008,
	VerifyTemplateOnly = 0x00000010,
	LogNodesOnly = 0x00000020
}

public interface ITextureCompositor : IRefCounted, IDisposable
{
	int GetRefCount();
	void Update();
	ITexture? GetResultTexture();
	CompositeResolveStatus GetResolveStatus();
	void ScheduleResolve();
}
