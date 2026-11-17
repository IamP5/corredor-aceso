using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Corredor.Guarda;

/// <summary>O que o interceptador faz com o achado: troca pela máscara ou só rotula.</summary>
public enum AcaoNaEntrada { Mascarar, Rotular }

public enum Validador { Cpf, Cns }

/// <summary>Como o valor achado vira máscara.</summary>
public enum Mascara
{
    /// <summary>123.456.789-00 vira ***.456.***-00.</summary>
    Cpf,
    /// <summary>12.345.678-9 vira **.***.***-9.</summary>
    Rg,
    /// <summary>Só os quatro últimos dígitos ficam.</summary>
    UltimosQuatro,
    /// <summary>A marca do tipo, como [SAUDE].</summary>
    Rotulo,
}

/// <summary>Padrão, validador, máscara e severidade de um tipo de dado. Vem de Regras/pii.json.</summary>
public sealed record RegraDePii(
    string Nome,
    TipoDeDado Tipo,
    Regex Padrao,
    Validador? Validador,
    Mascara Mascara,
    int Severidade,
    int SeveridadeComValidadorValido,
    AcaoNaEntrada NaEntrada)
{
    public int SeveridadeDe(string valor) =>
        Validador is { } v && Validadores.Valido(v, valor) ? SeveridadeComValidadorValido : Severidade;

    public string Mascarar(string valor) => Mascaras.Aplicar(Mascara, Tipo, valor);
}

/// <summary>As regras carregadas de pii.json mais as chaves de credencial.</summary>
public sealed class RegrasDePii
{
    private static readonly TimeSpan LimiteDoRegex = TimeSpan.FromMilliseconds(250);

    // Declarado antes de Padrao: a inicialização estática segue a ordem do arquivo.
    private static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private RegrasDePii(IReadOnlyList<RegraDePii> regras, IReadOnlySet<string> chavesDeCredencial)
    {
        Regras = regras;
        ChavesDeCredencial = chavesDeCredencial;
        _severidadePorTipo = Enum.GetValues<TipoDeDado>().ToDictionary(
            t => t,
            t => regras.Where(r => r.Tipo == t).Select(r => r.Severidade).DefaultIfEmpty(3).Max());
    }

    private readonly Dictionary<TipoDeDado, int> _severidadePorTipo;

    /// <summary>A severidade de um rótulo quando só o tipo é conhecido, como num TextoRotulado.</summary>
    public int SeveridadeDe(TipoDeDado tipo) => _severidadePorTipo[tipo];

    public IReadOnlyList<RegraDePii> Regras { get; }

    /// <summary>Nomes inteiros de chave JSON cujo valor é credencial. Comparação sem caixa.</summary>
    public IReadOnlySet<string> ChavesDeCredencial { get; }

    public static RegrasDePii Padrao { get; } = DoRecursoEmbutido();

    public static RegrasDePii DoRecursoEmbutido()
    {
        using var fluxo = Assembly.GetExecutingAssembly().GetManifestResourceStream("pii.json")
            ?? throw new InvalidOperationException("Regras/pii.json não está embutido no assembly");
        return Ler(fluxo);
    }

    public static RegrasDePii Ler(Stream json)
    {
        var arquivo = JsonSerializer.Deserialize<ArquivoDeRegras>(json, Opcoes)
            ?? throw new InvalidOperationException("pii.json vazio");
        var regras = arquivo.Regras.Select(r =>
        {
            if (r.Severidade is < 1 or > 5 || r.SeveridadeComValidadorValido is < 1 or > 5)
            {
                throw new InvalidOperationException($"regra {r.Nome}: severidade fora de 1 a 5");
            }

            return new RegraDePii(
                r.Nome,
                r.Tipo,
                new Regex(r.Padrao, RegexOptions.CultureInvariant, LimiteDoRegex),
                r.Validador,
                r.Mascara,
                r.Severidade,
                r.SeveridadeComValidadorValido,
                r.NaEntrada);
        }).ToList();
        return new RegrasDePii(regras, arquivo.ChavesDeCredencial.ToHashSet(StringComparer.OrdinalIgnoreCase));
    }

    private sealed record ArquivoDeRegras(int Versao, string[] ChavesDeCredencial, RegraNoArquivo[] Regras);

    private sealed record RegraNoArquivo(
        string Nome,
        TipoDeDado Tipo,
        string Padrao,
        Validador? Validador,
        Mascara Mascara,
        int Severidade,
        int SeveridadeComValidadorValido,
        AcaoNaEntrada NaEntrada);
}

internal static class Validadores
{
    public static bool Valido(Validador validador, string valor) => validador switch
    {
        Validador.Cpf => CpfValido(Digitos(valor)),
        Validador.Cns => CnsValido(Digitos(valor)),
        _ => false,
    };

    internal static int[] Digitos(string valor) => valor.Where(char.IsAsciiDigit).Select(c => c - '0').ToArray();

    private static bool CpfValido(int[] d)
    {
        if (d.Length != 11 || d.All(x => x == d[0]))
        {
            return false;
        }

        return Digito(d, 9) == d[9] && Digito(d, 10) == d[10];

        static int Digito(int[] d, int n)
        {
            var soma = 0;
            for (var i = 0; i < n; i++)
            {
                soma += d[i] * (n + 1 - i);
            }

            var resto = soma * 10 % 11;
            return resto == 10 ? 0 : resto;
        }
    }

    private static bool CnsValido(int[] d)
    {
        if (d.Length != 15)
        {
            return false;
        }

        var soma = 0;
        for (var i = 0; i < 15; i++)
        {
            soma += d[i] * (15 - i);
        }

        return soma % 11 == 0;
    }
}

internal static class Mascaras
{
    public static string Aplicar(Mascara mascara, TipoDeDado tipo, string valor) => mascara switch
    {
        Mascara.Cpf => Cpf(valor),
        Mascara.Rg => "**.***.***-" + valor[^1],
        Mascara.UltimosQuatro => UltimosQuatro(valor),
        _ => TextoRotulado.Marcador(tipo),
    };

    // Mantém o grupo do meio e o dígito verificador, como no exemplo do professor: ***.456.***-00.
    private static string Cpf(string valor)
    {
        var d = Validadores.Digitos(valor);
        return d.Length == 11 ? $"***.{d[3]}{d[4]}{d[5]}.***-{d[9]}{d[10]}" : TextoRotulado.Marcador(TipoDeDado.Cpf);
    }

    private static string UltimosQuatro(string valor)
    {
        var chars = valor.ToCharArray();
        var digitosVistos = 0;
        for (var i = chars.Length - 1; i >= 0; i--)
        {
            if (!char.IsAsciiDigit(chars[i]))
            {
                continue;
            }

            digitosVistos++;
            if (digitosVistos > 4)
            {
                chars[i] = '*';
            }
        }

        return new string(chars);
    }
}
