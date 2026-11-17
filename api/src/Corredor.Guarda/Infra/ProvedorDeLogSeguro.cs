using Microsoft.Extensions.Logging;

namespace Corredor.Guarda.Infra;

/// <summary>
/// Decorador de ILoggerProvider. Um argumento TextoRotulado com rótulo vai ao log como máscara
/// e registra SaidaBloqueada no canal Log. Uma exceção cuja mensagem tem dado pessoal vai ao
/// log mascarada, com o mesmo registro. Texto sem rótulo passa inteiro.
/// </summary>
/// <param name="saida">Resolvida na primeira linha de log, porque o provedor nasce antes do contêiner.</param>
public sealed class ProvedorDeLogSeguro(ILoggerProvider interno, Func<SaidaSegura> saida, Func<MotorDeParsing> motor) : ILoggerProvider
{
    public ILogger CreateLogger(string categoria) => new LoggerSeguro(interno.CreateLogger(categoria), ModuloDaCategoria(categoria), saida, motor);

    public void Dispose() => interno.Dispose();

    internal static string ModuloDaCategoria(string categoria)
    {
        var partes = categoria.Split('.');
        return partes.Length > 1 && partes[0] == "Corredor" ? partes[1] : "Api";
    }

    private sealed class LoggerSeguro(ILogger interno, string modulo, Func<SaidaSegura> saida, Func<MotorDeParsing> motor) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => interno.BeginScope(state);

        public bool IsEnabled(LogLevel nivel) => interno.IsEnabled(nivel);

        public void Log<TState>(LogLevel nivel, EventId id, TState state, Exception? excecao, Func<TState, Exception?, string> formatar)
        {
            if (!IsEnabled(nivel))
            {
                return;
            }

            var excecaoSegura = excecao is null ? null : Mascarar(excecao);
            if (state is not IReadOnlyList<KeyValuePair<string, object?>> valores || !valores.Any(v => v.Value is TextoRotulado))
            {
                interno.Log(nivel, id, state, excecaoSegura, (s, _) => formatar(s, excecao));
                return;
            }

            var limpos = valores
                .Select(v => new KeyValuePair<string, object?>(v.Key, v.Value is TextoRotulado texto ? Liberar(texto) : v.Value))
                .ToList();

            // A mensagem formatada usa TextoRotulado.ToString(), que já é a máscara.
            var mensagem = formatar(state, excecao);
            interno.Log(nivel, id, new EstadoSeguro(limpos, mensagem), excecaoSegura, static (s, _) => s.Mensagem);
        }

        private string Liberar(TextoRotulado texto) => saida().Liberar(texto, Canal.Log, modulo).ParaOCanal();

        private Exception Mascarar(Exception excecao)
        {
            var texto = motor().Rotular(excecao.ToString()).Texto;
            return texto.Rotulos.Count == 0 ? excecao : new ExcecaoMascarada(excecao.GetType().FullName, Liberar(texto));
        }
    }

    /// <summary>Fica no lugar de uma exceção cujo texto tinha dado pessoal. ToString() devolve a máscara.</summary>
    public sealed class ExcecaoMascarada(string? tipoOriginal, string textoMascarado) : Exception($"{tipoOriginal} com dado pessoal mascarado")
    {
        public override string ToString() => textoMascarado;
    }

    private sealed class EstadoSeguro(List<KeyValuePair<string, object?>> valores, string mensagem)
        : IReadOnlyList<KeyValuePair<string, object?>>
    {
        public string Mensagem { get; } = mensagem;
        public KeyValuePair<string, object?> this[int index] => valores[index];
        public int Count => valores.Count;
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => valores.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        public override string ToString() => Mensagem;
    }
}
