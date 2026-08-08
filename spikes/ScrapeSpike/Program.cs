using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

if (args.Length > 0 && args[0] == "playwright")
{
    await PlaywrightProbe.RunAsync();
    return;
}

// Spike M1 — Kabum via HTTP puro (sem browser):
// valida a extração em volume da listagem via __NEXT_DATA__ e mede
// taxa de sucesso + timing. NÃO é o collector de produção (M2).

const string CategoryUrl = "https://www.kabum.com.br/hardware/processadores?page_number={0}";
const string UserAgent =
    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36";

using var http = new HttpClient();
http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
http.Timeout = TimeSpan.FromSeconds(25);

var nextDataRegex = new Regex(
    "<script id=\"__NEXT_DATA__\" type=\"application/json\">(.*?)</script>",
    RegexOptions.Singleline | RegexOptions.Compiled);

var products = new List<KabumProduct>();
var failures = new List<(int Page, string Error)>();
var sw = Stopwatch.StartNew();

for (var page = 1; page <= 3; page++)
{
    var perRequest = Stopwatch.StartNew();
    try
    {
        using var resp = await http.GetAsync(string.Format(CategoryUrl, page));
        resp.EnsureSuccessStatusCode();
        var html = await resp.Content.ReadAsStringAsync();

        var m = nextDataRegex.Match(html);
        if (!m.Success)
        {
            failures.Add((page, "sem __NEXT_DATA__ (bloqueio?)"));
            continue;
        }

        using var doc = JsonDocument.Parse(m.Groups[1].Value);
        var data = doc.RootElement
            .GetProperty("props").GetProperty("pageProps").GetProperty("data")
            .GetString(); // JSON duplamente codificado
        using var inner = JsonDocument.Parse(data!);
        var list = inner.RootElement
            .GetProperty("catalogServer").GetProperty("data")
            .EnumerateArray().ToList();

        foreach (var p in list)
        {
            products.Add(new KabumProduct(
                Code: p.GetProperty("code").GetInt32(),
                Name: p.GetProperty("name").GetString() ?? "",
                Price: p.GetProperty("price").GetDecimal(),
                OldPrice: p.GetProperty("oldPrice").GetDecimal(),
                Installments: p.GetProperty("maxInstallment").GetString() ?? "",
                Thumbnail: p.GetProperty("thumbnail").GetString() ?? "",
                Available: p.GetProperty("available").GetBoolean(),
                Manufacturer: p.GetProperty("manufacturer").GetProperty("name").GetString() ?? "",
                FriendlyName: p.GetProperty("friendlyName").GetString() ?? ""));
        }

        Console.WriteLine($"página {page}: {list.Count} produtos em {perRequest.ElapsedMilliseconds} ms");
    }
    catch (Exception ex)
    {
        failures.Add((page, ex.Message));
    }

    Thread.Sleep(Random.Shared.Next(1500, 2500)); // delay conservador entre páginas
}

sw.Stop();

Console.WriteLine();
Console.WriteLine($"=== RESUMO KABUM (HTTP) ===");
Console.WriteLine($"produtos coletados: {products.Count}");
Console.WriteLine($"falhas: {failures.Count} {string.Join("; ", failures.Select(f => $"p{f.Page}: {f.Error}"))}");
Console.WriteLine($"tempo total: {sw.Elapsed.TotalSeconds:F1}s | {products.Count / Math.Max(sw.Elapsed.TotalSeconds, 0.1):F1} prod/s");
Console.WriteLine($"sucesso (%): {100.0 * products.Count / (products.Count + failures.Count):F1}%");

var sample = products.Where(p => p.Available).FirstOrDefault(p => p.Name.Contains("7600"));
if (sample is not null)
    Console.WriteLine($"amostra: {sample.Code} | {sample.Name[..60]} | R$ {sample.Price:F2} | {sample.Manufacturer}");

record KabumProduct(int Code, string Name, decimal Price, decimal OldPrice,
    string Installments, string Thumbnail, bool Available, string Manufacturer, string FriendlyName);
