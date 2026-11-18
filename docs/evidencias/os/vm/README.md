# Validação de OS em VM real

Ensaio de 18/11/2026 numa VM Ubuntu 24.04.5 LTS arm64 criada do zero pelo Lima 2.3.0 com o hipervisor Apple Virtualization.framework (VZ). **Não é contêiner.** A VM tem 2 CPUs, 4 GiB de RAM e disco de 20 GiB, sem pastas do Mac compartilhadas e sem Docker.

A instância de ensaio se chama `corredor-os-validacao`; o Bash configurou o hostname Linux como `corredor-ubs`. Uma VM já existente com esse nome no Lima foi preservada.

## Resultado

| Verificação | Resultado registrado |
|---|---|
| Provisionamento numa VM nova | Passou: rede, dependências, banco, Nginx, usuários, permissões e monitor instalados em 176 s |
| Backend e migrações | Passou: pacote self-contained `v0.0.1-vm`, 40 casas carregadas, API pronta e painel de Cyber HTTP 200 |
| App da ACS integrado à API da VM | Passou em VM recriada com o mesmo procedimento, mais tarde em 18/11: o app web, sem mock, abriu o dia crítico, o radar, a casa, registrou visita, teve mensagem bloqueada e manteve na fila a visita feita com a API parada, enviada uma vez só depois que ela voltou ([14](14-app-acs-integrado.txt), [capturas](#app-da-acs-contra-a-api-da-vm)) |
| Reexecução | Passou: 0 mudanças em 1 s e mesmos InvocationIDs da API e do monitor |
| Rede e DNS | Passou: IP `10.77.0.1/24`, nomes internos, firewall e dnsmasq com systemd-resolved ativo |
| Broker, API e monitor interrompidos | Passou: alerta, reenvio sem duplicação e recuperação dos processos |
| Limites padrão CPU 85%, RAM 85%, disco 80% | Passou antes e depois do boot: CPU chegou a 100%, RAM a 89-90% e disco a 82%, com alertas e normalização |
| Monitor congelado | Passou: watchdog detectou o travamento e o systemd iniciou outro processo |
| Start, stop e restart | Passou: parada administrativa respeitada, partida e reinício confirmados |
| Erro controlado | Passou: tag inválida recusada com código 1 e resultado na auditoria |
| Reinicialização da VM | Passou: boot_id mudou e API/monitor iniciaram automaticamente, sem start manual |
| Limpeza dos testes de carga | Passou: preservou outro stress-ng e encerrou as duas cargas próprias após TERM |
| Validação estática | Passou: 24 scripts em bash -n e ShellCheck, além das verificações existentes de configuração |

O limite de memória não foi reduzido. O stress-ng ocupou 3022 MB na primeira execução e 3082 MB após o boot; os arquivos de carga de disco ocuparam 11677 MB e 11689 MB e foram removidos. Esses testes só foram executados nesta VM dedicada. Os seis cenários completos passaram nas duas execuções, sem falhas ou cenários pulados.

## Registros completos

| Arquivo | Conteúdo |
|---|---|
| [00-ambiente.txt](00-ambiente.txt) | Ubuntu, kernel próprio, virtualização `apple`, recursos, estado inicial sem aplicação e hashes dos scripts |
| [01-provisionamento.txt](01-provisionamento.txt) | Primeiro provisionamento e API inicialmente pendente, antes do deploy |
| [02-aplicacao.txt](02-aplicacao.txt) | Hash do pacote, migrações, carga das 40 casas, versão da API e painel |
| [03-reprovisionamento.txt](03-reprovisionamento.txt) | Segunda execução sem mudanças ou reinícios |
| [04-servicos-e-permissoes.txt](04-servicos-e-permissoes.txt) | Serviços, contas, permissões, nomes das variáveis sem seus valores e unit do monitor |
| [05-rede-e-dns.txt](05-rede-e-dns.txt) | Hostname, rotas, IP, DNS, firewall e verificação completa aprovada |
| [06-teste-de-falha.txt](06-teste-de-falha.txt) | Seis cenários completos, limites padrão e resultados |
| [07-monitor-e-alertas.txt](07-monitor-e-alertas.txt) | Métricas periódicas, RAM disponível, alertas, normalização e journal |
| [08-recuperacao-e-watchdog.txt](08-recuperacao-e-watchdog.txt) | Processo congelado, timeout do watchdog e reinício automático |
| [09-auditoria.txt](09-auditoria.txt) | Início/fim dos scripts, códigos, erros esperados e arquivos de log |
| [10-controle-e-erro.txt](10-controle-e-erro.txt) | Start/stop/restart, rejeição de PID zero e erro de deploy registrado |
| [11-reinicializacao.txt](11-reinicializacao.txt) | Boot IDs diferentes, serviços habilitados/ativos e journal do novo boot |
| [12-isolamento-da-limpeza.txt](12-isolamento-da-limpeza.txt) | Regressão da limpeza por PID, interrupção de carga e recuperação de API/monitor |
| [13-validacao-estatica.txt](13-validacao-estatica.txt) | Instalação do ShellCheck ausente, diagnóstico e execução final aprovada |
| [14-app-acs-integrado.txt](14-app-acs-integrado.txt) | App da ACS contra a API da VM: caminho do túnel, cena do dia crítico, navegação, visitas no Postgres da VM e requisições do app no log do Nginx |

### Capturas

- [Painel de Cyber executado na VM, evidência auxiliar](01-aplicacao.png).
- [Saída real do provisionamento](02-provisionamento.png).
- [Serviços e permissões](03-servicos.png).
- [Resultado dos testes de falha](04-falhas.png).
- [Reinicialização e início automático](05-boot.png).
- [Auditoria e resultados dos scripts](06-auditoria.png).

#### App da ACS contra a API da VM

O app Expo no alvo web, com `EXPO_PUBLIC_API_URL=http://127.0.0.1:18080`, falou com o Nginx da VM pelo túnel SSH local. Chrome headless em 390x844, como um celular. O registro [14-app-acs-integrado.txt](14-app-acs-integrado.txt) traz as requisições do app no log de acesso do Nginx da VM.

| Captura | Mostra |
|---|---|
| [01-entrada.png](acs/01-entrada.png) | Entrada com o código de acesso da ACS |
| [02-dia-critico.png](acs/02-dia-critico.png) | Dia crítico declarado pela regra da noite quente, com o resumo do radar |
| [03-radar.png](acs/03-radar.png) | Radar da microárea 3: urgentes, aparelhos sem sinal e casas aguardando |
| [04-casa-e-linha-do-tempo.png](acs/04-casa-e-linha-do-tempo.png) | Casa urgente e linha do tempo do aparelho |
| [05-visita.png](acs/05-visita.png) | Registro da visita |
| [06-casa-depois-da-visita.png](acs/06-casa-depois-da-visita.png) | Visita enviada à API |
| [07-mensagem-bloqueada.png](acs/07-mensagem-bloqueada.png) | Mensagem para o grupo do bairro bloqueada pela Guarda, com versão segura |
| [08-fila-api-fora.png](acs/08-fila-api-fora.png) | `systemctl stop corredor-api` na VM: a visita fica na fila do app |
| [09-fila-enviada.png](acs/09-fila-enviada.png) | `systemctl start corredor-api`: a fila envia e o Postgres da VM tem uma linha só |

Esta VM foi recriada em outro notebook com o mesmo comando de `limactl start` desta página, a partir do commit `8709a4b`. A cena do dia crítico rodou dentro da VM, e a navegação veio de `tools/demo/telas.mjs` com `CORREDOR_VM`, que para e sobe a API com `systemctl` na VM e lê o Postgres dela:

```bash
limactl shell --workdir=/ corredor-os-validacao -- sudo -u corredor bash -c 'cd /opt/corredor/repo && CORREDOR_API_URL=http://127.0.0.1 CORREDOR_DEMO_ESTADO=/tmp/demo /opt/corredor/venv/bin/python tools/demo/cenas.py dia-critico'
cd app && EXPO_PUBLIC_API_URL=http://127.0.0.1:18080 npx expo start --web --port 8081   # outro terminal, com o túnel aberto
CORREDOR_VM=corredor-os-validacao CORREDOR_API_URL=http://127.0.0.1:18080 CORREDOR_SAIDA=docs/evidencias/os/vm/acs node tools/demo/telas.mjs
```

O relógio de demo mora na memória da API. Depois das 18h (hora real), religar a API encerra o turno antes de o relógio ser restaurado, porque passa do `FimDoTurno`. Foi o que aconteceu aqui, então o encerramento pela ACS não tem captura. O [registro](14-app-acs-integrado.txt) explica.

A primeira imagem é do **painel de Cybersecurity**, servido pelo Nginx da VM em `http://127.0.0.1:18080/painel/`, não da aplicação Docker em 5080. **Ela não mostra o app da ACS e não substitui a demonstração Mobile + API.** As demais são capturas do navegador exibindo os arquivos de registro reais a partir da cópia do guia; não simulam um terminal interativo. O navegador headless foi usado para capturar em resolução legível, sem editar o conteúdo dos registros.

### Comprovação visual ainda necessária

O cliente da ACS pode ser demonstrado no alvo Web do Expo, no notebook, apontando para a API da VM. Isso não significa hospedar o frontend na VM nem testar o aplicativo nativo no celular. O backend Linux e o cliente devem ser identificados separadamente.

Com o encaminhamento SSH da seção seguinte ativo, na raiz do repositório no notebook:

```bash
npm --prefix app ci
cd app && EXPO_PUBLIC_API_URL=http://127.0.0.1:18080 EXPO_NO_TELEMETRY=1 npx expo start --localhost --port 18081
```

Abra `http://127.0.0.1:18081`, entre com o código de desenvolvimento documentado e a microárea 3, navegue pelo dia e radar e registre as chamadas HTTP 200 do app para `http://127.0.0.1:18080/api/radar?microarea=3` e `/api/lista-de-nomes?microarea=3`. Confira também `/api/versao`, que deve identificar a API da VM. Não capture o código de acesso.

A restauração das dependências e o HTTP 200 da página inicial do Expo foram conferidos, assim como a versão `0.0.1-vm` e o radar HTTP da API. Isso apenas prepara o cenário: a autenticação e a navegação no navegador não foram concluídas, e as novas capturas ficaram para depois. Não há prova de integração completa somente por esses HTTP 200 isolados.

## Origem do código e correções

O pacote da aplicação corresponde ao commit `b17795139c3b23d9611be1445d244426579fb44c`. Os scripts foram copiados da árvore de trabalho local para incluir as correções ainda não commitadas. Os hashes iniciais e o hash final de `teste-de-falha.sh` estão em [00-ambiente.txt](00-ambiente.txt).

Antes do ensaio, a limpeza de memória encerrava processos por nome. Ela passou a rastrear apenas os PIDs que o teste iniciou; também limpa as cargas de CPU quando interrompida. A leitura de PID agora rejeita zero ou valor inválido antes de enviar sinais. Uma interrupção por TERM retornava 143, mas a auditoria podia registrar zero; os handlers de TERM/INT agora preservam os códigos 143/130 na saída e no log. As regressões estão em [12-isolamento-da-limpeza.txt](12-isolamento-da-limpeza.txt). O ShellCheck pediu substituir uma expressão `A && B || C` por `if`; a versão corrigida passou na validação e nos testes de recuperação.

Os registros preservam as tentativas e seus resultados, inclusive os diagnósticos:

Nos arquivos de texto, espaços finais e quebras de linha de saída do terminal foram normalizados para versionamento; mensagens, valores e resultados foram preservados.

- Em [02-aplicacao.txt](02-aplicacao.txt), a consulta inicial a `/healthz/ready` pelo Nginx retornou 404. Isso é intencional: saúde fica no loopback da porta 5000. A consulta interna retornou `Healthy` e o painel retornou 200.
- O primeiro status desse arquivo apontou DNS pendente porque a opção do dnsmasq tinha sido gravada, mas ainda não aplicada. [05-rede-e-dns.txt](05-rede-e-dns.txt) registra a execução do script de rede e a verificação completa aprovada.
- Os códigos 1 dos testes de entrada inválida e 143 da interrupção por TERM são esperados e estão identificados nos respectivos arquivos; não são falhas ocultadas da suíte completa.
- O monitor registrou indisponibilidade transitória durante o boot, antes de banco, broker e API ficarem prontos. Isso não impediu sua inicialização automática.

## Reproduzir com o mesmo hipervisor

Pré-requisitos no notebook: Lima, Git e SDK do .NET 10. Execute somente com um nome de VM livre; os comandos não apagam uma VM existente.

```bash
limactl start --name=corredor-os-validacao --vm-type=vz --cpus=2 --memory=4 --disk=20 --plain --mount-none --containerd=none --tty=false template:ubuntu-24.04
limactl shell --workdir=/ corredor-os-validacao -- sudo git clone --depth 1 https://github.com/IamP5/corredor-aceso.git /opt/corredor/repo
limactl shell --workdir=/ corredor-os-validacao -- sudo bash /opt/corredor/repo/infra/provisionar.sh
bash tools/empacotar.sh v0.0.1-vm linux-arm64
limactl copy dist/corredor-linux-arm64.tar.gz corredor-os-validacao:/tmp/corredor-linux-arm64.tar.gz
limactl shell --workdir=/ corredor-os-validacao -- sudo corredor deploy --arquivo /tmp/corredor-linux-arm64.tar.gz
limactl shell --workdir=/ corredor-os-validacao -- sudo corredor semear
limactl shell --workdir=/ corredor-os-validacao -- sudo corredor status
```

O clone busca a main publicada, não alterações locais. No ensaio registrado, os arquivos locais foram transferidos em um arquivo tar por não estarem commitados. Depois de publicar as correções, o clone deve conter os mesmos scripts.

Para entrar: `limactl shell --workdir=/ corredor-os-validacao`. Dentro dela, siga os comandos de monitoramento, autenticação do cenário do broker, testes de falha e erro controlado do [guia de infraestrutura](../../../../infra/README.md). O ensaio ligou a zona DNS com `CORREDOR_DNS_ZONA=sim` na configuração e executou novamente `01-rede.sh`.

Como o modo plain não publica portas automaticamente, para abrir o painel execute em outro terminal do notebook:

```bash
ssh -F "$HOME/.lima/corredor-os-validacao/ssh.config" -o ExitOnForwardFailure=yes -L 127.0.0.1:18080:127.0.0.1:80 -N lima-corredor-os-validacao
```

Abra `http://127.0.0.1:18080/painel/`. O encaminhamento é local, não uma publicação na internet. Encerre com Ctrl+C depois de usar. Para desligar a VM preservando seu disco: `limactl stop corredor-os-validacao`.

Ao fim da validação, `corredor-os-validacao` foi desligada, com seu disco preservado. A VM preexistente `corredor-ubs` permaneceu em execução e não foi modificada. Para retomar a VM do ensaio, use `limactl start --tty=false corredor-os-validacao` e reabra o encaminhamento SSH se precisar do painel.

## Limites e entrega

- Multipass e `infra/local/subir.sh` não foram executados. O ensaio comprova os scripts Bash e os serviços na VM Lima/VZ, não esse inicializador.
- O cloud-init do projeto, Azure, x64, swap com menos de 3 GB, HTTPS público, GitHub Release e app no celular continuam fora deste ensaio.
- O vídeo de até 7 minutos ainda precisa ser gravado e seu link publicado.
- O app da ACS foi demonstrado no alvo web, num navegador do notebook. O app no celular falando com a VM não foi testado. O encerramento do turno ficou de fora das capturas.
- Os arquivos e as capturas precisam ser commitados e publicados na main para aparecerem no GitHub Pages público.
