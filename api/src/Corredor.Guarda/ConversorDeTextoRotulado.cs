using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Corredor.Guarda;

// É a saída segura do canal Banco, que aceita todo rótulo. O banco guarda só o texto.
// Na leitura, os rótulos vêm da coluna, seção 2.4 de 04-modelo-de-dados.md.
public sealed class ConversorDeTextoRotulado(params TipoDeDado[] rotulosDaColuna)
    : ValueConverter<TextoRotulado, string>(t => t.Valor, s => TextoRotulado.DoBanco(s, rotulosDaColuna))
{
    // Devolve ValueConverter para HasConversion aceitar também TextoRotulado? sem aviso de nulidade.
    public static ValueConverter Rotulado(params TipoDeDado[] rotulosDaColuna) => new ConversorDeTextoRotulado(rotulosDaColuna);
}
