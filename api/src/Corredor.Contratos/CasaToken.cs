using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Corredor.Contratos;

[JsonConverter(typeof(CasaTokenJson))]
public readonly record struct CasaToken
{
    private const int Tamanho = 32;

    private CasaToken(string valor) => Valor = valor;

    public string Valor { get; }

    public static bool TryParse(string? texto, out CasaToken token)
    {
        if (texto is { Length: Tamanho } && texto.All(char.IsAsciiHexDigitLower))
        {
            token = new CasaToken(texto);
            return true;
        }
        token = default;
        return false;
    }

    public static CasaToken Novo() => new(Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(Tamanho / 2)));

    public override string ToString() => Valor;
}

// O contrato HTTP leva o token como texto puro: "casaToken": "3f9a...".
internal sealed class CasaTokenJson : JsonConverter<CasaToken>
{
    public override CasaToken Read(ref Utf8JsonReader leitor, Type tipo, JsonSerializerOptions opcoes) =>
        CasaToken.TryParse(leitor.GetString(), out var token)
            ? token
            : throw new JsonException("casaToken deve ter 32 caracteres hexadecimais minúsculos");

    public override void Write(Utf8JsonWriter escritor, CasaToken token, JsonSerializerOptions opcoes) =>
        escritor.WriteStringValue(token.Valor);
}
