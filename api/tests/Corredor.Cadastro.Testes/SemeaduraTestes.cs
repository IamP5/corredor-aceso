using System.Text.Json;
using Corredor.Cadastro.Aplicacao;
using Corredor.Cadastro.Testes.Apoio;
using Corredor.Contratos;
using Microsoft.EntityFrameworkCore;

namespace Corredor.Cadastro.Testes;

// A carga da demo, pelos casos de uso: 40 casas, tokens sorteados na carga e uma segunda carga que não muda nada.
public sealed class SemeaduraTestes(BancoDeTeste banco) : IClassFixture<BancoDeTeste>
{
    internal static string CopiarSemente()
    {
        var pasta = Directory.CreateTempSubdirectory("semente-");
        var destino = Path.Combine(pasta.FullName, "casas.json");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Semente", "casas.json"), destino);
        return destino;
    }

    private static async Task<string> RetratoAsync(ApiDeTeste api)
    {
        await using var db = api.NovoContexto();
        var linhas = await db.Idosos.AsNoTracking().OrderBy(i => i.Id)
            .Select(i => new
            {
                i.Id,
                Token = i.Casa.Token,
                Consentimentos = i.Consentimentos.Count,
                AceitouEm = i.Vizinho == null ? (DateTimeOffset?)null : i.Vizinho.AceitouEm,
            })
            .ToListAsync();
        return JsonSerializer.Serialize(linhas);
    }

    [Fact]
    public async Task Carga_entra_com_40_casas_e_a_segunda_carga_nao_muda_nada()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var arquivo = CopiarSemente();

        var primeira = await Semeadura.SemearAsync(api.Servicos, arquivo, TestContext.Current.CancellationToken);
        var depoisDaPrimeira = await RetratoAsync(api);
        api.Relogio.Agora = ApiDeTeste.Inicio.AddHours(1);
        var segunda = await Semeadura.SemearAsync(api.Servicos, arquivo, TestContext.Current.CancellationToken);
        var depoisDaSegunda = await RetratoAsync(api);

        Assert.Equal((40, 0, 26), (primeira.Cadastrados, primeira.Pulados, primeira.VizinhosIndicados));
        Assert.Equal((0, 40, 0), (segunda.Cadastrados, segunda.Pulados, segunda.VizinhosIndicados));
        Assert.Equal(depoisDaPrimeira, depoisDaSegunda);
        await using var db = api.NovoContexto();
        Assert.Equal(40, await db.Idosos.CountAsync(i => i.Casa.Token != null, TestContext.Current.CancellationToken));
        Assert.Equal(26, await db.Idosos.CountAsync(i => i.Vizinho != null, TestContext.Current.CancellationToken));
        Assert.Equal(30, await db.Idosos.CountAsync(i => i.Perfil.Contato == PerfilDeContato.Sozinho, TestContext.Current.CancellationToken));
    }
}

// Classe própria: um banco novo, porque a carga acima já deixaria os 40 idosos.
public sealed class SemeaduraNoMotorTestes(BancoDeTeste banco) : IClassFixture<BancoDeTeste>
{
    [Fact]
    public async Task Carga_ativa_as_casas_e_a_casa_do_teste_de_falha_no_Motor_e_grava_os_tokens_sem_nome()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var arquivo = SemeaduraTestes.CopiarSemente();

        var resultado = await Semeadura.SemearAsync(api.Servicos, arquivo, TestContext.Current.CancellationToken);

        Assert.True(CasaToken.TryParse(Semeadura.CasaDeTestePadrao, out var casaDeTeste));
        Assert.True(api.Motor.Ativas.ContainsKey(casaDeTeste));
        Assert.Equal(41, api.Motor.Ativas.Count);
        var tokens = await File.ReadAllTextAsync(resultado.ArquivoDeTokens, TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(tokens);
        Assert.Equal(40, json.RootElement.GetArrayLength());
        Assert.All(json.RootElement.EnumerateArray(), c => Assert.Matches("^[0-9a-f]{32}$", c.GetProperty("casaToken").GetString()));
        Assert.DoesNotContain("Jatobá", tokens);
        Assert.DoesNotContain("Rua do", tokens);
    }
}
