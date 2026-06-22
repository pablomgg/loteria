using Loteria.Console.Configurations;
using Loteria.Console.Configurations.OpenTelemetry;
using Loteria.Console.Hosts;
using Loteria.Console.Integrations;
using Loteria.Console.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

AppSettings appSettings = new(builder.Configuration);
builder.Services.AddOptions<AppSettings>()
    .Bind(builder.Configuration);

builder.Services.AddDatabase(appSettings.Data.AppConnection);
builder.Services.AddJaeger(appSettings);
builder.Services.AddCaixaLoterias();

builder.Services.AddScoped<IJogoImportService, LotofacilService>();
builder.Services.AddScoped<IJogoImportService, MegaSenaService>();
builder.Services.AddScoped<ILoteriaImportDispatcher, LoteriaImportDispatcher>();

builder.Services.AddHostedService<PoolingWorker>();
    
var host = builder.Build();
await host.RunAsync();