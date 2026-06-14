using Serilog;
using StoneBridge.Api.Extensions;

// ── Bootstrap logger — used before DI is configured ──────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting StoneBridge API...");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithThreadId()
        .WriteTo.Console(
            outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

    // ── Register all services via extension methods ───────────────────────────
    // Program.cs stays clean — all DI setup lives in dedicated extension methods.
    builder.Services.AddApplicationServices(builder.Configuration);
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddApiServices(builder.Configuration);

    // ── Build and configure the request pipeline ──────────────────────────────
    var app = builder.Build();
    app.ConfigurePipeline();

    Log.Information("StoneBridge API configured successfully. Listening...");
    await app.RunAsync();

    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "StoneBridge API terminated unexpectedly.");
    return 1;
}
finally
{
    Log.Information("StoneBridge API shutting down.");
    await Log.CloseAndFlushAsync();
}

// Makes Program visible to WebApplicationFactory in integration tests
public partial class Program;