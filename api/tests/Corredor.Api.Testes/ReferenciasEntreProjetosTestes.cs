using System.Reflection;
using System.Xml.Linq;
using Corredor.Api;
using Corredor.Cadastro;
using Corredor.Contratos;
using Corredor.Guarda;
using Corredor.Motor;

namespace Corredor.Api.Testes;

// A regra da seção 2 de 03-modelo-de-dominio.md. O compilador só barra o uso de um tipo
// depois que a referência existe. Este teste barra a própria referência.
public class ReferenciasEntreProjetosTestes
{
    private static readonly Dictionary<string, string[]> Permitidas = new()
    {
        ["Corredor.Contratos"] = [],
        ["Corredor.Motor"] = ["Corredor.Contratos"],
        ["Corredor.Guarda"] = [],
        ["Corredor.Cadastro"] = ["Corredor.Contratos", "Corredor.Guarda"],
        ["Corredor.Api"] = ["Corredor.Contratos", "Corredor.Motor", "Corredor.Cadastro", "Corredor.Guarda"],
    };

    private static readonly Assembly[] Assemblies =
    [
        typeof(CasaToken).Assembly,
        typeof(MotorModulo).Assembly,
        typeof(CadastroModulo).Assembly,
        typeof(GuardaModulo).Assembly,
        typeof(RespostaDaVersao).Assembly,
    ];

    public static TheoryData<string> Projetos => [.. Permitidas.Keys];

    [Fact]
    public void Todo_projeto_de_src_tem_linha_na_tabela_de_referencias()
    {
        var projetos = Directory.GetFiles(PastaSrc(), "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .Order();

        Assert.Equal(Permitidas.Keys.Order(), projetos);
    }

    [Theory]
    [MemberData(nameof(Projetos))]
    public void O_csproj_so_referencia_projetos_permitidos(string projeto)
    {
        var csproj = XDocument.Load(Path.Combine(PastaSrc(), projeto, projeto + ".csproj"));
        var referencias = csproj.Descendants("ProjectReference")
            .Select(r => Path.GetFileNameWithoutExtension(r.Attribute("Include")!.Value));

        Assert.Empty(referencias.Except(Permitidas[projeto]));
    }

    [Theory]
    [MemberData(nameof(Projetos))]
    public void O_assembly_compilado_so_usa_projetos_permitidos(string projeto)
    {
        var assembly = Assemblies.Single(a => a.GetName().Name == projeto);
        var usados = assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(nome => nome.StartsWith("Corredor.", StringComparison.Ordinal));

        Assert.Empty(usados.Except(Permitidas[projeto]));
    }

    private static string PastaSrc()
    {
        for (var pasta = new DirectoryInfo(AppContext.BaseDirectory); pasta is not null; pasta = pasta.Parent)
        {
            if (File.Exists(Path.Combine(pasta.FullName, "Corredor.slnx"))) return Path.Combine(pasta.FullName, "src");
        }
        throw new DirectoryNotFoundException("Corredor.slnx não encontrado acima de " + AppContext.BaseDirectory);
    }
}
