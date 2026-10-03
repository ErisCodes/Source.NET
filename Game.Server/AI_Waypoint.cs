global using static Game.Server.AI_WaypointGlobals;

using Source.Common;

using System.Numerics;

namespace Game.Server;

public static class AI_WaypointGlobals
{
	public const int NO_NODE = -1;

	public static void DeleteAll(AI_Waypoint_t? waypointList) {
		while (waypointList != null) {
			AI_Waypoint_t prevWaypoint = waypointList;
			waypointList = waypointList.GetNext();
			prevWaypoint.Delete();
		}
	}
}

[Flags]
public enum WaypointFlags_t
{
	bits_WP_TO_DETOUR = 0x01,
	bits_WP_TO_PATHCORNER = 0x02,
	bits_WP_TO_NODE = 0x04,
	bits_WP_TO_GOAL = 0x08,
	bits_WP_TO_DOOR = 0x10,

	bits_WP_DONT_SIMPLIFY = 0x20,
}

public class AI_Waypoint_t
{
	public AI_Waypoint_t() {
		VecLocation = vec3_invalid;
		NodeID = NO_NODE;
		PathDistGoal = -1;
	}

	public void Delete() {
		AssertValid();
		if (Next != null) {
			Next.AssertValid();
			Next.Prev = Prev;
		}
		if (Prev != null) {
			Prev.AssertValid();
			Prev.Next = Next;
		}
	}

	public void AssertValid() {
#if DEBUG
		Assert(Next == null || Next.Prev == this);
		Assert(Prev == null || Prev.Next == this);
#endif
	}

	public WaypointFlags_t Flags() => WaypointFlags;
	public Navigation_t NavType() => WPType;

	public AI_Waypoint_t? GetNext() => Next;
	public AI_Waypoint_t? GetPrev() => Prev;

	public ref readonly Vector3 GetPos() => ref VecLocation;
	public void SetPos(in Vector3 newPos) => VecLocation = newPos;

	public Vector3 VecLocation;
	public float Yaw;
	public int NodeID;

	public float PathDistGoal;

	public readonly EHANDLE PathCorner = new();

	public readonly EHANDLE Data = new();

	WaypointFlags_t WaypointFlags;
	Navigation_t WPType;

	AI_Waypoint_t? Next;
	AI_Waypoint_t? Prev;
}

public class AI_WaypointList
{
	public AI_WaypointList() {
		FirstWaypoint = null;
	}

	public AI_WaypointList(AI_Waypoint_t? firstWaypoint) {
		FirstWaypoint = firstWaypoint;
	}

	public bool IsEmpty() => FirstWaypoint == null;

	public AI_Waypoint_t? GetFirst() => FirstWaypoint;

	public void RemoveAll() {
		DeleteAll(FirstWaypoint);
		FirstWaypoint = null;
		Assert(FirstWaypoint == null);
	}

	AI_Waypoint_t? FirstWaypoint;
}
