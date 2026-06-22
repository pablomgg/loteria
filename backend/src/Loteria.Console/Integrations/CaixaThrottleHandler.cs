namespace Loteria.Console.Integrations;

public sealed class CaixaThrottleHandler : DelegatingHandler
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTime _lastUtc = DateTime.MinValue;

    private readonly TimeSpan _minInterval;

    public CaixaThrottleHandler(TimeSpan minInterval)
        => _minInterval = minInterval;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var now = DateTime.UtcNow;
            var elapsed = now - _lastUtc;

            var wait = _minInterval - elapsed;
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)), ct);

            _lastUtc = DateTime.UtcNow;
        }
        finally
        {
            _gate.Release();
        }

        return await base.SendAsync(request, ct);
    }
}