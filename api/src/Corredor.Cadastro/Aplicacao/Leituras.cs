using Corredor.Cadastro.Dominio;
using Corredor.Cadastro.Infra;
using Corredor.Contratos;
using Corredor.Guarda;
using Microsoft.EntityFrameworkCore;

namespace Corredor.Cadastro.Aplicacao;

public sealed record VizinhoDaLista(string Nome, string Telefone);

public sealed record ItemDaListaDeNomes(CasaToken CasaToken, string Nome, string Endereco, VizinhoDaLista? Vizinho);

public sealed class Leituras(CadastroDbContext db, ISaidaSegura saida, IMotorDeParsing parsing, TimeProvider relogio)
{
    // Só idoso com token ativo. Cada campo sai pela SaidaSegura no canal AppAutenticado.
    // Condições e medicamentos não entram na projeção, e o SQL lê cinco colunas.
    public async Task<IReadOnlyList<ItemDaListaDeNomes>> ListaDeNomesAsync(MicroareaId microarea, CancellationToken ct)
    {
        var linhas = await db.Idosos.AsNoTracking()
            .Where(i => i.Microarea == microarea && i.Casa.Token != null)
            .Select(i => new { i.Casa.Token, i.Nome, i.Casa.Endereco, NomeDoVizinho = i.Vizinho!.Nome, TelefoneDoVizinho = i.Vizinho!.Telefone })
            .ToListAsync(ct);

        return linhas
            .Select(l => new ItemDaListaDeNomes(
                l.Token!.Value,
                Sair(l.Nome),
                Sair(l.Endereco),
                l.NomeDoVizinho is null || l.TelefoneDoVizinho is null
                    ? null
                    : new VizinhoDaLista(Sair(l.NomeDoVizinho), Sair(l.TelefoneDoVizinho))))
            .OrderBy(i => i.Nome, StringComparer.Ordinal)
            .ToList();
    }

    // Os avisos das últimas 24 horas da microárea. "Hoje" poria o fuso de São Paulo no Cadastro.
    public async Task<IReadOnlyList<AvisoAoVizinho>> AvisosDoDiaAsync(MicroareaId microarea, CancellationToken ct)
    {
        var desde = relogio.GetUtcNow().AddHours(-24);
        return await db.Avisos.AsNoTracking()
            .Where(a => a.Microarea == microarea && a.RegistradoEm >= desde)
            .OrderBy(a => a.RegistradoEm)
            .ToListAsync(ct);
    }

    // Cada verificação é uma tentativa nova. O texto original nunca é gravado.
    public async Task<MensagemSaida> VerificarMensagemAsync(string texto, Canal canal, AcsId acs, CancellationToken ct)
    {
        var mensagem = MensagemSaida.Verificar(parsing.Rotular(texto).Texto, canal, acs, saida, relogio.GetUtcNow());
        db.Mensagens.Add(mensagem);
        await db.SaveChangesAsync(ct);
        return mensagem;
    }

    private string Sair(TextoRotulado texto) =>
        saida.Liberar(texto, Canal.AppAutenticado) switch
        {
            Veredito.Liberada l => l.Texto,
            Veredito.Bloqueada b => b.TextoMascarado,
            _ => texto.ToString(),
        };
}
