using System.Text.RegularExpressions;
using OpenPc.Scraper.Collectors;

namespace OpenPc.Scraper.Tests;

public class KabumPageParserTests
{
    [Fact]
    public void ParseListingsPage_ExtraiProdutosReaisDaFixture()
    {
        var html = File.ReadAllText("fixtures/kabum-listings.html");
        var items = KabumPageParser.ParseListingsPage(html);

        Assert.Equal(3, items.Count);
        var first = items[0];
        Assert.True(first.Code > 0);
        Assert.Contains("Ryzen", first.Title, StringComparison.OrdinalIgnoreCase);
        Assert.True(first.Price > 0);
        Assert.NotNull(first.FriendlyName);
    }

    [Fact]
    public void ParseListingsPage_SemNextData_Lanca()
    {
        Assert.Throws<InvalidOperationException>(() => KabumPageParser.ParseListingsPage("<html><body>oi</body></html>"));
    }
}

public class CardListingBuilderTests
{
    private static readonly Regex PichauPrice = new(@"por\s*\|?\s*R\$\s*([\d.]+,[\d]{2})", RegexOptions.IgnoreCase);
    private static readonly Regex TerabytePrice = new(@"por:\s*\|?\s*R\$\s*([\d.]+,[\d]{2})", RegexOptions.IgnoreCase);

    [Fact]
    public void Pichau_CardReal_ExtraiNomePrecoPartNumber()
    {
        // texto real capturado no spike M1 (card do Ryzen 5 7600X3D)
        var card = "20% | OFF | 60 | UNID | Frete Grátis: Sul e Sudeste | " +
                   "Processador AMD Ryzen 5 7600X3D, 6-Core, 12-Threads, 4.1GHz (4.7GHz Turbo), " +
                   "Cache 102MB, AM5, 100-100001721WOF | de R$ 2.352,93 por | R$ 1.599,99 | À vista | " +
                   "15% de desconto no PIX | R$ 1.882,34 | Em até 12x de R$ 156,86 | Sem juros no cartão";
        var href = "https://www.pichau.com.br/processador-amd-ryzen-5-7600x3d-6-core-12-threads-4-1ghz-4-7ghz-turbo-cache-102mb-am5-100-100001721wof";

        var listing = CardListingBuilder.Build(
            href, card, "cpu", PichauPrice, "de r$",
            h => h.TrimEnd('/').Split('/').Last());

        Assert.NotNull(listing);
        Assert.Equal(1599.99m, listing!.PriceCash);
        Assert.Equal(12, listing.Installments);
        Assert.True(listing.InStock);
        Assert.Equal("100100001721WOF", listing.PartNumber);
        Assert.Equal("amd 7600x3d", listing.MatchKey);
        Assert.Equal("am5", listing.Specs["socket"]);
        Assert.Contains("7600X3D", listing.Title);
    }

    [Fact]
    public void Terabyte_CardReal_ExtraiPreco()
    {
        // texto real capturado no spike M1 (card do Ryzen 5 5600GT)
        var card = " Frete grátis | 2º Mais vendido | " +
                   "Processador AMD Ryzen 5 5600GT, 3.6GHz (4.6GHz Turbo), 6-Cores 12-Threads, " +
                   "Cooler Wraith Stealth, AM4, 10 | (277) | De: R$ 1.265,90 por: | R$ 879,99 | " +
                   "à vista no Pix | 12x de R$ 86,27 sem juros no cartão | -30%";
        var href = "https://www.terabyteshop.com.br/produto/27314/processador-amd-ryzen-5-5600gt-36ghz-46ghz-turbo-6-cores-12-threads-cooler-wraith-stealth-am4-100-100001488box";

        var listing = CardListingBuilder.Build(
            href, card, "cpu", TerabytePrice, "de:",
            h => Regex.Match(h, @"/produto/(\d+)").Groups[1].Value);

        Assert.NotNull(listing);
        Assert.Equal(879.99m, listing!.PriceCash);
        Assert.Equal(12, listing.Installments);
        Assert.Equal("27314", listing.StoreSku);
        Assert.Equal("amd 5600gt", listing.MatchKey);
    }

    [Fact]
    public void SemPreco_RetornaNull()
    {
        var listing = CardListingBuilder.Build(
            "https://www.pichau.com.br/processador-x", "Produto sem preço visível", "cpu",
            PichauPrice, "de r$", h => h);
        Assert.Null(listing);
    }
}
