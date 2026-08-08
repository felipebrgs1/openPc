using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OpenPc.Domain.Entities;
using OpenPc.Scraper.Jobs;

namespace OpenPc.Scraper.Tests;

public class ScrapeAlertServiceTests
{
    private static ScrapeJob FailedJob() => new()
    {
        Id = Guid.NewGuid(),
        StoreId = Guid.NewGuid(),
        CategoryId = Guid.NewGuid(),
        ScheduleCron = "0 30 4 * * ?",
        Store = new Store { Id = Guid.NewGuid(), Slug = "kabum", Name = "KaBuM!", BaseUrl = "https://www.kabum.com.br" },
        Category = new Category { Id = Guid.NewGuid(), Slug = "cpu", Name = "Processadores" },
    };

    private static ScrapeRun FailedRun(string error = "timeout na coleta") => new()
    {
        Id = Guid.NewGuid(),
        JobId = Guid.NewGuid(),
        Status = "failed",
        Error = error,
        DurationMs = 1234,
        StartedAt = new DateTime(2026, 8, 8, 10, 0, 0, DateTimeKind.Utc),
        FinishedAt = new DateTime(2026, 8, 8, 10, 0, 5, DateTimeKind.Utc),
    };

    private static ScrapeAlertService BuildService(
        HttpMessageHandler handler, string webhookUrl)
    {
        var http = new HttpClient(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Alerts:WebhookUrl"] = webhookUrl,
            })
            .Build();
        return new ScrapeAlertService(http, config, NullLogger<ScrapeAlertService>.Instance);
    }

    [Fact]
    public async Task RunFailed_ComUrlConfigurada_EnviaPostJson()
    {
        HttpRequestMessage? captured = null;
        string? capturedBody = null;
        var handler = new DelegatingCaptureHandler(async r =>
        {
            captured = r;
            capturedBody = await r.Content!.ReadAsStringAsync();
        });

        var service = BuildService(handler, "https://hooks.example.com/scrape");
        var job = FailedJob();
        await service.SendRunFailedAsync(FailedRun(), job, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("https://hooks.example.com/scrape", captured.RequestUri!.ToString());

        Assert.NotNull(capturedBody);
        using var doc = JsonDocument.Parse(capturedBody!);
        var root = doc.RootElement;
        Assert.Equal("scrape_run_failed", root.GetProperty("event").GetString());
        Assert.Equal("kabum", root.GetProperty("store").GetString());
        Assert.Equal("cpu", root.GetProperty("category").GetString());
        Assert.Equal("failed", root.GetProperty("status").GetString());
        Assert.Equal("timeout na coleta", root.GetProperty("error").GetString());
        Assert.Equal(1234, root.GetProperty("durationMs").GetInt64());
    }

    [Fact]
    public async Task RunFailed_SemUrl_NAoEnviaNada()
    {
        var handler = new DelegatingCaptureHandler(_ => Task.CompletedTask);
        var service = BuildService(handler, "");

        await service.SendRunFailedAsync(FailedRun(), FailedJob(), CancellationToken.None);

        Assert.Null(handler.Captured);
    }

    [Fact]
    public async Task RunFailed_WebhookForaDoAr_NAoLanca()
    {
        var handler = new ThrowingHandler(new HttpRequestException("connection refused"));
        var service = BuildService(handler, "https://hooks.example.com/scrape");

        // O alerta nunca pode derrubar o job de scraping.
        await service.SendRunFailedAsync(FailedRun(), FailedJob(), CancellationToken.None);
    }

    private sealed class DelegatingCaptureHandler(Func<HttpRequestMessage, Task> onRequest) : HttpMessageHandler
    {
        public HttpRequestMessage? Captured { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Captured = request;
            await onRequest(request);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => throw exception;
    }
}
