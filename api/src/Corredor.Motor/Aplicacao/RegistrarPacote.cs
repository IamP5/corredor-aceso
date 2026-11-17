using Corredor.Contratos;
using Corredor.Motor.Dominio;
using Corredor.Motor.Infra;
using Microsoft.EntityFrameworkCore;

namespace Corredor.Motor.Aplicacao;

public enum ResultadoDoPacote { Gravado, Repetido, SoUltimoPacote, ForaDoPrazo, CasaInativa }

// O caminho mais quente: duas instrucoes numa transacao, sem carregar o agregado (secao 3.2 do modelo de dados).
public sealed class RegistrarPacote(MotorDbContext bd, TimeProvider relogio, ParametrosDoRelogio relogioDoContrato, ParametrosDoMotor parametros)
{
    public async Task<ResultadoDoPacote> ExecutarAsync(PacoteDoNo pacote, CancellationToken ct)
    {
        var recebidoEm = relogio.GetUtcNow();
        if (!pacote.DentroDoPrazo(recebidoEm, relogioDoContrato.ToleranciaDoRelogio, parametros.Retencao))
        {
            return ResultadoDoPacote.ForaDoPrazo;
        }

        await using var tx = await bd.Database.BeginTransactionAsync(ct);

        // Devolve 1 so para casa ativa, e so troca ultimo_pacote_em por uma hora maior (invariante 2).
        var casaAtiva = await bd.Casas
            .Where(c => c.Token == pacote.Token && c.DesativadaEm == null)
            .ExecuteUpdateAsync(s => s.SetProperty(
                c => c.UltimoPacoteEm,
                c => c.UltimoPacoteEm == null || c.UltimoPacoteEm < recebidoEm ? recebidoEm : c.UltimoPacoteEm), ct);
        if (casaAtiva == 0)
        {
            return ResultadoDoPacote.CasaInativa;
        }

        if (EventoSensor.De(pacote, recebidoEm) is not { } evento)
        {
            await tx.CommitAsync(ct);
            return ResultadoDoPacote.SoUltimoPacote;
        }

        // A chave (casa_token, ocorrido_em, seq) torna o reenvio inofensivo: a repeticao conta como sucesso.
        var novo = await bd.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO motor.evento_sensor (casa_token, ocorrido_em, seq, tipo, recebido_em, temp_c, tensao_v, bufferizado)
            VALUES ({evento.CasaToken.Valor}, {evento.OcorridoEm.ToUniversalTime()}, {evento.Seq}, {evento.Tipo.ToString()},
                {evento.RecebidoEm.ToUniversalTime()}, {evento.TempC?.Valor}, {evento.TensaoV?.Valor}, {evento.Bufferizado})
            ON CONFLICT (casa_token, ocorrido_em, seq) DO NOTHING
            """, ct);
        await tx.CommitAsync(ct);
        return novo == 1 ? ResultadoDoPacote.Gravado : ResultadoDoPacote.Repetido;
    }
}
