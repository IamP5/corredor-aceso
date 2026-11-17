# API do Corredor Aceso

Monólito modular em .NET 10. Um processo, um `Program.cs`, três módulos: Motor, Cadastro e Guarda. O modelo está em [03-modelo-de-dominio.md](../docs/arquitetura/03-modelo-de-dominio.md) e o banco em [04-modelo-de-dados.md](../docs/arquitetura/04-modelo-de-dados.md).

Rode todos os comandos a partir da raiz do repositório.

## Projetos

| Projeto | Papel | Pode referenciar |
|---|---|---|
| `src/Corredor.Contratos` | Tipos e interfaces que Motor e Cadastro dividem. Congelado. | nada |
| `src/Corredor.Motor` | Módulo Motor. `MotorDbContext`, schema `motor`. | `Corredor.Contratos` |
| `src/Corredor.Cadastro` | Módulo Cadastro. `CadastroDbContext`, schema `cadastro`. | `Corredor.Contratos`, `Corredor.Guarda` |
| `src/Corredor.Guarda` | Módulo Guarda. `GuardaDbContext`, schema `guarda`. | nada |
| `src/Corredor.Api` | Host. Swagger, saúde, versão, configuração e log. Sem regra de módulo. | os quatro |
| `tests/Corredor.<Modulo>.Testes` | Um projeto xUnit por módulo. | o módulo |
| `tests/Corredor.Api.Testes` | Testes do host, de `Corredor.Contratos` e da regra de referências. | `Corredor.Api` |

O teste `ReferenciasEntreProjetosTestes` falha se um `.csproj` de `src/` referencia um projeto fora da coluna "Pode referenciar". Ele também falha se um projeto novo aparece em `src/` sem linha na tabela do teste.

`Directory.Build.props` fixa `net10.0`, `Nullable`, `ImplicitUsings` e aviso como erro. `Directory.Packages.props` guarda a versão de todo pacote. Um `.csproj` cita o pacote sem versão. Os pacotes que os módulos vão usar já estão lá: `MQTTnet` para o Motor e `Testcontainers.PostgreSql` para os testes dos módulos. A Guarda chama o LLM com o `HttpClient` e lê o `pii.json` com o `System.Text.Json`, que vêm com o .NET. Se um módulo precisar de outro pacote, a sessão do módulo avisa o orquestrador. Ela não usa `VersionOverride` nem edita `Directory.Packages.props`.

## Rodar no notebook

Você precisa do SDK do .NET 10 e do Docker.

1. Suba o Postgres e o Mosquitto:

	```bash
	npm run dev:deps
	```

2. Para usar o LLM da Guarda, copie `.env.example` para `.env` na raiz e preencha `OPENROUTER_API_KEY`. O `.env` nunca entra no Git.

3. Suba a API em `http://localhost:5000`, no ambiente `Development`:

	```bash
	npm run dev:api
	```

4. Confira:

	```bash
	curl http://localhost:5000/healthz/ready
	curl http://localhost:5000/api/versao
	```

	O Swagger fica em `http://localhost:5000/swagger`. Os prints da visão geral e de uma rota do Cadastro e do Motor estão em [docs/evidencias/api/](../docs/evidencias/api/README.md).

	No macOS, o receptor do AirPlay ocupa a porta 5000 e responde 403 no lugar da API. Use outra porta:
	`dotnet run --project api/src/Corredor.Api --urls http://localhost:5080`.

5. Pare os contêineres quando terminar:

	```bash
	docker compose -f compose.dev.yml down
	```

## Rotas do host

| Rota | Responde |
|---|---|
| `GET /healthz/live` | 200 enquanto o processo vive. Sem token. |
| `GET /healthz/ready` | 200 só quando toda verificação registrada pelos módulos passa. Hoje são os três `DbContext`. O Motor acrescenta o cliente MQTT. |
| `GET /api/versao` | `{"versao":"0.3.0","commit":"<sha>","ambiente":"local"}`. A versão vem do `-p:Version` do `tools/empacotar.sh`. Build local responde `0.0.0`. |
| `GET /openapi/v1.json` e `/swagger` | O documento OpenAPI e a interface do Swagger, em todos os ambientes. |

`/healthz/*`, `/api/versao`, `/openapi/v1.json` e `/swagger` respondem sem token.

Uma `RegraDeDominioViolada` lançada por qualquer rota vira `{"codigo": "...", "mensagem": "..."}`. O status sai do `TipoDeViolacao`: `Invalida` 422, `Conflito` 409, `Desconhecido` 404, `Proibida` 403 e `Indisponivel` 503.

## Autenticação

O modelo de domínio não define a autenticação. O modelo de dados só exige um `AcsId` no formato `^[a-z0-9-]{3,40}$`, sem tabela de ACS. O host lê um token fixo por ACS da configuração:

```json
"Autenticacao": {
	"Acs": [ { "Id": "acs-demo-01", "Microarea": 3, "Token": "dev-acs-demo-01-so-no-notebook" } ]
}
```

O cliente manda o token no cabeçalho `Authorization`:

```bash
curl -H "Authorization: Bearer dev-acs-demo-01-so-no-notebook" "http://localhost:5000/api/radar?microarea=3"
```

No Swagger, clique em **Authorize** e cole o token, sem a palavra `Bearer`.

- Um token aceito vira as claims `acs`, com o `AcsId`, e `microarea`, com o número.
- Toda rota exige token, menos a que declara `.AllowAnonymous()`. Rota nova nasce fechada. Sem token, a resposta é 401.
- Quando a rota tem `?microarea=` e a microárea não é a da ACS, a resposta é 403 `MICROAREA_DE_OUTRA_ACS`.
- A ACS de `appsettings.Development.json` é `acs-demo-01`, na microárea 3. Pela seção 8.2 de `04-modelo-de-dados.md`, a migração `Inicial` do Cadastro cria essa ACS e essa microárea com `HasData`. Os dois lugares precisam continuar iguais.
- A VM local roda em `Development` e lê esse token, que está no Git. Troque na VM com `Autenticacao__Acs__0__Token=` no `/etc/corredor/segredos.env`. Em `Production`, sem `Autenticacao__Acs__*`, toda rota fechada responde 401.

Rotas que ficam abertas, pela coluna "Quem chama" da seção 6.3 de `03-modelo-de-dominio.md`. Cada módulo marca as suas com `.AllowAnonymous()`:

| Rota | Módulo | Motivo |
|---|---|---|
| `PUT /api/dias-criticos/{data}` | Motor | O job de ML chama sem ACS. |
| `PUT /api/demo/relogio` | Motor | Só existe em `Development`, para a equipe. |
| `POST /api/avisos/{id}/resposta` | Cadastro | É o canal simulado do vizinho, que não é ACS. |
| `GET /api/painel` | Guarda | O painel Risco Cidadão é aberto. |

As outras rotas da seção 6.3 exigem token. O `teste-de-falha.sh` lê `GET /api/casas/{token}/linha-do-tempo` com `TESTE_AUTORIZACAO="Bearer <token>"` em `/etc/corredor/teste-de-falha.env`.

## Configuração

Na VM, o `infra/02-app.sh` grava `/etc/corredor/api.env`, e a chave do LLM fica em `/etc/corredor/segredos.env`. No notebook, os valores vêm de `appsettings.Development.json`, de `Properties/launchSettings.json` e do `.env`.

| Chave no ambiente | Seção no `appsettings` | Quem lê |
|---|---|---|
| `ConnectionStrings__Motor`, `__Cadastro`, `__Guarda` | `ConnectionStrings` | cada módulo, com o papel `corredor_<modulo>` do Postgres |
| `Mqtt__Host`, `Mqtt__Porta`, `Mqtt__ClienteId` | `Mqtt` | Motor |
| `Corredor__Ambiente` | `Corredor:Ambiente` | `/api/versao` |
| `Llm__ApiKey`, `Llm__Modelo`, `Llm__UrlBase` | `Llm` | Guarda |
| `Autenticacao__Acs__<n>__Id`, `__Microarea`, `__Token` | `Autenticacao:Acs` | o host |
| `Relogio__ToleranciaDoRelogio` | `Relogio` | o host registra `ParametrosDoRelogio` para Motor e Cadastro |
| `Motor__*` | `Motor` | `ParametrosDoMotor`, com os valores de produção em `appsettings.json` e os de demo em `appsettings.Development.json` |

A seção `Llm` aceita também os nomes do `.env.example`. A ordem é esta: `Llm__*` no ambiente, `OPENROUTER_*` no ambiente, o `.env` da raiz e o padrão. O `.env` só é lido em `Development`.

O modelo padrão é `openai/gpt-4o-mini`. Com `openrouter/auto`, o OpenRouter escolhia um modelo diferente a cada chamada, e às vezes um que pensa antes e devolvia a resposta cortada. Em 17/11, cinco chamadas pelo `Tradutor` da Guarda, com a chave real, alternando um bloqueio de saída e um documento mascarado, deram estes resultados (medido):

| Modelo | Tradução do LLM | Duas frases | Tempo por chamada |
|---|---|---|---|
| `openai/gpt-4o-mini` | 5 de 5 | 5 de 5 | 1,1 a 1,5 s |
| `openai/gpt-4.1-nano` | 5 de 5 | 5 de 5 | 0,9 a 2,5 s |
| `mistralai/mistral-small-3.2-24b-instruct` | 5 de 5 | 5 de 5 | 0,9 a 1,5 s |
| `openrouter/auto` | 5 de 5 | 5 de 5 | 2,5 a 3,0 s |

Os três modelos fixos responderam inteiros. O `gpt-4o-mini` ficou porque foi o único que nunca citou com quem a pessoa mora, que o prompt proíbe. Os outros dois disseram "quem mora com você" em algumas respostas. Cinco chamadas por modelo é pouco para dizer que um modelo nunca falha. Para trocar, mude `OPENROUTER_MODEL` no `.env` ou `Llm__Modelo` no `segredos.env` da VM.

Fora de `Development`, o log sai em JSON numa linha por evento, que o journald guarda. O `dotnet run` usa o formato simples, pelo `launchSettings.json`.

## Testar

```bash
dotnet build api/Corredor.slnx
dotnet test api/Corredor.slnx
```

Os testes de banco e de MQTT sobem contêineres pelo Testcontainers 4.17, que pede a API 1.44 do Docker. Numa máquina com Docker mais antigo, como o Docker Desktop 4.22, que só fala a 1.43, esses testes falham ao conectar. Rode com a versão fixada:

```bash
DOCKER_API_VERSION=1.43 dotnet test api/Corredor.slnx
```

O CI do GitHub tem Docker novo e não precisa da variável.

## Demo integrada

`bash tools/demo.sh` sobe tudo e roda as cenas contra o sistema de verdade: Postgres e Mosquitto do `compose.dev.yml`, os papéis de módulo, as migrações aplicadas como no deploy, a API em `Development` na porta 5080, a carga da demo duas vezes, o simulador das casas e o job de ML. Os comandos, o que foi visto e as evidências estão em [docs/evidencias/integracao/RELATORIO.md](../docs/evidencias/integracao/RELATORIO.md).

O CI roda a mesma verificação com o banco:

```bash
npm run check:migracoes
npm run check:api
npm run check:pacote
```

Sem `CORREDOR_BD_TESTE`, o `check:api` compila e testa sem aplicar as migrações. Com ele, o script cria os papéis com `infra/config/bd-modulo.sql` e aplica cada script de migração duas vezes, como `corredor_<modulo>_migra`.

## Criar uma migração

Só a pessoa dona do módulo cria migração no contexto dele. Toda migração é aditiva até 20/11. O `check:migracoes` recusa `DropTable`, `DropColumn`, `RenameTable` e `RenameColumn` no `Up()`.

1. Mude o modelo no `DbContext` do módulo.
2. Gere a migração. Troque `Motor` pelo módulo:

	```bash
	dotnet tool restore
	dotnet ef migrations add <Nome> --project api/src/Corredor.Motor \
		--startup-project api/src/Corredor.Api --context MotorDbContext --output-dir Infra/Migrations
	```

3. Confira que o modelo e as migrações batem:

	```bash
	dotnet ef migrations has-pending-model-changes --project api/src/Corredor.Motor \
		--startup-project api/src/Corredor.Api --context MotorDbContext
	```

4. Veja o SQL que o deploy vai aplicar:

	```bash
	bash tools/migracoes.sh /tmp/migracoes
	```

A API nunca migra o banco. O deploy aplica o SQL gerado por `tools/migracoes.sh`.

Cada contexto já tem a migração vazia `Esqueleto`. Ela só cria a tabela `<schema>.__EFMigrationsHistory`, com as colunas `migration_id` e `product_version`. Ela existe porque o `check:api` com banco falha quando nenhum módulo tem migração. A primeira migração com tabelas de cada módulo é a `Inicial`, gerada por cima dela.

## Tipos públicos da Guarda

O Cadastro referencia a Guarda e grava `TextoRotulado`. Estes tipos ficam em `src/Corredor.Guarda/` e estão congelados como `Corredor.Contratos`. A sessão da Guarda troca as implementações registradas, sem mudar as assinaturas.

| Arquivo | Tipos |
|---|---|
| `TextoRotulado.cs` | `TipoDeDado`, `Canal`, `TextoRotulado` com `Do(string, params TipoDeDado[])`, `Rotulos`, `Juntar(TextoRotulado, string separador = " ")`, o operador `+`, `ToString()` mascarado e `Marcador(TipoDeDado)` |
| `ConversorDeTextoRotulado.cs` | `ConversorDeTextoRotulado(params TipoDeDado[] rotulosDaColuna)` e `ConversorDeTextoRotulado.Rotulado(params TipoDeDado[])`, que serve também para `TextoRotulado?` |
| `SaidaSegura.cs` | `Veredito.Liberada(string Texto)`, `Veredito.Bloqueada(IReadOnlySet<TipoDeDado> TiposDeDado, string TextoMascarado)` e `ISaidaSegura.Liberar(TextoRotulado, Canal)` |
| `MotorDeParsing.cs` | `Achado(TipoDeDado Tipo, string Campo, string ValorMascarado, int Severidade)`, `TextoAnalisado(TextoRotulado Texto, IReadOnlyList<Achado> Achados)` e `IMotorDeParsing.Rotular(string, params TipoDeDado[] rotulosDoCampo)` |

O valor de um `TextoRotulado` é `internal`. Fora da Guarda, ele só sai por `ISaidaSegura.Liberar` ou pelo conversor do EF Core. `ToString()` devolve `[NOME]`, `[MORA_SOZINHO]` e assim por diante, nunca o valor de um texto com rótulo.

O `AddGuarda` registra a `SaidaSegura`, que aplica a `PoliticaDeCanal` da seção 4.4 e registra um `SaidaBloqueada` a cada bloqueio, e o `MotorDeParsing`, que aplica as regras de `Regras/pii.json`.

## O que cada módulo implementa

Cada módulo começa vazio, com a pasta `Infra/` e o `DbContext`. A sessão do módulo preenche `Dominio/`, `Aplicacao/`, `Infra/` e `Rotas/`, e liga tudo pelos métodos abaixo. O `Program.cs` já chama cada um, então nenhuma sessão edita o host.

| Módulo | Arquivo | Métodos |
|---|---|---|
| Motor | `src/Corredor.Motor/MotorModulo.cs` | `AddMotor(IHostApplicationBuilder)`, `MapMotor(IEndpointRouteBuilder)` |
| Cadastro | `src/Corredor.Cadastro/CadastroModulo.cs` | `AddCadastro(IHostApplicationBuilder)`, `MapCadastro(IEndpointRouteBuilder)`, `SemearCadastroAsync(IServiceProvider, string, CancellationToken)` |
| Guarda | `src/Corredor.Guarda/GuardaModulo.cs` | `AddGuarda(IHostApplicationBuilder)`, `UseGuarda(IApplicationBuilder)`, `ComGuarda(RouteGroupBuilder)`, `MapGuarda(IEndpointRouteBuilder)` |

- Os métodos `Map*` recebem o grupo `/api`. A rota `"/radar"` no Motor vira `/api/radar`.
- O host chama `AddGuarda` antes de `AddMotor` e de `AddCadastro`.
- `UseGuarda` roda depois do `UseExceptionHandler` do host.
- `ComGuarda` recebe o grupo `/api` inteiro, para os filtros da Guarda valerem nas rotas dos três módulos.
- Um `IExceptionHandler` registrado em `AddGuarda` roda depois do tratador de `RegraDeDominioViolada` do host.
- Toda verificação de saúde registrada com `AddHealthChecks()` entra no `/healthz/ready`.
- O teste `Em_development_o_relogio_comeca_sem_deslocamento` confere que o relógio de `Development` começa sem deslocamento. O teste `Em_producao_vale_o_relogio_do_sistema_sem_deslocamento` confere `TimeProvider.System` em `Production`.
- O Motor registra o `RelogioDeDemo` como `TimeProvider` e como `IDeslocamentoDoRelogio` em `Development`. Sem esse registro, o host usa `TimeProvider.System` e deslocamento zero.
- O Cadastro implementa `IReceptorDeContato`, e o Motor implementa `IMotorDeCasas`. Cada um registra a própria implementação no próprio `Add*`.
- `Corredor.Api --semear <arquivo>` chama `SemearCadastroAsync` e encerra. Ele só roda em `Development` e `Demo`.
