using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;

namespace Loteria.Console.Integrations;

public static class CaixaResilience
{
    public static void Configure(ResiliencePipelineBuilder<HttpResponseMessage> builder)
    {
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            BackoffType = DelayBackoffType.Exponential,
            MaxRetryAttempts = 8,
            UseJitter = true,
            Delay = TimeSpan.FromMilliseconds(800),

            ShouldHandle = static args =>
            {
                if (args.Outcome.Exception is not null)
                    return ValueTask.FromResult(true);

                var response = args.Outcome.Result;
                if (response is null)
                    return ValueTask.FromResult(false);

                var status = response.StatusCode;
                
                var shouldRetry =
                    status == HttpStatusCode.TooManyRequests ||
                    status == HttpStatusCode.Forbidden ||
                    (int)status >= 500;

                return ValueTask.FromResult(shouldRetry);
            },

            DelayGenerator = static args =>
            {
                var response = args.Outcome.Result;

                if (response is not null)
                {
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        return ValueTask.FromResult<TimeSpan?>(TimeSpan.FromMinutes(5));
                    }

                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        return ValueTask.FromResult<TimeSpan?>(TimeSpan.FromSeconds(60));
                    }
                }
                
                return ValueTask.FromResult<TimeSpan?>(null);
            },

            OnRetry = static args =>
            {
                var status = args.Outcome.Result?.StatusCode;
                var ex = args.Outcome.Exception?.Message;

                System.Console.WriteLine(
                    $"Retry tentativa {args.AttemptNumber + 1} | " +
                    $"status={(int?)status} | " +
                    $"delay={args.RetryDelay.TotalSeconds:F1}s | " +
                    $"erro={ex}");

                return default;
            }
        });
        
        builder.AddTimeout(TimeSpan.FromSeconds(30));
    }
}