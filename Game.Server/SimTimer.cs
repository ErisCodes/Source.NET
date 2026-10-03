namespace Game.Server;

public class SimpleSimTimer
{
	public const double ST_EPS = 0.001;

	public SimpleSimTimer() {
		Next = -1;
	}

	public void Force() => Next = -1;

	public bool Expired() => gpGlobals.CurTime - Next > -ST_EPS;

	public TimeUnit_t GetNext() => Next;

	public void Set(float interval) => Next = gpGlobals.CurTime + interval;

	public void Set(float minInterval, float maxInterval) {
		if (maxInterval > 0.0)
			Next = gpGlobals.CurTime + RandomFloat(minInterval, maxInterval);
		else
			Next = gpGlobals.CurTime + minInterval;
	}

	protected TimeUnit_t Next;
}

public class SimTimer : SimpleSimTimer
{
	public SimTimer(float interval = 0.0f, bool startExpired = true) {
		Set(interval, startExpired);
	}

	public void Set(float interval, bool startExpired = true) {
		Interval = interval;
		Next = startExpired ? -1.0 : gpGlobals.CurTime + Interval;
	}

	float Interval;
}

public class StopwatchBase : SimpleSimTimer
{
	public StopwatchBase() {
		IsRunning = false;
	}

	public void Stop() => IsRunning = false;

	protected bool IsRunning;
}

public class RandStopwatch : StopwatchBase
{
	public RandStopwatch(float minInterval = 0.0f, float maxInterval = 0.0f) {
		Set(minInterval, maxInterval);
	}

	public new void Set(float minInterval, float maxInterval = 0.0f) {
		MinInterval = minInterval;
		MaxInterval = maxInterval;
	}

	float MinInterval;
	float MaxInterval;
}
