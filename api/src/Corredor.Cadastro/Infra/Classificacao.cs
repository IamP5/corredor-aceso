using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Corredor.Cadastro.Infra;

// A classificação de cada coluna (seção 5 de 04-modelo-de-dados.md). Vira COMMENT ON COLUMN pela migração.
internal static class Classificacao
{
    public static readonly IReadOnlyDictionary<string, string> Cadastro = new Dictionary<string, string>
    {
        ["microarea.id"] = "classe=nenhum",
        ["microarea.nome"] = "classe=nenhum",
        ["microarea.acs_responsavel"] = "classe=pessoal",
        ["idoso.id"] = "classe=pessoal",
        ["idoso.microarea_id"] = "classe=nenhum",
        ["idoso.nome"] = "classe=pessoal; rotulo=Nome",
        ["idoso.condicoes"] = "classe=sensivel; rotulo=Saude",
        ["idoso.medicamentos"] = "classe=sensivel; rotulo=Saude",
        ["idoso.perfil_contato"] = "classe=sensivel",
        ["idoso.perfil_eletrodependente"] = "classe=sensivel",
        ["idoso.casa_endereco"] = "classe=pessoal; rotulo=Endereco",
        ["idoso.casa_token"] = "classe=pessoal",
        ["consentimento.id"] = "classe=pessoal",
        ["consentimento.idoso_id"] = "classe=pessoal",
        ["consentimento.colhido_por"] = "classe=pessoal",
        ["consentimento.colhido_em"] = "classe=pessoal",
        ["consentimento.versao_do_termo"] = "classe=pessoal",
        ["consentimento.autorizacoes_avisar_vizinho"] = "classe=pessoal",
        ["consentimento.autorizacoes_avisar_familia"] = "classe=pessoal",
        ["consentimento.revogado_em"] = "classe=pessoal",
        ["vizinho_de_confianca.idoso_id"] = "classe=pessoal",
        ["vizinho_de_confianca.nome"] = "classe=pessoal; rotulo=Nome",
        ["vizinho_de_confianca.telefone"] = "classe=pessoal; rotulo=Telefone",
        ["vizinho_de_confianca.aceitou_em"] = "classe=pessoal",
        ["visita.id"] = "classe=sensivel",
        ["visita.idoso_id"] = "classe=sensivel",
        ["visita.acs_id"] = "classe=pessoal",
        ["visita.ocorrida_em"] = "classe=sensivel",
        ["visita.registrada_em"] = "classe=sensivel",
        ["visita.desfecho"] = "classe=sensivel",
        ["visita.observacao"] = "classe=sensivel; rotulo=Saude+MoraSozinho+Nome+Endereco+Telefone",
        ["mensagem_saida.id"] = "classe=nenhum",
        ["mensagem_saida.canal"] = "classe=nenhum",
        ["mensagem_saida.acs_id"] = "classe=pessoal",
        ["mensagem_saida.texto_mascarado"] = "classe=pessoal; mascarado",
        ["mensagem_saida.veredito"] = "classe=nenhum",
        ["mensagem_saida.tipos_de_dado"] = "classe=nenhum",
        ["mensagem_saida.verificada_em"] = "classe=nenhum",
        ["aviso_ao_vizinho.id"] = "classe=pseudonimizado",
        ["aviso_ao_vizinho.casa_token"] = "classe=pseudonimizado",
        ["aviso_ao_vizinho.microarea_id"] = "classe=nenhum",
        ["aviso_ao_vizinho.motivo"] = "classe=pseudonimizado+sensivel",
        ["aviso_ao_vizinho.desde"] = "classe=pseudonimizado",
        ["aviso_ao_vizinho.registrado_em"] = "classe=pseudonimizado",
        ["aviso_ao_vizinho.resposta"] = "classe=pseudonimizado",
        ["aviso_ao_vizinho.respondido_em"] = "classe=pseudonimizado",
    };
}

// Uma coluna mapeada sem classificação derruba a montagem do modelo.
internal sealed class ConvencaoDeClassificacao(IReadOnlyDictionary<string, string> classes) : IModelFinalizingConvention
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
