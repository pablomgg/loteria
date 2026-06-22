namespace Loteria.Console.Configurations;

public struct AppSettingKeys
{
    public struct Data
    {
        public const string AppConnection = "Data:AppConnection";
    }
    
    public struct LoteriaApi
    {
        public const string BaseUrl = "LoteriaApi:BaseUrl";
        public const string Throttle = "LoteriaApi:Throttle";
        public const string TimeoutSeconds = "LoteriaApi:TimeoutSeconds";
    }
    
    public struct OpenTelemetry
    {
        public struct Jaeger
        {
            public const string Endpoint = "OpenTelemetry:Jaeger:Endpoint";
            public const string Protocol = "OpenTelemetry:Jaeger:Protocol";
            public const string ServiceName = "OpenTelemetry:Jaeger:ServiceName";
            public const string ServiceVersion = "OpenTelemetry:Jaeger:ServiceVersion";
        }
    }
}