using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Server;

public interface ILagCompensationManager
{
	void StartLagCompensation(BasePlayer player, in Vector3 weaponPos = default, in QAngle weaponAngles = default, float weaponRange = 0.0f);
	void FinishLagCompensation(BasePlayer player);

	bool IsCurrentlyDoingLagCompensation();

	void AddAdditionalEntity(BaseEntity entity);
	void RemoveAdditionalEntity(BaseEntity entity);
	bool IsAdditionalEntity(BaseEntity entity);
}
