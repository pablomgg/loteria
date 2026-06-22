using Loteria.Console.Configurations;
using Loteria.Console.Integrations.LoteriasCaixaRefit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Loteria.Console.Integrations;

public static class DependencyInjection
{
    public static IServiceCollection AddCaixaLoterias(this IServiceCollection services)
    {
        services.AddTransient<CaixaHeadersHandler>();
        services.AddTransient(sp =>
        {
            var opt = sp.GetRequiredService<IOptions<AppSettings>>().Value;
            return new CaixaThrottleHandler(TimeSpan.FromMilliseconds(opt.LoteriaApi.Throttle));
        });
        
        services.AddCaixaRefitClient<ILotofacilApi>();
        services.AddCaixaRefitClient<IMegaSenaApi>();

        return services;
    }
}