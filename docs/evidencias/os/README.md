# Evidências de Operating Systems

**Situação da entrega:** o [ensaio em VM real de 18/11/2026](vm/README.md) comprovou provisionamento, API, rede/DNS, limites padrão, recuperação, auditoria e início automático após reinicialização. As [capturas do app da ACS](vm/README.md#app-da-acs-contra-a-api-da-vm) mostram o cliente web, sem mock, consumindo a API da VM. Falta o vídeo de até 7 minutos. A publicação destes arquivos também deve ser conferida na main/Pages.

## O Pages sozinho comprova 100% da entrega?

**Não.** O Pages é um índice verificável de código, instruções e provas; o enunciado também exige um vídeo demonstrativo de até 7 minutos. Uma captura de `/painel/` comprova que o painel de Cyber foi servido pelo Nginx, mas não comprova o app da ACS funcionando com a API. Os serviços e a API passaram nos testes de VM, e o cliente web da ACS foi capturado contra essa API ([registro](vm/14-app-acs-integrado.txt)).

Para fechar a entrega, é necessário demonstrar o app real sem mock, registrar suas chamadas à API da VM, adicionar as capturas, gravar/publicar o vídeo e conferir os links no site público. As caixas de conferência do guia são marcações locais da banca, não um resultado automático dos testes nem uma garantia de nota.

O [GitHub Pages, na seção de OS](https://iamp5.github.io/corredor-aceso/#os), reúne este índice, os registros da VM, as capturas, o ensaio anterior em contêiner, o guia de instalação e o roteiro. Os arquivos são copiados para o próprio Pages quando o workflow publica a main; a banca não precisa executar a infraestrutura para consultá-los. A aplicação e os serviços Linux rodam na VM, não no Pages.

Os nove arquivos diretamente nesta pasta preservam o ensaio anterior de `bash tools/ensaio-os.sh`, de 17/11/2026. A pasta [vm/](vm/README.md) contém a validação posterior em Ubuntu 24.04 no Lima/VZ, sem contêiner. Os scripts e o comando de operação estão descritos em [infra/README.md](../../../infra/README.md).

## Ensaio anterior: onde rodou

Num contêiner Docker `ubuntu:24.04` arm64, privilegiado, com o systemd 255 como PID 1, num Mac com Apple Silicon. **Esse ensaio de 17/11 não foi numa VM.** O posterior de 18/11 usou uma VM real no Lima/VZ; Multipass continua não validado.

Duas coisas foram ajustadas para o contêiner se parecer com uma VM, e as duas estão no `tools/ensaio-os.sh`:

- O Docker monta o `/etc/hostname` por cima do arquivo, e o `hostnamectl` não consegue trocar o nome. O ensaio desmonta esse arquivo antes de provisionar. Numa VM, o arquivo é comum.
- Os limites de memória e de disco do monitor ficaram alguns pontos acima do uso da hora, em `/etc/corredor/monitor.env`, para o teste não ocupar gigabytes da VM do Docker. A ocupação foi real: o `stress-ng` ocupou memória de verdade e o `fallocate` criou um arquivo de verdade. O limite de CPU ficou no padrão, 85%.

O pacote da API saiu do `tools/empacotar.sh`, no mesmo formato da release, e entrou por `sudo corredor deploy --arquivo`.

## Arquivos do ensaio anterior em contêiner

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

## Requisitos e comprovação

Os registros principais abaixo são da VM real. O [relatório da VM](vm/README.md) identifica origem, comandos, resultados, capturas e limites da validação.

| Requisito do enunciado | Registro na VM | Resultado |
|---|---|---|
| Hostname, IP, parâmetros de rede, DNS e resolução | [Rede e DNS](vm/05-rede-e-dns.txt) | Aprovado, inclusive dnsmasq com systemd-resolved ativo |
| Dependências, componentes, usuários, diretórios, variáveis e permissões | [Provisionamento](vm/01-provisionamento.txt), [serviços e permissões](vm/04-servicos-e-permissoes.txt) | Aprovado numa VM nova |
| Aplicação Web funcionando e serviço configurado | [Deploy da API](vm/02-aplicacao.txt), [app da ACS](vm/14-app-acs-integrado.txt), [painel de Cyber auxiliar](vm/01-aplicacao.png) | Aprovado: API saudável e app da ACS, sem mock, navegando contra ela |
| CPU, RAM consumida/disponível, disco e alertas por limites | [Falhas e carga](vm/06-teste-de-falha.txt), [métricas](vm/07-monitor-e-alertas.txt) | Aprovado com limites padrão de 85%, 85% e 80% |
| Serviço periódico, controle start/stop/restart e recuperação | [Watchdog](vm/08-recuperacao-e-watchdog.txt), [controle](vm/10-controle-e-erro.txt), [boot](vm/11-reinicializacao.txt) | Aprovado, com novo boot e início automático |
| Logs de instalação, configuração, execução, verificações, alertas e resultados | [Auditoria](vm/09-auditoria.txt), [erro controlado](vm/10-controle-e-erro.txt) | Aprovado, inclusive códigos de erro esperados |
| Reexecução reproduzível e testes de falha | [Reexecução](vm/03-reprovisionamento.txt), [falhas](vm/06-teste-de-falha.txt) | Zero mudanças; seis cenários completos aprovados e watchdog aprovado separadamente |
| README, vídeo de até 7 minutos e capturas organizadas | [Guia](../../../infra/README.md), [capturas](vm/README.md#capturas), [roteiro](../../entrega/roteiro-os.md) | Registros e capturas da infra e do app da ACS disponíveis; faltam vídeo e conferência da publicação |

## Limites do ensaio anterior em contêiner

Esta lista descreve apenas o ensaio de 17/11. VM real, DNS com resolved ativo, boot e limites padrão foram comprovados depois no [ensaio de 18/11](vm/README.md).

- Que o provisionamento funciona numa VM. Um contêiner divide o kernel com a VM do Docker. Rede, firewall e systemd se comportam parecido, mas não igual.
- O cloud-init, o Multipass e o `infra/local/subir.sh`. Nenhum rodou.
- O Azure, o túnel do cloudflared, o Let's Encrypt e o app no celular.
- O deploy pelo GitHub: o repositório ainda não tem release, então `corredor deploy --ultima` e `corredor deploy vX.Y.Z` não rodaram.
- A zona DNS com o systemd-resolved ativo, como numa VM real. No contêiner ele estava inativo.
- Os limites padrão do monitor, de 85% de memória e 80% de disco. Numa VM de 4 GB e 20 GB, o teste ocuparia cerca de 2,5 GB de memória e um arquivo de mais de 10 GB (inferido, não medido).
- O provisionamento em x64. O ensaio foi em arm64.

## Fechamento da entrega

O enunciado exige as evidências no repositório; o Pages funciona como índice de acesso. Não é obrigatório usar Azure nem demonstrar VM e WSL: o ambiente validado foi uma VM local Ubuntu 24.04 no Lima/VZ. Os comandos alternativos do Multipass no guia ainda não foram executados.

- [x] Provisionar uma VM nova até a API estar saudável e o painel abrir pelo encaminhamento local para o Nginx da VM.
- [x] Guardar comandos, saídas, data, versão do Ubuntu, arquitetura e commit/hashes dos scripts e do pacote.
- [x] Registrar rede, DNS, usuários, permissões, serviços, métricas, limites, alertas, logs e reexecução sem mudanças.
- [x] Demonstrar start, stop e restart do monitor, morte do processo e recuperação automática.
- [x] Executar CPU, memória e disco sem cenários falhos ou pulados e registrar normalização.
- [x] Provocar erro controlado e guardar mensagem e código de saída no log.
- [x] Reiniciar a VM e registrar API e monitor iniciados automaticamente e journal do novo boot.
- [x] Salvar registros e capturas em `vm/`, preservando a origem dos registros antigos.
- [x] Adicionar capturas da infraestrutura, dos registros reais e do painel de Cyber, identificado como evidência auxiliar.
- [x] Demonstrar o app da ACS, sem mock, consumindo a API hospedada na VM; registrar dia/radar e as respostas HTTP reais.
- [ ] Adicionar as capturas dessa integração. O frontend Web no notebook deve ser identificado como cliente, sem alegar que foi hospedado na VM.
- [ ] Gravar o vídeo de até 7 minutos conforme o [roteiro](../../entrega/roteiro-os.md) e publicar o link no [README principal](../../../README.md#vídeos).
- [ ] Atualizar a situação deste índice e do Pages somente com o que foi comprovado. Publicar na `main`, aguardar o workflow do Pages e conferir que os novos arquivos abrem pela seção de OS.
