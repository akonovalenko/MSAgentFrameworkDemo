using BitcoinAgent.Api;
using BitcoinAgent.Application.Agents;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddApiServices(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = false;
    options.TimestampFormat = "HH:mm:ss ";
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () =>
        HealthCheckResult.Healthy("Application is running"))
    .AddUrlGroup(new Uri("https://api.openai.com/v1/models"), 
        timeout: TimeSpan.FromSeconds(5),
        name: "openai")
    .AddUrlGroup(new Uri("https://api.coingecko.com/api/v3/ping"),
        timeout: TimeSpan.FromSeconds(5),
        name: "coingecko");

var app = builder.Build();

// Global exception handler
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var logger = context.RequestServices
            .GetRequiredService<ILogger<Program>>();

        var feature = context.Features.Get<IExceptionHandlerFeature>();

        if (feature?.Error is not null)
        {
            logger.LogError(
                feature.Error,
                "Unhandled exception.");
        }

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = app.Environment.IsDevelopment()
                ? feature?.Error.Message
                : "An unexpected error occurred."
        };

        context.Response.StatusCode = problem.Status!.Value;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem);
    });
});

/*
    builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
    });*/

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration.TotalMilliseconds,
                description = entry.Value.Description,
                error = entry.Value.Exception?.Message
            }),
            timestampUtc = DateTimeOffset.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
    }
});

app.MapGet("/", () =>
{
    var version = typeof(Program)
        .Assembly
        .GetName()
        .Version?
        .ToString() ?? "1.0.0";

    return TypedResults.Ok(new
    {
        Name = "Bitcoin Agent",
        Version = version,
        Environment = app.Environment.EnvironmentName,
        UtcTime = DateTimeOffset.UtcNow
    });
});


app.MapPost("/chat", async (
    ChatRequest request,
    BitcoinAgentFactory factory,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new ProblemDetails
        {
            Title = "Validation Error",
            Detail = "Message cannot be empty."
        });
    }

    var agent = factory.Create();

    var answer = await agent.AskAsync(
        request.Message,
        cancellationToken);

    return Results.Ok(new ChatResponse(answer));
})
.WithName("Chat")
.WithOpenApi()
.Produces<ChatResponse>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

app.Run();

public sealed record ChatRequest(string Message);

public sealed record ChatResponse(string Response);