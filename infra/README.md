# Infraestrutura da VM do Corredor Aceso

Esta pasta provisiona uma VM Ubuntu 24.04 do zero: rede, nomes, firewall, Postgres, Mosquitto, Nginx, a API e o monitor. É a entrega de Operating Systems. O desenho completo, com as decisões e os runbooks, está em [05-infra-e-entrega.md](../docs/arquitetura/05-infra-e-entrega.md).

Todo script pode rodar de novo quantas vezes for preciso. Cada função lê o estado atual e muda só a diferença. Uma segunda execução sem mudança termina com `0 mudança(s)` e não reinicia nada.

## Onde está cada item da rubrica

| Item da rubrica | Pontos | Arquivo | Evidência |
|---|---|---|---|
| Script de infra: rede, DNS, IP | 2 | `01-rede.sh` | [05-rede.txt](../docs/evidencias/os/05-rede.txt) |
| Script da aplicação | 2 | `02-app.sh`, `provisionar.sh`, `deploy.sh` | [01-provisionamento.txt](../docs/evidencias/os/01-provisionamento.txt), [02-deploy-e-semente.txt](../docs/evidencias/os/02-deploy-e-semente.txt) |
| Monitor com alertas de CPU, disco e memória | 2 | `monitor.sh` | [07-monitor-e-alertas.txt](../docs/evidencias/os/07-monitor-e-alertas.txt) |
| Monitor como serviço tolerante a falhas | 1 | `03-monitor.sh`, `systemd/corredor-monitor.service` | [08-monitor-religado.txt](../docs/evidencias/os/08-monitor-religado.txt) |
| Logs de auditoria e de execução | 1 | `lib/auditoria.sh` | [09-auditoria.txt](../docs/evidencias/os/09-auditoria.txt) |
| Funcionamento completo | 2 | `teste-de-falha.sh`, `verificar.sh` | [06-teste-de-falha.txt](../docs/evidencias/os/06-teste-de-falha.txt), [03-reprovisionamento.txt](../docs/evidencias/os/03-reprovisionamento.txt) |

As evidências saíram de um contêiner com systemd, não de uma VM. O [README das evidências](../docs/evidencias/os/README.md) diz o que isso prova e o que não prova.

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
| `teste-de-falha.sh` | Provoca cada falha de propósito e confere que a VM se recupera sozinha. |
| `tunel.sh` | Liga uma URL HTTPS temporária do cloudflared, para o celular. Não faz parte do provisionamento. |
| `bin/corredor` | O comando de operação, instalado em `/usr/local/sbin/corredor`. `sudo corredor` sem argumento mostra a ajuda. |
| `lib/comum.sh` | As funções que conferem antes de mudar: pacote, usuário, pasta, arquivo, bloco gerenciado, serviço. |
| `lib/auditoria.sh` | Uma linha no `auditoria.log` e no journal no início e no fim de cada script, com usuário, argumentos, código de saída e número de mudanças. A saída completa de cada execução vai para `/var/log/corredor/execucao/`. |
| `systemd/` | As units da API, do monitor, do alerta de falha definitiva, do job de ML, do simulador e do túnel. |
| `config/` | Os valores padrão (`corredor.conf.padrao`), um arquivo por ambiente e os modelos do Nginx, do Mosquitto, do sudoers e do logrotate. |
| `cloud-init/corredor.yaml` | Clona o repositório e chama `provisionar.sh`. É o mesmo arquivo para o Azure e para o Multipass. |
| `local/subir.sh` | Cria a VM local no Multipass com o cloud-init acima. |
| `nuvem/` | Terraform da VM do Azure e o `preflight.sh`, que mostra o que a assinatura de estudante aceita. |

## Rodar numa VM nova

A VM precisa de Ubuntu 24.04, 2 CPUs, 4 GB de memória, 20 GB de disco e internet, para o `apt`, o GitHub e o broker público.

### Pelo Multipass, com o cloud-init

No notebook, na raiz do repositório:

```bash
bash infra/local/subir.sh
```

O script cria a VM `corredor-ubs`, espera o cloud-init clonar a `main` e rodar o `provisionar.sh`, e mostra o `sudo corredor status`. Para começar do zero, use `bash infra/local/subir.sh --recriar`. Para entrar, use `multipass shell corredor-ubs`.

### Numa VM comum, ou no Multipass sem cloud-init

Este é o caminho do vídeo, porque o provisionamento acontece na tela. Dentro da VM, troque `<endereço do repositório>` pelo mesmo endereço que o `cloud-init/corredor.yaml` clona:

```bash
sudo git clone --depth 1 <endereço do repositório> /opt/corredor/repo
sudo bash /opt/corredor/repo/infra/provisionar.sh
```

O fim mostra `== tudo certo ==`. Antes da primeira versão da API, a linha da API aparece como `[PENDENTE]`.

### A primeira versão da API

Com uma release publicada no GitHub:

```bash
sudo corredor deploy v1.0.0
```

Sem release, monte o pacote no notebook e copie para a VM. Num Mac com Apple Silicon, a VM é arm64. Num notebook x64, troque por `linux-x64`.

```bash
npm run empacotar -- v0.0.1 linux-arm64
multipass transfer dist/corredor-linux-arm64.tar.gz corredor-ubs:/tmp/
multipass exec corredor-ubs -- sudo corredor deploy --arquivo /tmp/corredor-linux-arm64.tar.gz
```

Depois, carregue as 40 casas da demo e a casa do teste de falha. Só funciona na VM local, que roda em `Development`:

```bash
sudo corredor semear
```

Para usar o LLM da Guarda, rode `sudoedit /etc/corredor/segredos.env`, preencha `Llm__ApiKey=` e rode `sudo corredor reiniciar api`.

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

## Ensaio em contêiner, sem VM

No notebook, com o Docker:

```bash
bash tools/ensaio-os.sh
```

O script monta o pacote do commit atual, sobe um contêiner Ubuntu 24.04 com systemd, roda o provisionamento, o deploy, a carga da demo, o segundo provisionamento, o teste de falha e grava as evidências em `docs/evidencias/os/`. No fim, apaga o contêiner. Com `--manter`, o contêiner fica para você entrar com `docker exec -it corredor-os-ensaio bash`. Docker antigo pede `DOCKER_API_VERSION=1.43` antes do comando.

## Vídeo

O roteiro de 7 minutos, com a sequência gravada e o item da rubrica de cada trecho, está na seção 6.4 de [05-infra-e-entrega.md](../docs/arquitetura/05-infra-e-entrega.md#64-gravar-o-vídeo-de-operating-systems).

## O que nunca rodou

O ensaio de 17/11 rodou num contêiner Docker. Estes caminhos nunca rodaram:

- Uma VM de verdade, no Multipass ou em outro hipervisor. A máquina do ensaio não tem Multipass. O `infra/local/subir.sh` nunca rodou.
- O cloud-init no primeiro boot.
- O Azure: `terraform apply`, o `preflight.sh`, a detecção do ambiente `producao` e o certificado do Let's Encrypt.
- O túnel do cloudflared (`tunel.sh` e `corredor-tunel.service`) e o app no celular pela URL do túnel.
- `sudo corredor deploy --ultima` e `sudo corredor deploy vX.Y.Z`, porque o repositório ainda não tem release publicada. O ensaio usou `--arquivo`.
- A zona do dnsmasq com o systemd-resolved ativo, que é o caso de uma VM real. No contêiner ele estava inativo.
- O provisionamento em x64. O ensaio foi em arm64.
- O ramo de swap do `02-app.sh`, que só age com menos de 3 GB de memória.
- Os limites padrão do monitor. O ensaio pôs os limites de memória e disco alguns pontos acima do uso da hora, para não encher a VM do Docker. A ocupação de memória e de disco foi real.
