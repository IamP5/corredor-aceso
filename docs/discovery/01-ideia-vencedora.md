# Corredor Aceso: a lista de quem ficou em silêncio

> **Aviso de método.** Todas as entrevistas, testes de usabilidade (SUS) e a "banca FIAP" deste documento foram **simulados com personas sintéticas** (Dona Cida, Edileuza, Cléber, Wesley, Larissa, Seu Edson, Josefa, Sandra, Jéssica e a banca). São hipóteses bem fundamentadas, não evidência de campo. O briefing exige **uma pessoa real que convive com o problema**. A equipe precisa repetir na semana 1, com pelo menos uma ACS real e de 3 a 5 idosos reais que moram sozinhos, o roteiro de entrevista e o teste de protótipo da seção 7. Sem isso, a frase "checa 2, não 9" continua sendo hipótese.

---

## 1. Ideia vencedora

**Nome:** Corredor Aceso
**One-liner:** Uma luz de corredor que protege a idosa do tropeço à noite. Em dia crítico de calor ou de apagão, a agente comunitária de saúde (ACS) abre o celular e vê só os 2 ou 3 idosos que ficaram em silêncio, não os 40.

### Por que vence

| Critério | Corredor Aceso | Vigília | Totem da Viela |
|---|---|---|---|
| Nota total dos juízes + personas | **7,1** (1º) | 6,73 | 6,35 |
| Disrupção | 8 | 8 | 7,5 |
| Viabilidade | **7,5** | 7 | 6,5 |
| Desejabilidade | **7,2** | 7 | 7 |
| Pré-mortem | **6,2** | 5,5 | 5 |
| Dataset verificado | Sim (INMET A701 2025 e SIH RDSP2502 baixados e abertos) | Sim, mas **sem iluminação** (0 de 484.232 linhas) | Sim (INMET). O rótulo de deslizamento foi reprovado (15 eventos) |
| SUS médio no teste de protótipo | 62 (60/68/58) | 62 (66/62/58) | 59 (58/66/55/58) |
| Tarefa concluída | 3 de 3 | 3 de 3 | 3 de 4 (a Edileuza não concluiu) |
| Banca FIAP simulada | 7 ("teto de 8,5 a 9") | **8** | 7 |

1. **Maior nota consolidada e o melhor pré-mortem.** O escopo enxuto saiu da validação Lean com cortes claros: sem URA, sem modo offline, sem SMS e com o LLM só no Cyber.
2. **O ML está dentro do produto, não ao lado dele.** O modelo produz a entidade `DiaCritico`, que abre o turno de radar. Na Vigília o modelo de reincidência do SP156 não decide nada sobre iluminação (crítica explícita da banca: "dois projetos lado a lado").
3. **Dataset verificado e com volume.** Cerca de 45 internações de urgência por dia de residentes de SP com 60+ e CID I00-I99, a partir do RDSP2502 aberto localmente. A série do INMET A701 tem 0,5% de lacunas.
4. **As entrevistas simuladas reforçaram o núcleo, não só o enfeite.** A Dona Cida bebe pouca água **de propósito** por medo de levantar no escuro, então a luz é a intervenção de saúde, não um brinde. A Edileuza confirma "eu sou uma só", checou 7 de 9 e deixou uns 30 de fora. O Cléber, o mais cético, diz que a ACS é o único intermediário em quem confia ("é uma pessoa que tem cara").
5. **O "uau" cabe em 20 segundos e é causa e efeito.** A tela começa com 40 nomes e esvazia até sobrarem 3. O nó fica mudo, o card volta **cinza** (nunca verde) e o taint bloqueia "mora sozinha + diurético" no grupo do bairro.

### Por que as outras perderam

- **Vigília (banca 8, total 6,73).** Foi a mais elogiada pela honestidade com os dados ("zero em 484 mil, por isso a rua mede"), mas perde em três pontos. (a) A física de um LDR atrás da vitrine de uma padaria acesa é o pior ambiente possível e não foi testada. (b) O funil é estreito: a regra exige padaria parceira com o poste no campo de visão, um "finalizado" recebido pelo 0800, 3 noites escuras e 2 humanos, o que dá poucos casos por ano. (c) O ML usa outro serviço (buraco, poda), e a variável de chuva pós-finalização vaza informação do futuro. É ótima, mas arrisca chegar ao dia 19 com a demo dependendo de uma calibração que ninguém fez.
- **Totem da Viela (total 6,35).** Tem uma contradição interna que a própria banca apontou: o selo numerado cria o cadastro de casas trincadas que o morador teme, e o "anônimo" acontece no balcão do mercadinho, onde todos se veem. O rótulo de deslizamento não se sustenta (15 eventos na capital entre 1991 e 2025), então o ML vira nowcast de chuva. Teve o pior SUS, e a Edileuza não concluiu a tarefa porque não existe tela para a ACS montar a lista de prioridade. Também depende de sete atores acordados de madrugada.

---

## 2. Ficha da CitizenTech

| Campo | Conteúdo |
|---|---|
| **Nome** | Corredor Aceso |
| **Cidade e bairro** | São Paulo (SP), Zona Leste. Piloto em uma microárea de UBS da ESF, com o Jardim Helena como referência, a confirmar com a UBS real que a equipe conseguir visitar. |
| **Cidadão beneficiado** | Idosos 60+ que moram sozinhos e casas com eletrodependente (concentrador de oxigênio). Operadora: a ACS da Estratégia Saúde da Família. |
| **Problema em uma frase** | Em dia de calor extremo ou de apagão, a ACS não consegue checar todos os idosos sozinhos da microárea e não sabe quem está em risco, enquanto a idosa bebe menos água e cai de madrugada por medo do escuro. |
| **Como funciona** | (1) De madrugada, um modelo de ML treinado com INMET e SIH/DATASUS decide se o dia é crítico para 60+. (2) Na casa, um nó ESP32 acende a luz do corredor quando a idosa passa, e esse movimento vale como "estou bem". Ele também mede a temperatura e a energia, tem um botão grande, bateria e buffer. (3) Em dia crítico, o app da ACS mostra só quem ficou em silêncio, em três estados: verde some, vermelho quer dizer perigo, cinza quer dizer aparelho sem sinal (e cinza nunca vira verde). (4) O vizinho de confiança, que a idosa escolheu, é chamado antes da ACS. (5) Toda mensagem que sai passa por um motor de PII e taint, e um LLM explica cada bloqueio. |
| **O que cada parte faz** | **ESP32 (Wokwi):** sensor de check-in (PIR), luz de caminho (LED), DHT22, sensor de tensão, botão e heartbeat. **App Expo:** radar da ACS com alto contraste e cache SQLite. **API C# (.NET):** Motor de Prioridade que aplica as regras de contato e ordena a lista. **Backend SOA (FastAPI):** cadastro, consentimento, vizinho de confiança, ingestão do `DiaCritico`, histórico e guarda de PII/taint. **ML (XGBoost):** decide o dia crítico. **Cyber:** PII, taint, explicação por LLM e dashboard "Risco Cidadão". **Linux:** VM com API, Mosquitto, painel, monitoramento systemd, auditd e teste de falha. |
| **Link do repositório** | (a preencher) |
| **Indicador de impacto** | **Principal:** % de idosos sozinhos da microárea com contato confirmado até 10h em dia crítico. **Operacional:** casas que a ACS precisa visitar em dia crítico (hipótese: de 9 para 2 ou 3, a validar com ACS real). **Segurança:** taxa de "falso verde" (meta 0 nos testes) e tempo entre o primeiro sinal de perigo e a visita. |
| **ODS conectados** | Ver abaixo. |

**ODS e por quê:**
- **ODS 3 (Saúde e bem-estar), metas 3.4 e 3.d.** Previne desidratação, quedas e descompensação cardiovascular em idosos, e transforma alerta climático em ação de saúde na atenção básica.
- **ODS 11 (Cidades sustentáveis), meta 11.5.** Reduz o dano de eventos extremos (calor e apagão) a quem é mais vulnerável no território.
- **ODS 13 (Ação climática), meta 13.1.** Adapta o cuidado ao calor extremo com um gatilho baseado em dados climáticos reais.
- **ODS 10 (Redução das desigualdades), meta 10.2.** A idosa não precisa de smartphone, Wi-Fi, crédito nem saber ler. O público escolhido é o de quem vive com BPC e sozinho.

---

## 3. As seis perguntas obrigatórias

1. **Quem é o cidadão beneficiado?** A idosa ou o idoso de 60+ que mora sozinho (persona Dona Cida, 79 anos, com BPC, catarata, diurético e sem Wi-Fi) e famílias com eletrodependente. A beneficiária indireta, e operadora, é a ACS, hoje sobrecarregada.
2. **Qual problema será resolvido?** No dia perigoso, ninguém sabe a tempo **quem** está em risco. A ACS liga para 9, deixa uns 30 de fora e termina perto do meio-dia. A idosa fica de 8 a 10 horas por dia (tarde e noite) sem que ninguém perceba uma queda, e restringe a água para não levantar no escuro.
3. **Como a solução funcionará?** O modelo abre o turno de radar no dia crítico. O nó em casa gera sinais passivos de vida. O vizinho de confiança confirma primeiro. A ACS recebe só os vermelhos e os cinzas, ordenados por risco. Para eletrodependentes, a perda de energia ou de heartbeat gera alerta imediato, a qualquer hora.
4. **Quais tecnologias?** ESP32 no Wokwi (PIR, LED, DHT22, potenciômetro simulando o sensor de tensão, botão, buzzer), MQTT/Mosquitto, Web API .NET com EF Core e LINQ, FastAPI com Postgres, Alembic e Swagger, app Expo (React Native, TypeScript, Expo Router, SQLite), XGBoost em Python, LLM restrito a explicações, Linux (Shell, systemd, auditd, Nginx) e Azure Boards.
5. **Quem participa do ecossistema?** A idosa e o vizinho de confiança, a ACS e a enfermeira/gerente da UBS, a OS gestora da ESF, a SMS-SP (Coordenação da Pessoa Idosa) como compradora, a distribuidora de energia (lembrete do cadastro de cliente vital; integração fica fora do escopo) e a família, só com opt-in escolhido pela idosa.
6. **Como o impacto será medido e ampliado?** **Medido:** % de contato confirmado até 10h, casas a visitar por dia crítico, tempo até a visita, falso verde, cinzas resolvidos pelo vizinho, % de aparelhos ligados depois de 30 dias e recall do modelo contra a baseline. **Ampliado:** piloto de uma microárea no verão, depois expansão para a UBS, em seguida edital de inovação da SMS e P&D ANEEL para o módulo de eletrodependentes. O modelo B2G não cobra nada da idosa nem da ACS.

---

## 4. Mapa por disciplina

| Disciplina | O que entregar | Ligação com a rubrica (pontos) |
|---|---|---|
| **SOA e Web Services** (FastAPI + Postgres) | Domínio `Idoso`, `Microarea`, `Consentimento`, `VizinhoDeConfianca`, `DiaCritico`, `Evento`, `Visita`. Ingestão do `DiaCritico` vindo do ML. Guarda de PII/taint na saída de mensagens. | Domínio 3: regras reais ("cinza nunca vira verde", "acamado entra direto na lista"). Banco 3: Postgres. Migrações 1,5: Alembic. Swagger 1,5: OpenAPI, que também é o contrato com o C# fechado no dia 2. Testes 1: pytest das regras. |
| **Cybersecurity** | Motor de PII (CPF com dígito verificador, CNS, CID, nomes de remédio, "mora sozinho", endereço, senhas). **Taint de verdade**: os campos do SOA (`moraSozinho`, `medicamentos`, eventos de presença) nascem rotulados na fonte, e o texto livre recebe o rótulo do classificador. Os sumidouros (grupo do bairro, log, prompt do LLM, export) checam o rótulo. LLM só explica o bloqueio e sugere a versão segura. Dashboard "Risco Cidadão" por canal, sem identificar a ACS. | PII 3, taint 3, LLM 2, dashboard 2. A resposta à banca ("isso é classificação ou taint?") está no rótulo de origem. |
| **C# Software Development** | Motor de Prioridade .NET: classe abstrata `RegraDeContato` com `SemMovimentoNaJanela`, `IdaNoturnaSemVolta`, `CalorInterno`, `SemHeartbeat` e `EnergiaEletrodependente`. LINQ para ordenar o radar e agrupar apagões por rua. Async para notificar o vizinho e a ACS com retry. | POO 3 (herança e polimorfismo legítimos), Web API 2,5, EF Core + LINQ 1,5, async/await 1, qualidade 2 (xUnit, camadas, logs sem PII). |
| **IA & ML** | Pergunta, dataset e modelo da seção 5. Notebook com baseline obrigatória, grid de 24 combinações, TimeSeriesSplit, métricas de treino contra teste e conclusão para o cidadão. | Dados reais e públicos (INMET + SIH), 10+ combinações de 3 hiperparâmetros, CV, métricas e conclusão. |
| **Mobile & IoT** (Expo) | App da ACS: entrada, dia crítico, radar, detalhe, mensagem bloqueada e encerramento. Alto contraste, fonte grande, cache SQLite dos cards e da linha do tempo, consumo da API C#. Desfechos: "estava bem", "estava fora/com família", "não abriu, chamei SAMU", "encaminhei". Vídeo de até 3 min. | Expo Router + TypeScript, consome API, salva em banco local, acessível. |
| **Operating Systems** (Linux) | `provision.sh` instala Postgres, Mosquitto, APIs, Nginx com o painel UBS e o dashboard. Monitor de CPU, disco e memória como serviço systemd. auditd registrando **quem consultou a lista**. Teste de falha: derrubar o Mosquitto, o nó guarda em buffer e reenvia com o horário original. Vídeo de até 7 min. | Shell, app web hospedada, systemd, auditoria e teste de falha, todos ligados à narrativa. |
| **Physical Computing** (ESP32/Wokwi) | Nó do corredor com PIR + LED de caminho, DHT22, potenciômetro como tensão, botão grande (um toque = "estou bem"; "hoje vou sair" por duplo toque, que é mais fácil que segurar 3 s), LED âmbar + buzzer para "dia perigoso: beba água", buffer + heartbeat. Mais um script que simula 39 casas, **dito no vídeo**. Vídeo de até 4 min. | O ESP32 é o sensor principal do produto, não um projeto paralelo. |
| **Testing, Compliance & QA** (Azure Boards) | Features com critérios de aceite. Positivos: "movimento remove o card do radar", "eletrodependente sem heartbeat gera alerta em menos de 1 min". Negativos: "silêncio nunca vira verde", "família não é avisada sem opt-in", "mensagem com 'mora sozinha' + remédio é bloqueada no grupo", "CPF nunca aparece em log nem em prompt", "duplo toque silencia a regra de movimento até 18h". | Features, critérios, casos positivos e negativos. Pitch enviado aqui. |

---

## 5. Pergunta de ML, dataset, modelo e hiperparâmetros

**Pergunta (refinada para fugir das críticas da banca):**
"Com o que se sabe às 5h da manhã (temperatura mínima da noite anterior, máxima de ontem, umidade, número de dias seguidos com mínima noturna acima do P90 do mês, mês e dia da semana), hoje será um dia de **excesso de internações de urgência 60+ por causas cardiovasculares** em São Paulo?"

- **Alvo:** internações com `CAR_INT=02` (urgência), `MUNIC_RES=355030` (6 dígitos), idade 60+ (`COD_IDADE=4`), `DIAG_PRINC` em I00-I99 (E86 e T67 entram, mas pesam quase nada), agregadas por `DT_INTER` empilhando competências e descartando os meses ainda incompletos. **Dia de excesso** = acima do **P90** (não P75, para não virar 1 dia em cada 4) da média móvel de 28 dias, ajustado por dia da semana.
- **Sazonalidade de inverno** (pergunta difícil da banca): a média móvel absorve a sazonalidade lenta, e o modelo final é treinado e reportado **também só na estação quente (outubro a março)**. Se o sinal de calor for fraco, isso é dito no pitch, e o produto continua valendo pelo gatilho de apagão e pela regra de baseline.
- **Sem vazamento:** só entram variáveis observadas até as 5h. Não é preciso proxy de previsão.
- **Baseline obrigatória:** "mínima noturna > X °C por 2 noites". O XGBoost só "vence" se superar a baseline em recall e PR-AUC no teste.

**Datasets (abertos e conferidos em 05/11/2026):**
- INMET, estações automáticas, um ZIP por ano: `https://portal.inmet.gov.br/uploads/dadoshistoricos/2025.zip` (padrão `.../AAAA.zip`). Arquivo da estação A701 Mirante de Santana. CSV `;`, decimal com vírgula, latin1, 8 linhas de metadados, hora em UTC (subtrair 3 h). Série de 2006 até hoje. Só o arquivo de 2025 foi aberto; os demais anos seguem o padrão, mas é preciso conferir no dia 1.
- SIH/SUS RD: `ftp://ftp.datasus.gov.br/dissemin/publicos/SIHSUS/200801_/Dados/` com os arquivos `RDSPaamm.dbc` (de RDSP0801 a RDSP2608). Leitura com `datasus-dbc` + `dbfread`. O RDSP2502 foi conferido: 231.291 AIHs, 113 colunas.
- Plano B, **não verificado**: SIM, óbitos (`DOSPaaaa.dbc`).

**Modelo e hiperparâmetros:**
- XGBoost (classificação binária) com `scale_pos_weight` (cerca de 9 para P90).
- Grid de **24 combinações**: `max_depth` {3, 4, 5, 6} x `learning_rate` {0,03, 0,1, 0,3} x `n_estimators` {100, 300}.
- `TimeSeriesSplit(n_splits=5, gap=3)` no treino (2015 a 2023). Teste em 2024 a 2025.
- Métricas de treino contra teste: recall da classe "excesso", F1, PR-AUC e matriz de confusão, sempre ao lado da baseline.
- **Conclusão para o cidadão:** "Quando a noite não esfria abaixo de X °C por 2 noites seguidas, a chance de um dia de excesso de internações de idosos sobe Y vezes. É nesse dia que a ACS recebe a lista." Se Y não aparecer, a frase passa a ser "a regra simples da noite quente já resolve, e o modelo não acrescenta", e isso também é um resultado honesto.

---

## 6. Arquitetura em uma visão

```
              [INMET A701 + SIH/DATASUS]
                         | (notebook / job diário 05h)
                         v
                  [Modelo XGBoost] --DiaCritico(prob, motivo)--> [Backend SOA - FastAPI + Postgres]
                                                                    | cadastro, consentimento,
 [Casa: ESP32 no Wokwi]                                             | vizinho de confiança,
  PIR+LED / DHT22 / tensão / botão / bateria+buffer                 | histórico, motor PII/taint
     | MQTT (tópico casa/<token>, sem nome)                         |
     v                                                              | REST (contrato OpenAPI)
 [Mosquitto] --bridge--> [Motor de Prioridade - API C# .NET] <------+
                           RegraDeContato x5, LINQ, async
                           |                    |
                           | lista ordenada     | alerta imediato (eletrodependente)
                           v                    v
                   [App Expo da ACS]     [Vizinho de confiança: canal simulado]
                   radar verde/vermelho/cinza, SQLite
                           | "pedir ajuda no grupo"
                           v
                   [SOA: taint + PII] --bloqueio--> [LLM: explica e sugere versão segura]
                           |
                           v
                   [Dashboard Risco Cidadão + painel UBS - Nginx]

 [Infra Linux (VM)]: provision.sh -> Postgres, Mosquitto, APIs, Nginx;
 systemd (monitor CPU/disco/mem); auditd (quem consultou a lista);
 teste de falha: derruba o broker -> nó guarda em buffer -> reenvia com horário original.
```

**Regra de ouro do fluxo:** o vínculo token-pessoa existe **só no SOA** (o ESP32 e o MQTT não conhecem nomes). Os eventos detalhados são apagados em 24 h. O registro "ACS visitou às 10h12, estava bem" é guardado à parte, como proteção da profissional (pedido da Edileuza).

---

## 7. Plano de 2 semanas (05/11 a 19/11), 5 pessoas

**Papéis fixos:** **P1** ESP32 + Linux · **P2** SOA + Cyber · **P3** C# · **P4** ML · **P5** Expo + Azure Boards + pitch (e quem conduz as entrevistas).

| Data | Marco | Detalhe |
|---|---|---|
| **Qui 05/11** | Kickoff | Repo, README com a ficha, `prompts.md`, Boards com épicos. P5 contata uma UBS ou uma ACS conhecida. |
| **Sex 06/11** | **Entrevista real 1 + spike ML** | P5 e P2 entrevistam uma ACS real (roteiro de 7 perguntas da validação Lean) e testam as telas impressas A (40 nomes) e B (radar com 3). P4 baixa de 12 a 24 competências do RDSP e o INMET. |
| **Sáb 07/11** | **Experimento Lean** | Instalar luminárias LED com sensor de presença (R$ 25 a 40 cada) no corredor de 3 a 5 idosos reais (avós ou vizinhos), mais o papel da geladeira. **Contrato OpenAPI entre C# e SOA fechado.** |
| **Dom 08/11** | **Portão de decisão** | Sucesso se: a ACS começa pelos vermelhos e cinzas e aceita não ligar para os verdes em 70% ou mais dos casos; 3 de 5 idosos querem ficar com a luz; lift da baseline de mínima noturna de 1,3 ou mais. **Pivôs:** se a ACS não confia no verde, o produto passa a só ordenar (não esconde ninguém); se o sinal de calor não aparece, entra o SIM ou o gatilho de apagão. |
| 09 a 12/11 | Construção | P1: nó Wokwi + script de 39 casas + `provision.sh`. P2: domínio, migrações, PII e taint. P3: regras e LINQ. P4: grid e baseline. P5: telas C0 a C5, critérios no Boards. |
| **Sex 13/11** | **Integração ponta a ponta v1** | Movimento no Wokwi, MQTT, C#, app: o card some. |
| 14 a 16/11 | Endurecimento | Estado cinza, alerta para eletrodependente, cena do taint com LLM, auditd, teste de falha do broker, testes automatizados. Segunda rodada com a ACS real (teste de 5 min do app), se possível. |
| **Seg 17/11** | **Congelamento de código** | Cortar o que não estiver integrado (ordem de corte: mapa a pé, botão "vou sair", dashboard detalhado). |
| Ter 18/11 | Vídeos | Mobile (3 min), Linux (7 min), ESP32 (4 min), pitch (4 min). Gravar o plano B da demo. |
| **Qua 19/11** | Entrega até 23h55 | Revisão do README, links e Boards. |

---

## 8. Roteiro de pitch (4 min)

| Tempo | Cena | Fala-chave |
|---|---|---|
| 0:00-0:20 | Tela do celular da ACS com 40 nomes | "Isto não é um botão de pânico. É o celular da Edileuza num dia de 36 °C: 40 idosos, uma ACS só." |
| 0:20-0:50 | Foto do corredor escuro + depoimento da entrevista **real** (sem identificar) | "Uma idosa nos contou que bebe pouca água de propósito para não levantar no escuro. O problema de calor começa no corredor." |
| 0:50-1:30 | **Momento uau, parte 1:** Wokwi, PIR acionado às 3h, LED acende, e ao vivo os cards somem até sobrarem 3 | "Passar no corredor já é 'estou bem'. A Edileuza não checa mais 9 casas, checa 2." |
| 1:30-2:00 | **Uau, parte 2:** potenciômetro a zero na casa do concentrador, o nó fica mudo, o card fica **cinza** no topo e o alerta sai na hora | "O silêncio do aparelho nunca vira verde. No apagão, quem depende de tomada vem primeiro, sem esperar as 9h." |
| 2:00-2:30 | **Uau, parte 3:** a ACS digita no grupo "mora sozinho e toma diurético", a tela fica vermelha, o LLM explica e sugere a versão segura | "No grupo do bairro, isso é mapa pra ladrão. O sistema impede." |
| 2:30-3:10 | ML: gráfico de mínima noturna contra excesso de internações, modelo contra baseline | "Quando a noite não esfria abaixo de X °C por duas noites, o risco sobe Y vezes. É aí que o radar liga, e só aí." |
| 3:10-3:40 | Arquitetura em um slide + ODS 3, 11, 13, 10 | "Uma solução, oito disciplinas, dados públicos, nenhum dado pessoal real." |
| 3:40-4:00 | Indicador + próximo passo | "Meta: 100% dos idosos sozinhos com contato até 10h. Próximo passo: um verão numa UBS de verdade." |

Na tela, aparece honestamente: "1 nó real no Wokwi + 39 casas simuladas".

---

## 9. Riscos, plano B e perguntas difíceis da banca

**Riscos principais e plano B**

| Risco | Plano B |
|---|---|
| Nenhuma ACS real disponível até 08/11 | Ligar para a ouvidoria/gerência de duas UBS e procurar ACS na família ou na igreja de algum membro da equipe. No mínimo, entrevistar um familiar cuidador real e um idoso real, e declarar a limitação. |
| Sinal calor contra internação fraco (pico de inverno) | Reportar com transparência. Usar a baseline como gatilho e acrescentar o SIM como alvo. O gatilho de apagão para eletrodependentes funciona sem ML. |
| Demo ao vivo falha (Wokwi, MQTT ou app) | Vídeo gravado em 18/11. Broker local na VM, nunca um público. |
| Falso verde (gato, visita) | O check-in passivo só vale para quem mora sozinho. Duas janelas (10h e 15h) e regra de calor interno. O idoso acamado entra direto na lista. |
| Escopo de dois backends | Contrato OpenAPI no dia 2. Se travar, o SOA vira só cadastro + PII e o C# assume o restante. |

**Perguntas difíceis e como responder**

1. **"Vocês falaram com alguém de verdade?"** → Mostrar a entrevista com a ACS real de 06/11 e o resultado das luminárias (quantos idosos ficaram com a luz). Dizer que as personas foram o ponto de partida e foram substituídas por gente real.
2. **"Em SP o pico cardiovascular é no inverno."** → "Por isso o alvo é excesso sobre a média móvel e avaliamos só a estação quente. Aqui está a comparação com a baseline. Se empatar, a regra simples basta, e mostramos isso."
3. **"O P75 liga o radar 90 dias por ano."** → "Mudamos para P90 e estação quente: cerca de 15 a 20 dias críticos por ano."
4. **"Às 5h vocês não têm a previsão do dia."** → "Usamos só variáveis observadas até as 5h (noite anterior), então não há proxy e não há vazamento."
5. **"O PIR fica no corredor, e à tarde ela está no quarto dos fundos."** → "Em dia crítico, às 15h o nó acende âmbar e toca o buzzer pedindo um toque no botão. Sem toque, o DHT acima de 33 °C entra na regra e o card fica vermelho."
6. **"Por qual canal o vizinho recebe? Não é o mesmo problema do SMS?"** → "Por WhatsApp da UBS, combinado presencialmente na UBS com ligação de teste. No protótipo, o canal é simulado e isso é dito."
7. **"Queda numa noite comum é detectada?"** → "A regra de ida noturna sem volta em 20 min roda toda noite para quem optou, porque esse é o maior medo da idosa. O vizinho é avisado na hora."
8. **"Isso é taint ou classificação?"** → Mostrar no código: o rótulo nasce no campo do SOA (`moraSozinho`, `medicamentos`) e é checado no sumidouro (grupo, log, prompt, export). O texto livre ganha rótulo pelo classificador.
9. **"A versão segura sugerida ainda expõe o endereço."** → "Corrigimos: a sugestão é chamar o vizinho de confiança em privado, e no grupo vai só 'quem estiver perto da Rua X, me chame no privado'."
10. **"Qual a base legal na LGPD?"** → Art. 11, II, "f" (tutela da saúde por profissional de saúde), somado a consentimento registrado na UBS com a ACS presente. Em caso de declínio cognitivo, o responsável legal participa.
11. **"Os 40 são reais?"** → "Um nó Wokwi real e 39 simulados por script. Está escrito na tela."
12. **"Por que mais um app se a ACS vive no WhatsApp e no e-SUS?"** → "O radar só abre em dia crítico. É uma tela, não um cadastro novo. A integração com o e-SUS e a visita contar como produção estão no roadmap e foram levadas à gerente."

---

## 10. O que as personas ensinaram (simulado)

1. **Dona Cida (entrevista):** *"Eu prefiro passar sede do que levantar três vezes de madrugada."*
   → A luz do corredor deixou de ser brinde e virou a intervenção de saúde principal. O pitch abre por ela.
2. **Dona Cida (entrevista):** *"Pra Rose eu falei que escorreguei [...] não falei que desmaiei, senão ela já ia querer me levar pra morar com ela."*
   → A família **nunca** é avisada por padrão. É escolha da idosa, feita na UBS, com a opção "avisar se eu cair" explicada com exemplos.
3. **Edileuza (teste de protótipo):** *"Só a sua ACS vê: quer dizer que se der ruim, a responsável sou eu?"*
   → O papel da geladeira passou a dizer "só a equipe da sua UBS vê", com substituto em folga e férias. O registro da visita da ACS é guardado como proteção dela.
4. **Edileuza (teste do conceito):** *"Eu passo o ano inteiro falando no grupo pros idosos NÃO responderem SMS pedindo número."*
   → O SMS "responda 1" saiu de vez. Entraram o botão físico e o vizinho de confiança.
5. **Cléber (teste do conceito):** *"Se a luz para de funcionar justamente no apagão, que é o dia que eu mais preciso, então ela é só uma lâmpada com sensor de R$ 30."*
   → Nasceram o **estado cinza** (silêncio nunca vira verde), a bateria com buffer e o **alerta imediato para eletrodependente**, sem esperar as 9h.

Outras mudanças vieram dos testes de usabilidade: confirmação do botão por som em vez de pisca verde ou azul (catarata), duplo toque no lugar de "segurar 3 s", agrupamento de apagão por rua ("Rua X: 12 casas sem sinal") e a troca de "sinal de vida" por "deram sinal hoje".

---

## 11. Apêndice: as duas finalistas

**Vigília: o poste que testemunha contra o "finalizado"** (total 6,73 · banca 8)
Um ESP32 com LDR colimado, atrás do vidro de um comércio, grava a luz do poste à noite, offline. Se o órgão marca "finalizado" e o sensor vê 3 noites escuras com 2 confirmações humanas, o caso vira "REABERTO PELA RUA" e sai um dossiê com hash para a SP Regula.
Tem o melhor achado de dados (iluminação não aparece no SP156: 0 de 484.232 linhas), mas a física da vitrine não foi testada, o funil de casos é estreito e o ML fica ao lado do sistema.

**Totem da Viela: o aviso sem nome que sobe, a rota que desce** (total 6,35 · banca 7)
Uma caixa com três botões de pictograma, num ponto com dono na viela, recebe o aviso anônimo. O NUPDEC confirma, a Defesa Civil decide com um comando assinado, e a caixa fala por onde sair e qual abrigo está aberto.
A arquitetura de segurança é forte (decisão humana, HMAC, P-07), mas o selo numerado contradiz a premissa de anonimato, o rótulo de deslizamento é raro demais e a parte da ACS não foi desenhada.

---

*Arquivos de referência:* briefing em `docs/briefing.md`. Scripts de verificação do SIH (citados na validação Lean) em `(pasta temporária) scripts/sih.py` e `(pasta temporária) scripts/sih2.py`.