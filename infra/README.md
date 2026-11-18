# Infraestrutura da VM do Corredor Aceso

Esta pasta provisiona uma VM Ubuntu 24.04 do zero: rede, nomes, firewall, Postgres, Mosquitto, Nginx, a API e o monitor. É a entrega de Operating Systems. O desenho completo, com as decisões e os runbooks, está em [05-infra-e-entrega.md](../docs/arquitetura/05-infra-e-entrega.md).

Todo script pode rodar de novo quantas vezes for preciso. Cada função lê o estado atual e muda só a diferença. Uma segunda execução sem mudança termina com `0 mudança(s)` e não reinicia nada.

## Onde está cada item da rubrica

| Item da rubrica | Pontos | Arquivo | Evidência |
|---|---|---|---|
| Script de infra: rede, DNS, IP | 2 | `01-rede.sh` | [Rede e DNS na VM](../docs/evidencias/os/vm/05-rede-e-dns.txt) |
| Script da aplicação | 2 | `02-app.sh`, `provisionar.sh`, `deploy.sh` | [Provisionamento](../docs/evidencias/os/vm/01-provisionamento.txt), [aplicação](../docs/evidencias/os/vm/02-aplicacao.txt) |
| Monitor com alertas de CPU, disco e memória | 2 | `monitor.sh` | [Métricas e alertas](../docs/evidencias/os/vm/07-monitor-e-alertas.txt) |
| Monitor como serviço tolerante a falhas | 1 | `03-monitor.sh`, `systemd/corredor-monitor.service` | [Recuperação](../docs/evidencias/os/vm/08-recuperacao-e-watchdog.txt), [boot](../docs/evidencias/os/vm/11-reinicializacao.txt) |
| Logs de auditoria e de execução | 1 | `lib/auditoria.sh` | [Auditoria](../docs/evidencias/os/vm/09-auditoria.txt), [erro controlado](../docs/evidencias/os/vm/10-controle-e-erro.txt) |
| Funcionamento completo | 2 | `teste-de-falha.sh`, `verificar.sh` | [Falhas](../docs/evidencias/os/vm/06-teste-de-falha.txt), [reexecução](../docs/evidencias/os/vm/03-reprovisionamento.txt) |

O [ensaio de 18/11](../docs/evidencias/os/vm/README.md) rodou numa VM real Ubuntu 24.04 no Lima/VZ: provisionamento, API, DNS, limites padrão, falhas, watchdog e reinicialização aprovados. A captura Web atual é do painel de Cyber, não do app da ACS integrado à API; essa demonstração e o vídeo de OS ainda faltam. Os registros antigos do contêiner foram preservados. Multipass e seu inicializador continuam não executados.

## O que cada arquivo faz

| Arquivo | O que faz |
|---|---|
| `provisionar.sh` | Entrada única. Roda `01-rede.sh`, `02-app.sh`, `03-monitor.sh` e `verificar.sh`, nessa ordem, e para na primeira etapa que falhar. No fim, diz se a API e o monitor reiniciaram, pelo `InvocationID` do systemd. |
| `01-rede.sh` | Hostname `corredor-ubs`, fuso de São Paulo, IP fixo `10.77.0.1/24` numa interface própria (`corredor0`), os nomes `api.corredor.internal` e `painel.corredor.internal` no `/etc/hosts`, DNS de reserva, firewall `ufw` (só 22, 80 e 443 entram) e as chaves SSH de `chaves/equipe/`. Com `CORREDOR_DNS_ZONA=sim`, sobe também um dnsmasq que responde pelo domínio interno. |
| `02-app.sh` | Instala Postgres, Mosquitto com ponte para o broker público e Nginx. Cria o usuário `corredor`, o banco, um schema e dois papéis por módulo, o `/etc/corredor/api.env` e as units do systemd. Implanta a última release se ainda não há versão. |
| `03-monitor.sh` | Instala o `monitor.sh` como `corredor-monitor.service`, com usuário próprio, `Restart=always` e watchdog de 60 s. Instala a unit de alerta de falha definitiva e a rotação dos logs. |
| `monitor.sh` | Mede CPU, memória, disco e os serviços a cada 15 s. Grava uma linha de métricas e gera um alerta quando um valor passa do limite (CPU 85%, memória 85%, disco 80%) ou um serviço para. Quando o problema some, grava `NORMALIZADO`. |
| `deploy.sh` | Implanta uma release do GitHub ou um pacote local. Guarda um `pg_dump`, aplica as migrações como o dono de cada schema e troca a versão. Se a API não ficar saudável, volta sozinho para a anterior. |
| `verificar.sh` | Confere a VM de ponta a ponta sem mudar nada. É o `sudo corredor status`. |
| `teste-de-falha.sh` | Provoca cada falha de propósito e confere a recuperação. Só encerra os PIDs das cargas que iniciou e rejeita PID inválido antes de enviar sinais. |
| `tunel.sh` | Liga uma URL HTTPS temporária do cloudflared, para o celular. Não faz parte do provisionamento. |
| `bin/corredor` | O comando de operação, instalado em `/usr/local/sbin/corredor`. `sudo corredor` sem argumento mostra a ajuda. |
| `lib/comum.sh` | As funções que conferem antes de mudar: pacote, usuário, pasta, arquivo, bloco gerenciado, serviço. |
| `lib/auditoria.sh` | Uma linha no `auditoria.log` e no journal no início e no fim de cada script, com usuário, argumentos, código de saída e número de mudanças. A saída completa de cada execução vai para `/var/log/corredor/execucao/`. |
| `systemd/` | As units da API, do monitor, do alerta de falha definitiva, do job de ML, do simulador e do túnel. |
| `config/` | Os valores padrão (`corredor.conf.padrao`), um arquivo por ambiente e os modelos do Nginx, do Mosquitto, do sudoers e do logrotate. |
| `cloud-init/corredor.yaml` | Clona o repositório e chama `provisionar.sh`. É o mesmo arquivo para o Azure e para o Multipass. |
| `local/subir.sh` | Cria a VM local no Multipass com o cloud-init acima. |
| `nuvem/` | Terraform da VM do Azure e o `preflight.sh`, que mostra o que a assinatura de estudante aceita. |

## Caminho oficial: VM local Ubuntu 24.04

A VM precisa de Ubuntu 24.04, 2 CPUs, 4 GB de memória, 20 GB de disco e internet, para o `apt`, o GitHub e o broker público.

Para OS, use uma VM, não o Docker Compose do README da raiz. Azure, Terraform, HTTPS público, túnel e deploy por GitHub Actions são opcionais e não são necessários para demonstrar a rubrica. Para repetir com o hipervisor efetivamente testado, siga [Reproduzir com Lima/VZ](../docs/evidencias/os/vm/README.md#reproduzir-com-o-mesmo-hipervisor). Os comandos abaixo são a alternativa Multipass, ainda não executada; os scripts dentro do Ubuntu foram validados no Lima.

No notebook, instale [Multipass](https://canonical.com/multipass/install), Git e o SDK do .NET 10, clone o repositório e entre na raiz. Não é preciso Node nem Docker para esta sequência. Todos os comandos desta seção rodam no notebook; `multipass exec` executa o comando dentro da VM.

### 1. Criar e provisionar

```bash
multipass launch 24.04 --name corredor-ubs --cpus 2 --memory 4G --disk 20G
multipass exec corredor-ubs -- sudo git clone --depth 1 https://github.com/IamP5/corredor-aceso.git /opt/corredor/repo
multipass exec corredor-ubs -- sudo bash /opt/corredor/repo/infra/provisionar.sh
```

Use uma VM nova. Se `corredor-ubs` já existe, `multipass start corredor-ubs` reutiliza os dados; não repita o clone se o repositório já estiver lá. Para apagar a VM e seus dados e começar do zero, execute `multipass delete --purge corredor-ubs` somente depois de guardar as evidências.

O clone busca a `main` publicada, não as alterações locais do notebook. Sem release publicada, o provisionamento prepara os componentes, mas a API aparece como `[PENDENTE]`. Isso não comprova a aplicação funcionando: conclua o passo 2.

### 2. Implantar a aplicação sem depender de release

Na raiz do repositório no notebook, monte o pacote e copie para a VM. Num Mac com Apple Silicon, a VM é arm64. Num notebook x64, troque todas as ocorrências de `linux-arm64` por `linux-x64`. O SDK fica no notebook; a API é self-contained e a VM não precisa dele.

```bash
bash tools/empacotar.sh v0.0.1 linux-arm64
multipass transfer dist/corredor-linux-arm64.tar.gz corredor-ubs:/tmp/
multipass exec corredor-ubs -- sudo corredor deploy --arquivo /tmp/corredor-linux-arm64.tar.gz
multipass exec corredor-ubs -- sudo corredor semear
```

O último comando carrega as 40 casas da demo e a casa do teste de falha. Só funciona na VM local, que roda em `Development`.

### 3. Conferir a aplicação e operar a VM

```bash
multipass info corredor-ubs
multipass exec corredor-ubs -- sudo corredor status
multipass exec corredor-ubs -- curl -fsS http://api.corredor.internal/api/versao
```

Abra `http://<IP da VM>/painel/` no navegador do notebook. O IP é o da placa principal mostrado pelo Multipass, não o IP interno `10.77.0.1` da interface de serviço. O painel de Cyber deve estar acessível pelo Nginx e o status não pode ter API pendente; isso ainda não demonstra o app da ACS.

Essa rota mostra somente o painel de Cyber. Para a evidência principal da CitizenTech, demonstre o [app da ACS na versão Web consumindo a API da VM](../docs/evidencias/os/vm/README.md#comprovação-visual-ainda-necessária). O cliente pode rodar no notebook; identifique isso nas capturas e não apresente o painel de Cyber como se fosse o app Mobile.

Para os comandos das seções seguintes, entre na VM com `multipass shell corredor-ubs`. A chave do LLM é opcional para OS; a API e o painel funcionam sem ela.

### Alternativas opcionais

- `bash infra/local/subir.sh` cria e provisiona pelo cloud-init. É uma alternativa à etapa 1, não um passo adicional. Esse inicializador ainda ignora falhas da espera do cloud-init e da consulta final; confira o resultado com `sudo corredor status` e não trate só seu código de saída como prova de sucesso.
- Com uma release efetivamente publicada, `sudo corredor deploy vX.Y.Z` pode substituir a implantação por arquivo. Os registros atuais usaram pacote local.
- Azure, Terraform e túnel não fazem parte do caminho oficial da demonstração local.

## Antes de abrir o túnel

A VM local roda em `Development` e aceita o token de ACS de desenvolvimento, que está no Git. Troque o token antes de `sudo corredor tunel`, porque o túnel deixa a API aberta na internet:

```bash
sudoedit /etc/corredor/segredos.env    # acrescente Autenticacao__Acs__0__Token=<um token novo, com 16 caracteres ou mais>
sudo corredor reiniciar api
```

O app entra com o token novo. O teste de falha também passa a usar o token novo, como diz a seção seguinte.

## Ver o monitor, os alertas e os logs

| Comando | Mostra |
|---|---|
| `sudo corredor status` | versão no ar, rede, serviços, saúde da API e os últimos alertas |
| `systemctl status corredor-monitor` | o monitor como serviço, com o PID e as últimas linhas |
| `sudo corredor logs monitor` | as linhas de métricas de `/var/log/corredor/monitor/monitor.log` |
| `sudo corredor logs alertas` | os alertas e as normalizações de `/var/log/corredor/monitor/alertas.log` |
| `journalctl -u corredor-monitor -p warning` | os mesmos alertas no journal |
| `sudo corredor logs auditoria` | quem rodou qual script, quando e como terminou |
| `ls /var/log/corredor/execucao` | a saída completa de cada execução de script |
| `sudo corredor logs deploy` | cada deploy e cada rollback |
| `sudo corredor logs api` | as últimas 100 linhas da API no journal |

Um ciclo do monitor na tela, sem gravar nada: `bash /usr/local/lib/corredor/monitor.sh --uma-vez`.

Os limites ficam em `/etc/corredor/monitor.env`, com os nomes `MONITOR_LIMITE_CPU`, `MONITOR_LIMITE_MEM` e `MONITOR_LIMITE_DISCO`. Depois de mudar, rode `sudo bash /opt/corredor/repo/infra/03-monitor.sh`, que reinicia o monitor.

## Teste de falha

O cenário do broker lê a linha do tempo da casa de teste, e a rota pede o token da ACS. Uma vez por VM:

```bash
echo 'TESTE_AUTORIZACAO="Bearer <token da ACS>"' | sudo tee /etc/corredor/teste-de-falha.env
sudo chmod 640 /etc/corredor/teste-de-falha.env
```

Depois, num terminal, `sudo tail -f /var/log/corredor/monitor/alertas.log`. No outro:

```bash
sudo corredor teste-de-falha
```

Os seis cenários levam cerca de dois minutos e meio: Mosquitto parado com reenvio sem duplicar, API morta com `kill -9`, monitor morto com `kill -9`, CPU cheia, memória acima do limite e disco acima do limite. Cada cenário devolve a VM ao estado em que a encontrou. O resumo no fim lista `PASSOU`, `FALHOU` ou `PULADO` por item, e o código de saída é 1 se algum falhou. O cenário `travado` é opcional e leva mais um minuto: `sudo corredor teste-de-falha travado` congela o monitor, e o watchdog do systemd o mata e religa.

Para demonstrar apenas os itens de OS, sem configurar a autenticação do cenário do broker:

```bash
sudo corredor teste-de-falha monitor carga memoria disco
```

Rode testes de carga somente na VM dedicada. Eles ocupam CPU, memória e disco de verdade. Um cenário `PULADO` não comprova o requisito; investigue e repita até todos os cenários escolhidos passarem.

Ao interromper o teste, a limpeza encerra somente os PIDs que ele criou. TERM e INT são registrados na auditoria com os códigos 143 e 130, em vez de aparentarem execução bem-sucedida.

### Controle do serviço, erro registrado e reinicialização

Dentro da VM, demonstre os comandos administrativos:

```bash
sudo systemctl stop corredor-monitor
systemctl is-active corredor-monitor
sudo systemctl start corredor-monitor
sudo systemctl restart corredor-monitor
systemctl status corredor-monitor --no-pager
```

Depois do `stop`, o estado esperado é `inactive` e `is-active` retorna código diferente de zero. Isso é uma parada administrativa: `Restart=always` não a desfaz. A recuperação automática é comprovada pelo cenário `monitor`, que mata o processo e verifica o novo PID.

Para provocar um erro de entrada sem derrubar a aplicação:

```bash
sudo corredor deploy tag-invalida
echo "Código esperado: 1; recebido: $?"
sudo corredor logs auditoria
```

Guarde a mensagem de erro e o evento de fim de `deploy.sh` com `codigo=1`. Não confunda esse teste controlado com um deploy bem-sucedido.

Saia da VM com `exit` e, no notebook:

```bash
multipass restart corredor-ubs
multipass exec corredor-ubs -- systemctl is-enabled corredor-api corredor-monitor
multipass exec corredor-ubs -- sudo corredor status
multipass exec corredor-ubs -- sudo journalctl -b -u corredor-api -u corredor-monitor --no-pager -n 40
```

Aguarde o boot e confira os serviços ativos sem executar `start` manualmente. Repita também o provisionamento com a API no ar e guarde o resultado sem mudanças. O [checklist das evidências](../docs/evidencias/os/README.md#fechamento-da-entrega) lista o que registrar antes de considerar a entrega concluída.

## Ensaio em contêiner, sem VM

No notebook, com o Docker:

```bash
bash tools/ensaio-os.sh
```

O script monta o pacote do commit atual, sobe um contêiner Ubuntu 24.04 com systemd, roda o provisionamento, o deploy, a carga da demo, o segundo provisionamento, o teste de falha e grava as evidências em `docs/evidencias/os/`. No fim, apaga o contêiner. Com `--manter`, o contêiner fica para você entrar com `docker exec -it corredor-os-ensaio bash`. Docker antigo pede `DOCKER_API_VERSION=1.43` antes do comando.

## Vídeo

O roteiro planejado para até 7 minutos, com a sequência e o item da rubrica de cada trecho, está em [roteiro-os.md](../docs/entrega/roteiro-os.md). A gravação ainda não foi iniciada.

## Publicar o guia de avaliação

Antes de publicar:

```bash
node --check site/guia.js
bash tools/montar-site.sh _site
git diff --check
```

Revise e commite o conjunto completo: fontes do guia, documentação, scripts alterados e a pasta de evidências de OS. Publicar apenas `site/` deixaria os links sem as provas e o código correspondentes. A pasta `_site/` é gerada e não deve ser commitada.

Com uma conta autorizada a escrever no repositório, publique os commits na main e acompanhe o [workflow do Pages](../.github/workflows/pages.yml):

```bash
git push origin main
gh run list --workflow pages.yml --limit 1
```

Só depois do workflow aprovado confira a [seção pública de OS](https://iamp5.github.io/corredor-aceso/#os), abra o relatório, os logs e as capturas e verifique que as pendências continuam explícitas. A publicação do guia, por si só, não conclui o vídeo nem a demonstração ACS/API.

## O que nunca rodou

O ensaio de 18/11 validou uma VM Lima/VZ arm64, inclusive DNS com systemd-resolved ativo, limites padrão e boot automático. Estes caminhos continuam não executados:

- Multipass e `infra/local/subir.sh`. A VM real testada usou Lima/VZ.
- O cloud-init no primeiro boot.
- O Azure: `terraform apply`, o `preflight.sh`, a detecção do ambiente `producao` e o certificado do Let's Encrypt.
- O túnel do cloudflared (`tunel.sh` e `corredor-tunel.service`) e o app no celular pela URL do túnel.
- `sudo corredor deploy --ultima` e `sudo corredor deploy vX.Y.Z`, porque o repositório ainda não tem release publicada. O ensaio usou `--arquivo`.
- O provisionamento em x64. O ensaio foi em arm64.
- O ramo de swap do `02-app.sh`, que só age com menos de 3 GB de memória.
