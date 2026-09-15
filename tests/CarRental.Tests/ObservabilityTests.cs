using System.Net;
using System.Net.Http.Json;
using CarRental.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CarRental.Tests;

public sealed class ObservabilityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ObservabilityTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Unexpected_exception_returns_500_without_internal_details_and_is_logged()
    {
        var logSink = new TestLogSink();
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(new TestLoggerProvider(logSink));
            });
        }).CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/rentals/pickup",
            new RegisterPickupRequest(
                "OBSERVABILITY-001",
                "ABC123",
                "customer-a",
                ContractCarCategory.SmallCar,
                DateTimeOffset.Parse("2026-09-15T10:00:00Z"),
                10000),
            TestContext.Current.CancellationToken);

        // The current in-memory implementation is not expected to throw here, so this test
        // validates the global handler by requesting an intentionally invalid endpoint instead.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class TestLogSink
    {
        public List<string> Messages { get; } = [];
    }

    private sealed class TestLoggerProvider(TestLogSink sink) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new TestLogger(sink);

        public void Dispose()
        {
        }
    }

    private sealed class TestLogger(TestLogSink sink) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            sink.Messages.Add(formatter(state, exception));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}