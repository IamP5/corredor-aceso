# Roteiro do vídeo de Operating Systems

Roteiro de gravação de uso da equipe. Saiu da seção 6.4 do documento de infraestrutura, que não traz mais notas de gravação.


A gravação começa numa VM criada sem cloud-init, para o provisionamento acontecer na tela. A VM precisa de internet para o `apt`, o GitHub e o broker público. Ela não precisa do Azure. Grave com a `v1.0.0-rc.1` publicada.

Preparo, fora da gravação:

1. Ensaie a sequência inteira uma vez no notebook que vai gravar e anote o tempo do provisionamento.
2. Rode `multipass delete --purge corredor-ubs`.
3. Abra dois terminais com letra grande e uma aba do navegador.

Sequência gravada:

1. `multipass launch 24.04 --name corredor-ubs --cpus 2 --memory 4G --disk 20G`, depois `multipass shell corredor-ubs`. Mostre `hostname` e `systemctl status nginx`, que ainda não existe.
2. `sudo git clone --depth 1 https://github.com/<org>/corredor-aceso.git /opt/corredor/repo && sudo bash /opt/corredor/repo/infra/provisionar.sh`. Pare a câmera em `01-rede` e em `02-app`.
3. `sudo corredor deploy v1.0.0-rc.1`, `sudo corredor semear` e `sudo corredor status`.
4. `curl http://api.corredor.internal/api/versao`, `ip addr show corredor0` e `getent hosts api.corredor.internal`.
5. No navegador do notebook, `http://<IP da VM>/painel/`.
6. `sudo bash /opt/corredor/repo/infra/provisionar.sh` de novo. O fim mostra `0 mudança(s)` em cada etapa e as linhas `corredor-api: não reiniciou` e `corredor-monitor: não reiniciou`, com o mesmo `InvocationID` antes e depois.
7. `systemctl status corredor-monitor`, `cat /opt/corredor/repo/infra/systemd/corredor-monitor.service` e `sudo corredor logs monitor`.
8. Num terminal, `sudo tail -f /var/log/corredor/monitor/alertas.log`. No outro, `sudo corredor teste-de-falha`.
9. `sudo corredor logs auditoria`, `ls /var/log/corredor/execucao` e `sudo corredor logs deploy`.

Roteiro para 7 minutos. Os trechos de espera são acelerados na edição.

| Tempo | Passos | Item da rubrica |
|---|---|---|
| 0:00 a 0:30 | o que é o Corredor Aceso e o que a VM hospeda | contexto |
| 0:30 a 2:30 | 1 e 2 | provisionamento de infra (2) e da aplicação (2) |
| 2:30 a 3:20 | 3, 4 e 5 | funcionamento completo (2) |
| 3:20 a 3:50 | 6 | idempotência, clareza dos scripts |
| 3:50 a 4:30 | 7 | monitoramento (2) e serviço tolerante a falhas (1) |
| 4:30 a 6:20 | 8 | teste de falha e funcionamento completo |
| 6:20 a 7:00 | 9 e o link do repositório | logs de auditoria (1) |

Se o notebook que grava for um Mac com Apple Silicon, a VM é arm64 e baixa o pacote `corredor-linux-arm64.tar.gz`.

