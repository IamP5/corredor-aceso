using System.Text.Json;
using Corredor.Contratos;

namespace Corredor.Api.Testes;

public class ContratosTestes
{
    [Theory]
    [InlineData("3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3b", true)]
    [InlineData("fa11fa11fa11fa11fa11fa11fa11fa11", true)]
    [InlineData("3F9A1C0E5B7D4A2E8C6F1B0D9E7A5C3B", false)]
    [InlineData("3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3", false)]
    [InlineData("3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3bb", false)]
    [InlineData("3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3g", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void CasaToken_aceita_so_32_hexadecimais_minusculos(string? texto, bool aceito)
    {
        Assert.Equal(aceito, CasaToken.TryParse(texto, out var token));
        if (aceito) Assert.Equal(texto, token.Valor);
    }

    [Fact]
    public void CasaToken_novo_tem_o_formato_do_contrato_e_nao_repete()
    {
        var a = CasaToken.Novo();
        var b = CasaToken.Novo();

        Assert.True(CasaToken.TryParse(a.Valor, out var lido));
        Assert.Equal(a, lido);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Json_leva_token_como_texto_e_microarea_como_numero()
    {
        CasaToken.TryParse("3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3b", out var token);
        var pedido = new { CasaToken = token, MicroareaId = new MicroareaId(3) };
        var web = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var json = JsonSerializer.Serialize(pedido, web);

        Assert.Equal("""{"casaToken":"3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3b","microareaId":3}""", json);
        Assert.Equal(token, JsonSerializer.Deserialize<CasaToken>("\"3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3b\"", web));
    }

    [Theory]
    [InlineData("\"3F9A\"")]
    [InlineData("\"nao-e-token\"")]
    public void Json_recusa_token_fora_do_formato(string json) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CasaToken>(json));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Microarea_recusa_numero_que_nao_e_positivo(int valor)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MicroareaId(valor));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MicroareaId>(valor.ToString()));
    }
}
