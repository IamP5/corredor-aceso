# Roteiro do vídeo de Operating Systems

Vídeo de até 7 minutos, como pede o enunciado (PDF da GS, páginas 35 a 37): o provisionamento automatizado por Shell Script, até a aplicação hospedada funcionar por completo. A rubrica vale 10 pontos: rede, DNS e IP (2), aplicação (2), monitor de CPU, disco e memória (2), monitor como serviço tolerante a falhas (1), logs de auditoria (1) e funcionamento completo (2). A nota se apoia em quatro pilares: clareza e lógica dos scripts, monitoramento via log e serviço, logs de auditoria e teste de falha.

O caminho é o mesmo validado na VM Lima/VZ ([relatório](../evidencias/os/vm/README.md)). A tela é gravada **sem som**, em tomadas separadas, e a narração é gravada **depois, à parte**. A edição em HyperFrames acelera as esperas, adiciona títulos e destaques e encaixa a voz.

## 1. Antes de gravar

Faça uma vez, fora da gravação:

1. Publique na `main` o branch `os/app-acs-na-vm`. A VM clona o repositório do GitHub, então o que não está na `main` não entra no vídeo.
2. Desligue a VM de validação, que ocupa a porta 18080: `limactl stop corredor-os-validacao`. Encerre qualquer `expo start` aberto.
3. Gere o pacote: `bash tools/empacotar.sh v0.0.1 linux-arm64`. Se der `NETSDK1004`, rode `dotnet restore api` antes.
4. **Grave antes das 18h, se puder.** O relógio de demo fica só na memória da API. Depois das 18h, religar a API encerra o turno da ACS. A ordem abaixo já mostra o app antes de qualquer religamento, então o vídeo funciona em qualquer horário, mas antes das 18h não há surpresa.
5. Faça um ensaio rápido das tomadas 06 e 09, que são as que mais erram.

### Configuração da tela

- **Não perturbe** ligado, notificações desligadas, Dock oculto, mesa sem ícones.
- **Terminal:** tema escuro, fonte de 22 pt ou mais, até 100 colunas. Rode `clear` antes de cada comando importante. Encurte o prompt: `PS1='$ '` no Mac e `PS1='corredor-ubs$ '` na VM.
- **Navegador:** Chrome com zoom de 125%, sem favoritos nem extensões visíveis. Para o app, use a janela estreita, como um celular: DevTools, barra de dispositivo, iPhone 14 Pro.
- Na tomada 06, deixe o navegador à esquerda e o terminal à direita, lado a lado.

### Configuração da gravação

| Item | Valor |
|---|---|
| Ferramenta | OBS (recomendado) ou QuickTime, gravando uma área |
| Área | 1920x1080, fixa em todas as tomadas |
| Taxa | 30 fps **constante**. No OBS: Vídeo, 30 fps. O QuickTime grava taxa variável; eu converto na edição, mas avise |
| Arquivo | MP4, H.264, qualidade alta (no OBS, CRF 18 ou "Qualidade indistinguível") |
| Áudio da tela | desligado |
| Cursor | visível, sem destaque de cliques (eu adiciono na edição) |

Regras de cada tomada:

- Deixe **2 segundos parados** no começo e no fim.
- **Não corte as esperas.** Deixe o provisionamento e os testes rodarem inteiros; eu acelero.
- Se errar um comando, apague a linha e digite de novo, sem parar a gravação. Eu corto.
- Não role a tela enquanto um texto importante aparece. Espere o comando terminar e deixe a saída parada por 3 segundos.

### Narração

- Grave depois das tomadas, lendo o texto da seção 3 **um bloco por arquivo**: `voz-01.m4a`, `voz-02.m4a` e assim por diante.
- Grave também `tom-de-sala.m4a`: 15 segundos de silêncio, no mesmo lugar e com o mesmo microfone.
- Use um lugar silencioso, com o microfone a um palmo da boca, e fale um pouco mais devagar que numa conversa. Se errar, pause e repita a frase inteira; eu escolho a melhor.

### Onde guardar

Fora do repositório, porque os arquivos são grandes: `~/Movies/corredor-os/brutos/`.

```
~/Movies/corredor-os/brutos/
  T01-vm-nova.mp4
  T02-provisionamento.mp4
  ...
  T12-fechamento.mp4
  voz-01.m4a ... voz-12.m4a
  tom-de-sala.m4a
```

Use exatamente esses nomes. Se uma tomada precisar de duas partes, use `T06a-...` e `T06b-...`.

## 2. Tomadas

Os comandos com `$ ` rodam no Mac, e os com `corredor-ubs$ ` rodam dentro da VM. Na coluna "No vídeo", o tempo é o que fica depois da edição.

| Tomada | O que mostra | Bruto | No vídeo | Rubrica |
|---|---|---|---|---|
| T01 | VM nova, ainda sem nada | ~2 min | 0:15 | contexto |
| T02 | `provisionar.sh` do zero | ~3 min | 0:50 | rede (2) e aplicação (2) |
| T03 | Deploy, migrações, carga e status | ~1 min | 0:25 | aplicação (2) |
| T04 | Hostname, IP, DNS e API pelo nome | ~30 s | 0:20 | rede (2) |
| T05 | Dia crítico declarado na VM | ~3 min | 0:10 | funcionamento (2) |
| T06 | App da ACS consumindo a API da VM | ~3 min | 1:00 | funcionamento (2) |
| T07 | Segunda execução, sem mudanças | ~30 s | 0:20 | clareza dos scripts |
| T08 | Monitor como serviço | ~1 min | 0:35 | monitor (2) e serviço (1) |
| T09 | Teste de falha com alertas | ~3 min | 1:05 | teste de falha (2) |
| T10 | Erro controlado e auditoria | ~1 min | 0:30 | auditoria (1) |
| T11 | Reinicialização e início automático | ~2 min | 0:30 | serviço (1) |
| T12 | Repositório e evidências | ~30 s | 0:15 | entrega |
| | **Total** | | **6:15** | + abertura e fechamento: 6:45 |

### T01 · VM nova

```bash
$ limactl start --name=corredor-ubs --vm-type=vz --cpus=2 --memory=4 --disk=20 --plain --mount-none --containerd=none --tty=false template:ubuntu-24.04
$ limactl shell --workdir=/ corredor-ubs
corredor-ubs$ hostname; cat /etc/os-release | head -2
corredor-ubs$ systemctl status nginx
```

O que precisa aparecer: o Ubuntu 24.04 e o `Unit nginx.service could not be found`, a prova de que a máquina está vazia.

### T02 · Provisionamento

```bash
corredor-ubs$ sudo git clone --depth 1 https://github.com/IamP5/corredor-aceso.git /opt/corredor/repo
corredor-ubs$ sudo bash /opt/corredor/repo/infra/provisionar.sh
```

Deixe rodar até o fim (cerca de 2 minutos). O que precisa aparecer: as etapas `01-rede`, `02-app` e `03-monitor`, o bloco de verificação com `[OK]`, a API em `[PENDENTE]` (ainda sem versão) e a linha final `provisionamento concluído em ... s, N mudança(s)`.

### T03 · Deploy da aplicação

Abra um segundo terminal no Mac:

```bash
$ limactl copy dist/corredor-linux-arm64.tar.gz corredor-ubs:/tmp/corredor-linux-arm64.tar.gz
```

De volta à VM:

```bash
corredor-ubs$ sudo corredor deploy --arquivo /tmp/corredor-linux-arm64.tar.gz
corredor-ubs$ sudo corredor semear
corredor-ubs$ sudo corredor status
```

O que precisa aparecer: as três migrações, cada uma aplicada pelo dono do seu schema, `deploy_ok`, `40 idosos cadastrados` e o status todo em `[OK]`, agora com a API.

### T04 · Rede e DNS

```bash
corredor-ubs$ hostnamectl --static
corredor-ubs$ ip -brief addr show corredor0
corredor-ubs$ getent hosts api.corredor.internal
corredor-ubs$ curl -s http://api.corredor.internal/api/versao; echo
corredor-ubs$ sudo ufw status
```

### T05 · Dia crítico

Prepara os dados que o app vai mostrar. Na edição, só os primeiros e os últimos segundos aparecem.

```bash
corredor-ubs$ sudo -u corredor bash -c 'cd /opt/corredor/repo && CORREDOR_API_URL=http://127.0.0.1 CORREDOR_DEMO_ESTADO=/tmp/demo /opt/corredor/venv/bin/python tools/demo/cenas.py dia-critico'
```

O que precisa aparecer: a linha do job de ML com `"critico": true` e o fim, `== todas as conferências passaram`.

### T06 · App da ACS contra a API da VM

Prepare sem gravar: abra dois terminais no Mac.

```bash
$ ssh -F "$HOME/.lima/corredor-ubs/ssh.config" -o ExitOnForwardFailure=yes -L 127.0.0.1:18080:127.0.0.1:80 -N lima-corredor-ubs
$ cd app && EXPO_PUBLIC_API_URL=http://127.0.0.1:18080 npx expo start --web --port 8081
```

Na tela, deixe à direita o terminal da VM com o log do Nginx correndo:

```bash
corredor-ubs$ sudo tail -f /var/log/nginx/access.log
```

À esquerda, o Chrome em `http://localhost:8081`, no modo celular. **Comece a gravar** e siga:

1. Código de acesso `dev-acs-demo-01-so-no-notebook`, depois **Entrar**. Aparece "Hoje é dia crítico".
2. **Ver radar**. Role devagar até "Aguardando".
3. Toque na primeira casa urgente. Mostre a linha do tempo.
4. **Registrar visita**, "Estava fora", **Salvar visita**. Aparece "Enviado".
5. Volte duas vezes, **Escrever para o grupo do bairro**, digite `ele mora sozinho e toma diurético` e toque em **Verificar mensagem**. Aparece o bloqueio com a versão segura.

O que precisa aparecer: cada toque gerando linhas `GET /api/radar`, `POST /api/visitas` e `POST /api/mensagens/verificar` com status 200 ou 201 no log da direita. Essa é a prova de que o app fala com a VM.

### T07 · Segunda execução

```bash
corredor-ubs$ sudo bash /opt/corredor/repo/infra/provisionar.sh
```

O que precisa aparecer: `0 mudança(s)` em cada etapa e `corredor-api: não reiniciou` e `corredor-monitor: não reiniciou`, com os mesmos `InvocationID`.

### T08 · Monitor como serviço

```bash
corredor-ubs$ cat /opt/corredor/repo/infra/systemd/corredor-monitor.service
corredor-ubs$ systemctl status corredor-monitor --no-pager
corredor-ubs$ sudo corredor logs monitor
corredor-ubs$ sudo systemctl stop corredor-monitor; systemctl is-active corredor-monitor
corredor-ubs$ sudo systemctl start corredor-monitor; systemctl is-active corredor-monitor
corredor-ubs$ sudo systemctl restart corredor-monitor; systemctl is-active corredor-monitor
```

No arquivo da unit, deixe parado por 3 segundos em `Restart=always` e `WatchdogSec`. Eu destaco essas linhas.

### T09 · Teste de falha

Dois terminais da VM, lado a lado. À esquerda:

```bash
corredor-ubs$ sudo tail -f /var/log/corredor/monitor/alertas.log
```

À direita:

```bash
corredor-ubs$ sudo corredor teste-de-falha monitor carga memoria disco
```

Deixe rodar até o resumo, com cada cenário em `PASSOU`. O que precisa aparecer: o monitor morto com `kill -9` e religado com outro PID, os alertas de CPU, memória e disco chegando à esquerda, as normalizações e o resumo final. Rode só nesta VM: o teste ocupa memória e disco de verdade.

### T10 · Erro controlado e auditoria

```bash
corredor-ubs$ sudo corredor deploy tag-invalida; echo "Código esperado: 1; recebido: $?"
corredor-ubs$ sudo corredor logs auditoria
corredor-ubs$ sudo ls -l /var/log/corredor/execucao | tail -5
```

O que precisa aparecer: o erro com código 1 e o mesmo erro registrado na auditoria, com data, usuário, operação e resultado.

### T11 · Reinicialização

Saia da VM (`exit`) e rode no Mac:

```bash
$ limactl stop corredor-ubs && limactl start --tty=false corredor-ubs
$ limactl shell --workdir=/ corredor-ubs -- systemctl is-enabled corredor-api corredor-monitor
$ limactl shell --workdir=/ corredor-ubs -- sudo corredor status
$ limactl shell --workdir=/ corredor-ubs -- sudo journalctl -b -u corredor-monitor --no-pager -n 8
```

O que precisa aparecer: os dois serviços `enabled`, o status em `[OK]` e o journal do novo boot, tudo sem nenhum `start` manual.

### T12 · Fechamento

No navegador: o README do repositório no GitHub, com a ficha no topo, depois `infra/README.md` e a seção de OS do Pages (`https://iamp5.github.io/corredor-aceso/#os`). Role devagar.

## 3. Narração

São cerca de 630 palavras, uns 4 minutos e meio de voz. O restante dos 6:45 é tela com respiro entre as falas. Os tempos de cada bloco são a janela de vídeo, não a duração da fala. Cada bloco é um arquivo `voz-NN.m4a`. O bloco 00 é a abertura: eu monto um título sobre ele, sem tomada de tela.

**voz-00 · Abertura (0:00 a 0:25)**
> O Corredor Aceso ajuda a agente comunitária de saúde em dia de calor extremo: o aparelho no corredor da casa registra sinal de vida, e o aplicativo mostra só os idosos que ficaram em silêncio. Neste vídeo, mostramos a infraestrutura Linux que hospeda essa solução, montada do zero, por Shell Script, numa máquina virtual Ubuntu.

**voz-01 · VM nova (0:25 a 0:40)**
> Começamos com uma VM Ubuntu 24.04 recém-criada. Ela não tem nada instalado: nem o Nginx existe ainda.

**voz-02 · Provisionamento (0:40 a 1:30)**
> Um único comando, o provisionar.sh, prepara tudo. Ele roda três etapas. A primeira, 01-rede, configura hostname, IP fixo, DNS interno e firewall. A segunda, 02-app, instala as dependências, cria os usuários de serviço, os diretórios com permissões restritas, as variáveis de ambiente, o banco Postgres, o broker MQTT e o Nginx. A terceira, 03-monitor, instala o monitoramento como serviço do systemd. No fim, o próprio script verifica cada item e conta quantas mudanças fez. A API aparece pendente porque ainda não implantamos nenhuma versão.

**voz-03 · Deploy (1:30 a 1:55)**
> O deploy recebe o pacote da aplicação, aplica as migrações do banco, cada uma com o dono do seu schema, e sobe a API. Depois, carregamos as quarenta casas simuladas da demonstração. O status confirma tudo no ar.

**voz-04 · Rede (1:55 a 2:15)**
> A rede está como planejamos: hostname corredor-ubs, IP fixo na interface corredor0 e um nome interno resolvido pelo DNS local. A API responde por esse nome, e o firewall libera só SSH, HTTP e HTTPS.

**voz-05 · Dia crítico (2:15 a 2:25)**
> Agora simulamos um dia de calor: as casas mandam sinais pelo MQTT, e o job de previsão declara o dia crítico.

**voz-06 · App da ACS (2:25 a 3:25)**
> Este é o aplicativo da agente, rodando no notebook como cliente. Ele consome a API hospedada na VM: à direita, o log do Nginx da VM mostra cada requisição chegando. A agente entra e vê que hoje é dia crítico. O radar lista só quem precisa de visita. Ela abre uma casa urgente, vê a linha do tempo do aparelho e registra a visita, que vai para o banco da VM. Por fim, ela tenta mandar uma mensagem para o grupo do bairro que expõe a saúde da pessoa, e o sistema bloqueia e sugere uma versão segura.

**voz-07 · Segunda execução (3:25 a 3:45)**
> Rodar o provisionamento de novo não muda nada: zero mudanças em cada etapa, e os serviços nem reiniciam. Cada passo do script confere antes de agir, então ele pode ser repetido com segurança.

**voz-08 · Monitor como serviço (3:45 a 4:20)**
> O monitor é um serviço do systemd. A unit tem reinício automático e watchdog: se o processo morrer ou travar, o systemd o inicia de novo. A cada quinze segundos, ele mede CPU, memória e disco e grava no log. Paramos, iniciamos e reiniciamos o serviço pelo systemctl, e uma parada administrativa é respeitada.

**voz-09 · Teste de falha (4:20 a 5:25)**
> Agora, o teste de falha. O script mata o monitor com kill menos nove, e o systemd o religa com outro PID. Depois, ocupa a CPU, a memória e o disco de verdade, acima dos limites de oitenta e cinco, oitenta e cinco e oitenta por cento. À esquerda, os alertas chegam ao log, e quando a carga sai, cada um normaliza. No fim, o resumo mostra todos os cenários aprovados, e a VM volta ao estado em que estava.

**voz-10 · Auditoria (5:25 a 5:55)**
> Todo script deixa rastro. Aqui provocamos um erro de propósito: um deploy com uma versão inválida, recusado com código um. A auditoria registra a data, o usuário, a operação e o resultado, inclusive dessa falha. Cada execução também guarda a saída completa num log próprio.

**voz-11 · Reinicialização (5:55 a 6:25)**
> Por último, reiniciamos a VM inteira. Ninguém inicia nada à mão: a API e o monitor estão habilitados no boot, o status volta todo verde, e o journal mostra o monitor medindo desde o novo boot.

**voz-12 · Fechamento (6:25 a 6:45)**
> Os scripts, as configurações, as evidências e as instruções estão no repositório, com o passo a passo para repetir tudo numa VM nova. Obrigado.

## 4. O que me mandar

- As tomadas `T01` a `T12` em `~/Movies/corredor-os/brutos/`.
- `voz-00` a `voz-12` e `tom-de-sala.m4a`.
- Uma linha por tomada que deu algo diferente do roteiro, por exemplo: "T09: o cenário de disco demorou 2 minutos". Não esconda erros: se algo falhou e você repetiu, eu escolho a versão certa e o vídeo continua honesto.

Com isso eu monto o vídeo em HyperFrames, em 1920x1080 e 30 fps:
- um título por capítulo com o item da rubrica;
- zoom nas linhas que importam (`[OK]`, `0 mudança(s)`, `PASSOU`, código 1);
- as esperas aceleradas, com um contador indicando a aceleração;
- a narração mixada sobre o tom de sala;
- legenda opcional.

Depois que o vídeo for publicado, o link entra na tabela "Vídeos" do [README principal](../../README.md#vídeos).
