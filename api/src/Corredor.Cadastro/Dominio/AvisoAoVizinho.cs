using Corredor.Contratos;

namespace Corredor.Cadastro.Dominio;

public sealed record VizinhoRespondeu(Guid Aviso, CasaToken Token, RespostaDoVizinho Resposta, DateTimeOffset RespondidoEm);

// Um aviso por episódio: (CasaToken, Motivo, Desde). A linha é o registro do envio simulado.
public sealed class AvisoAoVizinho
{
    // Fixo e neutro: sem nome, sem endereço e sem motivo de saúde. Sai pela SaidaSegura no canal Vizinho.
    public const string Texto =
        "A UBS pede que você veja agora se está tudo bem com a pessoa que você acompanha. Depois, responda aqui.";

    private AvisoAoVizinho() { }

    public Guid Id { get; private set; }
    public CasaToken CasaToken { get; private set; }
    public MicroareaId Microarea { get; private set; }
    public MotivoDoAlerta Motivo { get; private set; }
    public DateTimeOffset Desde { get; private set; }
    public DateTimeOffset RegistradoEm { get; private set; }
    public RespostaDoVizinho? Resposta { get; private set; }
    public DateTimeOffset? RespondidoEm { get; private set; }
    public uint Versao { get; private set; }

    // Só existe se o idoso pode avisar o vizinho (invariante 2). Devolve null quando não pode.
    public static AvisoAoVizinho? Registrar(Idoso idoso, PedidoDeContato pedido, DateTimeOffset agora) =>
        idoso.PodeAvisarVizinho && idoso.TokenAtivo == pedido.Token
            ? new AvisoAoVizinho
            {
                Id = Guid.NewGuid(),
                CasaToken = pedido.Token,
                Microarea = idoso.Microarea,
                Motivo = pedido.Motivo,
                Desde = pedido.Desde,
                RegistradoEm = agora,
            }
            : null;

    // A mesma resposta repetida não muda nada e devolve null. Uma resposta diferente é recusada.
    public VizinhoRespondeu? RegistrarResposta(RespostaDoVizinho resposta, DateTimeOffset agora)
    {
        if (Resposta is { } anterior)
        {
            return anterior == resposta ? null : throw Erros.AvisoJaRespondido();
        }

        Resposta = resposta;
        RespondidoEm = agora;
        return new VizinhoRespondeu(Id, CasaToken, resposta, agora);
    }
}
