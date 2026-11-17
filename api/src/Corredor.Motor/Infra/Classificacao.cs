using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Corredor.Motor.Infra;

// A classificacao de cada coluna mora aqui e vira comentario da coluna na migracao.
// Coluna mapeada sem entrada nesta tabela derruba a montagem do modelo.
public static class Classificacao
{
    public static readonly IReadOnlyDictionary<string, string> Motor = new Dictionary<string, string>
    {
        ["casa_monitorada.casa_token"] = "classe=pseudonimizado",
        ["casa_monitorada.microarea_id"] = "classe=pseudonimizado",
        ["casa_monitorada.perfil_contato"] = "classe=pseudonimizado+sensivel",
        ["casa_monitorada.perfil_eletrodependente"] = "classe=pseudonimizado+sensivel",
        ["casa_monitorada.ativada_em"] = "classe=pseudonimizado",
        ["casa_monitorada.desativada_em"] = "classe=pseudonimizado",
        ["casa_monitorada.ultimo_pacote_em"] = "classe=pseudonimizado",
        ["confirmacao.id"] = "classe=pseudonimizado",
        ["confirmacao.casa_token"] = "classe=pseudonimizado",
        ["confirmacao.origem"] = "classe=pseudonimizado",
        ["confirmacao.resultado"] = "classe=pseudonimizado",
        ["confirmacao.quando"] = "classe=pseudonimizado",
        ["evento_sensor.casa_token"] = "classe=pseudonimizado",
        ["evento_sensor.ocorrido_em"] = "classe=pseudonimizado",
        ["evento_sensor.seq"] = "classe=pseudonimizado",
        ["evento_sensor.tipo"] = "classe=pseudonimizado",
        ["evento_sensor.recebido_em"] = "classe=pseudonimizado",
        ["evento_sensor.temp_c"] = "classe=pseudonimizado",
        ["evento_sensor.tensao_v"] = "classe=pseudonimizado",
        ["evento_sensor.bufferizado"] = "classe=pseudonimizado",
        ["dia_critico.data"] = "classe=nenhum",
        ["dia_critico.probabilidade"] = "classe=nenhum",
        ["dia_critico.motivo"] = "classe=nenhum",
        ["dia_critico.origem"] = "classe=nenhum",
        ["dia_critico.aberto_em"] = "classe=nenhum",
        ["dia_critico.toque_pedido_em"] = "classe=nenhum",
        ["dia_critico.encerrado_em"] = "classe=nenhum",
        ["dia_critico.resumo_casas_ativas"] = "classe=nenhum",
        ["dia_critico.resumo_com_sinal_de_vida_ate_10h"] = "classe=nenhum",
        ["dia_critico.resumo_atendidas"] = "classe=nenhum",
        ["dia_critico.resumo_vermelhas_pendentes"] = "classe=nenhum",
        ["dia_critico.resumo_cinzas_pendentes"] = "classe=nenhum",
    };
}

public sealed class ConvencaoDeClassificacao(IReadOnlyDictionary<string, string> classes) : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entidade in modelBuilder.Metadata.GetEntityTypes())
        {
            var tabela = entidade.GetTableName();
            if (tabela is null)
            {
                continue;
            }

            var loja = StoreObjectIdentifier.Table(tabela, entidade.GetSchema());
            var propriedades = entidade.GetDeclaredProperties()
                .Concat(entidade.GetDeclaredComplexProperties().SelectMany(c => c.ComplexType.GetProperties()));
            foreach (var propriedade in propriedades)
            {
                var coluna = propriedade.GetColumnName(loja);
                if (coluna is null or "xmin")
                {
                    continue;
                }

                if (!classes.TryGetValue($"{tabela}.{coluna}", out var classe))
                {
                    throw new InvalidOperationException($"coluna sem classificacao: {tabela}.{coluna}");
                }

                propriedade.Builder.HasComment(classe);
            }
        }
    }
}
