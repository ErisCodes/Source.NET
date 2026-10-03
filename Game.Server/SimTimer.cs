namespace Game.Server;

public class SimpleSimTimer
{
	public SimpleSimTimer() {
		Next = -1;
	}

	protected TimeUnit_t Next;
}

public class StopwatchBase : SimpleSimTimer
{
	public StopwatchBase() {
		IsRunning = false;
	}

	protected bool IsRunning;
}

public class RandStopwatch : StopwatchBase
{
	public RandStopwatch(float minInterval = 0.0f, float maxInterval = 0.0f) {
		Set(minInterval, maxInterval);
	}

	public void Set(float minInterval, float maxInterval = 0.0f) {
		MinInterval = minInterval;
		MaxInterval = maxInterval;
	}

	float MinInterval;
	float MaxInterval;
}
