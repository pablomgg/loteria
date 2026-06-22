using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Loteria.Console.Configurations.OpenTelemetry;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddJaeger(this IServiceCollection services, AppSettings settings)
    {
        var otel = settings.OpenTelemetrySettings;
        var jaeger = otel.Jaeger;
        System.Console.WriteLine(JsonSerializer.Serialize(otel));

        services.AddOpenTelemetry()
            .WithTracing(tracer =>
            {
                tracer
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                            .AddService(
                                serviceName: jaeger.ServiceName,
                                serviceVersion: jaeger.ServiceVersion))
                    .AddSource(Telemetry.ActivitySourceName)
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.Filter = (providerName, command) =>
                        {
                            // Exemplo: só coletar SqlServer
                            return providerName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true;
                        };
                        
                        // Enriquecimento: em dev, colocar statement ajuda MUITO
                        if (otel.EfCore.EnableDbStatement)
                        {
                            options.EnrichWithIDbCommand = (activity, command) =>
                            {
                                // CUIDADO: CommandText pode expor dados sensíveis dependendo do seu uso
                                activity.SetTag("db.statement", command.CommandText);

                                // tags úteis
                                activity.SetTag("db.command_type", command.CommandType.ToString());
                                activity.SetTag("db.database", command.Connection?.Database);
                            };
                        }
                    })
                    .AddOtlpExporter(otlp =>
                    {
                        otlp.Protocol = Enum.Parse<OtlpExportProtocol>(jaeger.Protocol, ignoreCase: true);
                        otlp.Endpoint = new Uri(jaeger.Endpoint);
                    });
            });

        return services;
    }
}