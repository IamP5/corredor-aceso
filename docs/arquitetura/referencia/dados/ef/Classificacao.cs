using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Corredor.Infra;

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

	public static readonly IReadOnlyDictionary<string, string> Guarda = new Dictionary<string, string>
	{
		["evento_de_seguranca.id"] = "classe=nenhum",
		["evento_de_seguranca.ocorrido_em"] = "classe=nenhum",
		["evento_de_seguranca.tipo"] = "classe=nenhum",
		["evento_de_seguranca.tipos_de_dado"] = "classe=nenhum",
		["evento_de_seguranca.canal"] = "classe=nenhum",
		["evento_de_seguranca.modulo"] = "classe=nenhum",
		["evento_de_seguranca.rota"] = "classe=nenhum",
		["evento_de_seguranca.severidade"] = "classe=nenhum",
		["evento_de_seguranca.mensagem_texto"] = "classe=nenhum",
		["evento_de_seguranca.mensagem_origem"] = "classe=nenhum",
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
