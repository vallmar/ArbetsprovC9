using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CarRental.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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
    public async Task Unexpected_exception_returns_sanitized_500_and_is_logged()
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

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/test/unhandled-error");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "tenant-a");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal("An unexpected error occurred.", body!.Error);
        Assert.DoesNotContain("Intentional test exception", body.Error);
        Assert.DoesNotContain("InvalidOperationException", body.Error);

        Assert.Contains(
            logSink.Entries,
            entry => entry.LogLevel == LogLevel.Error
                      && entry.Message.Contains("Unhandled exception while processing GET /api/test/unhandled-error"));

        Assert.Contains(
            logSink.Entries,
            entry => entry.Exception?.Message == "Intentional test exception.");
    }

    private sealed class TestLogSink
    {
        public List<LogEntry> Entries { get; } = [];
    }

    private sealed record LogEntry(LogLevel LogLevel, string Message, Exception? Exception);

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

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            sink.Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
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
