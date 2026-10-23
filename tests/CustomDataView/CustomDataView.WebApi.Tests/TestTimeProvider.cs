namespace CustomDataView.WebApi.Tests;

public sealed class TestTimeProvider : TimeProvider
{
    private long offsetTicks;

    public void Advance(TimeSpan time) => Interlocked.Add(ref offsetTicks, time.Ticks);

    public void Reset() => Interlocked.Exchange(ref offsetTicks, 0);

    public override DateTimeOffset GetUtcNow() => System.GetUtcNow().AddTicks(Interlocked.Read(ref offsetTicks));

    public override long GetTimestamp()
        => System.GetTimestamp() + Interlocked.Read(ref offsetTicks) * System.TimestampFrequency / TimeSpan.TicksPerSecond;
}
