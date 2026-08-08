using Microsoft.Playwright;

namespace OpenPc.Scraper.Collectors;

/// <summary>
/// Pool de browsers Playwright (Chromium completo, não headless-shell — o
/// Cloudflare detecta o shell). O browser é iniciado uma vez e reutilizado
/// entre coletas: o desafio do Cloudflare é resolvido uma vez por sessão.
/// </summary>
public sealed class BrowserPool : IAsyncDisposable
{
    private const string UserAgent =
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _disposed;

    public async Task<IPage> NewPageAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _gate.WaitAsync();
        try
        {
            _playwright ??= await Playwright.CreateAsync();
            _browser ??= await _playwright.Chromium.LaunchAsync(new()
            {
                Headless = true,
                Channel = "chromium",
                Args = ["--disable-blink-features=AutomationControlled"],
            });
        }
        finally
        {
            _gate.Release();
        }

        return await _browser.NewPageAsync(new() { UserAgent = UserAgent });
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (_browser is not null)
            await _browser.CloseAsync();
        if (_playwright is not null)
            _playwright.Dispose();
        _gate.Dispose();
    }
}
