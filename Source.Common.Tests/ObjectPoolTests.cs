namespace Source.Common.Tests;

public class ObjectPoolTests
{
	sealed class TestPoolable : IPoolableObject
	{
		public int InitCalls;
		public int ResetCalls;
		public int InUse;
		public bool HandedOutTwice;

		public void Init() {
			InitCalls++;
			if (Interlocked.Exchange(ref InUse, 1) != 0)
				HandedOutTwice = true;
		}

		public void Reset() {
			ResetCalls++;
			Interlocked.Exchange(ref InUse, 0);
		}
	}

	[Fact]
	public void AllocInitsAndFreeResets() {
		ObjectPool<TestPoolable> pool = new();

		TestPoolable obj = pool.Alloc();
		Assert.Equal(1, obj.InitCalls);
		Assert.Equal(0, obj.ResetCalls);

		pool.Free(obj);
		Assert.Equal(1, obj.ResetCalls);
	}

	[Fact]
	public void FreedInstanceIsReused() {
		ObjectPool<TestPoolable> pool = new();

		TestPoolable first = pool.Alloc();
		pool.Free(first);
		TestPoolable second = pool.Alloc();

		Assert.Same(first, second);
		Assert.Equal(2, second.InitCalls);
	}

	[Fact]
	public void LiveInstancesAreDistinct() {
		ObjectPool<TestPoolable> pool = new();

		TestPoolable a = pool.Alloc();
		TestPoolable b = pool.Alloc();

		Assert.NotSame(a, b);
	}

	[Fact]
	public void CountTracksOutstandingInstances() {
		ObjectPool<TestPoolable> pool = new();

		TestPoolable a = pool.Alloc();
		TestPoolable b = pool.Alloc();
		Assert.Equal(2, pool.Count());

		pool.Free(a);
		Assert.Equal(1, pool.Count());

		pool.Free(b);
		Assert.Equal(0, pool.Count());
	}

	[Fact]
	public void FreeNullIsIgnored() {
		ObjectPool<TestPoolable> pool = new();

		pool.Free(null);

		Assert.Equal(0, pool.Count());
	}

	[Fact]
	public void ConcurrentUseNeverHandsOutAnInstanceTwice() {
		ObjectPool<TestPoolable> pool = new();
		const int threads = 8;
		const int iterations = 20000;
		bool handedOutTwice = false;

		Parallel.For(0, threads, _ => {
			for (int i = 0; i < iterations; i++) {
				TestPoolable obj = pool.Alloc();
				if (obj.HandedOutTwice)
					Volatile.Write(ref handedOutTwice, true);
				pool.Free(obj);
			}
		});

		Assert.False(handedOutTwice);
		Assert.Equal(0, pool.Count());
	}
}
