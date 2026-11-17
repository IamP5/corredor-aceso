using System.Globalization;
using System.Text.Json;
using Corredor.Contratos;
using Corredor.Motor.Dominio;

namespace Corredor.Motor.Infra;

// Camada anticorrupcao do contrato MQTT v1: converte topico e JSON em PacoteDoNo.
// Recusa token fora do formato, tipo desconhecido, campo faltando e corpo maior que 256 bytes.
// O motivo da recusa nunca leva o corpo, que nao vai ao log.
public static class TradutorDePacote
{
    public const int TamanhoMaximo = 256;
    public const string FiltroDeEventos = "corredor/v1/+/evento";

    private const long SeqMaximo = 1L << 53;

    private static readonly Dictionary<string, TipoDePacote> Tipos = new(StringComparer.Ordinal)
    {
        ["MOVIMENTO"] = TipoDePacote.Movimento,
        ["BOTAO_BEM"] = TipoDePacote.BotaoBem,
        ["BOTAO_SAIR"] = TipoDePacote.BotaoSair,
        ["TELEMETRIA"] = TipoDePacote.Telemetria,
        ["HEARTBEAT"] = TipoDePacote.Heartbeat,
    };

    public static string TopicoDeComando(CasaToken token) => $"corredor/v1/{token.Valor}/comando";

    // Com o RelogioDeDemo deslocado, o ts recebe o mesmo deslocamento, e o no continua coerente com o Motor.
    public static PacoteDoNo? Traduzir(string topico, ReadOnlySpan<byte> corpo, TimeSpan deslocamento, out string? recusa)
    {
        recusa = null;
        var partes = topico.Split('/');
        if (partes is not ["corredor", "v1", var textoDoToken, "evento"] || !CasaToken.TryParse(textoDoToken, out var token))
        {
            recusa = "topico fora do contrato";
            return null;
        }

        if (corpo.Length > TamanhoMaximo)
        {
            recusa = "corpo maior que 256 bytes";
            return null;
        }

        try
        {
            var leitor = new Utf8JsonReader(corpo);
            using var json = JsonDocument.ParseValue(ref leitor);
            var raiz = json.RootElement;
            if (raiz.ValueKind != JsonValueKind.Object
                || !raiz.TryGetProperty("seq", out var seq) || !seq.TryGetInt64(out var numero) || numero is < 0 or > SeqMaximo
                || !raiz.TryGetProperty("ts", out var ts) || !LerHora(ts, out var ocorridoEm)
                || !raiz.TryGetProperty("tipo", out var tipo) || tipo.ValueKind != JsonValueKind.String || !Tipos.TryGetValue(tipo.GetString()!, out var tipoDePacote)
                || !raiz.TryGetProperty("bufferizado", out var bufferizado) || bufferizado.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                recusa = "campo obrigatorio ausente ou fora do contrato";
                return null;
            }

            return new PacoteDoNo(token, numero, tipoDePacote, ocorridoEm + deslocamento,
                LerNumero(raiz, "tempC"), LerNumero(raiz, "tensaoV"), bufferizado.GetBoolean());
        }
        catch (JsonException)
        {
            recusa = "corpo nao e JSON";
            return null;
        }
    }

    // ISO 8601 em UTC, com Z.
    private static bool LerHora(JsonElement ts, out DateTimeOffset hora)
    {
        hora = default;
        return ts.ValueKind == JsonValueKind.String
            && ts.GetString() is { } texto
            && texto.EndsWith('Z')
            && DateTimeOffset.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out hora);
    }

    private static decimal? LerNumero(JsonElement raiz, string campo) =>
        raiz.TryGetProperty(campo, out var valor) && valor.ValueKind == JsonValueKind.Number && valor.TryGetDecimal(out var numero)
            ? numero
            : null;
}
