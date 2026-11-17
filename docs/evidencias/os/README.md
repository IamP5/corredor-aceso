# Evidências de Operating Systems

Estes arquivos são a saída de `bash tools/ensaio-os.sh`, gravada em 17/11/2026 com o código da `main` no commit que aparece no cabeçalho de cada arquivo. O script roda os scripts de `infra/` do zero e grava cada comando seguido da saída dele. Os scripts e o comando de operação estão descritos em [infra/README.md](../../../infra/README.md).

## Onde rodou

Num contêiner Docker `ubuntu:24.04` arm64, privilegiado, com o systemd 255 como PID 1, num Mac com Apple Silicon. **Não foi numa VM.** A máquina do ensaio não tem Multipass nem outro hipervisor.

Duas coisas foram ajustadas para o contêiner se parecer com uma VM, e as duas estão no `tools/ensaio-os.sh`:

- O Docker monta o `/etc/hostname` por cima do arquivo, e o `hostnamectl` não consegue trocar o nome. O ensaio desmonta esse arquivo antes de provisionar. Numa VM, o arquivo é comum.
- Os limites de memória e de disco do monitor ficaram alguns pontos acima do uso da hora, em `/etc/corredor/monitor.env`, para o teste não ocupar gigabytes da VM do Docker. A ocupação foi real: o `stress-ng` ocupou memória de verdade e o `fallocate` criou um arquivo de verdade. O limite de CPU ficou no padrão, 85%.

O pacote da API saiu do `tools/empacotar.sh`, no mesmo formato da release, e entrou por `sudo corredor deploy --arquivo`.

## O que cada arquivo mostra

| Arquivo | Mostra |
|---|---|
| [01-provisionamento.txt](01-provisionamento.txt) | O primeiro `provisionar.sh` numa máquina nova: o Nginx que ainda não existe, cada mudança das três etapas, o `verificar.sh` com a API em `[PENDENTE]` e o tempo total. |
| [02-deploy-e-semente.txt](02-deploy-e-semente.txt) | O deploy do pacote, com as migrações aplicadas pelo dono de cada schema, a carga das 40 casas da demo e o `/api/versao` pelo Nginx. |
| [03-reprovisionamento.txt](03-reprovisionamento.txt) | O segundo `provisionar.sh`, com a API no ar: `0 mudança(s)` em cada etapa e o mesmo `InvocationID` da API e do monitor antes e depois. |
| [04-servicos.txt](04-servicos.txt) | `sudo corredor status`, o `systemctl status` da API, do monitor, do Mosquitto, do Nginx, do Postgres e do timer do ML, e o `api.env` com as senhas trocadas por `***`. |
| [05-rede.txt](05-rede.txt) | Hostname, fuso, o IP fixo na `corredor0`, as rotas, os nomes internos no `/etc/hosts`, nomes da internet resolvidos, o firewall, o Nginx pelos nomes internos e a zona DNS do dnsmasq ligada e respondendo. |
| [06-teste-de-falha.txt](06-teste-de-falha.txt) | `sudo corredor teste-de-falha` com os seis cenários e o resumo, e o cenário `travado`, em que o watchdog religa o monitor congelado. |
| [07-monitor-e-alertas.txt](07-monitor-e-alertas.txt) | Os alertas de CPU, memória e disco e as normalizações no `alertas.log`, as linhas de métricas, os mesmos alertas no journal e um ciclo do monitor na tela. |
| [08-monitor-religado.txt](08-monitor-religado.txt) | A configuração de reinício da unit, o journal do monitor morto com `kill -9` e pelo watchdog, e mais um `kill -9` com o PID antes e depois. |
| [09-auditoria.txt](09-auditoria.txt) | O `auditoria.log`, a lista dos logs de execução, o começo de um deles, as mesmas linhas no journal e o registro de deploy. |

## O que este ensaio não prova

- Que o provisionamento funciona numa VM. Um contêiner divide o kernel com a VM do Docker. Rede, firewall e systemd se comportam parecido, mas não igual.
- O cloud-init, o Multipass e o `infra/local/subir.sh`. Nenhum rodou.
- O Azure, o túnel do cloudflared, o Let's Encrypt e o app no celular.
- O deploy pelo GitHub: o repositório ainda não tem release, então `corredor deploy --ultima` e `corredor deploy vX.Y.Z` não rodaram.
- A zona DNS com o systemd-resolved ativo, como numa VM real. No contêiner ele estava inativo.
- Os limites padrão do monitor, de 85% de memória e 80% de disco. Numa VM de 4 GB e 20 GB, o teste ocuparia cerca de 2,5 GB de memória e um arquivo de mais de 10 GB (inferido, não medido).
- O provisionamento em x64. O ensaio foi em arm64.
