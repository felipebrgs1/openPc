using OpenPc.Scraper.Ingest;

namespace OpenPc.Scraper.Tests;

public class ImageKeysTests
{
    [Fact]
    public void KeyFor_MesmaUrl_MesmaChave()
    {
        const string url = "https://img.terabyteshop.com.br/produto/p/placa-mae-asrock.jpg";
        Assert.Equal(ImageKeys.KeyFor(url), ImageKeys.KeyFor(url));
    }

    [Fact]
    public void KeyFor_UrlsDiferentes_ChavesDiferentes()
    {
        Assert.NotEqual(
            ImageKeys.KeyFor("https://img.terabyteshop.com.br/a.jpg"),
            ImageKeys.KeyFor("https://img.terabyteshop.com.br/b.jpg"));
    }

    [Theory]
    [InlineData("https://img.x.com/foto.jpg", ".jpg")]
    [InlineData("https://img.x.com/foto.JPEG", ".jpeg")]
    [InlineData("https://img.x.com/foto.png?w=100", ".png")]
    [InlineData("https://img.x.com/foto.webp", ".webp")]
    [InlineData("https://img.x.com/foto-sem-extensao", ".img")]
    public void KeyFor_ExtensaoVemDaUrl(string url, string expectedExt)
    {
        var key = ImageKeys.KeyFor(url);
        Assert.EndsWith(expectedExt, key);
        Assert.Equal(40 + expectedExt.Length, key.Length); // 40 hex + ext
    }

    [Fact]
    public void PublicUrl_CaminhoRelativoPorPadrao()
    {
        Assert.Equal("/images/abc.jpg", ImageKeys.PublicUrl(null, "abc.jpg"));
        Assert.Equal("/images/abc.jpg", ImageKeys.PublicUrl("", "abc.jpg"));
        Assert.Equal("/images/abc.jpg", ImageKeys.PublicUrl("/images", "abc.jpg"));
        Assert.Equal("/imgs/abc.jpg", ImageKeys.PublicUrl("/imgs/", "abc.jpg"));
    }
}
