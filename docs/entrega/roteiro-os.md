# Roteiro do vídeo de Operating Systems

Roteiro de gravação de uso da equipe. Saiu da seção 6.4 do documento de infraestrutura, que não traz mais notas de gravação.


A gravação começa numa VM criada sem o cloud-init do projeto, para o provisionamento acontecer na tela. A VM precisa de internet para o `apt`, o GitHub e o broker público. Ela não precisa do Azure. Use o pacote local do [guia de instalação](../../infra/README.md), sem depender de release publicada. O [ensaio real no Lima/VZ](../evidencias/os/vm/README.md) passou com `v0.0.1-vm`; o vídeo ainda está pendente. Os comandos abaixo são a alternativa Multipass, ainda não executada. Para gravar com o ambiente já validado, use os equivalentes Lima e o acesso por SSH descritos no relatório do ensaio.

Preparo, fora da gravação:

1. Ensaie a sequência inteira uma vez no notebook que vai gravar e anote o tempo do provisionamento.
2. Gere o pacote no notebook com `bash tools/empacotar.sh v0.0.1 linux-arm64`. Em computador x64, use `linux-x64`.
3. Use uma VM nova. Se precisar apagar uma VM de ensaio chamada `corredor-ubs`, guarde as evidências antes: `multipass delete --purge corredor-ubs` apaga seus dados.
4. Abra dois terminais com letra grande e uma aba do navegador.

Sequência gravada:

1. `multipass launch 24.04 --name corredor-ubs --cpus 2 --memory 4G --disk 20G`, depois `multipass shell corredor-ubs`. Mostre `hostname` e `systemctl status nginx`, que ainda não existe.
2. Dentro da VM: `sudo git clone --depth 1 https://github.com/IamP5/corredor-aceso.git /opt/corredor/repo && sudo bash /opt/corredor/repo/infra/provisionar.sh`. Mostre as etapas `01-rede`, `02-app` e `03-monitor`. Sem release publicada, a API fica pendente até o deploy.
3. Em outro terminal no notebook: `multipass transfer dist/corredor-linux-arm64.tar.gz corredor-ubs:/tmp/`. Dentro da VM: `sudo corredor deploy --arquivo /tmp/corredor-linux-arm64.tar.gz`, `sudo corredor semear` e `sudo corredor status`. Em x64, ajuste o nome do pacote.
4. `curl http://api.corredor.internal/api/versao`, `ip addr show corredor0` e `getent hosts api.corredor.internal`.
5. Demonstre o app da ACS na versão Web consumindo a API da VM, não apenas o painel de Cyber. No Lima, mantenha o encaminhamento da API em `127.0.0.1:18080` e execute, na pasta `app` do notebook, `EXPO_PUBLIC_API_URL=http://127.0.0.1:18080 npx expo start --localhost --port 18081`. Abra `http://127.0.0.1:18081`, entre com o código de desenvolvimento e microárea 3 e mostre dia/radar e suas requisições à API da VM. No Multipass, ajuste a URL para o IP alcançável da VM. O frontend no notebook deve ser identificado como cliente; não alegue que ele foi hospedado na VM. `/painel/` é o painel de Cyber e só pode aparecer como complemento.
6. `sudo bash /opt/corredor/repo/infra/provisionar.sh` de novo. O fim mostra `0 mudança(s)` em cada etapa e as linhas `corredor-api: não reiniciou` e `corredor-monitor: não reiniciou`, com o mesmo `InvocationID` antes e depois.
7. `systemctl status corredor-monitor --no-pager`, `cat /opt/corredor/repo/infra/systemd/corredor-monitor.service` e `sudo corredor logs monitor`. Mostre `sudo systemctl stop corredor-monitor`, o estado `inactive`, `sudo systemctl start corredor-monitor` e `sudo systemctl restart corredor-monitor`.
8. Num terminal dentro da VM, `sudo tail -f /var/log/corredor/monitor/alertas.log`. No outro, `sudo corredor teste-de-falha monitor carga memoria disco`. Mostre a recuperação com novo PID, os alertas e a normalização. Execute somente na VM dedicada. Uma parada por `systemctl stop` não reinicia sozinha; o cenário `monitor` mata o processo de propósito.
9. `sudo corredor deploy tag-invalida`, `echo "Código esperado: 1; recebido: $?"`, `sudo corredor logs auditoria` e `ls /var/log/corredor/execucao`. Mostre o erro registrado, não um resultado de sucesso.
10. No notebook, `multipass restart corredor-ubs`. Aguarde o boot e mostre `multipass exec corredor-ubs -- systemctl is-enabled corredor-api corredor-monitor`, `multipass exec corredor-ubs -- sudo corredor status` e `multipass exec corredor-ubs -- sudo journalctl -b -u corredor-monitor --no-pager -n 10`, sem iniciar os serviços manualmente.

Roteiro para 7 minutos. Os trechos de espera são acelerados na edição.

| Tempo | Passos | Item da rubrica |
|---|---|---|
| 0:00 a 0:30 | o que é o Corredor Aceso e o que a VM hospeda | contexto |
| 0:30 a 2:30 | 1 e 2 | provisionamento de infra (2) e da aplicação (2) |
| 2:30 a 3:20 | 3, 4 e 5 | funcionamento completo (2) |
| 3:20 a 3:50 | 6 | idempotência, clareza dos scripts |
| 3:50 a 4:30 | 7 | monitoramento (2) e controle do serviço (1) |
| 4:30 a 5:50 | 8 | teste de falha e recuperação |
| 5:50 a 6:20 | 9 | logs de auditoria (1) e erro controlado |
| 6:20 a 7:00 | 10 e o link do repositório/Pages | início automático após reinicialização e funcionamento completo |

Se o notebook que grava for um Mac com Apple Silicon, a VM é arm64 e usa o pacote `corredor-linux-arm64.tar.gz`. O pacote vem do notebook, não de uma release ainda inexistente.

Guarde as capturas e os registros originais de acordo com o [checklist das evidências](../evidencias/os/README.md#fechamento-da-entrega). Acelere as esperas na edição, sem ocultar erros, e publique o link do vídeo de até 7 minutos no [README principal](../../README.md#vídeos).

**Situação atual:** a infraestrutura e a API foram validadas, mas a navegação/captura do app da ACS integrado à VM ficou para depois. A gravação ainda não começou. Não trate o Pages nem a captura do painel de Cyber como substitutos do vídeo ou dessa integração visual.

