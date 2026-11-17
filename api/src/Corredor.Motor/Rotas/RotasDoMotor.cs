using Corredor.Contratos;
using Corredor.Motor.Aplicacao;
using Corredor.Motor.Dominio;
using Corredor.Motor.Infra;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Corredor.Motor.Rotas;

// O contrato HTTP da secao 6.3. As horas saem com o fuso de Sao Paulo, -03:00.
public sealed record DeclaracaoDoDiaCritico(decimal Probabilidade, string Motivo, string Origem);

public sealed record DiaCriticoResposta(DateOnly Data, DateTimeOffset AbertoEm, decimal Probabilidade, string Motivo, string Origem, DateTimeOffset? EncerradoEm);

public sealed record EncerramentoResposta(DateOnly Data, DateTimeOffset EncerradoEm, ResumoDoTurno Resumo);

public sealed record CardResposta(CasaToken CasaToken, SituacaoDaCasa Situacao, MotivoDoAlerta Motivo, DateTimeOffset Desde, bool Atendida, PerfilDaCasa Perfil, DateTimeOffset? UltimoPacoteEm);

public sealed record RadarResposta(bool TurnoAberto, DateTimeOffset Agora, IReadOnlyList<CardResposta> Pendentes, IReadOnlyList<CardResposta> Atendidas, IReadOnlyList<CasaAguardando> Aguardando, int Verdes);

public sealed record EventoResposta(long Seq, string Tipo, DateTimeOffset OcorridoEm, DateTimeOffset RecebidoEm, decimal? TempC, decimal? TensaoV, bool Bufferizado);

public sealed record ConfirmacaoResposta(OrigemDaConfirmacao Origem, ResultadoDaConfirmacao Resultado, DateTimeOffset Quando);

public sealed record LinhaDoTempoResposta(CasaToken CasaToken, DateTimeOffset? UltimoPacoteEm, IReadOnlyList<EventoResposta> Eventos, IReadOnlyList<ConfirmacaoResposta> Confirmacoes);

public sealed record PedidoDeRelogio(DateTimeOffset Agora);

public sealed record RelogioResposta(DateTimeOffset Agora, long DeslocamentoEmSegundos);

public static class RotasDoMotor
{
    private static readonly Dictionary<string, OrigemDoDiaCritico> Origens = new(StringComparer.Ordinal)
    {
        ["MODELO"] = OrigemDoDiaCritico.Modelo,
        ["REGRA_BASE"] = OrigemDoDiaCritico.RegraBase,
        ["MANUAL"] = OrigemDoDiaCritico.Manual,
    };

    private static readonly Dictionary<TipoDePacote, string> TiposNoContrato = new()
    {
        [TipoDePacote.Movimento] = "MOVIMENTO",
        [TipoDePacote.BotaoBem] = "BOTAO_BEM",
        [TipoDePacote.BotaoSair] = "BOTAO_SAIR",
        [TipoDePacote.Telemetria] = "TELEMETRIA",
    };

    // O job de ML e a equipe da demo chamam sem token de ACS. As outras rotas seguem a politica do host.
    public static IEndpointRouteBuilder MapearRotas(this IEndpointRouteBuilder rotas, bool comRelogioDeDemo)
    {
        var motor = rotas.MapGroup("").WithTags("Motor");

        motor.MapPut("/dias-criticos/{data}", async (DateOnly data, DeclaracaoDoDiaCritico corpo, DeclararDiaCritico caso, CancellationToken ct) =>
            Resposta(await caso.ExecutarAsync(data, Previsao(corpo), ct)))
            .AllowAnonymous();

        // Com ?microarea=, o resumo da resposta conta so as casas da microarea, como o radar. Sem, o resumo gravado do dia.
        motor.MapPost("/dias-criticos/{data}/encerramento", async (DateOnly data, int? microarea, EncerrarTurno caso, CancellationToken ct) =>
        {
            var dia = await caso.ExecutarAsync(data, ct);
            var resumo = microarea is { } m ? await caso.ResumirMicroareaAsync(dia, Microarea(m), ct) : dia.Resumo!;
            return new EncerramentoResposta(dia.Data, Local(dia.EncerradoEm!.Value), resumo);
        });

        motor.MapGet("/radar", async (int microarea, ConsultarRadar caso, CancellationToken ct) =>
        {
            var radar = await caso.ExecutarAsync(Microarea(microarea), ct);
            return new RadarResposta(radar.TurnoAberto, Local(radar.Agora), radar.Pendentes.Select(Card).ToList(),
                radar.Atendidas.Select(Card).ToList(), radar.Aguardando, radar.Verdes);
        });

        motor.MapGet("/casas/{token}/linha-do-tempo", async (string token, ConsultarLinhaDoTempo caso, CancellationToken ct) =>
        {
            var casa = CasaToken.TryParse(token, out var t) ? t : throw Violacoes.CasaDesconhecida();
            var linha = await caso.ExecutarAsync(casa, ct);
            return new LinhaDoTempoResposta(
                linha.CasaToken,
                Local(linha.UltimoPacoteEm),
                linha.Eventos.Select(e => new EventoResposta(e.Seq, TiposNoContrato[e.Tipo], Local(e.OcorridoEm), Local(e.RecebidoEm), e.TempC?.Valor, e.TensaoV?.Valor, e.Bufferizado)).ToList(),
                linha.Confirmacoes.Select(c => new ConfirmacaoResposta(c.Origem, c.Resultado, Local(c.Quando))).ToList());
        });

        if (comRelogioDeDemo)
        {
            motor.MapPut("/demo/relogio", (PedidoDeRelogio corpo, RelogioDeDemo relogio) =>
            {
                var deslocamento = relogio.MoverPara(corpo.Agora);
                return new RelogioResposta(Local(relogio.GetUtcNow()), (long)Math.Round(deslocamento.TotalSeconds));
            }).AllowAnonymous();
        }

        return rotas;
    }

    private static PrevisaoDoDia Previsao(DeclaracaoDoDiaCritico corpo) =>
        new(new Probabilidade(corpo.Probabilidade), corpo.Motivo ?? "",
            Origens.TryGetValue(corpo.Origem ?? "", out var origem) ? origem : throw Violacoes.CampoInvalido("A origem aceita MODELO, REGRA_BASE e MANUAL."));

    private static MicroareaId Microarea(int valor) =>
        valor > 0 ? new MicroareaId(valor) : throw Violacoes.CampoInvalido("A microarea e um numero maior que zero.");

    private static DiaCriticoResposta Resposta(DiaCritico dia) =>
        new(dia.Data, Local(dia.AbertoEm), dia.Probabilidade.Valor, dia.Motivo,
            Origens.Single(o => o.Value == dia.Origem).Key, Local(dia.EncerradoEm));

    private static CardResposta Card(CardDoRadar c) =>
        new(c.CasaToken, c.Situacao, c.Motivo, Local(c.Desde), c.Atendida, c.Perfil, Local(c.UltimoPacoteEm));

    private static DateTimeOffset Local(DateTimeOffset instante) => HorarioDeSaoPaulo.Local(instante);

    private static DateTimeOffset? Local(DateTimeOffset? instante) => instante is { } i ? HorarioDeSaoPaulo.Local(i) : null;
}
