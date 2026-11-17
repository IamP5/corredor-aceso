using Corredor.Cadastro.Dominio;
using Corredor.Contratos;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Corredor.Cadastro.Infra;

internal sealed class CasaTokenConverter() : ValueConverter<CasaToken, string>(t => t.Valor, s => Ler(s))
{
    private static CasaToken Ler(string s) =>
        CasaToken.TryParse(s, out var token) ? token : throw new InvalidOperationException("casa_token fora do formato");
}

internal sealed class MicroareaIdConverter() : ValueConverter<MicroareaId, int>(m => m.Valor, v => new MicroareaId(v));

internal sealed class IdosoIdConverter() : ValueConverter<IdosoId, Guid>(i => i.Valor, v => new IdosoId(v));

internal sealed class AcsIdConverter() : ValueConverter<AcsId, string>(a => a.Valor, v => new AcsId(v));

// O Npgsql recusa gravar DateTimeOffset com deslocamento diferente de zero. A rota recebe -03:00.
internal sealed class UtcConverter() : ValueConverter<DateTimeOffset, DateTimeOffset>(v => v.ToUniversalTime(), v => v);

// Trechos de SQL que se repetem em mais de uma tabela.
internal static class Sql
{
    public const string Token = "~ '^[0-9a-f]{32}$'";
    public const string Acs = "~ '^[a-z0-9-]{3,40}$'";
    public const string Cpf = "!~ '[0-9]{3}\\.[0-9]{3}\\.[0-9]{3}-[0-9]{2}'";
    public const string PerfisDeContato = "('Sozinho', 'Acamado', 'Coabitado')";
    public const string Canais = "('Log', 'RespostaSemAutenticacao', 'MensagemDeErro', 'Mqtt', 'GrupoDoBairro', 'PromptDoLlm', 'Vizinho', 'AppAutenticado', 'Banco')";
    public const string TiposDeDado = "ARRAY['Cpf', 'Rg', 'Cns', 'Saude', 'MoraSozinho', 'Endereco', 'Telefone', 'Nome', 'Credencial']";
}
