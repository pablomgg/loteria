using Microsoft.Extensions.Configuration;

namespace Loteria.Console.Configurations;

public class AppSettings
{
    public DataProvider Data { get; set; } 
    public LoteriaProvider LoteriaApi { get; set; }
    public OpenTelemetryProvider OpenTelemetrySettings { get; set; }

    public AppSettings(IConfiguration configuration)
    {
        Data = new(configuration);
        LoteriaApi = new(configuration);
        OpenTelemetrySettings = new(configuration);
    }

    public AppSettings()
    {
        
    }
}

public class DataProvider
{
    public string AppConnection { get; set; } 
    public DataProvider(IConfiguration configuration)
    {
        AppConnection = configuration.GetValue<string>(AppSettingKeys.Data.AppConnection)!;
    }
    
    public DataProvider()
    {
        
    }
}

public class LoteriaProvider
{
    public string BaseUrl { get; set; }
    public int Throttle { get; set; } 
    public int TimeoutSeconds { get; set; } 

    public LoteriaProvider(IConfiguration configuration)
    {
        BaseUrl = configuration.GetValue<string>(AppSettingKeys.LoteriaApi.BaseUrl)!;
        Throttle = configuration.GetValue<int>(AppSettingKeys.LoteriaApi.Throttle)!;
        TimeoutSeconds = configuration.GetValue<int>(AppSettingKeys.LoteriaApi.TimeoutSeconds)!;
    }

    public LoteriaProvider()
    {
        
    }
}

public class OpenTelemetryProvider(IConfiguration configuration)
{
    public JaegerSettings Jaeger { get; set; } = new(configuration);
    public EfCoreSettings EfCore { get; set; } = new(configuration);

    public sealed class JaegerSettings(IConfiguration configuration)
    {
        public string Endpoint { get; set; } = configuration.GetValue<string>(AppSettingKeys.OpenTelemetry.Jaeger.Endpoint)!;
        public string Protocol { get; set; } = configuration.GetValue<string>(AppSettingKeys.OpenTelemetry.Jaeger.Protocol)!;
        public string ServiceName { get; set; } = configuration.GetValue<string>(AppSettingKeys.OpenTelemetry.Jaeger.ServiceName)!;
        public string ServiceVersion { get; set; } = configuration.GetValue<string>(AppSettingKeys.OpenTelemetry.Jaeger.ServiceVersion)!;
    }

    public sealed class EfCoreSettings(IConfiguration configuration)
    {
        public bool EnableDbStatement { get; set; } = false;
        public bool EnableDbParameters { get; set; } = false;
    }
}