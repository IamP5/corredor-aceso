using System.Text.Json;
using System.Text.Json.Serialization;

namespace Corredor.Contratos;

[JsonConverter(typeof(MicroareaIdJson))]
public readonly record struct MicroareaId(int Valor)
{
    public int Valor { get; } = Valor > 0 ? Valor : throw new ArgumentOutOfRangeException(nameof(Valor), "a microárea é um número maior que zero");

    public override string ToString() => Valor.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

// O contrato HTTP leva a microárea como número: "microareaId": 3.
internal sealed class MicroareaIdJson : JsonConverter<MicroareaId>
{
    public override MicroareaId Read(ref Utf8JsonReader leitor, Type tipo, JsonSerializerOptions opcoes)
    {
        var valor = leitor.GetInt32();
        return valor > 0 ? new MicroareaId(valor) : throw new JsonException("microareaId deve ser maior que zero");
    }

    public override void Write(Utf8JsonWriter escritor, MicroareaId microarea, JsonSerializerOptions opcoes) =>
        escritor.WriteNumberValue(microarea.Valor);
}
