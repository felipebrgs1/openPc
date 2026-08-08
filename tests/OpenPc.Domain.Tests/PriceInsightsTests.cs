using OpenPc.Domain.Prices;

namespace OpenPc.Domain.Tests;

public class PriceInsightsTests
{
    private static readonly DateTime Day = new(2026, 8, 8, 0, 0, 0, DateTimeKind.Utc);

    private static List<PriceInsights.DailyPrice> Series(params (int DaysAgo, decimal Price)[] points) =>
        points
            .Select(p => new PriceInsights.DailyPrice(Day.AddDays(-p.DaysAgo), p.Price))
            .OrderBy(s => s.Date)
            .ToList();

    [Fact]
    public void Queda7d_CalculaPercentualContraInicioDaJanela()
    {
        var series = Series((7, 1000m), (0, 800m));
        var insight = PriceInsights.Evaluate(series);

        Assert.NotNull(insight);
        Assert.Equal(800m, insight!.CurrentPrice);
        Assert.Equal(1000m, insight.Price7dAgo);
        Assert.Equal(0.20m, insight.DropPercent7d!.Value, precision: 4);
        Assert.Null(insight.Price24hAgo); // sem ponto na janela de 24h
        Assert.Null(insight.DropPercent24h);
        Assert.False(insight.IsAnomaly);
    }

    [Fact]
    public void Queda24h_UsaJanelaDeUmDia()
    {
        var series = Series((3, 900m), (1, 1000m), (0, 850m));
        var insight = PriceInsights.Evaluate(series);

        Assert.NotNull(insight);
        Assert.Equal(1000m, insight!.Price24hAgo);
        Assert.Equal(0.15m, insight.DropPercent24h!.Value, precision: 4);
        // janela de 7d começa no ponto mais antigo dentro dela: (3, 900)
        Assert.Equal(900m, insight.Price7dAgo);
        Assert.Equal(0.0556m, insight.DropPercent7d!.Value, precision: 4); // (900-850)/900
    }

    [Fact]
    public void BadgeMenorPreco_PrecoAtualEMenorDaSerie_MarcaMaiorJanela()
    {
        // atual (950) é o menor de todos os pontos → badge = janela completa (11 dias)
        var series = Series((10, 1100m), (5, 1000m), (2, 1000m), (0, 950m));
        var insight = PriceInsights.Evaluate(series);

        Assert.NotNull(insight);
        Assert.Equal(11, insight!.LowestInDays);
    }

    [Fact]
    public void BadgeMenorPreco_PrecoSubiu_NaoEMenor()
    {
        var series = Series((1, 1000m), (0, 1200m));
        var insight = PriceInsights.Evaluate(series);

        Assert.NotNull(insight);
        Assert.Equal(0, insight!.LowestInDays); // ontem foi menor → não é o menor
    }

    [Fact]
    public void Anomalia_QuedaAcimaDe15PorcentoVsMediana()
    {
        // mediana dos 30 dias = 1000; atual 800 = queda 20% → anomalia
        var series = Series(
            (30, 1000m), (29, 1000m), (28, 1000m), (27, 1000m), (26, 1000m),
            (25, 1000m), (24, 1000m), (23, 1000m), (22, 1000m), (21, 1000m),
            (20, 1000m), (19, 1000m), (18, 1000m), (17, 1000m), (16, 1000m),
            (15, 1000m), (14, 1000m), (13, 1000m), (12, 1000m), (11, 1000m),
            (10, 1000m), (9, 1000m), (8, 1000m), (7, 1000m), (6, 1000m),
            (5, 1000m), (4, 1000m), (3, 1000m), (2, 1000m), (1, 1000m),
            (0, 800m));
        var insight = PriceInsights.Evaluate(series);

        Assert.NotNull(insight);
        Assert.True(insight!.IsAnomaly);
    }

    [Fact]
    public void SemAnomalia_ComMenosDeTresPontos()
    {
        var series = Series((1, 1000m), (0, 800m));
        var insight = PriceInsights.Evaluate(series);

        Assert.NotNull(insight);
        Assert.False(insight!.IsAnomaly); // dados insuficientes → sem falso positivo
    }

    [Fact]
    public void SemDados_RetornaNull()
    {
        Assert.Null(PriceInsights.Evaluate([]));
    }

    [Fact]
    public void PrecoSubiu_SemQueda_DropNaoPositivo()
    {
        var series = Series((7, 800m), (0, 1000m));
        var insight = PriceInsights.Evaluate(series);

        Assert.NotNull(insight);
        Assert.Equal(-0.25m, insight!.DropPercent7d!.Value, precision: 4);
    }
}
