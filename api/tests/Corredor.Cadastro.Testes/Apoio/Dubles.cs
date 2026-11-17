using System.Collections.Concurrent;
using Corredor.Contratos;

namespace Corredor.Cadastro.Testes.Apoio;

// O que o Motor recebeu pela IMotorDeCasas. É o efeito do Cadastro num sistema de fora, então é comportamento.
// Converge como o Motor real promete: ativar de novo atualiza o perfil, Confirmacao.Id repetido é ignorado.
public sealed class MotorFalso : IMotorDeCasas
{
    public ConcurrentQueue<string> Chamadas { get; } = new();
    public ConcurrentDictionary<CasaToken, PerfilDaCasa> Ativas { get; } = new();
    public ConcurrentDictionary<CasaToken, byte> Desativadas { get; } = new();
    public ConcurrentDictionary<Guid, (CasaToken Token, Confirmacao Confirmacao)> Confirmacoes { get; } = new();

    // Simula o Motor fora do ar: toda chamada falha com erro de transporte.
    public bool ForaDoAr { get; set; }

    // Simula a recusa da confirmação com mais de 24 horas.
    public RegraDeDominioViolada? RecusaDeConfirmacao { get; set; }

    public Task AtivarCasaAsync(CasaToken token, MicroareaId microarea, PerfilDaCasa perfil, CancellationToken ct)
    {
        Registrar($"ativar {token}");
        if (Desativadas.ContainsKey(token))
        {
            throw new RegraDeDominioViolada("CASA_DESATIVADA", "casa desativada");
        }

        Ativas[token] = perfil;
        return Task.CompletedTask;
    }

    public Task DefinirPerfilAsync(CasaToken token, PerfilDaCasa perfil, CancellationToken ct)
    {
        Registrar($"perfil {token}");
        Ativas[token] = perfil;
        return Task.CompletedTask;
    }

    public Task ConfirmarAsync(CasaToken token, Confirmacao confirmacao, CancellationToken ct)
    {
        Registrar($"confirmar {token}");
        if (RecusaDeConfirmacao is { } recusa)
        {
            throw recusa;
        }

        Confirmacoes.TryAdd(confirmacao.Id, (token, confirmacao));
        return Task.CompletedTask;
    }

    public Task DesativarCasaAsync(CasaToken token, CancellationToken ct)
    {
        Registrar($"desativar {token}");
        Ativas.TryRemove(token, out _);
        Desativadas[token] = 0;
        return Task.CompletedTask;
    }

    public int Contar(string prefixo) => Chamadas.Count(c => c.StartsWith(prefixo, StringComparison.Ordinal));

    private void Registrar(string chamada)
    {
        if (ForaDoAr)
        {
            throw new HttpRequestException("motor fora do ar");
        }

        Chamadas.Enqueue(chamada);
    }
}

public sealed class RelogioDeTeste(DateTimeOffset agora) : TimeProvider
{
    public DateTimeOffset Agora { get; set; } = agora;

    public override DateTimeOffset GetUtcNow() => Agora.ToUniversalTime();
}

public sealed class DeslocamentoDeTeste : IDeslocamentoDoRelogio
{
    public TimeSpan Deslocamento { get; set; }
}
