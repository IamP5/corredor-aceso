# Infraestrutura, CI/CD, releases e repositório do Corredor Aceso

Este documento descreve onde a VM roda, como ela é criada e provisionada, como o código se torna uma release, como a release chega à VM e como a equipe opera o ambiente em execução. Ele complementa a [visão geral](01-visao-geral.md) e o [modelo de domínio](03-modelo-de-dominio.md).

Os 60 arquivos de infraestrutura estão em [referencia/repo/](referencia/repo/), com os mesmos caminhos que ocupam na raiz do repositório. O [LEIA-ME](referencia/repo/LEIA-ME.md) dessa pasta descreve a cópia dos arquivos e as checagens. Este texto mostra apenas trechos curtos e aponta para o arquivo completo.

A organização da infraestrutura segue a rubrica de Operating Systems, que exige o provisionamento por script dentro da VM. Dentro da VM, todo o trabalho é feito por scripts Bash. O Terraform apenas cria a VM do Azure. O cloud-init apenas clona o repositório e chama um script. O GitHub Actions apenas executa um comando por SSH. Nenhuma dessas ferramentas instala pacotes, escreve units ou configura a rede.

Cada afirmação indica a sua origem. "Medido" significa que a afirmação foi verificada no ensaio descrito na seção 8. "Só lint" significa que o arquivo passou em `shellcheck`, `actionlint` ou `terraform validate`, mas não foi executado. "Nunca rodou" significa que o item não foi executado.

**Atualização de validação, 18/11/2026:** a seção 8 preserva o ensaio anterior em contêiner. Depois dele, os scripts foram validados numa [VM real Ubuntu 24.04 no Lima/VZ](../evidencias/os/vm/README.md), com rede/DNS, aplicação, limites padrão de recursos, recuperação, watchdog e início automático após novo boot. Consulte esse relatório para a comprovação atual; Multipass, Azure e o cloud-init do projeto continuam não executados.

## Visão geral

O desenho mostra o que roda dentro da VM e o que vem de fora. Somente o Nginx recebe conexões externas. A API, o Postgres e o Mosquitto escutam em `127.0.0.1`.

![VM corredor-ubs e seus serviços](diagramas/21-vm-e-servicos.mmd)

```text
notebook                          GitHub                                   VM corredor-ubs (Multipass; Azure opcional)
┌──────────────────────┐   PR    ┌───────────────────────────────┐       ┌────────────────────────────────────────────┐
│ dotnet run           │ ──────> │ ci.yml: só a pasta que mudou   │       │ Nginx :80 (e :443 se houver certificado)   │
│ compose.dev.yml      │         │ check obrigatório: ci-ok       │       │   /api -> API .NET em 127.0.0.1:5000       │
└──────────────────────┘         └───────────────────────────────┘       │   /painel/ -> página estática da release   │
                           tag vX.Y.Z na main                            │ Postgres e Mosquitto só em 127.0.0.1       │
                                 ┌───────────────────────────────┐ SSH   │ corredor-monitor.service (Restart=always)  │
                                 │ release.yml: testa, gera SQL,  │ ────> │ sudo corredor deploy vX.Y.Z                │
                                 │ empacota x64 e arm64, publica, │       │   baixa da Release, pg_dump, migra como o  │
                                 │ espera aprovação e implanta    │       │   dono de cada schema, troca o link, confere│
                                 └───────────────────────────────┘       │   a saúde e volta sozinho se falhar        │
                                                                          └────────────────────────────────────────────┘
VM local:  multipass launch (sem cloud-init) -> git clone -> sudo infra/provisionar.sh -> 01-rede, 02-app, 03-monitor, verificar
Azure:     terraform apply -> cloud-init -> git clone -> o mesmo infra/provisionar.sh
HTTPS da demo:   sudo corredor tunel -> https://<aleatorio>.trycloudflare.com
```

## 1. Decisões

### 1.1 Local primeiro, Azure como acréscimo

**Escolha.** A entrega usa VM local Ubuntu 24.04, 2 CPUs, 4 GB de memória e 20 GB de disco. O ambiente efetivamente validado em 18/11 usa Lima/VZ; os comandos de Multipass permanecem como alternativa não executada. A VM do Azure for Students é opcional. O Terraform que a cria está no repositório, mas nunca foi aplicado (seção 8).

**Motivo.** Nenhum item da rubrica depende da nuvem. O enunciado pede o provisionamento automático dentro da VM, e a VM local o executa sem depender da política de regiões da assinatura de estudante, que a equipe não testou. A VM local alcança o GitHub e o broker MQTT público por conexões de saída, então o deploy por pacote e a ponte MQTT funcionam atrás do NAT do notebook. O celular alcança a VM local pelo túnel HTTPS da seção 1.8.

**Rejeitado.**

- Azure como ambiente oficial. Um problema de região ou de cota inviabilizaria a demonstração inteira.
- Oracle Cloud, AWS e GCP. Pedem cartão.
- Vagrant com VirtualBox. Tem suporte limitado em Mac com Apple Silicon, segundo o levantamento de ferramentas de IaC feito pela equipe.

### 1.2 Terraform, cloud-init e Bash, cada um no seu papel

**Escolha.** O Terraform com `azurerm` 4.x cria grupo, rede, NSG, IP público e VM em [infra/nuvem/](referencia/repo/infra/nuvem/main.tf). O arquivo [corredor.yaml](referencia/repo/infra/cloud-init/corredor.yaml) do cloud-init tem dois comandos: `git clone` e `bash /opt/corredor/repo/infra/provisionar.sh`. Na VM local, o operador executa o mesmo `provisionar.sh` manualmente. Não há Ansible.

**Motivo.** O prazo curto do projeto comporta apenas uma ferramenta nova. O cloud-init não tem variável nem segredo, então o arquivo é idêntico na nuvem e no Multipass. O ambiente é detectado dentro da VM. O `cloud-id` igual a `azure` resulta em `producao`, qualquer outro valor resulta em `local`, e o arquivo `/etc/corredor/ambiente` tem precedência sobre a detecção. Uma mudança no `custom_data` do `azurerm` recria a VM e apaga o banco, por isso o `main.tf` ignora mudanças nesse campo depois da criação.

**Rejeitado.**

- Ansible. Seria a terceira ferramenta, e o provisionamento deixaria de ser feito pelos scripts Bash.
- `templatefile()` no Terraform e `envsubst` no Multipass. Dois caminhos de preenchimento para o mesmo arquivo.
- Provisioner `remote-exec`. A documentação do Terraform o trata como último recurso.
- State remoto no Azure Storage. O state tem oito recursos e nenhum segredo. Ele fica no notebook do responsável pela infraestrutura, fora do Git.

### 1.3 .NET 10 e pacote self-contained para x64 e arm64

**Escolha.** `net10.0` em todos os projetos e `global.json` em 10.0.100. A release tem dois pacotes, `corredor-linux-x64.tar.gz` e `corredor-linux-arm64.tar.gz`, publicados self-contained a partir de um runner x64 só. Cada pacote traz a API publicada, os scripts SQL das migrações, a página do painel, `VERSION` e `COMMIT`.

**Motivo.** O .NET 8 saiu de suporte em 10/11/2026. Com a publicação self-contained, a VM não instala runtime. O pacote arm64 existe porque uma VM do Multipass num Mac com Apple Silicon é arm64. No ensaio, [empacotar.sh](referencia/repo/tools/empacotar.sh) gerou os dois pacotes em 13 a 14 s num Mac arm64, e o pacote arm64 subiu como serviço (medido). O C# compila igual para as duas arquiteturas.

**Atenção.** `dotnet tool install dotnet-ef` sem versão instala a 11.x. O [dotnet-tools.json](referencia/repo/dotnet-tools.json) fixa a 10.0.15. O SDK 10 cria `Corredor.slnx`, não `.sln`.

**Rejeitado.** Publicação dependente de runtime, que prende a VM ao pacote `aspnetcore-runtime`. Um pacote por arquitetura gerado em runner próprio, que dobra o tempo do workflow sem ganho.

### 1.4 Monorepo sem orquestrador

**Escolha.** Um repositório, uma pasta por parte, cada parte com a ferramenta da própria linguagem. O [package.json](referencia/repo/package.json) da raiz só dá nome aos comandos: `npm run check:api`, `check:migracoes`, `check:pacote`, `check:infra`, `check:terraform`, `check:app`, `check:ml` e `check:iot`. O CI chama os mesmos comandos. O `dorny/paths-filter` decide quais jobs rodam, e um job agregador, `ci-ok`, é o único check obrigatório da `main`.

**Motivo.** As partes conversam por HTTP e MQTT, não por import. Um grafo de dependências não mudaria o que é testado. Um check obrigatório por workflow com filtro `paths` fica pendente quando o workflow é pulado e trava o merge. O `ci-ok` trata job pulado como sucesso.

**Rejeitado.** Nx, Turborepo e Bazel (horas a dias de configuração, segundo o levantamento de ferramentas de monorepo feito pela equipe). npm workspaces, cujo hoisting quebra o Expo. Makefile, porque o `make` não vem no Windows.

### 1.5 Branches, tags e releases

**Escolha.**

- Uma branch longa, a `main`, protegida: PR, uma aprovação e o check `ci-ok`. Branches curtas `tipo/descricao`, merge por squash.
- Uma release é uma tag `vX.Y.Z` criada na `main` pelo responsável pela release. O [release.yml](referencia/repo/.github/workflows/release.yml) confere a tag, testa, gera o SQL das migrações, empacota, confere os pacotes e publica a GitHub Release.

**Motivo.** Uma equipe de cinco pessoas precisa de um único ponto de controle. A tag identifica o que foi testado, o que foi empacotado e o que está no ar, e `GET /api/versao` responde a mesma versão.

**Rejeitado.** Git Flow. Deploy a cada merge, porque, com uma única VM, um merge defeituoso derrubaria o ambiente de demonstração. release-please e changesets, porque a tag criada por bot não dispara outro workflow.

### 1.6 Deploy pelo workflow de release, com aprovação

**Escolha.** Na VM, `sudo corredor deploy vX.Y.Z` baixa o pacote da Release por HTTPS, confere o SHA-256, guarda um `pg_dump`, aplica as migrações, troca o link `current` de forma atômica, reinicia a API e confere `/healthz/ready` e `/api/versao`. Se a versão nova não ficar saudável, o [deploy.sh](referencia/repo/infra/deploy.sh) volta o link para a anterior e sai com código 1 (medido).

Quem dispara:

- Na VM local, a pessoa no terminal.
- Na VM do Azure, o job `implantar` do `release.yml`, por SSH, como o usuário `deploy`, só depois que uma pessoa aprova o ambiente `producao` no GitHub. Uma tag sozinha nunca implanta. Sem a variável `VM_HOST`, o job nem roda.

O desenho mostra a release inteira. A verificação de saúde fica no fim do deploy. Se ela falhar, o `deploy.sh` restaura o link sem intervenção.

![Release e rollback: do que roda no GitHub ao que roda na VM](diagramas/24-release-e-rollback.mmd)

**O usuário `deploy`.** O [sudoers-deploy](referencia/repo/infra/config/sudoers-deploy) deixa esse usuário rodar quatro comandos como root, e o deploy só com uma tag. A regra:

```text
deploy ALL=(root) NOPASSWD: /usr/local/sbin/corredor ^deploy v[0-9]+[.][0-9]+[.][0-9]+(-[0-9A-Za-z.]+)?$, /usr/local/sbin/corredor voltar, ...
```

Um curinga como `corredor deploy *` também aceitaria `deploy --arquivo <qualquer arquivo>`, ou seja, um pacote arbitrário executado como root. A expressão regular do sudo 1.9.10 em diante compara a linha de argumentos inteira. No sudo 1.9.15p5 do Ubuntu 24.04, `deploy v0.3.0` e `deploy v1.0.0-rc.1` passaram, e `deploy --arquivo /etc/shadow`, `deploy v0.3.0 --arquivo /etc/shadow`, `deploy --ultima`, `deploy v0.3.0x`, `voltar x` e `infra main` foram negados (medido).

**Rejeitado.**

- Runner self-hosted na VM. O GitHub desaconselha em repositório público.
- `git pull` e `dotnet publish` na VM. O binário não seria o testado, e a VM precisaria do SDK.
- Docker na VM. Os serviços passariam a depender do runtime de contêineres, e não de units do systemd e do provisionamento por script.
- Deploy por polling num timer da VM. O log do deploy não apareceria no workflow.

### 1.7 Migrações: EF Core é a fonte, o SQL é gerado, o dono do schema aplica

**Escolha.**

1. As migrações são do EF Core: uma pasta `Migrations/` e uma tabela `__EFMigrationsHistory` por `DbContext`, nos schemas `motor`, `cadastro` e `guarda`, criadas com `dotnet ef migrations add`.
2. O [migracoes.sh](referencia/repo/tools/migracoes.sh) gera um script por contexto com `dotnet ef migrations script --idempotent --no-transactions`. Nenhum SQL é escrito à mão. O `empacotar.sh` chama o `migracoes.sh` dentro do `release.yml`, e o script gerado vai no pacote.
3. O CI aplica os scripts gerados duas vezes num Postgres 16 de teste ([verificar-api.sh](referencia/repo/tools/verificar-api.sh)). A segunda rodada prova que o script é idempotente.
4. O deploy guarda um `pg_dump` e aplica cada `<modulo>.sql` com `psql --single-transaction`, conectado como `corredor_<modulo>_migra`, o dono do schema daquele módulo.
5. Nesta entrega, só são aceitas migrações aditivas: criar tabela ou criar coluna que aceita nulo. O [conferir-migracoes.sh](referencia/repo/tools/conferir-migracoes.sh) roda no CI e recusa `DropTable`, `DropColumn`, `RenameTable` e `RenameColumn` no `Up()` de qualquer migração. A regra expira na data definida pela variável `FIM_DA_REGRA` do script.

Trecho do deploy:

```bash
PGPASSWORD="$(segredo_bd "$m-migra")" psql -X -q -v ON_ERROR_STOP=1 --single-transaction \
	-h 127.0.0.1 -U "corredor_${m}_migra" -d "$BD_NOME" \
	-c 'SET client_min_messages = warning' -f "$sql" >/dev/null
```

**Motivo.** O script SQL é texto, serve às duas arquiteturas e só precisa do `psql` na VM. A migração aditiva permite que a versão anterior do binário rode no schema novo, então o rollback nunca altera o banco. Aplicar as migrações como o superusuário `postgres` com `SET ROLE` não isola o módulo, porque um `RESET ROLE` dentro do SQL restaura o superusuário. Por isso a conexão usa um papel sem superusuário. No ensaio, `corredor_motor_migra` fez `RESET ROLE` e continuou `corredor_motor_migra` sem superusuário, e foi recusado ao tentar `SET ROLE postgres`, criar tabela em `cadastro` e criar papel (medido). O mesmo [bd-modulo.sql](referencia/repo/infra/config/bd-modulo.sql) cria os papéis na VM e no CI, então o CI prova o caminho que o deploy usa.

**Rejeitado.**

- Bundle `efbundle`. É um executável por arquitetura, e a release teria quatro artefatos.
- `Database.Migrate()` na partida da API. A API ganharia permissão de mudar tabela.
- SQL escrito à mão. As migrações do EF Core deixariam de ser a fonte única do schema.

### 1.8 HTTPS por túnel, HTTP como caminho estável

**Escolha.**

- O acesso HTTPS da demonstração usa o quick tunnel do cloudflared. `sudo corredor tunel` roda o [tunel.sh](referencia/repo/infra/tunel.sh), que instala o cloudflared, liga o [corredor-tunel.service](referencia/repo/infra/systemd/corredor-tunel.service) e mostra a URL `https://<aleatorio>.trycloudflare.com`. A URL muda a cada partida do serviço, por isso o túnel é ligado uma única vez antes da demonstração.
- Na VM do Azure, o HTTP no nome `<rotulo>.<regiao>.cloudapp.azure.com` é o caminho estável.
- O `02-app.sh` ainda tenta um certificado Let's Encrypt quando há nome público, mas a falha nunca bloqueia o provisionamento. O Nginx não redireciona HTTP para HTTPS e não manda HSTS, então um app configurado com `http://` continua funcionando.
- O app não tem URL padrão escrita no código. `app/src/config.ts` lê `EXPO_PUBLIC_API_URL` e para com erro claro se a variável faltar. Cada desenvolvedor define a URL em `app/.env.local`.

**Motivo.** O sufixo `cloudapp.azure.com` não está na Public Suffix List, então o limite de emissão do Let's Encrypt é dividido com todo o `azure.com` e a emissão pode falhar sem aviso. O túnel funciona na VM local atrás do NAT, sem conta e sem porta aberta. Um padrão `https://` no app apontaria para um endereço que pode não existir, e o HSTS prenderia o navegador ao HTTPS por um dia mesmo quando só o HTTP funciona.

**Rejeitado.** Let's Encrypt como caminho principal. Nome próprio com DNS pago. Expor a porta 1883 ou a 5000.

### 1.9 Rede, IP fixo e nomes

**Escolha.** O [01-rede.sh](referencia/repo/infra/01-rede.sh) cria a interface dummy `corredor0` com o IP fixo `10.77.0.1/24`, pelo systemd-networkd. Os nomes `api.corredor.internal` e `painel.corredor.internal` apontam para esse IP num bloco gerenciado do `/etc/hosts`. A placa principal continua com o DHCP da nuvem ou do Multipass e nunca é tocada. No Azure, o IP privado da placa é fixado pela nuvem no Terraform (`10.20.1.10`).

Com `CORREDOR_DNS_ZONA="sim"` em `/etc/corredor/corredor.conf`, o mesmo script instala o dnsmasq, que responde por `corredor.internal` só no IP de serviço. A configuração entra antes do pacote, então o dnsmasq sobe sem disputar a porta 53 com o systemd-resolved. O bloco do `/etc/hosts` continua valendo. No ensaio, o dnsmasq respondeu `10.77.0.1` para os dois nomes e nada para `example.com` (medido, com o systemd-resolved inativo no contêiner).

**Motivo.** Um script que grava IP fixo na placa principal de uma VM de nuvem pode derrubar o SSH. A interface própria garante um IP fixo real, igual nos dois ambientes. O sufixo `.local` é do mDNS (RFC 6762), e o celular e o systemd-resolved o tratam como multicast. O `.internal` é reservado para uso privado.

**Rejeitado.** Netplan com IP estático na placa principal. dnsmasq como única fonte de nomes, porque o caminho com o systemd-resolved ativo não foi medido.

### 1.10 Banco, papéis e segredos

**Escolha.** Um banco `corredor`, um schema por módulo e dois papéis por módulo, nenhum superusuário:

| Papel | Quem usa | Pode |
|---|---|---|
| `corredor_<modulo>_migra` | o deploy | é dono do schema do módulo, roda as migrações dele, não enxerga outro schema |
| `corredor_<modulo>` | a API, pela string de conexão do módulo | lê e escreve no schema do módulo, não muda tabela, não enxerga outro schema |

As senhas são geradas dentro da VM na primeira execução e ficam em `/etc/corredor/segredos/bd-<modulo>.senha` e `bd-<modulo>-migra.senha`, modo 600, dono root.

**Motivo.** O modelo de domínio diz que nenhum módulo lê o schema de outro. Com um papel por módulo, o Postgres também impede. No ensaio, `corredor_motor` leu `motor."Eventos"`, foi recusado em `cadastro."Idosos"` e não conseguiu criar tabela em `motor` (medido).

**Rejeitado.** Um papel só para a API. Senha vinda do Terraform ou do GitHub, que seria mais um ponto de vazamento.

### 1.11 App, nó do Wokwi, simulador e relógio de demo

**Nó do Wokwi.** O nó publica no broker público `broker.hivemq.com:1883`, no tópico `<prefixo>/corredor/v1/{casaToken}/evento`. O Mosquitto da VM abre uma ponte de saída, com client id próprio por VM derivado do `/etc/machine-id`, e traz os eventos para `corredor/v1/{casaToken}/evento`. Os comandos fazem o caminho inverso. A VM não abre porta MQTT, e o Mosquitto escuta só em `127.0.0.1`.

**O que foi medido na ponte, e o que não foi.** Em um ensaio preliminar, um evento publicado no broker público chegou ao Motor em 2 s, e um comando local chegou ao broker público (medido). Num teste único, um evento publicado no broker público com o Mosquitto da VM parado por 20 s chegou ao Motor 2 s depois da volta (medido uma vez). O log não registra a QoS com que esse evento foi publicado. O nó do Wokwi publica com QoS 0, porque o PubSubClient só publica assim, e nem o MQTT nem o broker público garantem guardar uma mensagem QoS 0 para uma sessão desconectada. Portanto este documento não afirma que os eventos do nó do Wokwi sobrevivem a uma parada do Mosquitto da VM. O buffer do próprio nó é demonstrado na entrega de Physical Computing, com o Wi-Fi do Wokwi desligado.

**Simulador de 39 casas.** Roda dentro da VM como `corredor-simulador.service`, ligado manualmente, e publica no Mosquitto local.

**Relógio de demo e parâmetros.** Pelo modelo de domínio, `PUT /api/demo/relogio` e os valores de demo de `ParametrosDoMotor` só existem no ambiente `Development`. Por isso a variável `API_AMBIENTE_ASPNET` define o `ASPNETCORE_ENVIRONMENT` do `api.env`: `Production` por padrão e `Development` na VM local, que hospeda a demonstração ([local.conf](referencia/repo/infra/config/ambientes/local.conf)). Sem essa configuração, a demonstração não alcança as janelas das 10h e das 15h. Em contrapartida, o ambiente `Development` também liga a página de exceção do ASP.NET Core. O `TratadorDeErros` da Guarda precisa cobrir esse caminho, e a VM do Azure fica em `Production`.

### 1.12 CI

**Escolha.** O [ci.yml](referencia/repo/.github/workflows/ci.yml) roda só a parte que mudou e termina no `ci-ok`.

| Job | Roda quando muda | Passos |
|---|---|---|
| `api` | `api/`, `tools/`, `global.json`, `dotnet-tools.json` | `check:migracoes`, `check:api` (compila, gera e aplica o SQL duas vezes, testa), `check:pacote` (monta o pacote x64 e confere o conteúdo) |
| `infra` | `infra/`, `tools/` | `check:infra` (`bash -n`, `shellcheck`, CRLF, repositório do cloud-init), `check:terraform` (`fmt -check`, `init -backend=false`, `validate`) |
| `app` | `app/` | `typecheck` e `lint` |
| `ml`, `iot` | `ml/`, `iot/` | compila os `.py` e exige o notebook executado |

No desenho, o `ci-ok` é o único check obrigatório da `main`, e cada job só roda quando a pasta dele muda.

![Pipeline de CI: ci.yml](diagramas/23-ci.mmd)

Ficam fora do check obrigatório: lock de NuGet, `dotnet format`, compilação do ESP32 e um job de ponta a ponta que provisionaria um runner do GitHub. Cada um deles aumentaria o risco de bloqueio da `main`. O job de ponta a ponta também exigiria nos scripts um ramo `ci` nunca executado, e o ensaio em contêiner da seção 8 cobre a mesma verificação.

## 2. Árvore do repositório

As pastas `api/`, `app/`, `ml/` e `iot/` aparecem até o nível que a infra e o CI usam. O restante dessas pastas está descrito no modelo de domínio. Os arquivos marcados com `*` estão em [referencia/repo/](referencia/repo/).

O desenho resume a árvore em uma linha por parte. A lista completa vem logo abaixo.

![Árvore do repositório, resumida](diagramas/25-repositorio.mmd)

```text
corredor-aceso/
├── README.md                                ficha, uma seção por disciplina, tabela da rubrica de OS, links dos vídeos
├── prompts.md                               prompts de IA usados, exigido pelo enunciado
├── integrantes.txt                          nomes e RMs, exigido por Physical Computing
├── package.json *                           só nomes de comando (check:*, dev:*, empacotar)
├── global.json *                            fixa o SDK .NET 10.0.100
├── dotnet-tools.json *                      fixa o dotnet-ef 10.0.15
├── compose.dev.yml *                        Postgres e Mosquitto no notebook de quem desenvolve
├── .editorconfig *                          indentação e fim de linha por tipo de arquivo
├── .gitattributes *                         força LF, para o Bash não quebrar com CRLF do Windows
├── .gitignore *                             ignora state, segredos e toda chave que não seja .pub
├── .shellcheckrc *                          faz o shellcheck seguir os source de infra/lib
├── .github/
│   ├── CODEOWNERS *                         pede revisão ao dono de cada projeto e pasta
│   ├── pull_request_template.md *           três perguntas de conferência antes do merge
│   ├── actions/ssh-vm/action.yml *          roda sudo corredor <comando> na VM, com lista de comandos aceitos
│   └── workflows/
│       ├── ci.yml *                         jobs por pasta e o agregador ci-ok
│       ├── release.yml *                    tag: testa, gera SQL, empacota, publica e implanta com aprovação
│       └── operacao.yml *                   status, voltar e reiniciar a API pelo GitHub, com aprovação
├── docs/arquitetura/                        arquitetura, modelo de domínio e este documento
├── api/
│   ├── Corredor.slnx                        solução (o SDK 10 cria .slnx)
│   ├── Directory.Build.props                net10.0, Nullable e ImplicitUsings
│   ├── src/Corredor.Contratos/              tipos e interfaces compartilhados
│   ├── src/Corredor.Motor/                  módulo Motor, MotorDbContext e Infra/Migrations, schema motor
│   ├── src/Corredor.Cadastro/               módulo Cadastro, CadastroDbContext e Infra/Migrations, schema cadastro
│   ├── src/Corredor.Guarda/                 módulo Guarda, GuardaDbContext e Infra/Migrations, schema guarda, painel/index.html
│   ├── src/Corredor.Api/                    host: Program.cs, /healthz/live, /healthz/ready e /api/versao
│   └── tests/Corredor.{Motor,Cadastro,Guarda}.Testes/   um projeto xUnit por módulo
├── app/                                     Expo; src/config.ts lê EXPO_PUBLIC_API_URL, sem URL padrão
├── ml/                                      notebook executado, prever_hoje.py, requirements-vm.txt
├── iot/                                     sketch do Wokwi e simulador/ de 39 casas
├── infra/
│   ├── provisionar.sh *                     ponto de entrada: 01, 02, 03 e verificar; compara InvocationID antes e depois
│   ├── 01-rede.sh *                         hostname, fuso, IP fixo em corredor0, nomes, zona DNS opcional, firewall, chaves SSH
│   ├── 02-app.sh *                          pacotes, usuários, Postgres e papéis, Mosquitto com ponte, Nginx, TLS opcional, units, primeira versão
│   ├── 03-monitor.sh *                      monitor como serviço, unit de alerta de falha definitiva e rotação de logs
│   ├── monitor.sh *                         mede CPU, memória, disco e serviços e gera alertas
│   ├── verificar.sh *                       confere a VM de ponta a ponta; é o sudo corredor status
│   ├── deploy.sh *                          implanta uma release, migra como o dono de cada schema, rollback automático e manual
│   ├── teste-de-falha.sh *                  broker, API, monitor, CPU, memória e disco, com resumo PASSOU e FALHOU
│   ├── tunel.sh *                           instala o cloudflared e liga o túnel HTTPS temporário
│   ├── bin/corredor *                       comando único de operação da VM
│   ├── lib/comum.sh *                       funções idempotentes: pacotes, arquivos, usuários, trava, senhas do banco
│   ├── lib/auditoria.sh *                   quem rodou o quê, quando e como terminou
│   ├── cloud-init/corredor.yaml *           primeiro boot no Azure: git clone e provisionar.sh
│   ├── config/
│   │   ├── corredor.conf.padrao *           configuração comum: rede, módulos do banco, broker, casa de teste
│   │   ├── ambientes/producao.conf *        VM do Azure: nome público
│   │   ├── ambientes/local.conf *           VM do Multipass: API em Development, para o relógio de demo
│   │   ├── bd-modulo.sql *                  dono e papel da API de um módulo; o mesmo arquivo na VM e no CI
│   │   ├── mosquitto-corredor.conf.modelo * broker local e ponte de saída
│   │   ├── nginx-rotas.conf.modelo *        /api para a API, /painel/ estático, /healthz bloqueado
│   │   ├── nginx-site.conf.modelo *         servidor HTTP
│   │   ├── nginx-tls.conf.modelo *          servidor HTTPS, só com certificado, sem redirecionamento e sem HSTS
│   │   ├── logrotate-corredor *             rotação de /var/log/corredor
│   │   ├── sudoers-deploy *                 os quatro comandos do usuário deploy, o deploy só com tag
│   │   └── dev/mosquitto.conf *             broker do compose.dev.yml
│   ├── systemd/
│   │   ├── corredor-api.service *           API, Restart=on-failure com espera crescente, OnFailure para o alerta
│   │   ├── corredor-monitor.service *       monitor, Restart=always e watchdog de 60 s
│   │   ├── corredor-alerta@.service *       grava ALERTA de falha definitiva no alertas.log e no journal
│   │   ├── corredor-ml.service *            roda o prever_hoje.py uma vez, OnFailure para o alerta
│   │   ├── corredor-ml.timer *              dispara o job às 05h
│   │   ├── corredor-simulador.service *     simulador de 39 casas, ligado à mão
│   │   └── corredor-tunel.service *         quick tunnel do cloudflared, ligado à mão
│   ├── nuvem/
│   │   ├── versions.tf *                    Terraform e provider azurerm 4.x
│   │   ├── variables.tf *                   região, tamanho, rótulo de DNS e origem do SSH
│   │   ├── producao.auto.tfvars *           valores versionados, sem segredo
│   │   ├── main.tf *                        grupo, rede, NSG, IP público, placa e VM
│   │   ├── outputs.tf *                     IP, FQDN e comando para acompanhar o primeiro boot
│   │   ├── .terraform.lock.hcl              versão exata do provider, gerada pelo primeiro terraform init
│   │   └── preflight.sh *                   regiões, tamanhos e imagem que a assinatura aceita
│   ├── local/subir.sh *                     VM do Multipass com o cloud-init, para desenvolver os scripts
│   └── chaves/
│       ├── README.md *                      regra: só .pub entra aqui
│       ├── equipe/<nome>.pub                chave pública de cada pessoa
│       └── deploy.pub                       chave pública do GitHub Actions
└── tools/
    ├── verificar-api.sh *                   compila, aplica o SQL duas vezes como o dono de cada schema e testa
    ├── migracoes.sh *                       gera o SQL idempotente de cada DbContext com dotnet ef
    ├── conferir-migracoes.sh *              recusa migração que apaga ou renomeia tabela ou coluna até 20/11
    ├── empacotar.sh *                       monta os pacotes x64 e arm64
    ├── conferir-pacote.sh *                 confere o conteúdo e a versão de um pacote
    ├── conferir-infra.sh *                  bash -n, shellcheck, CRLF e coerência do cloud-init
    └── conferir-python.py *                 compila os .py e exige notebook executado
```

Na VM:

```text
/opt/corredor/repo/                    clone do repositório
/opt/corredor/releases/vX.Y.Z/         api/, migracoes/, painel/, VERSION, COMMIT (dono root)
/opt/corredor/current -> releases/vX.Y.Z      versão no ar
/opt/corredor/previous -> releases/vX.Y.W     alvo do rollback
/etc/corredor/api.env                  gerado pelo 02-app.sh: ambiente, MQTT e as três strings de conexão (640 root:corredor)
/etc/corredor/segredos.env             segredos digitados à mão, como a chave do LLM (640 root:corredor)
/etc/corredor/segredos/bd-*.senha      senhas dos papéis do banco, geradas na VM (600 root)
/etc/corredor/monitor.env              limites próprios do monitor, opcional
/var/log/corredor/auditoria.log        uma linha por início, erro e fim de cada script
/var/log/corredor/execucao/            saída completa de cada execução
/var/log/corredor/deploy.log           eventos de deploy e de rollback
/var/log/corredor/monitor/             monitor.log (métricas) e alertas.log
/var/backups/corredor/                 pg_dump antes de cada deploy, os 5 mais recentes
```

## 3. Ambientes e segredos

### 3.1 Ambientes

| | `dev` | `local` | `producao` |
|---|---|---|---|
| Onde | notebook de cada pessoa | VM do Multipass | VM do Azure, opcional |
| Para quê | escrever código | demonstração de todas as disciplinas, ensaio, infra | endereço estável para o celular, se existir |
| Como nasce | `npm run dev:deps` e `npm run dev:api` | `multipass launch` e `provisionar.sh` à mão | `terraform apply` e cloud-init |
| Migrações | `dotnet ef database update` | SQL gerado, da release | SQL gerado, da release |
| Recebe versão | código da branch | `sudo corredor deploy vX.Y.Z` | `release.yml` com aprovação |
| Endereço para o app | `http://<ip-do-notebook>:5000` | URL do túnel | `http://<fqdn>`, ou o túnel |
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Development` | `Production` |

### 3.2 O que fica onde

| Item | Onde | No Git? |
|---|---|---|
| Configuração comum | `infra/config/corredor.conf.padrao` | sim |
| Configuração por ambiente | `infra/config/ambientes/<ambiente>.conf` | sim |
| Ajuste local de uma VM, como `CORREDOR_DNS_ZONA=sim` | `/etc/corredor/corredor.conf` | não |
| Limites do monitor | `/etc/corredor/monitor.env` | não |
| Região, tamanho e rótulo de DNS do Azure | `infra/nuvem/producao.auto.tfvars` | sim |
| Assinatura do Azure | `ARM_SUBSCRIPTION_ID` no notebook do responsável pela infraestrutura | não |
| State do Terraform | notebook do responsável pela infraestrutura | não |
| Chaves públicas da equipe e do CI | `infra/chaves/` | só `.pub` |
| Chave privada do CI | segredo `VM_CHAVE_DEPLOY` do ambiente `producao` | não |
| Nome da VM e linha de `known_hosts` | variáveis `VM_HOST` e `VM_HOSTS_CONHECIDOS` | não, e não são segredo |
| Senhas do banco | `/etc/corredor/segredos/`, geradas na VM | não |
| Chave do LLM | `/etc/corredor/segredos.env`, com `sudoedit` | não |
| URL da API para o app | `app/.env.local` | não |
| Senha do Postgres do `compose.dev.yml` | o próprio arquivo, só vale em `localhost` | sim, de propósito |

Nunca entram no Git: chave privada de qualquer tipo, `terraform.tfstate`, `.terraform/`, arquivos `.env`, senha de banco de VM, token do GitHub e dado real de pessoa. O [.gitignore](referencia/repo/.gitignore) recusa em `infra/chaves/` todo arquivo que não termine em `.pub`. O secret scanning e a push protection do GitHub, gratuitos em repositório público, devem ficar ligados.

### 3.3 Configuração do GitHub, feita uma vez

1. Repositório público, merge por squash como único método.
2. Proteção da `main`: PR, uma aprovação, check obrigatório `ci-ok`, sem force-push.
3. Ambiente `producao` com duas pessoas como revisoras obrigatórias e tags permitidas `v*`. Sem revisor obrigatório, a tag implantaria sozinha, por isso `VM_HOST` só é gravada depois deste passo.
4. Segredo `VM_CHAVE_DEPLOY` no ambiente `producao`. Variáveis `VM_HOST` e `VM_HOSTS_CONHECIDOS` no repositório. Os passos 3 e 4 só existem se houver VM do Azure.

## 4. Controle de runtime

### 4.1 Um comando para operar a VM

```text
sudo corredor status                      versão no ar, rede, serviços, saúde, túnel e últimos alertas
sudo corredor deploy vX.Y.Z | --ultima    implanta uma release
sudo corredor deploy --arquivo PACOTE     implanta um pacote feito no notebook
sudo corredor voltar                      rollback manual
sudo corredor semear                      carrega a demo e ativa a casa do teste de falha (só Development)
sudo corredor reiniciar api
sudo corredor logs api|monitor|alertas|deploy|auditoria
sudo corredor infra [ref]                 atualiza infra/ para uma ref do Git e provisiona de novo
sudo corredor teste-de-falha [cenário]
sudo corredor tunel [parar]               liga ou desliga a URL HTTPS temporária
```

Pelo GitHub, o [operacao.yml](referencia/repo/.github/workflows/operacao.yml) roda `status`, `voltar` e `reiniciar api` na VM do Azure, com aprovação.

### 4.2 Saúde e versão

São rotas do host `Corredor.Api`, fora da tabela de rotas do modelo de domínio:

| Rota | Quem chama | Exposta pelo Nginx? |
|---|---|---|
| `GET /healthz/live` | diagnóstico | não, o Nginx responde 404 para `/healthz` |
| `GET /healthz/ready` | `deploy.sh`, `monitor.sh`, `verificar.sh`, `teste-de-falha.sh` | não |
| `GET /api/versao` | `deploy.sh`, o job `implantar`, qualquer pessoa | sim |
| `GET /api/casas/{token}/linha-do-tempo` | `teste-de-falha.sh`, para a casa de teste | sim, é rota do Motor |

### 4.3 Logs

| O que | Onde | Como ler |
|---|---|---|
| API | journald | `sudo corredor logs api` |
| Métricas, uma linha por ciclo | `/var/log/corredor/monitor/monitor.log` | `sudo corredor logs monitor` |
| Alertas do monitor e de falha definitiva | `/var/log/corredor/monitor/alertas.log` e journald | `sudo corredor logs alertas`, `journalctl -t corredor-monitor -p warning` |
| Auditoria dos scripts | `/var/log/corredor/auditoria.log` e journald | `sudo corredor logs auditoria` |
| Saída completa de cada script | `/var/log/corredor/execucao/` | `ls -t /var/log/corredor/execucao` |
| Deploys e rollbacks | `/var/log/corredor/deploy.log` | `sudo corredor logs deploy` |
| Quem aprovou cada deploy no Azure | página do workflow | aba Actions |

### 4.4 Monitor, reinício e alerta de falha definitiva

O [monitor.sh](referencia/repo/infra/monitor.sh) roda a cada 15 s como `corredor-monitor.service`, com o usuário `corredor-mon`. Ele mede CPU por `/proc/stat` (85%, dois ciclos), memória por `MemAvailable` (85%), disco por `df -P /` (80%), Mosquitto, Nginx, Postgres e `/healthz/ready`. Cada medida é uma máquina de dois estados: `ALERTA` quando começa, repetição a cada 20 ciclos, `NORMALIZADO` quando acaba. O monitor só avisa. Quem religa é o systemd.

| Serviço | Política | Efeito |
|---|---|---|
| `corredor-api` | `Restart=on-failure` com `RestartMode=direct`, 2 s crescendo até 30 s, no máximo 10 partidas em 5 minutos, `OnFailure=corredor-alerta@%n.service` | Religa depois de queda ou `kill -9` sem passar pelo estado `failed`. Se desistir, grava `ALERTA falha definitiva` no `alertas.log` e no journal. |
| `corredor-monitor` | `Restart=always`, sem limite de partidas, `WatchdogSec=60` | Religa sempre. Se o laço travar sem morrer, o watchdog mata e religa. |
| `corredor-ml` | `oneshot`, `OnFailure=corredor-alerta@%n.service` | Uma falha do job das 05h vira alerta. |
| Postgres, Mosquitto, Nginx | units da distribuição | O monitor alerta se pararem. |

O ensaio revelou dois cuidados necessários na unit de alerta:

- Sem `RestartMode=direct`, cada queda da API passa pelo estado `failed` antes do religamento, e o `OnFailure` disparou o alerta de falha definitiva cinco vezes num rollback comum e num `kill -9` (medido). Com `RestartMode=direct`, disponível no systemd 254 em diante, o alerta só sai quando o systemd desiste.
- A unit de alerta roda como `corredor-mon`. Rodando como root, ela criou o `alertas.log` com dono root, e o monitor parou de gravar os próprios alertas sem erro visível (medido).

### 4.5 Teste de falha

`sudo corredor teste-de-falha` roda seis cenários nesta ordem e termina com um resumo. Cada cenário devolve a VM ao estado em que a encontrou, e a limpeza roda também se o script for interrompido.

| Cenário | Falha provocada | O que o script confere |
|---|---|---|
| `broker` | `systemctl stop mosquitto` | O monitor alerta. O script, como publicador local, guarda 3 eventos que não saíram. Quando o broker volta, espera o `/healthz/ready`, que só responde com o Motor reconectado ao broker, e reenvia os 3 duas vezes com a hora original e `bufferizado: true`. A linha do tempo da casa de teste mostra cada evento uma vez só, pela chave `(casaToken, seq, ts)`. O monitor registra a volta. |
| `api` | `kill -9` na API | O systemd religa e `/healthz/ready` volta. |
| `monitor` | `kill -9` no monitor | O systemd religa com outro PID. |
| `carga` | um laço por CPU | Alerta de CPU e depois `NORMALIZADO`. |
| `memoria` | `stress-ng` ocupa memória até 5 pontos acima do limite | Alerta de memória e depois `NORMALIZADO`. Pula se a memória já está no limite ou se sobrariam menos de 256 MB. |
| `disco` | `fallocate` leva o disco `/` a 1 ponto acima do limite | Alerta de disco e depois `NORMALIZADO`. Pula se o disco já está no limite ou se sobrariam menos de 512 MB. |
| `travado`, opcional | `kill -STOP` no monitor | O watchdog mata e religa, depois de 60 s. |

O cenário `broker` não envolve o nó do Wokwi, pelo motivo da seção 1.11.

## 5. O que a infra exige da API

Estes itens são contrato entre a infra e quem escreve `Corredor.Api` e os módulos. A API mínima do ensaio cumpre os itens 1 a 6, o 9 e o 11.

1. `GET /healthz/live` responde 200 se o processo vive. `GET /healthz/ready` responde 200 só com os três `DbContext` conectando e o cliente MQTT conectado.
2. `GET /api/versao` responde `{"versao":"0.3.0","commit":"<sha>","ambiente":"local"}`, a partir do `AssemblyInformationalVersion` que o `empacotar.sh` preenche.
3. A configuração vem do ambiente: `ConnectionStrings__Motor`, `ConnectionStrings__Cadastro`, `ConnectionStrings__Guarda`, `Mqtt__Host`, `Mqtt__Porta`, `Mqtt__ClienteId`, `Corredor__Ambiente` e `Llm__ApiKey`.
4. Cada `DbContext` usa o schema do módulo e guarda o histórico nele: `MigrationsHistoryTable("__EFMigrationsHistory", "motor")`.
5. A API não chama `Database.Migrate()`, e o `dotnet ef migrations script` constrói o host sem banco e sem broker.
6. O índice único de `EventoSensor` é `(CasaToken, Seq, OcorridoEm)`, e uma repetição conta como sucesso.
7. O cliente MQTT do Motor usa client id fixo, QoS 1 e reconexão automática. Para o broker guardar as mensagens enquanto o Motor reconecta, a sessão precisa sobreviver à desconexão. No MQTT 3.1.1 isso é `CleanSession = false`. No MQTT 5, que é o padrão provável do MQTTnet 5, `CleanSession = false` não basta e falta um `SessionExpiryInterval` maior que zero (inferido). No ensaio, a API mínima reconectou 2 s depois da volta do Mosquitto, e as mensagens publicadas antes disso não chegaram (medido).
8. O seed do Motor ativa a casa `fa11fa11fa11fa11fa11fa11fa11fa11` (`CASA_TESTE_TOKEN`), usada pelo teste de falha. Se a linha do tempo exigir autenticação, o valor `TESTE_AUTORIZACAO="Bearer ..."` deve ser definido em `/etc/corredor/teste-de-falha.env`.
9. A página do painel fica em `api/src/Corredor.Guarda/painel/index.html`. O pacote a leva, e o Nginx a serve em `/painel/`.
10. Os valores de demo de `ParametrosDoMotor` e a rota `PUT /api/demo/relogio` ligam com `ASPNETCORE_ENVIRONMENT=Development`.
11. A API escreve log só na saída padrão.

## 6. Runbooks

Há dois papéis. O responsável pela infraestrutura responde por `infra/`, `tools/` e `.github/`. O responsável pela release cria as tags.

### 6.1 Primeiro provisionamento, do zero

O desenho mostra a ordem. A VM local e a do Azure chegam ao mesmo `provisionar.sh`, e o `lib/auditoria.sh` registra cada etapa.

![Ordem de provisionamento da VM](diagramas/22-provisionamento.mmd)

Repositório, uma vez:

1. Copie os arquivos seguindo o [LEIA-ME](referencia/repo/LEIA-ME.md) e troque os três valores que ele lista.
2. Cada pessoa cria uma chave com `ssh-keygen -t ed25519` e envia o `.pub` em `infra/chaves/equipe/<nome>.pub`, por PR.
3. Aplique a configuração do GitHub da seção 3.3, passos 1 e 2.

VM local:

1. Rode `multipass launch 24.04 --name corredor-ubs --cpus 2 --memory 4G --disk 20G`.
2. Rode `multipass shell corredor-ubs`.
3. Rode `sudo git clone --depth 1 https://github.com/<org>/corredor-aceso.git /opt/corredor/repo`.
4. Rode `sudo bash /opt/corredor/repo/infra/provisionar.sh`. O fim mostra `== tudo certo ==`, com a API em `[PENDENTE]` antes da primeira release.
5. Depois da primeira release, rode `sudo corredor deploy v0.1.0` e `sudo corredor semear`, que ativa a casa do teste de falha.
6. Rode `sudoedit /etc/corredor/segredos.env`, preencha `Llm__ApiKey=` e rode `sudo corredor reiniciar api`.

VM do Azure, pelo responsável pela infraestrutura:

1. Rode `az login` com a conta da FIAP e `infra/nuvem/preflight.sh eastus`. Se a região ou o tamanho não aparecerem, ajuste `producao.auto.tfvars`.
2. Rode `export ARM_SUBSCRIPTION_ID=$(az account show --query id -o tsv)`.
3. Em `infra/nuvem/`, rode `terraform init`, `terraform plan` e `terraform apply`. Envie o `.terraform.lock.hcl` por PR.
4. Copie `terraform output fqdn` para `CORREDOR_DOMINIO_PUBLICO` em `infra/config/ambientes/producao.conf`, por PR.
5. Rode o comando de `terraform output acompanhar_primeiro_boot`. A primeira linha do status deve dizer `ambiente producao`. Se disser `local`, rode `echo producao | sudo tee /etc/corredor/ambiente` e `sudo corredor infra main`.
6. Confira de fora: `curl http://<fqdn>/api/versao`.

Deploy pelo GitHub, só se a VM do Azure existir:

1. Crie a chave do CI: `ssh-keygen -t ed25519 -N '' -C github-actions -f /tmp/corredor-deploy`.
2. Envie `/tmp/corredor-deploy.pub` como `infra/chaves/deploy.pub`, por PR.
3. Rode `gh secret set VM_CHAVE_DEPLOY --env producao < /tmp/corredor-deploy` e apague os dois arquivos.
4. Rode `gh variable set VM_HOST --body <fqdn>` e `ssh-keyscan -t ed25519 <fqdn> | gh variable set VM_HOSTS_CONHECIDOS`.
5. Na VM, rode `sudo corredor infra main`. O `02-app.sh` cria o usuário `deploy` com a chave e o sudoers.

### 6.2 Uma release

1. Confira que o último `ci` da `main` está verde.
2. Crie a tag na `main`:

	```bash
	git switch main && git pull
	git tag -a v0.3.0 -m "v0.3.0"
	git push origin v0.3.0
	```

3. Acompanhe o job `publicar` do workflow `release`. Ele cria a Release com os dois pacotes e o `SHA256SUMS`.
4. Na VM local, rode `sudo corredor deploy v0.3.0`.
5. Se a VM do Azure existir, aprove o job `implantar` no ambiente `producao` e espere a resposta de `GET /api/versao` no resumo do job.
6. Se o deploy falhar, a VM já voltou para a versão anterior. Leia a causa com `sudo corredor logs deploy`.
7. Se a release mudou `infra/`, rode `sudo corredor infra v0.3.0` em cada VM.

Para testar um pacote antes da primeira release, rode `npm run empacotar -- v0.0.1 linux-arm64` no notebook, copie o pacote com `multipass transfer` e rode `sudo corredor deploy --arquivo <pacote>` na VM. Esse caminho existe só para quem tem shell na VM. O usuário `deploy` não consegue usá-lo.

### 6.3 Um rollback

| Situação | Ação |
|---|---|
| A versão nova não ficou saudável no deploy | Nada. O `deploy.sh` já voltou e saiu com código 1. |
| A versão nova subiu, mas tem defeito | `sudo corredor voltar`. No Azure, também pelo `operacao.yml`. |
| É preciso ir para uma versão mais antiga | `sudo corredor deploy v0.2.0`. No Azure, também pelo `release.yml` em execução manual. |
| Uma mudança em `infra/` quebrou a VM | `sudo corredor infra <tag anterior>`. |
| Uma migração estragou dado | Restaure o dump, abaixo. |

Restaurar o banco é o último recurso, porque apaga o que entrou depois do deploy:

```bash
sudo systemctl stop corredor-api
ls -t /var/backups/corredor/
sudo -u postgres pg_restore --clean --if-exists -d corredor /var/backups/corredor/<arquivo>.dump
sudo corredor deploy <tag anterior>
```

Para ensaiar o rollback, rode `sudo corredor deploy <tag nova> --simular-falha`.

## 7. Como cada item da rubrica de Operating Systems é atendido

A rubrica está nas páginas 35 a 37 do enunciado, que exige o provisionamento automático dentro da VM. A tabela relaciona cada item da rubrica aos arquivos que o implementam e aos comandos que o evidenciam.

| Item da rubrica | Pontos | Arquivo | Como verificar |
|---|---|---|---|
| Script de provisionamento de infra (redes, DNS, IP) | 2 | [01-rede.sh](referencia/repo/infra/01-rede.sh) | `hostname`, `ip addr show corredor0`, `getent hosts api.corredor.internal`, `sudo ufw status`. Com a zona ligada, `dig @10.77.0.1 api.corredor.internal`. |
| Script de provisionamento da aplicação (dependências, configurações, orquestração) | 2 | [02-app.sh](referencia/repo/infra/02-app.sh), [provisionar.sh](referencia/repo/infra/provisionar.sh), [bd-modulo.sql](referencia/repo/infra/config/bd-modulo.sql) | saída do provisionamento, `sudo corredor status`, `/etc/corredor/api.env` sem as senhas |
| Script de monitoramento com alertas de CPU, disco e memória | 2 | [monitor.sh](referencia/repo/infra/monitor.sh) | `sudo corredor logs monitor`, `sudo corredor logs alertas`, cenários `carga`, `memoria` e `disco` |
| Monitoramento como serviço tolerante a falhas | 1 | [corredor-monitor.service](referencia/repo/infra/systemd/corredor-monitor.service), [corredor-alerta@.service](referencia/repo/infra/systemd/corredor-alerta@.service), [03-monitor.sh](referencia/repo/infra/03-monitor.sh) | `systemctl status corredor-monitor`, cenário `monitor` |
| Logs de auditoria e execução dos scripts | 1 | [auditoria.sh](referencia/repo/infra/lib/auditoria.sh) | `sudo corredor logs auditoria`, `ls /var/log/corredor/execucao` |
| Funcionamento completo | 2 | [teste-de-falha.sh](referencia/repo/infra/teste-de-falha.sh), [verificar.sh](referencia/repo/infra/verificar.sh), [deploy.sh](referencia/repo/infra/deploy.sh) | `sudo corredor teste-de-falha`, o painel no navegador, a segunda execução sem mudança |

As migrações do banco não fazem parte da rubrica de Operating Systems. O README aponta as pastas `Migrations/` do EF Core de cada módulo, o `tools/migracoes.sh`, que gera o SQL a partir delas, e o job `api` do CI, que aplica o SQL duas vezes.

## 8. Medições e limitações

O ensaio rodou num contêiner Docker `ubuntu:24.04` arm64 privilegiado, com systemd 255 como PID 1, num Mac arm64, com os 60 arquivos finais. A API de ensaio é uma versão mínima da API com os três `DbContext`, o índice único `(CasaToken, Seq, Ts)`, a linha do tempo no formato do modelo de domínio e uma página `painel/index.html`. Os pacotes foram gerados pelo `empacotar.sh` final. Os logs do ensaio não estão no repositório.

Os arquivos rodaram quatro vezes nesta bancada, sempre numa instância nova. As três primeiras rodadas encontraram cinco problemas, todos corrigidos: dois na unit de `OnFailure` (seção 4.4), um nos cenários de memória e disco quando o uso já está no limite, uma condição de corrida no cenário do broker (o reenvio chegava antes de o Motor reconectar) e a permissão da pasta da release no `deploy.sh`. A quarta rodada, com os arquivos de [referencia/repo/](referencia/repo/) na versão atual, teve estes resultados:

| Passo | Código | Tempo | O que mostrou |
|---|---|---|---|
| Provisionamento do zero | 0 | 41 s | 13, 20 e 7 mudanças em `01-rede`, `02-app` e `03-monitor`. Três schemas. API em `[PENDENTE]`. |
| Provisionamento de novo | 0 | 1 s | 0 mudança nas três etapas. `corredor-monitor: não reiniciou`, mesmo `InvocationID`. |
| `deploy v0.0.1` | 0 | 2 s | Os três scripts gerados pelo EF Core aplicados como `corredor_motor_migra`, `corredor_cadastro_migra` e `corredor_guarda_migra`. |
| `deploy v0.0.2` | 0 | 3 s | Upgrade, `previous` em v0.0.1. |
| `deploy v0.0.3`, binário que termina com erro | 1 | 15 s | `deploy_falhou`, depois `rollback_ok` para v0.0.2. |
| Provisionamento com a API no ar | 0 | 1 s | 0 mudança. API e monitor com o mesmo `InvocationID` antes e depois. `/api/versao` e `/painel/` pelo Nginx. |
| `corredor teste-de-falha` | 0 | 147 s | 12 PASSOU: alerta do Mosquitto, reenvio duplo, cada evento uma vez na linha do tempo, volta do Mosquitto, API e monitor religados em 3 e 4 s, alerta e volta de CPU, de memória e de disco. |
| Permissões | 0 | 1 s | `deploy` negado em `--arquivo`, `--ultima` e `infra`. Papéis do banco como na seção 1.10. Único superusuário: `postgres`. `api.env` com `Development` e três strings de conexão. |
| `OnFailure` | 0 | 3 s | Uma unit de teste que falha gravou o `ALERTA falha definitiva`. Nenhum alerta desses saiu da API, que caiu e religou várias vezes. |
| Zona DNS ligada, depois de novo | 0 | 9 s e 2 s | 5 mudanças, depois 0. `dig @10.77.0.1` respondeu os dois nomes. |

Para que os cenários de memória e de disco não ocupassem a VM do Docker, compartilhada com outros contêineres, o ensaio ajustou os limites do monitor para perto do uso atual em `/etc/corredor/monitor.env` (memória 18%, disco 11%). A ocupação de memória e de disco foi real. Numa VM de 4 GB e 20 GB com os limites padrão, o `stress-ng` ocupa cerca de 2,5 GB e o arquivo do disco passa de 10 GB, e os dois cabem nas margens que o script confere (inferido, não rodou numa VM desse tamanho).

**Medido à parte, nos arquivos finais.**

- `bash -n` e `shellcheck -x` nos 20 scripts de `infra/` e `tools/`, sem aviso. `actionlint` nos três workflows, sem aviso.
- `terraform fmt -check` e `terraform validate` em `infra/nuvem/` com o provider `azurerm` 4.x baixado, com e sem chave em `infra/chaves/equipe/`.
- `tools/verificar-api.sh` contra um Postgres 16: três scripts gerados por `dotnet ef`, aplicados duas vezes, cada um como o dono do schema. As seis tabelas ficaram com o dono certo.
- `tools/conferir-migracoes.sh`: passou com as três migrações iniciais e falhou com código 1 numa migração de teste com `RenameColumn` no `Up()`.
- `tools/empacotar.sh` e `tools/conferir-pacote.sh`: pacotes x64 e arm64 de duas versões, e um pacote com `VERSION` errada recusado.
- A regra do sudoers no sudo 1.9.15p5, com as doze combinações da seção 1.6.

**Só lint, nunca rodou.**

- Os três workflows no GitHub: gatilhos, filtro por pasta, `ci-ok`, ambiente com aprovação, `gh release create` e o job de SSH. Os passos novos (`check:migracoes`, `check:pacote`, `check:terraform` e a conferência dos pacotes na release) rodaram no notebook, não no GitHub.
- `infra/nuvem/` com `terraform apply`. Só `validate` rodou.
- `corredor-ml.service` e `corredor-tunel.service`. Só `systemd-analyze verify` rodou, no systemd 255.

**Nunca rodou.**

1. Assinatura Azure for Students: região, tamanho `Standard_B2ls_v2`, imagem, chave ed25519 em `admin_ssh_key`.
2. O cloud-init no primeiro boot, no Azure e no Multipass, e a detecção de `producao` pelo `cloud-id`.
3. O `multipass launch` sem cloud-init da seção 6.1 e o provisionamento numa VM do Multipass. O ensaio foi em contêiner.
4. `infra/tunel.sh` e `corredor-tunel.service`: a instalação do cloudflared, a URL no journal e o Expo Go com a URL do túnel.
5. O redirecionamento de `/releases/latest` do GitHub, usado por `corredor deploy --ultima`.
6. A emissão do Let's Encrypt para um nome `cloudapp.azure.com`.
7. O provisionamento em x64. A bancada era arm64.
8. O ramo de swap do `02-app.sh`, que só age com menos de 3 GB de memória.
9. A zona do dnsmasq com o systemd-resolved ativo, que é o caso da VM real. No contêiner ele estava inativo.
10. O `OnFailure` disparado pela própria API depois de 10 partidas. O ensaio disparou a unit de alerta por uma unit de teste com o mesmo `OnFailure`.
11. Os eventos do nó do Wokwi durante uma parada do Mosquitto, pelo motivo da seção 1.11.
12. `infra/nuvem/preflight.sh` e `infra/local/subir.sh`.
