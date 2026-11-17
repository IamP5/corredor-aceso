# Roteiros dos vídeos

Seis vídeos, todos sobre as mesmas cenas da demo. Cada roteiro diz o que mostrar, o comando ou a tela, e o que falar. A fala é um guia: diga com as suas palavras, mas não prometa o que a demo não mostra.

| Vídeo | Limite | Disciplina | Onde o link vai |
|---|---|---|---|
| Mobile | 3 min | Mobile Development e IoT | `app/README.md` e texto do envio |
| Operating Systems | 7 min | Operating Systems | `infra/README.md` e texto do envio |
| Physical Computing | 4 min | Physical Computing | envio, com o link do Wokwi e `integrantes.txt` |
| ML | 5 min, YouTube não listado | IA e Machine Learning | envio, com o link do repositório |
| Dashboard de Cyber | sem limite nos documentos; mire 3 min | Cybersecurity | dentro do PDF de Cyber |
| Pitch | 4 min, YouTube | Testing | PDF de Testing e `README.md` |

## Preparo comum

As cenas dependem do relógio de demo, que só existe com a API em `Development`, como na VM local (seção 1.11 de [05-infra-e-entrega.md](../arquitetura/05-infra-e-entrega.md)). Antes de cada gravação:

1. Rode a carga da demo, se a VM é nova: `Corredor.Api --semear api/semente/casas.json`.
2. Se o relógio vai voltar no tempo, rode o reinício da seção 8.4 de [04-modelo-de-dados.md](../arquitetura/04-modelo-de-dados.md). Sem ele, a casa nunca fica cinza nem vermelha por silêncio.
3. Ligue o simulador das 39 casas: `sudo systemctl start corredor-simulador`.
4. Ligue o túnel uma vez, se o celular vai usar HTTPS: `sudo corredor tunel`. A URL muda a cada partida.
5. Deixe num terminal as variáveis `API` (a URL da API) e `AUTH` (o cabeçalho da ACS `acs-demo-01`) para os comandos abaixo.

Comandos que os roteiros usam:

```bash
# Mover o relógio de demo
curl -s -X PUT "$API/api/demo/relogio" -H "$AUTH" -H 'Content-Type: application/json' -d '{"agora":"2026-11-18T09:58:00-03:00"}'
```

```bash
# Declarar o dia crítico de hoje, como o job das 05h
curl -s -X PUT "$API/api/dias-criticos/2026-11-18" -H "$AUTH" -H 'Content-Type: application/json' -d '{"probabilidade":0.81,"motivo":"mínima de 24 °C pela segunda noite","origem":"MODELO"}'
```

```bash
# Ver o radar
curl -s "$API/api/radar?microarea=3" -H "$AUTH" | jq
```

As casas da carga (seção 8.1 de 04-modelo-de-dados.md): **casa 1** é o nó do Wokwi, `Sozinho` e eletrodependente. **Casa 4** avisa saída às 07h30. **Casas 5, 6 e 7** ficam em silêncio às 10h: a 5 tem a resposta do vizinho, a 6 a visita `EstavaBem`, a 7 a visita `NaoAbriu`. **Casa 8** tem calor de 34,5 °C desde as 11h. **Casa 9** aperta o botão às 14h40. **Casa 10** tem o aparelho mudo desde 09h10. As outras 32 têm movimento normal.

Duas frases valem para todos os vídeos e precisam aparecer quando a cena toca no assunto: "um nó real no Wokwi e 39 casas simuladas" e "os dados das pessoas são simulados; os de clima e de internação são públicos".

## Mobile, 3 minutos

Mostra o app Expo rodando no celular ou no emulador, ligado à API. A rubrica olha telas e navegação, consumo de dados, banco local, acessibilidade, aderência ao problema e documentação.

| Tempo | Tela e ação | Fala |
|---|---|---|
| 0:00 a 0:20 | Tela de entrada, depois a do dia crítico. Mova o relógio para 09h00 antes. | "Hoje é dia crítico. O app da ACS só abre o radar nesse dia." |
| 0:20 a 0:55 | Radar às 09h00: verdes contados, a casa 10 cinza no topo com "aparelho sem sinal desde 09h10". Toque no card e mostre a linha do tempo. | "O radar não lista 40 nomes. Mostra quem ficou em silêncio. O aparelho mudo aparece cinza e nunca verde." |
| 0:55 a 1:30 | Mova o relógio para 10h00 e puxe para atualizar. As casas 5, 6 e 7 ficam vermelhas com "sem sinal de vida desde 10h". Mostre que o texto acompanha a cor e ligue o leitor de tela num card. | "Às 10h vence a janela da manhã. Cada estado está escrito, não só na cor, e o leitor de tela lê o card." |
| 1:30 a 2:00 | Na casa 7, registre `NaoAbriu`. O card mostra "SAMU chamado" e continua vermelho. Na casa 6, registre `EstavaBem`: o card sai das pendentes no radar seguinte. | "Não abriu não resolve nada. Só ver a pessoa bem tira a casa da lista." |
| 2:00 a 2:25 | Ponha o celular em modo avião. Registre um desfecho: ele fica "pendente". Abra o radar: o título diz "sem conexão" e a hora. Tire do modo avião: o desfecho sobe. | "Na rua o sinal cai. O desfecho fica guardado no celular e sobe sozinho quando a rede volta. Um radar velho avisa que é velho." |
| 2:25 a 2:50 | Tela de mensagem: digite "ele mora sozinho e toma diurético" para o grupo do bairro. A tela mostra o bloqueio e a versão segura. | "Antes de pedir ajuda no grupo, o app confere a mensagem. Morar sozinho e remédio não vão para o grupo." |
| 2:50 a 3:00 | Tela de encerramento com o resumo do turno. | "No fim do turno fica o indicador: quantos deram sinal até as 10h." |

## Operating Systems, 7 minutos

Segue a sequência gravada da seção 6.4 de [05-infra-e-entrega.md](../arquitetura/05-infra-e-entrega.md), numa VM criada sem cloud-init. Os trechos de espera são acelerados na edição. Grave com a release `v1.0.0-rc.1` publicada.

| Tempo | Comando | Item da rubrica | Fala |
|---|---|---|---|
| 0:00 a 0:30 | Slide ou README | contexto | "O Corredor Aceso roda numa VM Linux que nasce por script: API, painel, Mosquitto e Postgres." |
| 0:30 a 1:00 | `multipass launch 24.04 --name corredor-ubs --cpus 2 --memory 4G --disk 20G`, `multipass shell corredor-ubs`, `hostname`, `systemctl status nginx` | infra | "A VM está vazia. Ainda não existe Nginx." |
| 1:00 a 2:30 | `sudo git clone --depth 1 https://github.com/IamP5/corredor-aceso.git /opt/corredor/repo && sudo bash /opt/corredor/repo/infra/provisionar.sh`. Pare em `01-rede` e em `02-app`. | infra (2), aplicação (2) | "O 01-rede cria a interface corredor0 com IP fixo, os nomes internos e o firewall. O 02-app instala dependências, cria o usuário de serviço, os papéis do banco e as units." |
| 2:30 a 3:20 | `sudo corredor deploy v1.0.0-rc.1`, `sudo corredor status`, `curl http://api.corredor.internal/api/versao`, `ip addr show corredor0`, `getent hosts api.corredor.internal`, e o painel no navegador | funcionamento completo (2) | "O deploy baixa a release, migra o banco e confere a saúde. A API responde pelo nome interno." |
| 3:20 a 3:50 | `sudo bash /opt/corredor/repo/infra/provisionar.sh` de novo | idempotência | "Rodar de novo não muda nada: 0 mudança em cada etapa, e os serviços não reiniciaram." |
| 3:50 a 4:30 | `systemctl status corredor-monitor`, `cat /opt/corredor/repo/infra/systemd/corredor-monitor.service`, `sudo corredor logs monitor` | monitoramento (2), serviço tolerante a falhas (1) | "O monitor mede CPU, memória e disco a cada 15 segundos, como serviço com Restart=always e watchdog." |
| 4:30 a 6:20 | Terminal 1: `sudo tail -f /var/log/corredor/monitor/alertas.log`. Terminal 2: `sudo corredor teste-de-falha`. | teste de falha, funcionamento completo | "O teste para o Mosquitto, reenvia eventos duas vezes e cada um aparece uma vez só. Depois mata a API e o monitor, e o systemd religa. Carga, memória e disco disparam ALERTA e depois NORMALIZADO." |
| 6:20 a 7:00 | `sudo corredor logs auditoria`, `ls /var/log/corredor/execucao`, `sudo corredor logs deploy`, o link do repositório | logs de auditoria (1) | "Cada script registra quem rodou, quando e o código de saída." |

Se o notebook for um Mac com Apple Silicon, a VM é arm64 e o deploy baixa `corredor-linux-arm64.tar.gz`. O cenário `broker` usa um publicador local, não o nó do Wokwi, e o vídeo não afirma que eventos do Wokwi sobrevivem a uma parada do Mosquitto da VM (seção 1.11 de 05-infra-e-entrega.md).

## Physical Computing, 4 minutos

Mostra o circuito ESP32 no Wokwi integrado ao radar, não um projeto paralelo. Use os nomes dos componentes que estão no projeto em `iot/no/`. Tela dividida: Wokwi à esquerda, radar ou linha do tempo à direita.

| Tempo | Cena | Fala |
|---|---|---|
| 0:00 a 0:30 | O circuito no Wokwi: sensor de presença, luz de caminho, LED âmbar, buzzer, botão, sensor de temperatura e o controle que simula a tensão da tomada | "Este é o nó do corredor. Ele fica na casa 1, de uma idosa que mora sozinha e usa concentrador de oxigênio." |
| 0:30 a 1:00 | Desligue o Wi-Fi do Wokwi e acione o sensor de presença: a luz acende. | "A luz acende sem rede. É o que tira a idosa do corredor escuro." |
| 1:00 a 1:40 | Religue o Wi-Fi. O nó reenvia o buffer. Mostre no monitor serial e na linha do tempo `bufferizado: true` com a hora original. | "Sem rede, o nó guardou o movimento. Quando a rede volta, ele chega com a hora em que aconteceu, e a repetição não duplica." |
| 1:40 a 2:10 | Declare o dia crítico: o LED âmbar acende. Com o relógio em 09h00, acione a presença e mostre a casa 1 saindo de `Aguardando` para verde no radar. | "O dia crítico acende o âmbar. Passar no corredor vale como 'estou bem'." |
| 2:10 a 2:35 | Sem acionar a presença depois das 10h, mova o relógio para 14h30: o buzzer da casa 1 toca. | "Às 14h30, quem não deu sinal ouve um lembrete antes do prazo das 15h." |
| 2:35 a 3:20 | Leve a tensão para 0. O radar mostra a casa 1 vermelha, motivo energia, no topo. Depois pare o nó: com 60 segundos sem pacote a casa continua vermelha, não cinza. | "Quem depende de tomada vem primeiro, em qualquer dia. A casa só sai do vermelho quando a ACS vê a pessoa depois da queda." |
| 3:20 a 4:00 | Arquitetura em um slide: Wokwi, broker público, ponte do Mosquitto, Motor, app | "O nó publica num broker público e a VM traz por ponte. O token viaja no tópico: quem lê o broker poderia forjar um movimento. A correção é assinar o pacote com um segredo por casa. Um nó real e 39 casas simuladas." |

A limitação do broker público é exigência da seção 8.4 do [modelo de domínio](../arquitetura/03-modelo-de-dominio.md): o vídeo de Physical Computing precisa dizê-la.

## ML, 5 minutos

Mostra o notebook executado e o relatório. A rubrica olha problema e dados reais, escolha do modelo, preparação e divisão, treino com parâmetros diferentes, resultados de treino e teste e a conclusão para o cidadão. Dados simulados não são aceitos. Os números entre colchetes saem do notebook; não diga um número que o notebook não mostra.

| Tempo | Tela | Fala |
|---|---|---|
| 0:00 a 0:40 | Pergunta no topo do notebook | "Com o que se sabe às 5h da manhã, hoje será um dia de excesso de internações de urgência de idosos por causa cardiovascular em São Paulo?" |
| 0:40 a 1:30 | `ml/dados/README.md` e as células de carga | "Os dados são públicos: o INMET da estação Mirante de Santana e as internações do SIH/DATASUS, filtradas para 60 anos ou mais em São Paulo. O período é [período]. Dia de excesso é acima do percentil 90 da média móvel de 28 dias." |
| 1:30 a 2:10 | Preparação e divisão | "Só entram variáveis observadas até as 5h, então não há vazamento. O treino vai até [ano] e o teste é [anos]. A validação cruzada é por série temporal e só no treino." |
| 2:10 a 3:00 | Grade de hiperparâmetros | "Usamos XGBoost, porque lida bem com classes desbalanceadas e variáveis em tabela. Testamos 24 combinações de profundidade, taxa de aprendizado e número de árvores." |
| 3:00 a 4:00 | Tabela de treino contra teste e a baseline da noite quente | "No treino o recall foi [x], no teste [y]. A regra simples da noite quente teve [z]. O modelo empatou com a regra: ficou um pouco acima do acaso, dentro do ruído, e aprendeu o inverno, não o calor." |
| 4:00 a 4:40 | Conclusão do relatório | "Em São Paulo, a noite quente não avisa, sozinha, que mais idosos vão ser internados por problema do coração. Por isso o produto não promete reduzir internação. O radar liga na noite quente porque quem mora sozinho corre risco direto no calor: 'a noite não esfriou; passe nas casas que ficaram em silêncio'." |
| 4:40 a 5:00 | `ml/prever_hoje.py` chamando `PUT /api/dias-criticos/{data}` | "Às 5h, o job manda a decisão para o sistema. Como o modelo não venceu a regra, o job usa a regra e diz isso na origem." |

O modelo empatou com a regra, e o vídeo precisa dizer isso. O relatório e o job usam a regra com `origem: REGRA_BASE` ([visão geral](../arquitetura/01-visao-geral.md#modelo-de-dia-crítico) e [relatório de ML](../../ml/RELATORIO_ML.md), seção 9).

## Dashboard de Cyber

Vai dentro do PDF de Cyber, na parte "Vídeo do Dashboard". Mostra o painel Risco Cidadão em `/painel` reagindo às evidências da auditoria. O esqueleto do PDF está em [cyber-documentacao.md](cyber-documentacao.md).

| Tempo | Tela e ação | Fala |
|---|---|---|
| 0:00 a 0:20 | Painel com o farol verde | "Este é o painel aberto ao cidadão. Verde: nenhum evento de segurança nas últimas 24 horas." |
| 0:20 a 1:00 | Swagger: `POST /api/idosos` com `123.456.789-00` em `condicoes`. Mostre o log e a linha do banco com `***.456.***-00`. Volte ao painel: amarelo. | "O CPF foi mascarado antes do log e do banco. Mascarar é o sistema funcionando, então o farol vai só até amarelo." |
| 1:00 a 1:50 | App: "ele mora sozinho e toma diurético" para o grupo do bairro. Bloqueado. Painel: vermelho, com a mensagem traduzida pelo LLM. | "Uma tentativa de saída com dado de saúde foi barrada. O LLM explicou em duas frases para quem não é técnico." |
| 1:50 a 2:20 | O evento no banco e o prompt enviado ao LLM | "O evento guarda só o tipo de dado, o canal e o módulo. O prompt também. O valor nunca sai." |
| 2:20 a 2:50 | Teste que grava `Idoso.Nome` no log: o log mostra `[NOME]`. Rota sem autenticação que devolveria `Idoso.Condicoes`: resposta bloqueada. | "O log e a resposta sem autenticação não aceitam dado rotulado." |
| 2:50 a 3:00 | Derrube a chave do LLM e bloqueie outra mensagem: o painel mostra a frase fixa. | "Sem o LLM, entra uma frase fixa. O painel nunca fica sem explicação." |

## Pitch, 4 minutos

Vai para o YouTube e o link vai no PDF de Testing. Conta a história com as cenas que funcionam na demo. Este roteiro atualiza o da seção 8 de [01-ideia-vencedora.md](../discovery/01-ideia-vencedora.md) com as regras finais: a casa do concentrador fica **vermelha**, não cinza, e o cinza aparece na casa 10.

| Tempo | Cena | Fala |
|---|---|---|
| 0:00 a 0:20 | Celular da ACS com 40 nomes | "Dia de 36 °C. Uma agente comunitária de saúde, 40 idosos que moram sozinhos. Para quem ela liga primeiro?" |
| 0:20 a 0:50 | Corredor escuro, a ficha do problema | "Idosos que moram sozinhos evitam levantar no escuro e bebem menos água. No calor, isso vira internação." Diga se a fala veio de entrevista real ou de persona simulada. |
| 0:50 a 1:30 | Wokwi: presença, a luz acende, a casa 1 vira verde. O radar mostra 27 verdes e só as casas em silêncio. | "Passar no corredor já é 'estou bem'. A ACS não checa 40 casas. Checa as que ficaram em silêncio." |
| 1:30 a 2:00 | Relógio em 10h00: casas 5, 6 e 7 vermelhas. A casa 10 cinza, "aparelho sem sinal". A tensão da casa 1 vai a zero: vermelha no topo. | "Às 10h, quem não deu sinal sobe. Aparelho mudo fica cinza e nunca vira verde. No apagão, quem depende de tomada vem primeiro." |
| 2:00 a 2:30 | Mensagem "mora sozinho e toma diurético" bloqueada, versão segura, farol vermelho no painel | "No grupo do bairro, essa frase é um mapa para quem quer fazer mal. O sistema barra e explica." |
| 2:30 a 3:10 | Gráfico do notebook: modelo contra a regra da noite quente | "Com dados públicos do INMET e do SUS, um modelo diz às 5h se o dia é crítico. [Resultado do notebook, sem arredondar para cima.]" |
| 3:10 a 3:40 | Arquitetura em um slide e os ODS 3, 10, 11 e 13 | "Uma solução, oito disciplinas. Um nó real e 39 casas simuladas. Nenhum dado pessoal real." |
| 3:40 a 4:00 | Tela de encerramento com o indicador | "Meta: 100% dos idosos sozinhos com contato confirmado até as 10h, e nenhuma casa verde sem prova de vida. Próximo passo: um verão numa UBS de verdade." |

Não diga no pitch que existe ida noturna sem volta, aviso à família ou envio real ao vizinho: os três estão fora da entrega (seções 8.2 e 8.4 do modelo de domínio). O aviso ao vizinho da demo é simulado, e o pitch diz isso se mostrar a cena.
