using System.Text.Json;
using Corredor.Cadastro.Dominio;
using Corredor.Cadastro.Infra;
using Corredor.Contratos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Corredor.Cadastro.Aplicacao;

// A carga da demo (seção 8.2 de 04-modelo-de-dados.md). Passa pelos casos de uso, não por SQL:
// o token nasce em RegistrarConsentimento, o Motor recebe a ativação pela P1 e nenhum token entra no Git.
// Rodar duas vezes dá o mesmo banco.
public static class Semeadura
{
    // A casa do teste de falha (05-infra-e-entrega.md, seção 5, item 8). Não tem pessoa: só o Motor a conhece.
    public const string CasaDeTestePadrao = "fa11fa11fa11fa11fa11fa11fa11fa11";

    // A casa de teste fica numa microárea sem ACS. Na microárea da demo ela aparecia no radar como um card
    // cinza sem nome, que a ACS não tem como visitar, e entrava no resumo do turno como a 41ª casa.
    public const int MicroareaDaCasaDeTeste = 999;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public sealed record Resultado(int Cadastrados, int Pulados, int VizinhosIndicados, string ArquivoDeTokens);

    public static async Task<Resultado> SemearAsync(IServiceProvider servicos, string arquivo, CancellationToken ct)
    {
        var semente = JsonSerializer.Deserialize<ArquivoDaSemente>(await File.ReadAllTextAsync(arquivo, ct), Json)
            ?? throw new InvalidDataException("semente vazia");
        var microarea = new MicroareaId(semente.MicroareaId);
        var log = servicos.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(Semeadura));

        int cadastrados = 0, pulados = 0, vizinhos = 0;
        var tokens = new List<object>();
        foreach (var casa in semente.Casas)
        {
            await using var escopo = servicos.CreateAsyncScope();
            var db = escopo.ServiceProvider.GetRequiredService<CadastroDbContext>();
            var casos = escopo.ServiceProvider.GetRequiredService<CasosDoIdoso>();
            var relogio = escopo.ServiceProvider.GetRequiredService<TimeProvider>();
            var id = new IdosoId(casa.Id);

            var existia = await db.Idosos.AnyAsync(i => i.Id == id, ct);
            await casos.CadastrarAsync(
                new NovoIdoso(microarea, casa.Nome, casa.Endereco, casa.Perfil, casa.Condicoes, casa.Medicamentos, id), ct);
            _ = existia ? pulados++ : cadastrados++;

            var acs = await db.Microareas.Where(m => m.Id == microarea).Select(m => m.AcsResponsavel).SingleAsync(ct);
            var token = await casos.RegistrarConsentimentoAsync(id, acs, semente.VersaoDoTermo, casa.Autorizacoes, ct);

            // Indicar de novo trocaria aceitouEm. A carga só indica quem ainda não tem vizinho, para a segunda carga não mudar nada.
            var temVizinho = await db.Idosos.Where(i => i.Id == id).Select(i => i.Vizinho != null).SingleAsync(ct);
            if (casa.Vizinho is { } vizinho && casa.Autorizacoes.AvisarVizinho && !temVizinho)
            {
                await casos.IndicarVizinhoAsync(id, vizinho.Nome, vizinho.Telefone, relogio.GetUtcNow(), ct);
                vizinhos++;
            }

            tokens.Add(new { n = casa.N, casaToken = token.Valor, papelNaDemo = casa.PapelNaDemo });
        }

        await AtivarCasaDeTesteAsync(servicos, ct);

        // Fica ao lado da semente, fora do Git pelo .gitignore da pasta. O simulador das 39 casas lê este arquivo.
        var arquivoDeTokens = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(arquivo))!, "tokens.local.json");
        var temporario = arquivoDeTokens + ".tmp";
        await File.WriteAllTextAsync(temporario, JsonSerializer.Serialize(tokens, new JsonSerializerOptions { WriteIndented = true }) + "\n", ct);
        File.Move(temporario, arquivoDeTokens, overwrite: true);

        log.LogInformation(
            "semente: {Cadastrados} idosos cadastrados, {Pulados} já existiam, {Vizinhos} vizinhos indicados, {Total} tokens",
            cadastrados, pulados, vizinhos, tokens.Count);
        return new Resultado(cadastrados, pulados, vizinhos, arquivoDeTokens);
    }

    // O teste de falha publica na casa CASA_TESTE_TOKEN e lê a linha do tempo dela. AtivarCasa converge.
    private static async Task AtivarCasaDeTesteAsync(IServiceProvider servicos, CancellationToken ct)
    {
        var texto = servicos.GetRequiredService<IConfiguration>()["CASA_TESTE_TOKEN"] ?? CasaDeTestePadrao;
        if (!CasaToken.TryParse(texto, out var token))
        {
            throw new InvalidDataException("CASA_TESTE_TOKEN fora do formato");
        }

        await using var escopo = servicos.CreateAsyncScope();
        var motor = escopo.ServiceProvider.GetRequiredService<IMotorDeCasas>();
        await motor.AtivarCasaAsync(token, new MicroareaId(MicroareaDaCasaDeTeste), new PerfilDaCasa(PerfilDeContato.Sozinho, false), ct);
    }

    private sealed record ArquivoDaSemente(int MicroareaId, string VersaoDoTermo, IReadOnlyList<CasaDaSemente> Casas);

    private sealed record CasaDaSemente(
        int N, Guid Id, string PapelNaDemo, string Nome, string Endereco, PerfilDaCasa Perfil,
        string? Condicoes, string? Medicamentos, Autorizacoes Autorizacoes, VizinhoDaSemente? Vizinho);

    private sealed record VizinhoDaSemente(string Nome, string Telefone);
}
