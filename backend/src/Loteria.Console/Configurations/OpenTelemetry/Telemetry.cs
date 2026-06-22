using System.Diagnostics;

namespace Loteria.Console.Configurations.OpenTelemetry;

public static class Telemetry
{
    public static readonly string ActivitySourceName = typeof(Program).Assembly.GetName().Name!;

    public static readonly ActivitySource Source =
        new(ActivitySourceName);
}