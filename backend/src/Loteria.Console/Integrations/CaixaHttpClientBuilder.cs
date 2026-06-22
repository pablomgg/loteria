using Loteria.Console.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace Loteria.Console.Integrations;

public static class CaixaHttpClientBuilder
{
    public static IHttpClientBuilder AddCaixaRefitClient<T>(
        this IServiceCollection services)
        where T : class
    {
        var builder = services
            .AddRefitClient<T>()
            .ConfigureHttpClient((sp, c) =>
            {
                var opt = sp.GetRequiredService<IOptions<AppSettings>>().Value;
                c.BaseAddress = new Uri(opt.LoteriaApi.BaseUrl);
                c.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddHttpMessageHandler<CaixaHeadersHandler>()
            .AddHttpMessageHandler(sp => sp.GetRequiredService<CaixaThrottleHandler>());
        
        builder.AddResilienceHandler("caixa-api", CaixaResilience.Configure);
        
        return builder;
    }
}