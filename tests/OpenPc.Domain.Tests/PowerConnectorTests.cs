using OpenPc.Domain.Compatibility;

namespace OpenPc.Domain.Tests;

public class PowerConnectorTests
{
    [Theory]
    [InlineData("2x8 pinos", 2, 0, 0)]
    [InlineData("2 x 8 pinos", 2, 0, 0)]
    [InlineData("1x16 pinos", 0, 0, 1)]
    [InlineData("16 pinos", 0, 0, 1)]        // sem contagem explícita
    [InlineData("8 pinos", 1, 0, 0)]
    [InlineData("1x12vhpwr", 0, 0, 1)]
    [InlineData("1x12v-2x6", 0, 0, 1)]
    [InlineData("3 x 8 pinos", 3, 0, 0)]
    [InlineData("2x6+2 pinos", 2, 0, 0)]     // 6+2 = 8-pin
    [InlineData("1x6 pinos", 0, 1, 0)]
    public void Parse_ContaConectores(string raw, int eight, int six, int sixteen)
    {
        var parsed = PowerConnectorSet.Parse(raw);

        Assert.NotNull(parsed);
        Assert.Equal(eight, parsed!.Value.EightPin);
        Assert.Equal(six, parsed.Value.SixPin);
        Assert.Equal(sixteen, parsed.Value.SixteenPin);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("conector proprietário")]
    public void Parse_IrreconhecivelRetornaNull(string? raw)
    {
        Assert.Null(PowerConnectorSet.Parse(raw));
    }

    [Fact]
    public void Satisfies_FonteComMaisConectoresAtende()
    {
        var psu = new PowerConnectorSet(EightPin: 3, SixPin: 0, SixteenPin: 0);

        Assert.True(psu.Satisfies(new PowerConnectorSet(2, 0, 0)));
        Assert.False(psu.Satisfies(new PowerConnectorSet(4, 0, 0)));
        Assert.False(psu.Satisfies(new PowerConnectorSet(0, 0, 1))); // 16-pin não sai de 8-pin (sem adaptador)
    }

    [Fact]
    public void Satisfies_Gpu6PinAceita6Ou8PinDaFonte()
    {
        var gpu = new PowerConnectorSet(0, 1, 0);

        Assert.True(new PowerConnectorSet(0, 1, 0).Satisfies(gpu));
        Assert.True(new PowerConnectorSet(1, 0, 0).Satisfies(gpu));
        Assert.False(new PowerConnectorSet(0, 0, 1).Satisfies(gpu));
    }

    [Fact]
    public void Satisfies_Gpu16PinExige16PinNaFonte()
    {
        var gpu = new PowerConnectorSet(0, 0, 1);

        Assert.True(new PowerConnectorSet(0, 0, 1).Satisfies(gpu));
        Assert.False(new PowerConnectorSet(3, 0, 0).Satisfies(gpu));
    }
}
