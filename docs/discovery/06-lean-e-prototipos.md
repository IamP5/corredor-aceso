# Validação Lean e teste de protótipo

## Corredor Aceso: a lista de quem ficou em silêncio (MVP enxuto)

**Conceito refinado.** TESE (abre o pitch em 20 s): "Não é um botão de pânico. Quando o dia vai ser perigoso para idosos, a ACS abre o celular e vê só quem ficou em silêncio." A tela começa com os 40 idosos da microárea e, ao vivo, esvazia até sobrarem 2 ou 3. Só depois entra a luz do corredor, que é o motivo de a Dona Cida aceitar o aparelho.

O QUE MUDOU EM RELAÇÃO AO CONCEITO ANTERIOR (fixes dos juízes + objeções das personas)
1. Escopo cortado. Saem do MVP e vão para o roadmap: URA para fixo, modo offline da ronda, resumo por LLM e aviso automático à família. O LLM fica só onde a rubrica de Cyber pede, explicando o bloqueio do taint.
2. Sai o SMS "responda 1" com o nome da ACS. As três personas recusaram ("vai contra tudo o que eu ensino sobre golpe", "golpista copia no dia seguinte"). No lugar dele entram dois sinais que golpista não copia:
   a) o botão grande no próprio aparelho, com duas funções: "Estou bem" (um toque) e "Hoje vou sair" (segurar 3 s, para a feira de terça do Seu Antônio não virar alarme falso);
   b) o vizinho de confiança, opt-in, que a Edileuza já anota no caderno. Quando um idoso fica em silêncio, o vizinho recebe primeiro um aviso neutro ("Pode bater na casa da Dona Cida?"), sem diagnóstico e sem "mora sozinha". Ele confirma com um toque. Só se ninguém confirmar em 1 h o nome sobe para a lista da ACS. Assim a ACS deixa de ser a única responsável.
3. Apagão sem contradição técnica. O radar passa a ter TRÊS estados, e o silêncio do aparelho nunca vira verde:
   - VERDE: houve sinal de vida recente (movimento ou botão).
   - VERMELHO: o aparelho está falando, mas há perigo (sem movimento na janela, quarto acima de 34 °C por 2 h, ida noturna ao banheiro sem volta em 20 min).
   - CINZA: "casa sem sinal do aparelho" (perda de heartbeat por 30 min). Fica acima do verde e pede confirmação por vizinho ou visita.
   O nó tem bateria (18650) e guarda os eventos em buffer. Quando a energia e a rede voltam, reenvia tudo com o horário original, e o histórico mostra o que aconteceu durante o apagão. No mundo real o uplink precisa ser próprio (4G Cat-M/NB-IoT ou LoRa), sem depender do Wi-Fi da casa (que Dona Cida nem tem). No Wokwi isso é simulado por um segundo tópico MQTT de "canal reserva" e pelo próprio heartbeat. É o roadmap de hardware, dito com honestidade no pitch.
4. Eletrodependente tem regra própria e imediata, sem esperar as 9h (pedido do Cléber). Se o último pacote mostra tensão caindo ou o heartbeat some numa casa marcada como eletrodependente, sai alerta na hora para a ACS e para o contato que a família escolheu, se houver opt-in. O app também mostra o lembrete "cadastro de cliente vital na Enel: feito? sim/não". Integração com a Enel fica fora do escopo; o lembrete é só um checklist.
5. Duas janelas de checagem no dia crítico: 10h e 15h. Dona Cida passa no corredor de manhã e fica a tarde no quarto dos fundos, que é o mais quente. A regra de calor interno (DHT22) cobre a tarde.
6. Público delimitado. O check-in por movimento só vale para quem MORA SOZINHO. Em casa com mais gente (caso do Cléber) o PIR não identifica quem passou, então ali o aparelho é só luz + alerta de energia para eletrodependente. Idosa acamada não usa o PIR: o cadastro marca "acamado" e ela entra direto na lista do dia crítico (ou usa só o botão, se conseguir).
7. Privacidade como argumento de venda, não como anexo: sem câmera e sem microfone; o vínculo token-pessoa fica só na UBS; o radar só existe em dia crítico ou em alerta de energia; os eventos detalhados são apagados em 24 h e só ficam agregados. Quem instala é a UBS (ACS acompanhada de agente de zoonoses/manutenção ou técnico da OS), nunca "o pessoal da FIAP de crachá". Dona Cida recebe um papel para a geladeira, em letra grande: o que o aparelho faz, quem vê, quando apaga e o telefone da UBS. O consentimento é feito na UBS, com a ACS ao lado, e não sozinha no celular.
8. Cena de taint plausível: o vizinho cuidador pergunta no grupo do bairro "alguém viu o Seu Antônio?". A ACS vai responder com "ele mora sozinho, toma diurético e está sem movimento desde cedo". O motor de taint bloqueia, porque saúde + "mora sozinho" + padrão de presença formam um "mapa pra ladrão", na fala da Edileuza e do Cléber. O LLM explica em linguagem simples e sugere a versão segura: "Alguém pode bater no nº 32 da Rua X? Obrigada."
9. ML com alvo que tem volume (dados conferidos no SIH): internações de URGÊNCIA (CAR_INT=02) de RESIDENTES em SP (MUNIC_RES=355030, código de 6 dígitos), 60+, CID I00-I99 (E86 e T67 entram, mas quase não pesam), agregadas por DT_INTER empilhando várias competências. "Dia de excesso" = acima do P75 da média móvel de 28 dias, ajustado por dia da semana. Variáveis: INMET A701 OBSERVADO (temperatura mínima noturna, máxima, umidade, dias seguidos acima do P90), usado como proxy da previsão, com isso declarado para não parecer vazamento. Comparação obrigatória com a baseline "temperatura mínima noturna > X °C" para provar que o XGBoost acrescenta algo. Plano B: óbitos do SIM (DOSP). O resultado do modelo vira a entidade DiaCritico, que abre o turno de radar.
10. Contrato entre os dois backends, escrito até o dia 2:
   - C# .NET = Motor de Prioridade. Recebe CheckIn por MQTT (via bridge), aplica a classe abstrata RegraDeContato (SemMovimentoNaJanela, IdaNoturnaSemVolta, CalorInterno, SemHeartbeat, EnergiaEletrodependente) e devolve a lista ordenada (LINQ). Em async, notifica vizinho e ACS.
   - SOA = cadastro Idoso/Microárea/Consentimento/VizinhoDeConfiança, ingestão do DiaCritico vindo do ML, histórico e motor de PII/taint na saída de mensagens.

Métrica de impacto: % de idosos sozinhos da microárea com contato confirmado até 10h em dia crítico, e "casas que a ACS precisa visitar" (meta: de 9 para 2 ou 3). A meta será validada com ACS real; hoje é hipótese.

Validação com personas: esta etapa não tinha ferramenta para criar um time de agentes. O re-teste abaixo é uma simulação escrita por mim a partir das falas registradas no personaTests, não uma rodada nova de agentes. Recomendo rodar de verdade com o agent team na próxima fase, e principalmente com pessoas reais.
- Dona Cida: as objeções principais foram atendidas (bateria, sem SMS, botão grande, checagem à tarde, consentimento com a Edileuza do lado). Dúvida que continua: "vai gastar luz?". Resposta: o consumo do ESP32 em deep sleep com o LED só acionado por presença é de centavos por mês, e isso deve ir no papel da geladeira.
- Edileuza: o vizinho antes dela e o "Hoje vou sair" resolvem a carga e o alarme falso. Continua aberto: o app novo versus o WhatsApp, e a visita contar como produção no e-SUS. Isso fica como pergunta para a gerente da UBS.
- Cléber: o estado cinza e o alerta imediato para eletrodependente respondem à crítica central. Continuam: quem mantém depois da GS e o uplink 4G real, que é custo de roadmap.

### Lean Canvas

- **problem.** 1) Em dia de calor extremo ou de apagão, a ACS não consegue checar todos os idosos que moram sozinhos e os eletrodependentes da microárea. Liga para 9, deixa ~30 de fora e termina perto do meio-dia. 2) O alerta de massa (SMS da Defesa Civil, TV) chega a quem tem celular bom e não diz à ACS QUEM está em risco. 3) Idosos bebem menos água e caem à noite por medo do escuro, e quem toma diurético desidrata mais. Alternativas atuais: ligações manuais da ACS, caderno de vizinhos, teleassistência particular paga (botão/pulseira voltados à família), cadastro de cliente vital na Enel (sem ligação com a saúde).
- **customerSegments.** Usuário-beneficiário: idosos 60+ que moram sozinhos (ex.: Dona Cida, 79, BPC, sem Wi-Fi) e casas com eletrodependente (concentrador de O2). Usuário operador: ACS da ESF em São Paulo (ex.: Edileuza). Comprador/pagador: Secretaria Municipal de Saúde/OS gestora da ESF e Coordenação de Atenção à Pessoa Idosa. Early adopters: uma UBS de periferia (ex.: Itaquera, sugerida pelo Cléber), com gerente aberta a piloto no verão, e as ACS que já mantêm lista de vizinhos de confiança.
- **uvp.** 'A Edileuza não checa mais 9 casas, checa 2.' No dia perigoso, a ACS vê só quem ficou em silêncio. A idosa ganha uma luz que evita a queda à noite e não precisa mexer em nada, sem câmera, sem SMS e sem cadastro no celular.
- **solution.** 1) Modelo de dia crítico 60+ (INMET A701 + SIH/DATASUS) que abre o turno de radar. 2) Nó ESP32 no corredor: PIR + LED de caminho, DHT22, sensor de tensão, botão grande 'Estou bem / Hoje vou sair', bateria com buffer e heartbeat. 3) Radar da ACS com 3 estados (verde some; vermelho e cinza ficam em cima, ordenados por risco), vizinho de confiança como primeira checagem e alerta imediato para eletrodependente. 4) Guarda de PII/taint em toda mensagem que sai, com o LLM explicando bloqueios.
- **channels.** Para o piloto: apresentação na reunião de equipe da UBS e com a gerente; a própria ACS apresenta aos idosos em visita domiciliar ('se a Edileuza fala, eles aceitam'); grupo de WhatsApp do bairro, igreja e mutirões para recrutar vizinhos de confiança. Para escala: Coordenação da Pessoa Idosa da SMS-SP, conselhos municipais do idoso, editais de inovação em saúde, e o Programa de P&D/Eficiência Energética da ANEEL via distribuidora (Enel), para o módulo de eletrodependentes. Divulgação: vídeo de pitch e depoimentos (o Cléber só grava depois que funcionar).
- **revenue.** B2G, sem custo para o idoso nem para a ACS. A SMS ou a OS compra o kit por domicílio e paga uma assinatura de plataforma por microárea (software + conectividade M2M). Alternativas: coinvestimento da distribuidora de energia no módulo eletrodependente (P&D ANEEL / responsabilidade regulatória), emenda parlamentar ou edital para o piloto. Estimativa a validar: kit de R$ 150-250 com uplink celular, mais R$ 8-15/mês de chip M2M por casa. Público estreito (sozinhos + eletrodependentes), não todos os idosos.
- **costs.** Hardware por casa (ESP32, PIR, DHT22, LED, bateria 18650 + carregador, módulo 4G/LoRa, caixa): estimativa de R$ 150-250. Conectividade M2M. Instalação e manutenção (troca de bateria, visita técnica): é o custo escondido, e não pode cair sobre a ACS. Nuvem/VM (API, Mosquitto, Postgres), LLM com chamadas só para explicar bloqueios (baixo volume). Conformidade LGPD (RIPD/DPIA, termo de consentimento) e treinamento da equipe da UBS. No protótipo da GS o custo é zero: Wokwi, VM local, dados públicos.
- **keyMetrics.** Impacto: % de idosos sozinhos da microárea com contato confirmado até 10h em dia crítico; número de casas que a ACS precisa visitar (de 9 para 2-3); tempo do primeiro sinal de perigo até a visita; tempo de resposta em queda de energia com eletrodependente. Qualidade: taxa de falso verde (verde mas a pessoa estava mal, meta = 0 nos testes), taxa de falso alarme por dia e % de cinzas resolvidos pelo vizinho antes de chegar à ACS. Adoção: % de idosos que mantêm o aparelho ligado após 30 dias e % de dias críticos em que a ACS abriu a lista até 9h. ML: recall do dia crítico no teste contra a baseline de temperatura mínima noturna. Segurança: mensagens com PII bloqueadas por canal (dashboard Risco Cidadão).
- **unfairAdvantage.** Encaixe nativo no território do SUS: a confiança vem da ACS que a idosa já conhece, não de um app ou de uma empresa. A lista é por exclusão e acionada pelo dia crítico, um desenho que nenhuma teleassistência comercial tem, porque elas vendem para a família e não para a ESF. O benefício próprio (luz contra queda) vence a rejeição à vigilância. A rede de vizinhos de confiança já está no caderno da ACS. É difícil de copiar sem acesso à UBS e às ACS.

**Hipótese mais arriscada.** Que, num dia crítico, o sinal passivo do corredor deixa a lista da ACS realmente curta SEM gerar falso verde, e que a ACS confia nesse verde o bastante para NÃO visitar quem sumiu da tela. Se a ACS não confia, ela continua checando as 9 casas e o produto não muda nada. Se confia e o verde está errado (gato no corredor, idoso que passou de manhã e passou mal à tarde), o produto causa o dano que quer evitar. Logo atrás vêm duas hipóteses dependentes: (a) a UBS assume a instalação e a manutenção (sem isso, a ACS rejeita: "vai ser eu"); (b) existe sinal real de excesso de internações 60+ ligado à temperatura mínima noturna nos dados de SP.

**Experimento.** Experimento de 3 dias (06 a 08/11), custo de até ~R$ 120.

DIA 1, manhã: teste de protótipo de papel/Figma com 1 a 2 ACS reais (UBS perto de casa de alguém da equipe, ou ACS conhecida da família) e, se der, com a enfermeira ou gerente. Material: duas telas impressas.
- Tela A: lista de 40 nomes sem filtro (o "hoje").
- Tela B: radar com 37 verdes escondidos, 2 vermelhos e 1 cinza, cada um com o motivo em uma linha.
Tarefas: (1) "É 9h de um dia de 36 °C. Por quem você começa?" (2) "Você deixaria de ligar para os que sumiram da tela? Em que caso não deixaria?" (3) Contar de cabeça quantos idosos sozinhos e eletrodependentes ela tem e quantos têm vizinho de confiança anotado. (4) Mostrar o papel da geladeira e perguntar se ela conseguiria explicar aquilo ao idoso.

DIA 1, tarde, e DIA 2: teste do benefício com 3 a 5 idosos reais que moram sozinhos (avós, vizinhos da equipe). Usar uma luminária LED com sensor de presença comprada pronta (R$ 25-40 cada) e instalada no corredor. Dizer a verdade: "isso pode um dia também avisar a UBS que a senhora está bem, sem câmera". Registrar a reação e as objeções, e no dia 3 perguntar se a luz ficou ligada e se ela levantou à noite com mais segurança.

DIA 2 e 3, em paralelo (spike técnico de ML, uma pessoa): baixar 12 a 24 competências do RD SP (FTP DATASUS) e INMET A701, montar a série diária do alvo (urgência, residentes 355030, 60+, I00-I99) e testar a correlação ou lift da baseline "temperatura mínima noturna > X por 2 dias".

CRITÉRIOS DE SUCESSO (decidir no dia 3):
- A ACS começa pelos vermelhos/cinzas em 100% das vezes e aceita não ligar para os verdes em pelo menos 70% dos casos, dizendo em que condições (essas condições viram regras).
- A ACS estima que menos de 30% dos seus idosos sozinhos ficariam na lista num dia típico.
- Pelo menos 3 de 5 idosos querem ficar com a luz e nenhum vê o aparelho como vigilância depois do papel da geladeira.
- Nos dados, dias com temperatura mínima noturna alta têm taxa de "excesso" visivelmente maior que a média (lift ≥ 1,3).

CRITÉRIOS DE PIVÔ:
- Se a ACS não confia no verde, o produto passa a ser só "ordenar por risco" (nunca esconder ninguém).
- Se os idosos rejeitam o aparelho, o check-in vai todo para o vizinho de confiança.
- Se o sinal no SIH não aparece, trocar o alvo para óbitos do SIM.

**Roteiro para entrevista real.** Roteiro para a ACS (20 min, presencial, sem gravar nome de paciente; começar pedindo licença e explicando que é trabalho de faculdade, sem coletar dado pessoal):
1. "Me conta como foi o último dia muito quente ou o último apagão na sua microárea. O que você fez desde a hora em que chegou na UBS?" (deixar contar a história e anotar horários e quem ficou de fora)
2. "Quantos idosos que moram sozinhos e quantas pessoas que dependem de aparelho na tomada você acompanha? Desses, quantos você consegue checar até as 10h num dia assim?"
3. "Quando você não consegue ir, quem vai? Você tem vizinho de confiança anotado para cada um? Já pediu para um vizinho bater na porta?"
4. (mostrar a Tela B) "Se esse celular te mostrasse só estes três nomes, você deixaria de ligar para os outros? O que precisaria ser verdade para você confiar? O que te faria nunca mais confiar?"
5. "Se precisasse instalar uma luzinha na casa dos idosos, quem na UBS faria isso? E se quebrar ou acabar a bateria?"
6. "Isso teria que chegar por app, por WhatsApp ou pelo e-SUS Território? Uma visita feita por causa dessa lista conta como produção para você?"
7. "O que é que um sistema desses NUNCA pode fazer com a informação dos seus idosos?" (anotar as palavras dela, que alimentam as regras de taint)

Roteiro curto para a idosa (10 min, com familiar presente; sem perguntar diagnóstico):
1. "A senhora levanta de noite? Como faz com a luz? Já tropeçou ou caiu?"
2. "Num dia muito quente, quem fala com a senhora? Por telefone, mensagem ou visita?"
3. (mostrar a luz e o papel da geladeira) "Se a Edileuza colocasse isto aqui, o que a senhora acharia? Tem alguma coisa aqui que a senhora não quer?"
4. "Se um dia a senhora não passasse no corredor, quem a senhora gostaria que viesse bater aqui primeiro?"

**Dataset (verificado: sim).** DATASETS ABERTOS E CONFERIDOS DE FATO em 05/11/2026 (download e leitura local, não só a página):

1) INMET, dados históricos das estações automáticas. URL: https://portal.inmet.gov.br/uploads/dadoshistoricos/2025.zip (um ZIP por ano; a página de índice https://portal.inmet.gov.br/dadoshistoricos derrubou a conexão sem User-Agent de navegador, mas o ZIP baixou normalmente com User-Agent). Baixei o 2025.zip (90,9 MB) e abri o arquivo "INMET_SE_SP_A701_SAO PAULO - MIRANTE_01-01-2025_A_31-12-2025.CSV":
- Formato: CSV com ';', decimal com vírgula, codificação latin1, 8 linhas de metadados (estação A701, lat -23,496, lon -46,620, altitude 785,64 m, fundação 25/07/2006) antes do cabeçalho.
- Período: horário, de 01/01/2025 00:00 UTC a 31/12/2025 23:00 UTC, 8.760 linhas. Só 47 horas sem temperatura (~0,5%).
- Colunas: Data; Hora UTC; precipitação; pressão (estação, máx, mín); radiação global; temperatura do ar bulbo seco; ponto de orvalho; temperatura máx/mín na hora anterior; orvalho máx/mín; umidade relativa máx/mín/horária; vento (direção, rajada, velocidade).
- Atenção: a hora é UTC (subtrair 3 h para obter a "mínima noturna" local). A série vai de 2006 até hoje. Só conferi o arquivo de 2025; os outros anos seguem o mesmo padrão de URL, mas não os abri.

2) SIH/SUS, AIH Reduzida (RD), FTP do DATASUS: ftp://ftp.datasus.gov.br/dissemin/publicos/SIHSUS/200801_/Dados/ . Listei 225 arquivos RDSPaamm.dbc, de RDSP0801 a RDSP2608 (competência ago/2026), com 18-20 MB cada. Baixei o RDSP2502.dbc e descompactei com o pacote Python datasus-dbc + dbfread:
- 231.291 AIHs, 113 colunas. As relevantes: DT_INTER (aaaammdd), DT_SAIDA, MUNIC_RES, MUNIC_MOV, IDADE, COD_IDADE (4 = anos), DIAG_PRINC (CID-10), CAR_INT (01 eletiva, 02 urgência), MORTE, SEXO, RACA_COR, CEP, DIAS_PERM, VAL_TOT.
- Achados importantes para o pipeline:
  (a) MUNIC_RES usa 6 dígitos: São Paulo é 355030, não 3550308.
  (b) O arquivo é por COMPETÊNCIA de processamento. O RDSP2502 traz internações de 09/2024 a 02/2025, então para fechar a série diária é preciso empilhar várias competências e agrupar por DT_INTER, descartando os últimos meses ainda incompletos.
  (c) Nesse arquivo há 14.844 AIHs de residentes em SP com 60+. Em urgência com CID I00-I99, as internações de fev/2025 dão média de ~45 por dia (mín 1, máx 76; parcial, porque competências posteriores ainda acrescentam casos). É um volume suficiente para definir "excesso".
  (d) T67 = 0 e E86 = 11 no arquivo inteiro: calor e desidratação como diagnóstico principal são raríssimos, então o alvo tem que se apoiar em I00-I99 (talvez também em J, respiratório, a testar).
- Responde à pergunta de ML? Sim, com o alvo redefinido (urgência, residentes 355030, 60+, I00-I99, por DT_INTER, excesso acima do P75 da média móvel de 28 dias ajustado por dia da semana) e INMET A701 observado como proxy da previsão.

Não verificado: o SIM (óbitos, arquivos DOSPaaaa.dbc), que é o plano B, e o tamanho do sinal calor → internação (só os volumes foram conferidos, não a correlação). Scripts de verificação, só para referência: (pasta temporária) scripts/sih.py e sih2.py.

**Concorrentes.** MUNDO
- Canary Care (Reino Unido): monitoramento passivo de atividade com sensores de movimento, porta, temperatura, luz e uso de eletrodomésticos, sem câmera e sem microfone. Tem portal para família e cuidadores e é usado por mais de 100 autoridades locais e pelo NHS (kit de ~£84-298 + ~£31/mês, segundo fonte possivelmente datada).
- Howz (Reino Unido): tomada inteligente, sensor de movimento e de porta com hub, aprendendo mudanças de rotina. Tem estudos de caso de queda detectada por "long-lie".
- Mimamori Hotline / i-Pot da Zojirushi (Japão, desde 2001, renovado em 2023): a chaleira manda e-mail para a família quando é usada. É o precedente clássico do check-in passivo por objeto cotidiano. Os Correios do Japão tentaram um serviço de "mimamori" e recuaram.
- Registre canicule (França, lei de 2004 após 2003): cadastro voluntário na prefeitura/CCAS de idosos isolados, que recebem ligação na onda de calor. Sem resposta, a prefeitura aciona parentes ou a polícia municipal.
- Outros do mesmo espaço (citados de memória, não verificados nesta pesquisa): teleassistência com pulseira/pingente (Tunstall), sensores de rotina para casas de repouso e assistentes de voz.

BRASIL
- Teleassistência municipal com empresa privada, como Santos/SP com a Telehelp desde 2012 e Joinville desde 2008: botão de pânico e central 24 h.
- Teleassistência particular paga, voltada à família.
- Programa Acompanhante de Idosos (PAI) da Prefeitura de SP, que é cuidado presencial, não tecnologia.
- Alertas por SMS da Defesa Civil (40199), em massa e sem alvo.
- Cadastro de "cliente vital"/eletrodependente da Enel: dá atendimento preferencial e isenção após apagão, sem ligação com a saúde e exigindo cadastro prévio.
- e-SUS APS Território, o app de cadastro da ACS, que não tem radar de risco por evento climático.

COMO O CORREDOR ACESO SE DIFERENCIA
1) Quem recebe é a ACS da ESF, no território, e não a família nem uma central paga.
2) O radar só liga em dia crítico, decidido por modelo com dados públicos. Não é vigilância contínua.
3) A lista é por exclusão, com estado "sem sinal" separado de "bem": o silêncio do aparelho nunca vira verde.
4) O vizinho de confiança vem antes da ACS.
5) A idosa ganha um benefício próprio (luz contra queda), em vez de usar um botão que ela precisa lembrar de apertar.
6) Há proteção explícita de PII e taint contra o "mapa pra ladrão".
O modelo do Canary/Howz prova que sensor passivo sem câmera é aceito e comprado pelo poder público. O registre canicule prova que a lista nominal de vulneráveis em onda de calor é política pública viável. O Corredor Aceso junta as duas ideias no SUS.

Fontes: https://www.leicestershire.gov.uk/directories/information-and-support-directory/canary-care-global-limited ; https://scip.hull.ac.uk/?p=900 ; https://everon.net/wp-content/uploads/2025/03/Howz-case-studies-with-business-case-evidence.pdf ; https://www.zojirushi.com/zojirushi/about.html ; https://web-japan.org/trends/11_tech-life/tec180215.html ; https://babytech.jp/en/2017/07/japanpost_jp_withdrawal_from_senior_care_business/ ; https://metropole.toulouse.fr/espace-presse/plan-canicule-1 ; https://www.banquedesterritoires.fr/plan-canicule-le-gouvernement-rappelle-limportance-des-registres-de-personnes-fragiles ; https://saopaulo.sp.leg.br/iah/fulltext/justificativa/JPL0743-2019.pdf ; https://viva.com.br/cidadania-e-direitos/o-abandono-de-quem-depende-de-energia-eletrica-para-viver.html ; https://www.gazetasp.com.br/cotidiano/enel-anuncia-isencao-de-tres-contas-de-luz-para-parte-dos-clientes/

### Protótipo de baixa fidelidade

Tarefa principal: num dia crítico, a ACS descobre em 1 minuto quem precisa de visita e vai primeiro a essa casa, sem expor dados de ninguém. São três superfícies: o aparelho na casa, o cadastro na UBS e o app da ACS (Expo).

A. NA UBS: CADASTRO E CONSENTIMENTO (web simples do backend SOA, usado pela enfermeira com a ACS e a idosa juntas)
A1 "Novo acompanhamento": campos Nome (simulado), Microárea, "Mora sozinho? (sim/não)", "Usa aparelho na tomada para viver? (sim/não, qual)", "Acamado? (sim/não)". Não pede CPF nem foto. O CNS fica guardado só no SOA e criptografado. Botão [Continuar].
A2 "O que a pessoa escolheu", em perguntas lidas em voz alta: "Quer a luz no corredor?" / "Quer que um vizinho seja avisado primeiro se você ficar em silêncio? Quem?" (nome e telefone do vizinho) / "Quer que a família seja avisada? Quando? (nunca / só em falta de luz com aparelho / se não houver sinal o dia todo)". Botão [Gerar papel da geladeira].
A3 "Papel da geladeira" (impresso, letra 18 pt): "Esta luz acende quando você passa. Ela NÃO tem câmera nem microfone. Em dia de muito calor ou falta de luz, ela avisa a UBS se ninguém passou no corredor. Só a sua ACS vê. Tudo apaga em 24 horas. Para tirar, ligue para a UBS: (11) XXXX-XXXX." Botão [Vincular aparelho]: lê o QR do nó e associa o token à pessoa só dentro do SOA.

B. NA CASA: O APARELHO (ESP32 no Wokwi, que é o que a idosa "usa")
B1 Madrugada, 3h: Dona Cida passa no corredor, o PIR detecta e o LED de caminho acende por 60 s. O nó publica "movimento" no MQTT (tópico casa/<token>/evento, sem nome). A idosa não vê nada além da luz.
B2 Botão grande amarelo: um toque = "Estou bem" (LED pisca verde 2 vezes); segurar 3 s = "Hoje vou sair" (LED pisca azul e silencia a regra de movimento até as 18h).
B3 Heartbeat a cada 5 min. O DHT22 manda a temperatura do quarto. O sensor de tensão (potenciômetro no Wokwi) mede a energia da casa.
B4 Apagão: a tensão cai, o nó passa para a bateria, grava os eventos em buffer e tenta o canal reserva. Se não conseguir, fica em silêncio. Quando a energia volta, reenvia tudo com o horário original.

C. APP DA ACS (Expo, alto contraste, fonte grande, cache SQLite)
C0 Entrada: "Bom dia, Edileuza". Matrícula simulada + PIN. Em dia normal mostra "Hoje não é dia crítico. Nada para checar." e a lista de aparelhos com bateria fraca para a próxima visita.
C1 Dia crítico (o modelo abriu o turno às 5h): faixa laranja no topo, "HOJE: dia perigoso para idosos (noite não esfriou: 24 °C). Checagens às 10h e 15h." Contador grande: "40 acompanhados · 37 com sinal de vida · 3 precisam de você". Botão [Ver os 3].
   Na demo, a tela abre com os 40 e vai esvaziando conforme chegam os eventos do Wokwi. É o momento "uau".
C2 Radar, só com quem sobrou, ordenado por risco:
   - VERMELHO, "Seu Antônio, Rua X, 32: sem movimento desde 6h · quarto 35 °C há 2h · vizinho não confirmou". Botões [Cheguei] [Ligar vizinho].
   - CINZA, "Dona Iracema, eletrodependente: aparelho sem sinal há 40 min (possível falta de luz)". O alerta saiu na hora, não às 9h. Botões [Ligar família (opt-in)] [Cheguei] e checklist "Cadastro de cliente vital na Enel: sim".
   - CINZA, "Dona Lurdes: sem sinal do aparelho". Botão [Pedir ao vizinho].
   Rodapé: "Quem está verde não aparece. Sem sinal NUNCA vira verde." Link [Por que esta ordem?] explica a regra em linguagem simples.
   O mapa a pé é extensão opcional.
C3 Detalhe do card: linha do tempo do dia (3h12 passou no corredor; 6h05 passou; desde então nada; 12h quarto 35 °C). Nenhum diagnóstico aparece aqui, só eventos. Botões [Visitei: está bem] [Visitei: encaminhei] [Não encontrei].
C4 Mensagem ao grupo do bairro (a cena do taint): a ACS toca [Pedir ajuda no grupo] e digita "O Seu Antônio mora sozinho, toma diurético e não se mexe desde cedo". Ao tocar [Enviar], a tela fica vermelha: "Mensagem bloqueada. Ela diz que ele mora sozinho e conta um remédio. No grupo, isso pode virar mapa para ladrão e expõe a saúde dele." Sugestão do LLM: "Alguém pode bater no nº 32 da Rua X agora? Obrigada, Edileuza (UBS)." Botões [Usar sugestão] [Cancelar]. O bloqueio aparece no dashboard Risco Cidadão (canal: grupo).
C5 Fechamento: depois que todos os cards são resolvidos, aparece "Turno de radar encerrado. 40 de 40 com contato confirmado às 10h47. Detalhes apagados em 24h." Botão [Concluir].

D. DEMO WOKWI EM 4 PASSOS (com vídeo gravado como plano B)
(1) PIR acionado às 3h: o LED acende e o card da Dona Cida some do radar. (2) PIR congelado + DHT em 35 °C: depois do tempo acelerado, o card do Seu Antônio volta VERMELHO no topo. (3) Potenciômetro a zero na casa da eletrodependente: o nó para de publicar, o card fica CINZA e o alerta imediato aparece. Ao "religar", o buffer reenvia e a linha do tempo se preenche. (4) A tentativa de mensagem no grupo é bloqueada e o LLM explica.

### Think-aloud das personas

**Maria Aparecida dos Santos (Dona Cida), idosa negra, viúva, que mora sozinha numa casa de laje com telha de fibrocimento** (SUS 60, concluiu: sim, indicaria 7/10). Travou em: 1) A2, a pergunta da família: as opções ('nunca / só em falta de luz com aparelho / sem sinal o dia todo') não servem para ela. Falta a opção 'avisar minha filha se eu passar mal ou cair'. Por vergonha ela respondeu 'nunca', o contrário do que precisa. 2) O telefone da vizinha: ela não sabe de cabeça, e a vizinha não foi avisada de que vai receber ligação, então pode achar que é golpe. 3) Termos como 'CNS', 'criptografado', 'aparelho na tomada para viver' (ela achou que o ventilador contava) e 'tudo apaga em 24 horas' (achou que a luz ia apagar). 4) Botão amarelo: um toque e segurar 3 s são fáceis de confundir, e com catarata ela não diferencia o pisca verde do azul, então não sabe se funcionou. Vai esquecer de segurar quando sair para a igreja. 5) Não ficou claro se o aparelho precisa de Wi-Fi (ela não tem) nem quanto gasta de luz. Precisa mudar: 1) O aparelho tem que avisar a própria idosa em dia perigoso, com voz ou com uma cor única bem forte ('dia quente: beba água, vá para a sala da frente'). Hoje ele só vigia e não ajuda quem mora na casa. 2) Na escolha sobre a família, incluir 'avisar se eu cair ou passar mal' e explicar cada opção com um exemplo da vida dela. 3) Botão: confirmar por voz curta ('tá certo, Dona Cida') em vez de pisca verde ou azul, e trocar o 'segurar 3 s' por algo difícil de confundir. 4) No papel da geladeira, colocar o nome e a foto da ACS no lugar só do telefone da UBS, e trocar 'tudo apaga em 24 horas' por 'as informações são apagadas, a luz continua'. Dizer claramente que não precisa de Wi-Fi e quanto gasta de luz (ou que não custa nada). 5) Combinar com o vizinho na própria UBS, com uma ligação de teste, antes de ativar. 6) Revisar o intervalo 10h/15h: falta de movimento por muito tempo, com quarto acima de 33 °C, deveria alertar logo, sem esperar a próxima checagem.

> (Na UBS, com a enfermeira e a Edileuza do lado.)

A1. Primeiro deixa eu falar uma coisa: se tivesse pedido CPF ou foto, eu já levantava e ia embora. Minha amiga Nicinha perdeu novecentos reais no tal 'falso INSS' porque deu o CPF por telefone. Aqui não pediu, então tá bom. Mas quando a moça falou 'o CNS fica guardado e criptografado', eu não entendi nada. CNS é o cartãozinho do SUS? Então fala 'cartão do SUS', minha filha.
'Mora sozinha?' Moro, mas eu não gosto de falar isso alto, não. A sala tava cheia de gente esperando. Quem garante que isso não sai daqui? Respondi baixinho.
'Usa aparelho na tomada para viver?' Fiquei sem saber o que responder. Ventilador conta? Sem ventilador eu não vivo no quarto dos fundos, não, ha ha. A enfermeira teve que explicar que é aparelho de respirar. Então escreve 'aparelho de respirar ou de máquina'.
'Acamada?' Graças a Deus não.

A2. 'Quer a luz no corredor?' Quero, sim. É de noite que eu levanto para o banheiro, três, quatro vezes, por causa do remédio da pressão que faz urinar. Ano passado, em fevereiro, eu caí no banheiro... (pausa) ...bom, eu tropecei. Foi a Dalva que me achou. Uma luzinha ajuda.
'Quer que um vizinho seja avisado primeiro?' A Dalva. Mas o telefone dela eu não sei de cabeça, tá no celular e eu não sei achar. Tive que pedir para a Edileuza procurar. E tem uma coisa: a Dalva não sabe que vai receber ligação. Tem que perguntar para ela antes, senão ela vai achar que é golpe e não atende, igual eu faço.
'Quer que a família seja avisada? Nunca / só em falta de luz com aparelho / se não houver sinal o dia todo.' Aqui eu travei. 'Falta de luz com aparelho' não é comigo. 'Sem sinal o dia todo'? O dia todo? Se eu cair às onze horas, a Rosemeire só vai saber de noite? Primeiro eu falei 'nunca', porque não quero dar trabalho para ela, ela sai às cinco e meia da manhã. Depois pensei melhor. Se eu desmaiar, eu quero que ela saiba. Mas essa opção não tem. Falta uma: 'se a ACS me achar passando mal'.

A3. O papel da geladeira. A letra dá para ler de perto, de óculos. É grande, mas é muita coisa escrita. Eu li 'NÃO tem câmera nem microfone' e gostei de estar em letra grande. Mas eu só acredito porque foi a Edileuza que me disse olhando no meu olho. Se viesse pelo correio, ia para o lixo. 'Tudo apaga em 24 horas'? O que apaga? A luz? Achei que a luz ia apagar depois de um dia. 'Ligue para a UBS para tirar'? Eu nem atendo quando a UBS me liga, porque aparece número estranho. Escreve o nome da Edileuza no papel, com a foto dela, aí eu sei quem é.
Depois ela pegou o celular dela e apontou para o aparelhinho, 'vinculou'. Isso eu deixo para ela.
Perguntei duas coisas que ninguém tinha falado: isso gasta luz? A minha conta já vem alta por causa do ventilador. E precisa de internet? Eu não tenho Wi-Fi, uso o da Dalva quando ela me passa a senha. Se precisar da internet da Dalva, não vai funcionar.

(Em casa.)

B1. De madrugada, a luz acendeu quando eu passei. Isso é bom de verdade. Mas eu não sei de nada que vai pela internet, eu só vejo a luz. Tudo bem por mim.

B2. O botão amarelo. Um toque é 'estou bem', segurar é 'vou sair'. Já na primeira semana eu vou confundir. E a luzinha piscando verde ou azul? Com a minha catarata, verde e azul é tudo igual. Não sei se deu certo ou não. Fico apertando de novo. Quinta, quando a irmã da igreja passa e a gente sai para o culto, eu vou esquecer de segurar o botão. Aí a Edileuza vai bater na minha porta e eu não estou lá. Que vergonha. Melhor seria duas cores bem diferentes, ou o aparelho falar 'tá certo, dona Cida'.

B3/B4. A luz aqui cai em todo temporal. Se cai e o aparelho fica quieto, a Edileuza acha que eu estou mal? Eu entendi que fica 'cinza' e não 'verde', isso é certo. Mas e se cair a luz da rua inteira? Ela vai visitar cinquenta casas?

O que me incomodou mais: o aparelho não me fala nada. Em dia de calor eu queria saber que o dia é perigoso. Queria que a luz ficasse de outra cor e falasse 'bebe água, vai para a sala da frente'. Ele fica só contando para a UBS e eu não sei de nada. O SMS da Defesa Civil eu já não entendo, 'estado de atenção, NC3', e isso aqui também não me avisa.

C. A Edileuza me mostrou o celular dela. Letra grande, isso é bom para ela também. O que eu gostei mesmo: ela tentou mandar no grupo do bairro que o Seu Antônio mora sozinho e toma remédio, e o celular não deixou. Isso eu aprovo. No grupo da rua tem gente que eu nem sei quem é. Falar que velho mora sozinho é chamar ladrão. Eu tenho medo de abrir o portão justamente por isso. Agora, 'checagem às 10h e 15h'. Se eu cair às onze, fico até as três no chão? Da outra vez foram três horas e já foi muito.

**Edileuza Pereira Lima, agente comunitária de saúde da UBS e liderança comunitária que administra o grupo de WhatsApp do bairro** (SUS 68, concluiu: sim, indicaria 7/10). Travou em: 1) No cadastro A1: não sei se tenho que digitar de novo o que já está no e-SUS. Com 41 idosos, isso é trabalho dobrado sem hora reconhecida. 2) No papel da geladeira, "Só a sua ACS vê" me deixou insegura: e nas minhas férias, na folga ou na visita sem sinal? Fica parecendo que a responsabilidade é só minha. 3) Na mensagem ao grupo (C4) não entendi como a mensagem chega no WhatsApp. Se for copiar e colar, o bloqueio pode ser burlado. Também não sei se o meu nome aparece no dashboard como "erro". 4) Em C2 não sei o que acontece quando falta luz na rua toda e os 40 ficam cinza ao mesmo tempo. 5) [Ligar vizinho] usa meu celular e meu plano pessoal? 6) "Não encontrei" é ambíguo. 7) No C5, apagar tudo em 24h some também com a prova de que eu fiz a visita. Precisa mudar: 1) Puxar do e-SUS os dados que já existem, ou pelo menos ter cadastro rápido de 2 minutos, e a UBS reconhecer formalmente esse tempo (sem virar nova meta de produção). 2) Ter um substituto e plantão definidos: quando eu estou de folga ou sem sinal, o alerta vermelho vai também para a enfermeira ou outra ACS. Tirar "Só a sua ACS vê" do papel e colocar "Só a equipe da sua UBS vê". 3) Agrupar a falta de luz por rua ("Rua X: 12 casas sem sinal") com o botão da Enel e o número de protocolo, em vez de 40 cards cinza. 4) Resolver o C4 de verdade: integrar com o WhatsApp (compartilhar direto já com o texto seguro) e deixar claro que o dashboard mostra o canal, não a ACS que errou. 5) Separar o que apaga do que fica: apagar os eventos da casa em 24h, mas manter o registro "ACS visitou às 10h12, estava bem" para me proteger. 6) Botões de desfecho melhores: "Estava fora/na casa da família", "Não abriu, chamei SAMU", "Encaminhei para UBS". 7) Ligações pelo app sem custo no meu plano (número da UBS ou ramal virtual), ou celular da OS. 8) Trocar "sinal de vida" por "deram sinal hoje". 9) Botão "vou sair" com um toque longo mais fácil, ou o vizinho poder marcar, porque idoso esquece de segurar 3 segundos e vai gerar alarme falso toda semana. 10) Dizer no papel quem troca a bateria e quanto a luz gasta na conta.

> Tá, deixa eu ver isso aqui. Começa na UBS, A1, "Novo acompanhamento". Nome, microárea, mora sozinho, usa aparelho na tomada pra viver, acamado. Gostei de não pedir CPF nem foto. Semana passada mesmo um moço da OS pediu foto do RG de todo mundo pra um projeto de horta e metade dos idosos fugiu. Mas olha: eu já tenho tudo isso no e-SUS Território. Vou ter que digitar de novo? Se eu cadastro os 41 idosos aqui um por um, de novo, isso é uma tarde inteira minha. Quem paga essa tarde? Clico em [Continuar].

A2, as perguntas lidas em voz alta. Isso eu gostei, é do jeito que eu falo com eles. "Quer a luz no corredor?", a Dona Cida vai dizer "quanto custa a luz?". Ela vai perguntar da conta de energia, pode escrever. "Quer que um vizinho seja avisado primeiro?". Aqui tá certo, é o meu caderno. Eu já tenho o telefone de um vizinho de cada um dos 9 que moram sozinhos. Só que vizinho muda, briga, viaja. Em março a vizinha do Seu Zé mudou pra Guaianases e eu só fiquei sabendo quando liguei e não atendeu. Quem atualiza esse telefone? Se for eu, de novo é mais coisa. A família, "nunca / só em falta de luz / sem sinal o dia todo": dá pra entender. [Gerar papel da geladeira].

A3, o papel. Letra 18 tá bom. "NÃO tem câmera nem microfone", ótimo, isso vai ser a primeira pergunta, pode ter certeza. Agora "Só a sua ACS vê": peraí. Quer dizer que se der ruim, a responsável sou eu? Eu tô de férias em janeiro, quem vê? Eu tô na visita sem sinal, quem vê? Esse papel me coloca como a única pessoa olhando 40 casas. Isso me dá medo. E "(11) XXXX-XXXX" da UBS: o telefone da UBS ninguém atende, gente. Coloca o meu WhatsApp não, mas coloca alguma coisa que atende. [Vincular aparelho], ler o QR: quem lê é a enfermeira? Funciona no meu celular? Se for no meu Motorola pessoal, de novo trabalho no meu celular.

B, o aparelho. Luz que acende quando passa, isso é bom de verdade, idoso cai de madrugada indo no banheiro. Já tive duas quedas na microárea esse ano assim. Botão amarelo: um toque "estou bem", segurar 3 segundos "vou sair". Olha, a Dona Lurdes não vai lembrar de segurar 3 segundos. Ela vai apertar uma vez e sair pra feira. Aí às 10h ela aparece como sem movimento e eu corro pra casa dela e ela tá no Assaí. Isso vai acontecer toda semana. E tem idoso que mora com neto que passa no corredor, aí o sensor acha que tá tudo bem e a avó tá caída no quarto. O sensor fica onde? Só no corredor?

Apagão, B4: guarda na bateria e manda depois. Tá, mas em dezembro passado o que eu precisava era saber NA HORA. Mandar depois com horário certinho serve pra relatório, não serve pra Dona Iracema. Ah, mas no C2 diz que o cinza já aparece na hora. Ok, então entendi: o silêncio é o alarme. Isso é esperto.

C0, entrada. Matrícula e PIN. Por favor, PIN que não expira a cada 30 dias igual o do e-SUS. "Hoje não é dia crítico. Nada para checar." Gostei MUITO disso. Um sistema que me diz que hoje eu não preciso fazer nada, nunca vi. E a lista de bateria fraca pra próxima visita: tá, mas aí eu viro técnica de bateria? Quem troca a pilha? Eu não vou carregar pilha na bolsa sem a OS me dar.

C1, dia crítico. Faixa laranja, "noite não esfriou: 24 °C". Esse é o dado que eu nunca tenho, eu só sei que tá quente quando eu tô suando na rua. "40 acompanhados, 37 com sinal de vida, 3 precisam de você". Isso aqui em 1 minuto eu entendi. Clico em [Ver os 3]. Agora, "sinal de vida" é feio, pelo amor. Se a filha de alguém ver por cima do meu ombro "sem sinal de vida" ela desmaia. Troca por "deram sinal hoje".

C2, radar. Vermelho, Seu Antônio, sem movimento desde 6h, quarto 35 °C, vizinho não confirmou. Ok, eu vou lá primeiro. Clico [Ligar vizinho] enquanto ando. Liga do meu celular? Do meu plano de 60 reais? Isso tem que ser pensado. Cinza, Dona Iracema eletrodependente, sem sinal há 40 min. Tem checklist "cliente vital na Enel: sim". Isso vale ouro, porque eu passei a noite discutindo com a Enel se ela era cadastrada. Mas o app tinha que me dar o protocolo e o número da Enel ali, não só "sim". Dona Lurdes cinza, [Pedir ao vizinho]. "Sem sinal NUNCA vira verde": concordo, isso é certo. [Por que esta ordem?] eu não ia clicar no dia, mas ia clicar na primeira semana pra confiar.

Mas pera: são 3 vermelhos/cinzas em 40. Na vida real, se faltar luz na rua inteira, os 40 ficam cinza de uma vez. Aí o app vira o quê? 40 cards cinza e eu sozinha? Tinha que agrupar "falta de luz na Rua X, 12 casas" e me deixar ligar pra Enel uma vez só.

C3, detalhe. Linha do tempo, 3h12 passou, 6h05 passou, depois nada, 12h quarto 35 °C. Sem diagnóstico, só evento. Isso me deixa tranquila com a LGPD. [Visitei: está bem] [Visitei: encaminhei] [Não encontrei]. Falta um "Não abriu a porta, chamei o SAMU" e falta "estava na casa da filha". "Não encontrei" é muito vago: não encontrei a casa? Não encontrei a pessoa?

C4, mensagem no grupo. Eu ia digitar exatamente isso que tá aí, confesso. Eu já fiz isso. Em fevereiro eu escrevi no grupo "alguém passa na Dona Neusa que ela tá sozinha e tomou o remédio da pressão e não atende" e depois uma vizinha me chamou de lado e disse que tinha gente estranha perguntando da casa. Então o bloqueio com tela vermelha: tem razão. A sugestão "Alguém pode bater no nº 32 da Rua X agora? Obrigada, Edileuza (UBS)" é boa. Eu clicaria [Usar sugestão]. Mas se mandar pelo app e não pelo meu WhatsApp, como chega no grupo? O grupo é no WhatsApp, o app não tá dentro dele. Se for copiar e colar, eu posso colar o que eu quiser e o bloqueio não serviu de nada. E "o bloqueio aparece no dashboard Risco Cidadão": aparece com meu nome? Vira advertência pra mim? Se vira meta de "erro da ACS", eu odeio isso.

C5, "Turno encerrado. 40 de 40 às 10h47. Apagado em 24h." Gostei de saber que acabou. Mas 24 horas apaga? Se a família processar porque a vó morreu, como eu provo que eu fui lá às 10h? Eu quero o registro de que EU visitei guardado, pra me proteger. Apaga o dado da idosa, não apaga a minha prova.

Tarefa: consegui. Em 1 minuto eu sei que é o Seu Antônio primeiro. Isso funciona.

**Cléber Andrade dos Reis, motorista de aplicativo que já desistiu do SP156 e de apps 'cidadãos' e desconfia de tecnologia do governo** (SUS 58, concluiu: sim, indicaria 4/10). Travou em: 1) B4: "se não conseguir, fica em silêncio" no apagão. Travei achando que o sistema some justo na hora crítica. Só entendi que o silêncio vira alerta cinza quando li o rodapé do C2, então a explicação tá longe demais. 2) A2: não diz por qual canal a família é avisada (SMS, WhatsApp, ligação), e "se não houver sinal o dia todo" é tempo demais pra quem usa concentrador. 3) C2 num apagão de bairro inteiro: com 40 cards cinza ao mesmo tempo, não ficou claro como a ordem por risco funciona, nem se eletrodependente fica sempre no topo. 4) C3/C5: "Visitei: está bem" e "40 de 40 confirmado" podem ser clicados sem visita, igual protocolo "finalizado". 5) A3: "para tirar, ligue para a UBS". Não diz o que acontece se eu tirar da tomada. 6) B2: segurar 3 segundos não é coisa que a sogra de 80 anos vai lembrar. Precisa mudar: 1) Família avisada na hora, por WhatsApp ou ligação, assim que o eletrodependente perde sinal (opção "imediato", não "o dia todo"), e quando a ACS chega também ("ACS esteve aí às 10h47"). Isso é prova e combate o "finalizado" falso. 2) Deixar explícito na tela e no papel da geladeira que silêncio no apagão vira alerta, e que tirar da tomada também vira alerta e visita. 3) Regra clara pra apagão em massa: eletrodependente sempre no topo, e o sistema aciona a Enel/Defesa Civil junto, não só a ACS. 4) Transparência no cadastro: quem vê, onde fica, quem me avisa se vazar, e uma forma de sair que não dependa de ligar pra UBS. 5) Botão "estou bem" de um toque só. Trocar o "segurar 3s" por algo que idosa consiga fazer, ou tirar.

> Tá, deixa eu ver. Já aviso que app de estudante eu desinstalo em 30 segundos se pedir CPF.

A1, Novo acompanhamento. Opa, não pede CPF nem foto. Primeira vez que vejo isso, ponto pra vocês. Aí vem "Mora sozinho? sim/não". Pera aí. É justamente isso que eu tenho medo que vaze. Uma lista de idosa que mora sozinha, com microárea, é cardápio pra ladrão. Vocês falam que o CNS fica "criptografado no SOA". Não sei o que é SOA. Quem abre esse banco? A enfermeira? O estagiário da prefeitura? Em 2024 abriram conta no meu CPF e ninguém me avisou nada. Se esse banco vazar, quem me liga? Clicaria em Continuar, mas desconfiado.

"Usa aparelho na tomada pra viver? Qual?" Concentrador de oxigênio, igual a Dona Iracema. Aqui pelo menos tá perguntando a coisa certa.

A2. "Quer que um vizinho seja avisado primeiro?" Nome e telefone do vizinho... e o vizinho autorizou? Eu tô dando o telefone do Seu Zé pra prefeitura sem ele saber. Depois "família avisada: nunca / só em falta de luz com aparelho / se não houver sinal o dia todo". Eu marcaria a do meio na hora, mas quero saber COMO chega. Eu tô dirigindo 12 horas, só vejo WhatsApp e notificação do app de corrida. SMS eu nem abro, só vem golpe. E "o dia todo"? O dia todo é tempo demais. Em dezembro o concentrador parou à noite. Se eu fico sabendo de manhã, já era.

A3, papel da geladeira. Letra grande, bom. "NÃO tem câmera nem microfone". A sogra vai olhar pra essa caixinha e achar que tem câmera sim, ela acha que a TV escuta. Mas o papel ajuda, vou ler pra ela. "Tudo apaga em 24 horas". Quem garante? Protocolo meu também dizia "finalizado". "Para tirar, ligue para a UBS". Ligar pra UBS? A UBS não atende telefone. Eu quero poder tirar da tomada e pronto. E se eu tirar da tomada, ela vira "sem sinal" e a ACS aparece? Acho que é isso, mas o papel não fala.

B, o aparelho. Luz no corredor de madrugada é boa, de verdade. A sogra já caiu indo no banheiro. Botão amarelo, um toque "estou bem", segurar 3 segundos "vou sair". A sogra não vai lembrar de segurar 3 segundos. Vai apertar uma vez quando sair e pronto. Ou aí a ACS bate na porta à toa, ou ela fica marcada como saiu e não saiu.

B4, apagão. Aqui eu parei. "Passa pra bateria e tenta o canal reserva. Se não conseguir, fica em silêncio." No apagão de dezembro o roteador morreu na hora. Canal reserva é o quê, um chip? Quem paga? Ficar em silêncio é justamente na hora que a gente mais precisa. Aí li lá embaixo "sem sinal NUNCA vira verde" e o alerta cinza sai na hora. Tá, então o silêncio já é o aviso. Isso eu entendi depois, mas tive que juntar duas telas pra entender.

C, o app da ACS. Não é pra mim, é pra Edileuza. Bom dia, matrícula, PIN, beleza. C1, faixa laranja, "40 acompanhados, 37 com sinal, 3 precisam de você". Isso aí é claro, até eu entendi em 5 segundos. Botão Ver os 3.

C2. Engraçado, tem uma "Dona Iracema, eletrodependente" no exemplo, igual a minha sogra. "Aparelho sem sinal há 40 min, possível falta de luz". Botão "Ligar família". E o checklist "Cadastro de cliente vital na Enel: sim". Rapaz, a minha sogra É cadastrada na Enel e a luz ficou 52 horas fora. Esse "sim" não serve pra nada se a Enel não liga. A ACS chegar lá faz o quê? Leva gerador? Leva a sogra pra UPA? O app ajuda a achar, mas não resolve o problema da luz.

E outra: num calor desses tá 3 cinza. Num vendaval é o bairro inteiro sem luz. Vão ser 40 cinza de uma vez. Aí "ordenado por risco" ordena como, se ninguém tem sinal? Eu cliquei em "Por que esta ordem?" e queria ver que eletrodependente vem primeiro quando tá tudo apagado. Se não for assim, falha justamente no dia que importa.

C3, linha do tempo. "3h12 passou no corredor, 6h05 passou, desde então nada". Isso é bom, é fato, não tem achismo. Botões "Visitei: está bem". E se a ACS apertar sem ir? Igual o cara da poda que fechou meus três pedidos sem aparecer. Eu quero receber "a ACS esteve aí às 10h47". Aí eu acredito.

C4, mensagem no grupo bloqueada. Achei legal mesmo, porque grupo de bairro vaza tudo, eu sei, tô em quatro. Mas ela pode copiar e mandar pelo WhatsApp dela fora do app, né? Então o bloqueio só pega quem usa o app direitinho. E esse "LLM" que reescreve a mensagem: o texto vai pra qual empresa? Pros Estados Unidos?

C5. "40 de 40 com contato confirmado às 10h47". Essa frase me deu arrepio. Parece o "finalizado" do SP156. Bonito no número, mas quem confere?

Resumindo: dá pra entender quem visitar primeiro, isso funciona no papel. Mas eu vi isso como genro de eletrodependente, e o meu pior dia (apagão de 52h com todo mundo sem sinal) é justamente onde o protótipo fica mais vago.

### Banca FIAP simulada (nota 7)

**Memorável.** A frase "o silêncio do aparelho nunca vira verde" e a tela que abre com 40 idosos e vai esvaziando até sobrarem 3. É uma lista por exclusão, acionada pelo dia crítico e entregue à ACS, não à família. A expressão "mapa pra ladrão" como motivo do bloqueio de PII também fica na cabeça. Das centenas de GS de calor e idoso que já vi, é a primeira que inverte o botão de pânico.

**Já visto?** O tema é muito batido: todo semestre aparecem botão de pânico, pulseira de queda, sensor de presença para idoso e alerta de onda de calor. O desenho é menos comum: radar só em dia crítico, decidido por modelo com dados públicos, operado pela ACS da ESF, com o vizinho como primeiro nível e o estado cinza separado do verde. O benchmark (Canary Care, Howz, registre canicule, i-Pot) está acima da média e mostra que a equipe estudou. Eu diria que 70% da ideia já vi, mas a combinação e o encaixe no SUS são originais no contexto da FIAP.

**Perguntas difíceis.**
- Vocês falaram com alguma ACS, gerente de UBS ou idosa de verdade? O briefing pede 'uma pessoa real que convive com o problema'. Dona Cida, Edileuza e Cléber são personas geradas, e o próprio documento admite que o re-teste foi escrito por vocês. De onde saiu o 'liga para 9, deixa 30 de fora'? E a meta de 9 para 2 ou 3?
- Em São Paulo, a internação cardiovascular de idosos tem pico no INVERNO, não no calor. Se o XGBoost aprender que frio gera excesso de I00-I99, o modelo contradiz a tese do pitch. Vocês já olharam a correlação? Até agora só conferiram os volumes. O que fazem se o sinal de calor for fraco ou se a baseline de mínima noturna empatar com o XGBoost?
- Com o P75 como limiar, 25% dos dias viram 'dia de excesso', cerca de 90 dias por ano. Isso ainda é 'dia crítico' ou o radar fica ligado um a cada quatro dias e vira vigilância contínua, que é justamente o que vocês dizem não ser?
- Usar o INMET observado como proxy da previsão: às 5h da manhã vocês não têm a máxima do dia nem a mínima da noite seguinte. Qual previsão real alimentaria o modelo em produção, e quanto o recall cai com o erro de previsão?
- O nó fica no corredor, mas o DHT22 mede o 'quarto dos fundos', e a Dona Cida passa a tarde nesse quarto, fora do alcance do PIR. Às 15h ela vai aparecer VERMELHA todo dia crítico? É um aparelho ou dois? Isso muda o custo e o enunciado da regra.
- Vocês tiraram o SMS porque 'golpista copia'. Por qual canal o vizinho de confiança recebe 'Pode bater na casa da Dona Cida?'? Se for SMS ou WhatsApp de um número desconhecido, a mesma objeção volta.
- A regra 'ida noturna ao banheiro sem volta em 20 min' convive com uma espera de 1 h pelo vizinho antes de avisar a ACS. Numa queda às 3h, quanto tempo leva até alguém chegar? O radar só existe em dia crítico. Então a queda numa noite comum não é detectada?
- Isso é taint analysis ou só classificação de conteúdo? Em taint é preciso marcar uma fonte (dado vindo do SOA) e um sumidouro (o canal do grupo). Um texto livre digitado pela ACS não carrega rótulo de origem. Mostrem no código onde o rótulo nasce e onde ele é checado.
- A 'versão segura' sugerida pelo LLM ('Alguém pode bater no nº 32 da Rua X agora?') publica num grupo de bairro o endereço de alguém que talvez esteja caído e sozinho. Isso não é exatamente o mapa pra ladrão com menos palavras?
- Qual é a base legal LGPD para tratar dado de saúde de idoso? Consentimento (art. 11, I) ou tutela da saúde (art. 11, II, f)? Como vocês tratam o consentimento de idosa com declínio cognitivo?
- Por que dois backends (C# e SOA) com domínios sobrepostos, além de bridge MQTT, Expo, ML, Wokwi e uma VM Linux com systemd, em 14 dias? Quem da equipe faz o quê, e qual é o corte mínimo se no dia 12 algo não estiver integrado?
- Na demo, os 40 idosos que vão sumindo da tela são 40 nós Wokwi ou 1 nó Wokwi mais 39 mocks? Digam isso no vídeo. A banca percebe, e a honestidade conta pontos.
- Por que a ACS vai abrir mais um app se ela vive no WhatsApp e no e-SUS Território? A visita gerada pelo radar conta como produção dela? Sem isso, a gerente não adota.

**Veredito.** É um dos pitches mais maduros que vi nesta área, e a equipe fez a lição de casa onde a maioria não faz. Conferiram de verdade o SIH e o INMET, com achados concretos: MUNIC_RES de 6 dígitos, arquivo por competência, T67 quase zero. Cortaram escopo, criaram o estado cinza com uma regra que não deixa o silêncio virar verde, e deram ao idoso um benefício próprio, a luz contra queda, que justifica aceitar o aparelho. O benchmark é sério (canicule, Canary Care, Howz, i-Pot), e a privacidade aparece como desenho do produto, não como anexo. A integração entre disciplinas é real: o DiaCritico do ML abre o turno, o Wokwi alimenta o motor C#, e o SOA guarda o vínculo e aplica a guarda de PII.

Por que não passa de 7 hoje:

1) Validação. Toda a validação com usuário é simulada, e o briefing exige uma pessoa real. A métrica de impacto inteira ('de 9 para 2 ou 3') é hipótese sem uma única conversa com ACS. Uma hora com uma ACS de verdade vale mais que todas as personas juntas.

2) Risco de ML. A pergunta científica central, se calor gera excesso de internação 60+ em SP, não foi testada. A sazonalidade cardiovascular paulistana puxa para o frio, e o P75 transforma um quarto do ano em 'dia crítico'. Se a baseline empatar, a tese do 'modelo que abre o turno' perde força. Precisam mostrar isso com transparência.

3) Inconsistências de hardware e regra. O PIR está no corredor e o DHT no quarto. O vizinho é avisado por um canal que não foi definido e talvez reabra a objeção do golpe. Há uma hora de espera numa regra de queda de 20 min. Fica a dúvida se queda em noite comum é coberta.

4) Taint. O que está descrito é classificação de conteúdo. Taint de verdade exige fonte e sumidouro rotulados no fluxo, e a sugestão do LLM ainda expõe endereço num grupo.

5) Escopo e prazo. Ainda é muito para 14 dias: dois backends, bridge, Expo, ML, Wokwi e a VM Linux, e OS e Testing nem aparecem no pitch. O 'uau' dos 40 sumindo depende de mocks, e isso precisa ser dito.

Recomendação de banca: até o dia 10, entrevistar uma ACS real, rodar a correlação calor × internação com a baseline (e reportar mesmo que o resultado seja fraco) e resolver a posição dos sensores. Com isso feito e uma demo honesta funcionando ponta a ponta, o projeto sobe para 8,5 ou 9 e entra forte na disputa do prêmio. Sem isso, é um conceito excelente que corre o risco de ficar pela metade, e a própria FIAP avisa que 'uma solução simples que funciona vale mais'.

## Vigília: o poste que testemunha contra o "finalizado"

**Conceito refinado.** VIGÍLIA, versão enxuta: só iluminação pública, um verificador e uma regra.

O QUE MUDOU EM RELAÇÃO AO CONCEITO ORIGINAL (fixes dos 4 juízes + objeções das 3 personas)
1. Um só serviço: poste ou luminária apagada. Saem a árvore, o MPU6050, a poda e o "Pregão" como módulo separado. Fica só a regra de publicar também os acertos do órgão. Também saem o blur de rosto e placa, o áudio próprio (usamos o leitor de tela do sistema), o roteamento automático por IA e a redação do protocolo por LLM. Para Cléber, a resposta fica sendo: "a caixinha só vê luz, e é isso que prometemos".
2. Correção de fato, achada ao abrir o dataset: pedido de lâmpada apagada NÃO aparece nos dados abertos do SP156. No 3º tri de 2026 há 0 linhas de iluminação em 484.232. Em SP, o reparo é pedido no Ligue Ilume / SP Regula (0800 779 0156), e não achamos painel aberto com esses chamados. Isso vira argumento do pitch: "a iluminação é o serviço mais cego da cidade, porque nem o 'finalizado' dela é público; por isso a rua mede". A frase "a prefeitura não publica desde 2022" sai. O dossiê vai para a SP Regula, agência que fiscaliza o contrato da PPP de iluminação, e não só para a Ouvidoria. Assim ele tem destino institucional com poder de cobrança contratual.
3. Gatilho honesto: não existe API de status. O cidadão (ou o comércio) registra no app "o órgão disse que finalizou" e informa o número do protocolo. Na demo isso é dito com todas as letras.
4. Física do sensor (Disrupção, Desejabilidade, Pré-mortem): o LDR fica dentro de um tubo colimador (tubo de caneta ou PVC preto de 10 cm) apontado para a luminária. Um segundo LDR de referência mede o ambiente. O limiar é calibrado na instalação: o comerciante aperta "está aceso agora" e depois "está apagado". Picos de menos de 2 minutos (farol de carro) são ignorados. A noite conta das 19h às 5h e é "escura" se pelo menos 80% das leituras ficam abaixo do limiar. Antifraude e saúde do sensor: um heartbeat a cada 15 min mostra que o sensor está vivo. Durante o dia o LDR precisa ver claridade, senão o sensor está tampado e a noite não conta. Sem energia, uma bateria 18650 segura a gravação no microSD. Com isso, "sensor parado" nunca vira "rua escura", que era a dúvida de Larissa, e o sensor funciona no apagão, que era o pedido de Cléber.
5. Sem botão às 23h35 (Larissa): ela marca uma vez o trajeto a pé. No dia seguinte, no horário que ela escolheu, o app pergunta "ontem o poste da R. X estava apagado?" e a resposta é um toque. A localização é usada só na hora do toque, nunca em segundo plano.
6. Quem não tem app coassina sem instalar nada (Cléber, Dona Marlene, Seu Edson): botão físico no display da padaria, a 90 cm de altura para cadeirante alcançar, ou QR code que abre uma página web leve, sem login e sem CPF.
7. Privacidade: nenhum nome, foto ou horário individual aparece em público. O placar mostra só contagens. A coassinatura é um token anônimo por aparelho. Uma tela "o que guardamos" diz que não pedimos CPF, que a localização só é lida no toque e que os dados são apagados em 90 dias. Campo livre com CPF, número da casa ou saúde ("minha mãe é diabética") é limpo antes de salvar, e um LLM explica ao cidadão o que foi retirado.
8. Fronteira entre os backends (Viabilidade): o FastAPI (Python) cuida da ingestão MQTT, das leituras no Postgres, do cálculo de "noite escura" e do endpoint do modelo de ML. A Web API .NET é a única dona da máquina de estados da Contraprova (Aberto, FinalizadoPeloOrgao, EmVerificacao, ReabertoPelaRua, ConfirmadoResolvido, RespostaDoOrgao) e das Evidências (Sensor, Vizinho, Orgao). Ela usa EF Core + LINQ no placar e chama o FastAPI de forma assíncrona para perguntar se a noite N foi escura.
9. A regra virou a contradição resolvida (Pré-mortem): REABERTO PELA RUA = órgão finalizou + 3 noites escuras com sensor saudável + 2 confirmações humanas distintas. Sensor sozinho não reabre. Humanos sem sensor geram só "contestado por vizinhos", um estado mais fraco. Se a luz acender, o sistema publica "RESOLVIDO, CONFIRMADO em N dias" e dá o crédito ao órgão. Se o órgão disser que o poste é da Enel ou de particular, isso aparece como estado próprio, com o caminho certo.
10. ML coerente com a tese: o rótulo deixa de ser "demorou mais de 30 dias" (que mede o carimbo) e passa a ser "o finalizado voltou?". A pergunta é se, depois do "finalizado" no SP156, surgiu novo pedido do mesmo serviço no mesmo endereço em até 30 dias. Esse é o proxy público de "finalizado falso". Medi no 3º tri de 2026: 5,5% em árvore, 5,7% em guias/calçada, 7,0% em tapa-buraco e 9,0% em bueiro, como limite inferior. Assim o dado público prova a tese em vez de contradizê-la. Achado secundário verificado: entre os pedidos de poda já finalizados, 43% passaram de 30 dias em Itaquera e 2% no Jabaquara. Isso sustenta a frase "sua espera não é azar, é CEP", desde que se avise que os dados são censurados. A iluminação, cega nos dados, é justamente onde a Vigília entra.
11. Persona: sai Seu Edson, cuja dor era calçada. Entram Dona Cida (72 anos, volta da igreja às 21h, sem smartphone, coassina pelo botão da padaria) e Dona Marlene (dona da padaria, hospeda o sensor). A extensão para calçada com verificação só humana, sugerida por Seu Edson, fica como roadmap.
12. Indicador de impacto explícito: (a) mediana de noites entre o "finalizado" do órgão e a luz realmente acesa, medida pela Vigília; (b) % de "finalizados" contestados que viram conserto em até 15 dias depois do dossiê. A linha de base pública é a taxa de reincidência pós-finalizado por subprefeitura, do modelo.
13. Demo robusta: uma noite real gravada com LDR físico, ou série de luz do celular, carregada no SD e reproduzida no Wokwi, sem depender de slider ao vivo. Desligar o Wi-Fi e ver os dados chegarem inteiros continua na demo.

RODADA SIMULADA DE VALIDAÇÃO COM PERSONAS (pedido do usuário: agent team de personas). É simulação e não substitui as entrevistas reais. Larissa aprovou trajeto + um toque no almoço; o veto dela continua sendo localização "sempre". Cléber aceita o QR e o botão sem documento e exige que o placar compare com a cidade. Como não há dado de iluminação, o placar compara "noites medidas pela Vigília" com a reincidência do SP156 da subprefeitura. Dona Marlene (simulada) topa se o display tiver tom neutro ("VERIFICAÇÃO DA RUA", e não "PREFEITURA MENTE"), se mostrar também os acertos e se o consumo for perto de zero (USB + 18650). Ela teme atrito com fiscal e vandalismo. Por isso o sensor fica do lado de dentro do vidro e só o display aparece. Dona Cida entende "apagado há 34 noites" e não entende porcentagem. Por isso a previsão em porcentagem sai do display e fica só no app, como ícone de "chance alta/média/baixa".

ODS: 11 (zeladoria e segurança no espaço público), 10 (desigualdade de atendimento entre subprefeituras, medida no SP156), 9 (infraestrutura e inovação cidadã de baixo custo).

Mapa das disciplinas:
- SOA: FastAPI com domínio Leitura/Noite/Sensor, Postgres, Alembic, Swagger e testes da regra de noite escura.
- C#: State pattern da Contraprova.
- Cyber: PII em texto livre e EXIF, taint para feed, log e prompt, LLM explicando, dashboard "Risco Cidadão".
- ML: XGBoost de reincidência no SP156 + INMET.
- Mobile: Expo com trajeto, pergunta de um toque, fila SQLite offline e alto contraste.
- Linux: Nginx servindo o painel "Placar do Finalizado" e a página do QR, Mosquitto, systemd, auditoria de mudança de estado, teste de queda do broker.
- ESP32: Vigília com LDR colimado, LDR de referência, RTC, SD, OLED, botão e LEDs de saúde.
- Testing: "só sensor não reabre", "leitura de dia não conta", "sensor tampado invalida a noite", "CPF no texto nunca chega ao feed".

### Lean Canvas

- **problem.** 1) O órgão, ou a concessionária da PPP via Ligue Ilume/SP Regula, marca 'finalizado' e o poste continua apagado. O cidadão não tem como contestar com prova. 2) A iluminação é invisível nos dados abertos: 0 pedidos de lâmpada na base do SP156 (3º tri de 2026), então ninguém mede o tempo real de conserto. 3) O cidadão não sabe quem cuida (Prefeitura/SP Regula, Enel ou particular) nem quantos vizinhos já reclamaram. Por isso não registra. Alternativas atuais: ligar de novo no 0800, reclamar no grupo de WhatsApp, postar vídeo no TikTok, chamar vereador.
- **customerSegments.** Usuários: quem anda a pé à noite em bairro periférico de SP, como estudante noturna, trabalhador de turno e idosos voltando da igreja. Hospedeiros: comércio de esquina aberto à noite (padaria, farmácia, bar) e associações de moradores. Early adopters: grupos de bairro no WhatsApp já mobilizados (ex.: grupo do ônibus 6091 da Larissa, motoristas de app da Zona Leste) e um comerciante com um poste apagado na própria porta. Interessados institucionais: SP Regula (fiscal do contrato), Ouvidoria, vereadores da região, imprensa local.
- **uvp.** O poste testemunha por você: se disseram que consertaram e a luz não voltou, a rua prova em 3 noites, sem você precisar saber de quem é o poste, instalar app ou mostrar seu nome.
- **solution.** 1) Vigília: ESP32 atrás do vidro do comércio, com LDR colimado apontado para a luminária, LDR de referência, RTC e microSD. Grava offline das 19h às 5h, tem heartbeat e detecta se foi tampado. 2) Confirmação humana de um toque: no app, pela pergunta do dia seguinte sobre o trajeto marcado; no balcão, pelo botão do display ou pelo QR sem login. 3) Regra pública 'finalizado + 3 noites escuras + 2 humanos = REABERTO PELA RUA', dossiê com cadeia de hash para a SP Regula e a Ouvidoria e publicação também dos acertos ('RESOLVIDO, CONFIRMADO').
- **channels.** Corpo a corpo no território: a padaria parceira e a associação de moradores. Grupos de WhatsApp do bairro e do ônibus, com link da página leve, sem instalar nada. QR code no placar da vitrine. Criadores locais de TikTok, como o Cléber, quando um caso reaberto vira conserto. Vereadores e imprensa local recebem o dossiê anonimizado. Depois, apresentação à SP Regula como 'telegestão cidadã'.
- **revenue.** Sem cobrança do cidadão, que não pagaria nem R$ 5 segundo as personas. Fontes possíveis: comércio ou igreja patrocina a própria Vigília (~R$ 100 com display e bateria) em troca de selo 'Rua Verificada'; editais de inovação cívica e fundos GovTech; contrato via CPSI/Marco Legal das Startups com a agência reguladora ou a prefeitura, para usar a Vigília como fiscalização independente de SLA da PPP; relatórios agregados para Câmara e imprensa. No escopo da GS, a sustentabilidade é hipótese, não promessa.
- **costs.** Hardware por Vigília: ~R$ 90 a 120, corrigindo os 'R$ 40' do conceito (ESP32 ~R$ 35, LDRs e tubo ~R$ 5, RTC DS3231 ~R$ 15, módulo microSD + cartão ~R$ 25, OLED 0,96'' ~R$ 20, botão, LEDs, 18650 + TP4056 ~R$ 25). VM Linux pequena (~R$ 30 a 60/mês) com Nginx, Postgres e Mosquitto. Chamadas de LLM só para as mensagens de PII, poucos centavos por dia. Tempo da equipe: 5 pessoas x 2 semanas. Manutenção: troca de bateria e visita mensal ao comércio.
- **keyMetrics.** Impacto: mediana de noites entre o 'finalizado' do órgão e a luz realmente acesa (medida pela Vigília); % de finalizados contestados que viram conserto em até 15 dias depois do dossiê. Adoção: nº de comércios hospedando Vigília; coassinaturas por poste por semana; % de confirmações via botão/QR vs app. Confiança: % de noites invalidadas por sensor tampado ou sem heartbeat (meta < 10%); falsos 'escuro' na calibração (meta < 5%). Linha de base pública: taxa de reincidência pós-finalizado no SP156 por subprefeitura (5,5% a 9,0% no 3º tri de 2026, por serviço).
- **unfairAdvantage.** É a única proposta que verifica o 'finalizado' de forma independente, com evidência física + humana auditável (hash). Os concorrentes registram e acompanham; a Vigília confere. Somam-se a isso a relação de confiança com o comércio e a associação do próprio bairro da equipe, uma série histórica própria de noites medidas onde não existe dado público de iluminação e a regra publicada que dá crédito ao órgão quando ele acerta, o que reduz atrito institucional.

**Hipótese mais arriscada.** Comerciantes de bairro aceitam hospedar a Vigília e o placar na fachada, apontados para um poste e contestando o órgão. Os vizinhos que passam, inclusive quem não tem app, coassinam com um toque, de modo que se juntam pelo menos 2 confirmações humanas distintas por poste dentro da janela de 3 noites. Se isso falhar, a regra 'sensor + 2 humanos' nunca dispara e a solução vira só um medidor de luz. Hipótese técnica que fica logo atrás e é testada em paralelo: um LDR colimado atrás do vidro distingue a luminária-alvo de faróis, vitrines e da luz da própria loja, com erro abaixo de 5% por noite.

**Experimento.** Experimento 'Placar de Papel + Uma Noite de LDR' (2 a 3 dias, custo menor que R$ 60).

Dia 1 (tarde):
(a) A equipe escolhe um trajeto real de um colega que volta à noite e mapeia 3 a 5 postes apagados. Liga no 0800 779 0156 para abrir protocolos de verdade. Isso também mede quanto tempo leva para descobrir de quem é o poste.
(b) Visita 5 comércios abertos à noite perto desses postes e mostra um mockup em papel do display ('POSTE R. X, 120 / APAGADO HÁ N NOITES / N VIZINHOS VIRAM / [EU TAMBÉM]') e da caixinha atrás do vidro. Pede para colar por 48h um placar A4 com QR. O QR abre uma página estática sem login e sem dados pessoais, com um único botão 'eu também vi apagado hoje' que só incrementa um contador por dia. Em paralelo, há uma folha de tracinhos no balcão para quem não tem celular.

Noite 1:
(c) Spike técnico com 1 ESP32 (ou Arduino) + LDR em tubo preto atrás do vidro de uma janela da equipe, apontado para um poste. Grava a cada 1 min no Serial/SD das 19h às 5h. Um integrante anota manualmente, ou grava vídeo com timelapse do celular, os faróis e quando o poste está aceso ou apagado.

Dias 2 e 3: recolher contagens e entrevistar os comerciantes (roteiro abaixo) e 3 transeuntes que apertaram.

Critérios de sucesso (pré-definidos):
- pelo menos 2 de 5 comerciantes aceitam colar o placar e pelo menos 1 diz que hospedaria o sensor;
- pelo menos 2 confirmações distintas por poste por noite somando QR + tracinhos, e pelo menos 10 no total em 48h;
- o filtro de picos menores que 2 min + limiar classifica corretamente pelo menos 95% dos intervalos de 5 min da noite gravada contra a anotação manual;
- pelo menos 1 dos protocolos abertos recebe resposta, o que informa se e quando o 'finalizado' chega.

Sinais de pivô:
- Se nenhum comerciante aceitar: a Vigília vai para a janela de um membro da associação de moradores e o placar vira só digital, no grupo de WhatsApp.
- Se houver menos de 2 confirmações por noite: a regra passa a aceitar confirmações em até 5 noites.
- Se o LDR errar mais de 5%: entra o LDR de referência diferencial e um tubo mais longo.

Tudo vai para o README como evidência de design thinking: fotos do mockup, contagens e gráfico da noite.

**Roteiro para entrevista real.** Roteiro de 15 min. Aplicar a pelo menos 3 pessoas reais: um estudante ou trabalhador que volta a pé à noite, o dono de um comércio aberto à noite e uma pessoa idosa. Não citar a solução antes da pergunta 5. Pedir histórias, não opiniões.

1) Me conta a última vez que você passou por um poste apagado no seu caminho. Onde era, que horas, o que você sentiu e o que fez na hora (desviou, apressou, ligou para alguém)?
2) Você já reclamou de poste apagado ou de outro serviço da rua? Por qual canal (156, 0800, app, vereador, grupo de WhatsApp)? O que aconteceu depois? Alguma vez disseram que estava resolvido e não estava? Como você ficou sabendo?
3) Quando você pensa em reclamar, o que trava: não saber de quem é o poste, falta de tempo, medo de expor seus dados, ou achar que não adianta? O que você toparia dar: 30 segundos, um toque, seu telefone, seu nome?
4) [Comerciante] Se a rua da sua loja fica escura, o que isso muda para você (movimento, segurança, horário de fechar)? Você já fez algo por conta própria, como pôr refletor ou falar com a prefeitura?
5) [Mostrar o mockup de papel do display e do card 'você + 13 vizinhos; sensor viu apagado 9 de 9 noites'] O que você entende que está escrito aqui? O que faria agora? (Observar sem ajudar. Para idoso: entende 'noites'? E porcentagem?)
6) [Comerciante] Você deixaria essa caixinha atrás do seu vidro e esse placar na vitrine? O que te faria dizer não: energia, briga com fiscal, vandalismo, cliente achar que é política? E o que faria dizer sim? [Transeunte] Você apertaria o 'eu também'? E se pedisse nome, CPF ou instalar um app?
7) Se a rua provasse que o poste não foi consertado, para quem você gostaria que essa prova fosse (agência reguladora, Ouvidoria, vereador, imprensa, grupo do bairro)? O que precisaria acontecer em até 1 mês para você confiar e indicar para alguém?

Encerramento: 'Tem alguém que você acha que a gente deveria ouvir?' Registrar frases literais, contradições entre o que a pessoa diz e o que fez, e gestos de confusão diante do mockup. Não registrar nome completo nem endereço (combinado da GS: nada de dado pessoal real).

**Dataset (verificado: sim).** ABRI E BAIXEI o dataset. Portal: https://dados.prefeitura.sp.gov.br/dataset/dados-do-sp156 ('Dados do SP156 - Solicitações'), licença CC0, página atualizada em 19/10/2026.
- Arquivos: CSV trimestrais de 2020 em diante (até o 3º TRI 2026), semestrais de 2015 a 2019 e de 2012 a 2014, de outro sistema e não comparável.
- Arquivo baixado: o mais recente, arquivo-final-3-trim-26.csv (recurso f67fcc12-8da4-485f-92a3-d2af40cc771e), com 105,6 MB, UTF-8, separador vírgula e datas MM/DD/AAAA.
- Linhas: 484.232 pedidos, com abertura de 01/07/2026 a 30/09/2026 e finalização até 08/10/2026.
- Colunas (15): Data de Abertura, Data de Finalização, Meio de Comunicação, Tema, Assunto, Serviço, Status, Órgão, Logradouro, Número, CEP, Latitude, Longitude, Prefeitura Operacional, Distrito.
- Status: Finalizada 396.501; Aberto 79.926; Em Andamento 6.473; Cancelada 1.331.
- Dicionário oficial (TXT, atualizado em 10/08/2026): 'Finalizado: a solicitação foi finalizada pelo órgão competente', e o status vale na data de emissão do relatório.
- Correção: a coluna se chama 'Data de Finalização', não 'Data do Parecer'.

ACHADO CRÍTICO: não há nenhum pedido de iluminação pública ou lâmpada apagada nesse trimestre. Busquei 'ilumina' e 'lâmpada' em Tema, Assunto e Serviço e o resultado foi 0. O assunto 'Fiação e Postes' traz guias, calçada, fiação solta e 'poste em mau estado' (225), mas não lâmpada. Pela SP Regula, o reparo de iluminação é pedido pelo 0800 779 0156 (Ligue Ilume / SP Regula), e não encontrei base aberta com esses chamados.

Consequência: a pergunta original de ML (prazo de iluminação) NÃO é respondível com essa base. A pergunta foi trocada por uma que a base responde: 'dado um pedido de zeladoria finalizado (tapa-buraco, árvore/poda, guias/calçada, bueiro), qual a probabilidade de nascer um novo pedido do mesmo serviço no mesmo logradouro e número em até 30 dias depois do finalizado?'. É o proxy de 'finalizado falso'. Medições preliminares feitas por mim (só no 3º tri de 2026, intra-trimestre, portanto limite inferior):
- Tapa-buraco: 30.173 pedidos, 7,0% de reincidência.
- Árvore: 19.986 pedidos, 5,5%.
- Fiação e Postes: 13.332 pedidos, 5,7%.
- Drenagem/bueiro: 9.073 pedidos, 9,0%.
- Lat/long preenchida em 96% a 100% desses assuntos.
- Descritivo verificado: entre os pedidos de árvore finalizados, 43% passaram de 30 dias em Itaquera e 45% no Butantã, contra 2% no Jabaquara (mediana geral de 12 dias). Os pedidos ainda abertos (33%) ficam fora, então há censura e isso precisa ser dito no slide.

Plano: concatenar de 4 a 7 trimestres (2025 T1 a 2026 T3) para a janela de 30 dias atravessar a virada de trimestre. Usar split temporal (treino até 2026 T1, teste 2026 T2) para evitar vazamento. Variáveis: serviço, subprefeitura, distrito, mês, canal, dias até finalizar, nº de pedidos anteriores no endereço e chuva acumulada INMET (Mirante de Santana, A701) nos 7 dias depois da finalização. A chuva faz sentido para buraco e bueiro. O INMET não foi aberto nesta rodada e deve ser verificado no dia 1.

Modelo: XGBoost com classe rara (~6%), scale_pos_weight, grid de 12+ combinações de max_depth, n_estimators e learning_rate, StratifiedKFold no treino, PR-AUC/AUC e recall treino vs teste.

Conclusão para o cidadão: 'em tal subprefeitura, 1 em cada N finalizados volta em um mês'. Isso responde a pergunta de ML com dado real e sustenta a tese. A iluminação fica com a Vigília, justamente porque é cega nos dados abertos.

**Concorrentes.** BRASIL
- SP156 (app, portal, 156) e Ligue Ilume / SP Regula (0800 779 0156): são os canais oficiais. Registram e marcam o status, mas o próprio órgão se autodeclara 'finalizado' e não há verificação independente. A iluminação nem aparece na base aberta do SP156.
- Colab (colab.com.br): app de zeladoria em mais de 100 cidades, com mais de 250 mil usuários e investimento do fundo GovTech. Permite consultar pedidos por endereço e foi contratado por prefeituras como Santo André. É o concorrente mais forte em registro e acompanhamento, mas funciona do lado da prefeitura, depende do cliente-governo e não confere o conserto com evidência física.
- Fala.BR e Ouvidorias: canal de contestação, mas sem evidência estruturada.
- O juiz de pré-mortem citou o 'Radar 156' (GitHub) e painéis independentes sobre o SP156. Não consegui verificá-los nesta rodada. São análises de dados, não verificação em campo.
- Telegestão das concessionárias de iluminação (luminárias com nó de telemetria): faz o que a Vigília faz, do lado do operador. Segundo o juiz de disrupção (Gazeta SP/Folhapress, não verificado por mim), a PPP de SP abriu mão dessa telegestão, e é essa lacuna que a Vigília ocupa do lado do cidadão.

MUNDO
- FixMyStreet (mySociety, Reino Unido, código aberto) e SeeClickFix/CivicPlus (EUA): referência em relato e acompanhamento público, com atualização pelo cidadão. A verificação continua sendo a palavra de alguém.
- 'How Dark is Your Block?' (Vital City, NYC): protótipo que mede luz com a câmera do celular, geolocaliza e guia o relato ao 311. É o mais próximo em espírito. É pontual, feito por uma pessoa num momento, sem série noturna contínua e sem contraprova pós-finalizado.
- 'Lights On' (Little Rock, EUA): mutirões de moradores que mapeiam postes apagados porque a população sub-relata. Valida a tese de baixo registro, mas é manual e sem sensor.
- Sensor.Community / Smart Citizen Kit: modelo de sensor cidadão hospedado voluntariamente, para ar e ruído. Prova que voluntários hospedam sensores, mas não está ligado a chamados de serviço.

COMO A VIGÍLIA SE DIFERENCIA
- Atua depois do 'finalizado', não antes: é contraprova, não mais um canal de reclamação.
- A evidência é híbrida: série noturna contínua e offline, com antifraude (heartbeat, sensor tampado), mais 2 humanos.
- O resultado é auditável (hash) e vai para quem fiscaliza o contrato (SP Regula).
- Publica também os acertos do órgão.
- Inclui quem não tem app, pelo botão e pelo QR no comércio.
- Cobre um serviço que não tem dado aberto.

### Protótipo de baixa fidelidade

PROTÓTIPO DE BAIXA FIDELIDADE. Tarefa principal: 'provar que o poste continua apagado depois do finalizado'. Três superfícies: app Expo (Larissa), display ESP32 na padaria (Dona Marlene / Dona Cida / Cléber) e página web do QR, sem instalar nada.

TELA 0 · 'O que guardamos' (app, primeira abertura, uma tela só, alto contraste, fonte grande)
- Título: 'Antes de começar'.
- Três linhas com ícone: 'Não pedimos CPF nem nome.' / 'Sua localização só é usada quando você toca em um botão. Nunca em segundo plano.' / 'Nada seu aparece em público. Só números. Apagamos tudo em 90 dias.'
- Botão grande: [Entendi, começar].

TELA 1 · 'Meu caminho a pé'
- Mapa com o texto 'Toque nos postes do caminho que você faz à noite' e alfinetes nos postes já cadastrados pela comunidade, cada um com bolinha: cinza = sem informação; vermelho = apagado; verde = aceso.
- Larissa toca em 3 postes da R. das Flores. Abaixo: 'A que horas você costuma passar?' [23h30].
- Botão [Salvar caminho]. Funciona sem internet: fica na fila (SQLite) e um selo mostra 'guardado no celular · envia quando tiver rede'.

TELA 2 · Pergunta do dia seguinte (notificação local às 12h, no horário que ela escolheu)
- Texto: 'Ontem às 23h30, o poste da R. das Flores, em frente ao nº 120, estava…'
- Três botões grandes: [Apagado] [Aceso] [Não passei].
- Um toque e pronto. Retorno: 'Obrigada! Você e mais 13 vizinhos viram esse poste apagado.'

TELA 3 · Card do poste
- Topo: 'POSTE R. das Flores, 120'. Chip vermelho: 'APAGADO'.
- Linha 1: 'O sensor da Padaria Pão Quente viu apagado em 9 de 9 noites.' (mini-gráfico de 9 barrinhas pretas)
- Linha 2: 'Você + 13 vizinhos confirmaram.'
- Bloco 'Quem cuida?', com duas perguntas de sim/não: 'A lâmpada fica num poste de concreto ou de metal da rua?' → 'É da Prefeitura: peça reparo no Ligue Ilume / SP Regula, 0800 779 0156.' / 'É poste de madeira com fios da Enel?' → 'Ligue para a Enel.'
- Botões: [Ligar e pedir reparo], que abre o discador com um roteiro curto para ler: 'Lâmpada apagada à noite na R. das Flores em frente ao 120, há 9 noites' · [Já tenho protocolo] (campo para digitar o número).

TELA 4 · 'O órgão respondeu?'
- Opções: [Disse que finalizou] [Disse que é de outro responsável] [Ainda não respondeu].
- Ao tocar em 'Disse que finalizou': 'Vamos conferir por 3 noites.' Três quadradinhos: Noite 1 ▢ Noite 2 ▢ Noite 3 ▢. Abaixo: 'Precisamos também de 2 vizinhos confirmando.'
- Texto pequeno e honesto: 'O sensor sozinho não reabre o caso. Se a luz voltar, avisamos que foi resolvido.'

TELA 5A · Resultado 'REABERTO PELA RUA' (fundo amarelo, ícone de poste com X)
- Texto: 'O órgão disse que finalizou em 12/10. A rua viu escuro nas noites 13, 14 e 15/10. 4 vizinhos confirmaram.'
- Botões: [Enviar dossiê para a SP Regula] [Enviar para a Ouvidoria] [Compartilhar sem nomes (WhatsApp, vereador, imprensa)].
- O dossiê é um PDF com gráfico das noites, contagem de confirmações anônimas, número do protocolo e código de verificação (hash).

TELA 5B · Resultado 'RESOLVIDO, CONFIRMADO' (fundo verde)
- Texto: 'A luz voltou na noite de 13/10. O órgão resolveu em 6 dias. Obrigada por acompanhar.'

TELA 5C · 'Responsável é outro'
- Texto: 'O órgão disse que o poste é da Enel / de um condomínio. Veja como pedir.' É um estado próprio, sem tom de acusação.

TELA 6 · Alerta de privacidade (Cyber), quando ela digita observação livre
- Larissa escreve: 'meu CPF 123... minha mãe é diabética e mora no 42'.
- Ao salvar aparece: 'Tiramos do texto o seu CPF, o número da casa e uma informação de saúde. Isso não fica salvo nem aparece para ninguém.' Botão [Ok]. O texto explicativo é gerado por LLM, e o LLM nunca recebe o dado original, só a categoria retirada.

DISPLAY DA VIGÍLIA NA PADARIA (ESP32 atrás do vidro, OLED, botão físico a 90 cm do chão)
- Tela normal, 4 linhas: 'VERIFICAÇÃO DA RUA' / 'POSTE R. FLORES 120' / 'APAGADO HÁ 34 NOITES' / '17 VIZINHOS VIRAM'. Um QR impresso ao lado com o texto 'Viu apagado? Aperte ou escaneie'.
- LED de saúde: verde = 'gravando'; amarelo = 'sem internet, guardando no cartão'; vermelho = 'sensor tampado ou sem energia' (essa noite não conta).
- Dona Cida aperta o botão grande. O display mostra por 3 s: 'OBRIGADO! 18 VIZINHOS'. Vale no máximo 1 confirmação de balcão por noite para a regra, e o placar mostra todos os apoios.
- Quando o caso reabre, o display pisca 'REABERTO PELA RUA'. Quando acende de verdade: 'RESOLVIDO EM 6 DIAS'.
- Instalação, uma vez: Dona Marlene aperta [ACESO AGORA] numa noite boa e [APAGADO AGORA] para calibrar o limiar.
- Por dentro: LDR no tubo preto apontado para a luminária, LDR de referência, relógio, cartão SD e bateria. Grava a cada 5 min das 19h às 5h, ignora faróis (picos menores que 2 min) e manda por MQTT quando há Wi-Fi.

PÁGINA WEB DO QR (Cléber, sem app e sem login)
- Mostra 'Poste R. das Flores, 120 · apagado há 34 noites'.
- Pergunta: 'Você viu esse poste apagado hoje?' [Sim, apagado] [Não, aceso].
- Depois: 'Contado. Quer aviso quando o caso andar? (opcional) [deixar WhatsApp]'.
- Rodapé: 'Quem vê os dados e o que nunca aparece'.

PAINEL PÚBLICO 'PLACAR DO FINALIZADO' (Nginx na VM Linux)
- Lista de postes com estado e noites; mediana de noites entre 'finalizado' e luz acesa; % de finalizados contestados que viraram conserto.
- Ao lado, o mapa do modelo de ML: '1 em cada N finalizados de zeladoria no SP156 volta em 30 dias nesta subprefeitura'.

ROTEIRO DE DEMO (90 s)
- Print do protocolo 'finalizado' + gráfico de luz real gravado no SD.
- Avançar as 3 noites reproduzidas no Wokwi.
- 2 confirmações (app + botão).
- O display pisca REABERTO e sai o dossiê com hash.
- Desligar o Wi-Fi do ESP32 e mostrar os dados chegando inteiros quando a rede volta.
- Fechar com o achado do SP156: '43% das podas finalizadas passaram de 30 dias em Itaquera, contra 2% no Jabaquara', com aviso de censura.

### Think-aloud das personas

**Larissa Oliveira Cruz, estudante de tecnologia no período noturno e atendente de telemarketing de dia, depende de dois ônibus e do metrô** (SUS 66, concluiu: sim, indicaria 7/10). Travou em: 1) Tela 3, 'Quem cuida?': travei de verdade. Às 23h30 eu não vejo de que é feito o poste, e poste de concreto com fio da Enel faz as duas perguntas serem 'sim'. Foi exatamente essa dúvida que sempre me impediu de registrar, e o app devolveu ela pra mim. Também não entendi se é 'Ligue Ilume' ou 'SP Regula' com o mesmo 0800. 2) [Ligar e pedir reparo]: trabalho em call center, não quero ligar pra 0800. Queria abrir o SP156 com o texto pronto. 3) Tela 4: não ficou claro que eu tenho que ir no SP156 conferir o status e voltar pra contar. Se eu esquecer, o caso morre. 4) Os números não batem: 9 noites e 13 vizinhos no app, 34 noites e 17 no display. 5) Tela 5A: hesitei antes de compartilhar porque o protocolo no PDF 'sem nomes' pode chegar no meu CPF. 6) No mapa da Tela 1 eu não sabia qual alfinete era qual poste. Precisa mudar: 1) Trocar o 'Quem cuida?' por uma resposta direta. Lâmpada de rua em SP é da Prefeitura (Ilume) independente do poste, e a Enel fica só pra fio e poste caído. Se não der pra ter certeza, o app manda pros dois. Um nome por número. 2) Botão 'abrir no SP156 com o texto copiado' como opção principal, deixando a ligação como alternativa. 3) Lembrete automático pra eu conferir o protocolo depois de X dias, ou o app puxar o status. 4) Unificar os números entre app, display e QR, ou explicar cada um ('9 noites gravadas pelo sensor, 34 desde a primeira queixa'). 5) Prévia do PDF antes de enviar e opção de tirar o protocolo na versão de compartilhar. 6) Deixar explícito na tela que meu caminho e meu horário ficam só no celular e que o servidor recebe só 'poste X: apagado'. 7) Mapa leve ou lista de postes por endereço, pra não gastar meus dados.

> Tá, abri. TELA 0, 'Antes de começar'. Gostei que é uma tela só e não um termo de 14 páginas. 'Não pedimos CPF nem nome', beleza. 'Localização só quando você toca no botão, nunca em segundo plano': isso é o que me faz continuar, porque app que pede 'sempre' eu já desinstalo. Mas fiquei com uma pulga atrás da orelha no 'apagamos tudo em 90 dias'. Se o poste fica apagado 120 noites, o dossiê some no meio do caminho? Tudo de quem? Do meu caminho ou da contagem do poste? Tocaria em [Entendi, começar] mesmo assim, mas queria um 'saiba mais' ali.

TELA 1, 'Meu caminho a pé'. Mapa. Primeira coisa que penso: quanto de dado isso come? Eu tenho 15 GB e economizo. Se o mapa carrega igual Google Maps, eu faço isso no Wi-Fi da faculdade, não no ônibus. Toco nos 3 postes da R. das Flores... só que, sinceramente, eu sei que são 'os três ali depois da lombada', não sei qual alfinete é qual. Se os alfinetes forem bolinhas pequenas no mapa, com o dedo no iPhone 11 vou errar. Ia ajudar mostrar 'em frente ao nº 120' quando eu toco. 23h30, certo. [Salvar caminho]. O selo 'guardado no celular, envia quando tiver rede' é ótimo, porque no metrô eu desligo os dados mesmo.

TELA 2, notificação ao meio-dia. Meio-dia eu tô no call center, no headset, não posso pegar celular fora da pausa. Mas é um toque só, então dá pra responder na pausa de 15 min ou no ônibus de volta às 14h30. Bom que não me pergunta nada às 23h30, porque nessa hora meu celular tá com 10% e eu tô andando rápido com a chave na mão, não vou parar no escuro pra apertar botão. [Apagado]. 'Você e mais 13 vizinhos viram'. Isso aí me pegou, é a primeira vez que eu sei que não sou só eu reclamando. E o [Não passei] é real: semana passada choveu e eu dormi na casa da Bia perto do metrô.

TELA 3, card do poste. 'APAGADO', sensor da padaria viu 9 de 9 noites, eu + 13. Fácil de entender. Aí chego no 'Quem cuida?' e travo. 'A lâmpada fica num poste de concreto ou de metal?' Gente, eu passo lá às 23h30 no breu, nunca olhei de que é o poste. E poste de concreto com fio da Enel em cima, que é o da minha rua, é de quem? As duas perguntas podem ser 'sim' ao mesmo tempo. É exatamente a confusão que me fez nunca registrar: prefeitura, Enel ou Ilume. O app repetiu a minha dúvida em forma de pergunta em vez de resolver. E outra coisa: 'Ligue Ilume / SP Regula, 0800...' são dois nomes pra um número, fiquei sem saber pra quem eu tô ligando. [Ligar e pedir reparo]: eu trabalho seis horas por dia em telefone, a última coisa que eu quero é ligar pra 0800 e ficar na espera. Eu queria um botão 'abrir no SP156' com o texto já copiado, aí colo e pronto. O roteirinho pra ler é legal pra minha mãe, pra mim não. Digamos que eu registrei e peguei o protocolo, toco em [Já tenho protocolo] e digito.

TELA 4, 'O órgão respondeu?'. Hmm, quem tem que saber se respondeu sou eu? Então eu tenho que ficar entrando no SP156 pra ver o status e voltar aqui pra contar? Se eu esquecer, o caso para. Ia ser muito melhor se o app me lembrasse 'faz 5 dias, olha seu protocolo'. Toco em [Disse que finalizou]. 'Vamos conferir por 3 noites', Noite 1, 2, 3, mais 2 vizinhos. Isso eu entendi de primeira e achei justo. O texto 'o sensor sozinho não reabre o caso' me deu mais confiança, não menos, porque mostra que não é mágica.

TELA 5A, 'REABERTO PELA RUA'. Amarelo, fala o que aconteceu com data. Fiquei feliz, é a prova que eu nunca tive. Mas antes de tocar em [Enviar dossiê], quero saber: o protocolo vai no PDF, né? Eu registrei o protocolo no SP156 com o meu gov.br, com CPF. Se eu toco em [Compartilhar sem nomes] e mando pro grupo do vereador, alguém na prefeitura puxa o protocolo e chega em mim. Então 'sem nomes' com protocolo dentro não é sem nomes. Queria ver a prévia do PDF antes de mandar e poder escolher tirar o protocolo na versão do WhatsApp. O hash eu entendo, é bonito, mas a Dona Cida vai achar que é código de boleto.

5B, verde, 'a luz voltou, resolveu em 6 dias'. Bom. 5C 'responsável é outro', ok, sem drama.

TELA 6. Eu realmente escreveria algo assim no campo livre, tipo 'minha mãe passa aqui às 6h voltando do plantão'. O aviso 'tiramos o CPF, o número da casa e uma informação de saúde' é claro e eu gostei. Mas se vocês detectaram que era CPF, alguém leu o texto original. Isso acontece no meu celular ou no servidor de vocês? Diz isso na tela, porque 'o LLM nunca recebe o dado' não me diz onde o dado passou.

DISPLAY NA PADARIA. A Pão Quente eu conheço, é onde minha mãe compra pão. 'APAGADO HÁ 34 NOITES / 17 VIZINHOS VIRAM', fácil. Só que no app tava '9 de 9 noites' e '13 vizinhos', e no display '34 noites' e '17'. Eu sou chata com número: se não bate, eu acho que tá errado e paro de confiar. Explica que 9 é o que o sensor gravou e 34 é desde a primeira queixa, ou usa o mesmo número. Outra: padaria fecha que horas? Às 23h30 tá fechada, então quem aperta o botão é quem passa de dia e viu de noite, tudo bem, mas aí é confirmação de memória. O botão a 90 cm pra Dona Cida tá certo. E se alguém zoar e apertar 50 vezes? Vocês disseram que vale 1 por noite pra regra, ok, mas o placar sobe 50, isso eu acharia estranho.

PÁGINA DO QR. Essa é a que eu mandaria no grupo 'Ônibus 6091 Jd. Ângela'. Sem app, sem login, dois botões. O 'deixar WhatsApp' opcional é ok, mas depois de golpe de falso emprego eu não deixo meu número em página de QR colado em vidro de padaria, qualquer um pode colar um QR falso por cima. Como eu sei que esse QR é de vocês mesmo? O rodapé 'quem vê os dados' eu clicaria, sim.

PAINEL PÚBLICO. Isso é o que eu mais quero ver, 'o que mudou'. A estatística de Itaquera versus Jabaquara dá raiva do jeito certo. Só quero o M'Boi Mirim lá.

**Edson Benedito Ferreira, homem negro, cadeirante após amputação por diabetes, ex-motorista de ônibus** (SUS 62, concluiu: sim, indicaria 6/10). Travou em: 1) Na Tela 1, 'Meu caminho a pé, postes que você faz à noite': eu não ando a pé e não saio à noite justamente porque é perigoso. Quase desisti achando que não era pra mim. 2) No 'Quem cuida?' travei de vez: não sei distinguir poste de concreto, metal ou madeira olhando de baixo, sentado e de noite. Se escolho errado, ligo pro lugar errado. Isso o app devia saber sozinho. 3) Não entendi se 'Ligue Ilume / SP Regula' é um lugar ou são dois. 4) Os alfinetes do mapa são pequenos pro dedo, toquei errado duas vezes. 5) Na Tela 4 não ficou claro se eu recebo aviso quando as 3 noites terminam. 6) Na Tela 6 não sei o que o filtro vai apagar: se apagar 'cadeirante', some justamente o motivo da urgência. Precisa mudar: 1) Tirar o 'a pé' e o 'à noite' como se fosse obrigatório. Deixar escrito 'a pé ou de cadeira' e aceitar horário de fim de tarde, que no inverno já é escuro. Quem tem medo de sair à noite é justamente quem mais precisa do poste aceso. 2) O app tem que dizer sozinho de quem é o poste, sem me fazer adivinhar o material. 3) Prometer, na tela 0, que o caminho salvo fica só no celular, e deixar eu ver e editar o que vai no dossiê antes de enviar. 4) Botões e alfinetes maiores, e microfone pra ditar o protocolo e a observação. 5) Mandar um aviso quando as 3 noites terminarem, com resultado. 6) Deixar marcar 'risco de queda / cadeirante' como motivo de urgência sem virar dado pessoal. E fazer a mesma coisa pra calçada quebrada e elevador do metrô, que é o meu problema de verdade: o poste apagado só piora a calçada que eu não enxergo. 7) Display e QR: letra maior, protegidos do sol, e verificar se a padaria tem degrau na porta, senão o botão a 90 cm não adianta nada pra mim.

> Tá, o rapaz da FIAP me deu o celular. Deixa eu pôr o óculos. Tô na sombra aqui, senão não enxergo nada.

TELA 0, 'Antes de começar'. Letra grande, ótimo, essa eu leio. 'Não pedimos CPF nem nome.' Bom. 'Localização só quando você toca no botão.' Hmm, vou acreditar desconfiando, viu. 'Nada aparece em público, só números, apagamos em 90 dias.' Isso aqui eu gostei, porque meu medo é aparecer em cadastro que vai parar em financeira. Só que fica faltando uma coisa: vocês perguntam se eu tenho deficiência? Se perguntar, quero saber disso aqui, logo de cara. Aperto [Entendi, começar].

TELA 1, 'Meu caminho a pé'. Já começou errado pra mim: 'a pé'. Eu não ando a pé, eu ando de cadeira. Parece bobagem, mas foi a primeira coisa que bateu. 'Toque nos postes do caminho que você faz à noite.' Eu não saio à noite, moço. Depois das 19h é o meu filho que me leva de carro, ou eu fico em casa. Escuro, calçada levantada e moto: é assim que eu caio. Mas o meu caminho até a farmácia tem um poste apagado, ali na esquina, e às 18h30 no inverno já é noite. Então vou tocar nos postes. Os alfinetes são pequenos, meu dedo é grosso, toquei no errado duas vezes. Bolinha cinza, vermelha, verde... vermelho e verde, tá. 'A que horas você costuma passar?' Coloquei 18h30. [Salvar caminho]. Apareceu 'guardado no celular, envia quando tiver rede'. Isso é bom, porque o Wi-Fi lá de casa cai toda hora.
Só que agora me deu um frio na barriga. Eu acabei de dizer pra um aplicativo qual é o caminho que eu faço e a hora que eu passo, sozinho, de cadeira. Se isso vaza, o ladrão já sabe onde me esperar. A tela 0 falou da localização, mas não falou do CAMINHO que eu salvei. Quem vê isso?

TELA 2, a notificação do meio-dia. 'Ontem às 18h30, o poste da rua tal estava…' [Apagado] [Aceso] [Não passei]. Isso aqui é fácil, um toque só, gostei. Meio-dia eu tô em casa, o celular tá na mão, dá pra fazer. Na rua eu não ia conseguir, porque preciso das duas mãos pra tocar a roda. Toquei [Apagado]. 'Você e mais 13 vizinhos viram.' Opa, então não sou só eu, isso dá uma força.

TELA 3, card do poste. 'APAGADO', vermelho. 'O sensor da padaria viu apagado 9 de 9 noites.' Que sensor? Fiquei curioso, mas tudo bem, a barrinha preta eu entendi. Aí vem o bloco 'Quem cuida?': 'poste de concreto ou de metal' contra 'poste de madeira com fios da Enel'. Olha, eu dirigi ônibus 26 anos e não sei dizer de quem é cada poste. De baixo, sentado na cadeira, olhando pra cima, à noite? Não sei. Aqui eu travei. Se eu errar, vou ligar pro lugar errado e me mandam ligar pra outro. Foi isso que me aconteceu com o SP156 da calçada. O aplicativo devia saber, já que o poste tá cadastrado. E tem 'Ligue Ilume / SP Regula' no mesmo nome: é um órgão ou são dois?
[Ligar e pedir reparo] com o texto pronto pra ler: isso eu gostei MUITO. Eu prefiro falar a digitar, e o texto me salva de gaguejar no telefone. Liguei de mentirinha. A moça me deu um protocolo e eu apertei [Já tenho protocolo]. Digitar número com um dedo só demora, mas vai. Podia ter um microfone pra eu ditar.

TELA 4, 'O órgão respondeu?'. [Disse que finalizou]. Essa é a minha história. A calçada da farmácia ficou 40 dias 'em análise' e depois virou 'finalizado' sem ninguém aparecer. Apertei. 'Vamos conferir por 3 noites', com três quadradinhos. Entendi. 'Precisamos de 2 vizinhos confirmando.' Tá. 'O sensor sozinho não reabre o caso.' Gostei da honestidade, não tá me prometendo milagre.
Só que como eu sei que deu certo? Eu recebo aviso? Precisei perguntar pro rapaz.

TELA 5A, 'REABERTO PELA RUA', amarelo. 'O órgão disse que finalizou em 12/10, a rua viu escuro em 13, 14 e 15.' Agora sim. Isso eu queria ter tido em março. Botões: [SP Regula], [Ouvidoria] e [Compartilhar sem nomes, WhatsApp, vereador, imprensa]. Eu ia direto no WhatsApp, mandar no grupo 'Moradores Vila Prudente' e pro assessor do vereador. Prefeitura eu já sei que não adianta. Vereador e TV adiantam. O PDF com o 'código hash' eu não sei o que é, mas se o vereador entende, tá bom. Só me preocupa uma coisa: esse PDF mostra o meu caminho? Mostra o horário que eu passo? Eu precisaria ver antes de mandar.

5B, verde, 'a luz voltou', bonito. 5C, 'o responsável é outro': ok, pelo menos não me deixa sem saída.

TELA 6, privacidade. Ditei uma observação: 'sou cadeirante, caí aqui na frente da farmácia, sou diabético'. Se ele tirar o 'diabético', tudo bem. Mas e se ele tirar o 'cadeirante'? Essa é justamente a informação que importa pro conserto. O poste apagado pra mim é perigo de queda, não é só escuridão. Não ficou claro o que eu posso deixar.

DISPLAY NA PADARIA. Botão a 90 cm do chão: alguém pensou em cadeirante, olha só. Sentado, eu alcanço. Só que tem que ver se a padaria tem degrau na porta, porque a maioria aqui tem. Se o display tá atrás do vidro e a padaria tem degrau, eu aperto o quê? A letra do OLED é pequena e com sol não vou enxergar. Os 4 linhas eu leio se chegar perto. 'APAGADO HÁ 34 NOITES, 17 VIZINHOS VIRAM.' Isso é igual placar de futebol, chama atenção, eu ia parar pra ver. O LED colorido eu não ia entender sem explicação.

PÁGINA DO QR. Pra escanear eu tenho que parar a cadeira, tirar o celular do bolso e apontar a câmera com a mão suada. Com sol, não lê. Se escanear: 'Sim, apagado', um toque, beleza. 'Deixar WhatsApp': NÃO deixo. Já me ligam de empréstimo consignado 3 vezes por semana, aposentado é alvo.

PLACAR PÚBLICO: '1 em cada N finalizados volta em 30 dias'. Isso aí é coisa pra reportagem. Eu queria ver isso na TV.

**Cléber Andrade dos Reis, motorista de aplicativo que já desistiu do SP156 e de apps 'cidadãos' e desconfia de tecnologia do governo** (SUS 58, concluiu: sim, indicaria 5/10). Travou em: 1) Tela 1: 'caminho a pé' não serve pra mim, chego de carro de madrugada; não tem opção 'poste da frente de casa' nem fica claro se eu posso cadastrar poste novo quando a bolinha não existe. 2) Tela 3, 'Quem cuida?': as duas perguntas podem dar sim ao mesmo tempo (poste de concreto com fio da Enel), e de novo eu é que tenho que adivinhar de quem é, que foi exatamente o empurra-empurra que me ferrou em 2025. 3) Tela 3 também: não sei de que sensor nem de que padaria estão falando; se não tiver sensor na minha rua, a prova fica fraca. 4) Tela 4/5A: 'reabrir' só vale no app de vocês, o protocolo continua 'finalizado' na Prefeitura e ninguém me diz o que acontece depois que o dossiê é mandado. 5) Página do QR: parei no campo do WhatsApp por medo de golpe. 6) A palavra 'hash' não diz nada pra mim. Precisa mudar: 1) Trocar 'caminho a pé' por 'postes que importam pra mim': frente de casa, ponto de ônibus, caminho de carro, e deixar cadastrar poste novo. 2) Tirar de mim a responsabilidade de descobrir se é Prefeitura ou Enel: o app sugere pelo cadastro do poste e manda o pedido pros dois se tiver dúvida, guardando os dois protocolos. 3) Depois do dossiê, mostrar status com prazo ('enviado em 16/10, sem resposta há 5 dias') e lembrar de cobrar, senão é caixa-preta de novo. 4) Botão de compartilhar pro TikTok/WhatsApp bem grande e primeiro, com imagem pronta (placar + noites + '43% em Itaquera'), porque é isso que faz a coisa andar. 5) Trocar 'hash' por 'código que prova que ninguém editou'. 6) Rodapé 'Quem vê os dados' em 3 linhas, com nome do responsável, e aviso por WhatsApp que não exija número (ex.: seguir o poste pelo próprio link do QR). 7) Para a tela 0, mostrar quem é responsável e até quando o projeto vai existir. A padaria e o QR sem login estão certos: priorizem isso, não o app.

> Tá, me deram o celular pra testar. Vamos ver em 30 segundos se presta.

TELA 0 'Antes de começar'. 'Não pedimos CPF nem nome.' Opa, pelo menos isso. O SP156 pedia CPF, o Colab pedia selfie. Mas quem garante? Escrever qualquer app escreve. O app da Enel também dizia 'seus dados estão seguros' e meu CPF foi parar numa conta digital em 2024. 'Apagamos tudo em 90 dias.' Quem apaga? Vocês, estudantes? E quando o semestre acabar e o servidor ficar ligado sem ninguém olhar? Não diz QUEM é o dono disso. Aperto [Entendi, começar] porque não tem outra opção, que é o que todo app faz.

TELA 1 'Meu caminho a pé'. Aqui já travei. Eu não ando a pé à noite, eu ando de Onix. Eu chego 1h da manhã de carro. 'Toque nos postes do caminho que você faz à noite'... e eu sou o quê, então? Pulo? Não tem botão de 'eu passo de carro' ou 'é o poste da frente da minha casa'. Mas beleza, faço de conta: toco no poste da frente de casa. Bolinha cinza. Se a comunidade não cadastrou o poste da minha rua, eu cadastro? Não fica claro. Coloco horário 1h. Salvar. 'Guardado no celular, envia quando tiver rede' — isso é bom, no apagão de dezembro eu fiquei 52 horas sem nada. Ponto pra vocês.

TELA 2 'Ontem às 23h30 o poste estava…' Notificação às 12h. Meio-dia eu tô no meio da corrida, com o Waze aberto. Notificação que não é corrida eu arrasto pro lado. Mas se é um toque só, [Apagado], eu até faço no semáforo. 'Você e mais 13 vizinhos viram.' Isso eu gostei, é tipo o contador do TikTok, dá sensação que não sou só eu chato reclamando.

TELA 3 Card do poste. 'O sensor da Padaria viu apagado em 9 de 9 noites.' Que sensor? Que padaria? Eu nunca vi isso. Se a padaria da minha rua não tiver, essa linha some e o card fica só com 'você + 13 vizinhos', que é igual abaixo-assinado. Bloco 'Quem cuida?': essa é a parte que me pega. 'Poste de concreto é Prefeitura, madeira com fio da Enel'. Eu passei UM ANO com Prefeitura e Enel jogando um pro outro. Vocês tão me pedindo pra eu descobrir sozinho de quem é? Poste de concreto com fio da Enel e lâmpada da Prefeitura, que é o da minha rua, eu respondo o quê? As duas perguntas são sim/não e as duas podem ser sim. Aí o [Ligar e pedir reparo] com roteiro pra ler: isso é útil de verdade, eu sempre esqueço o que falar e o atendente me enrola. Mas eu vou ligar de novo, igual sempre liguei. O app não abre o protocolo por mim. [Já tenho protocolo] — digito o número que eu já anoto no bloco de notas. Ok.

TELA 4 'O órgão respondeu?' [Disse que finalizou]. Isso, ISSO aqui é a minha vida. Três pedidos de poda 'finalizados'. Toco. 'Vamos conferir por 3 noites. Precisamos também de 2 vizinhos.' 'O sensor sozinho não reabre o caso.' Gostei que é honesto. Mas espera: 'reabre' onde? Vocês reabrem o protocolo no SP156? Não, né. Vocês reabrem no app de vocês. Pra Prefeitura continua 'finalizado'.

TELA 5A 'REABERTO PELA RUA', fundo amarelo. 'Disse que finalizou em 12/10, a rua viu escuro 13, 14 e 15.' Isso aqui é o print que eu queria ter em dezembro. Botões: [Enviar dossiê SP Regula] [Ouvidoria] [Compartilhar sem nomes]. Eu vou direto no terceiro. WhatsApp do grupo dos motoristas, TikTok, marcar o vereador e a Globo. É o que funciona. O PDF com 'hash'... nem sei o que é hash, mas se é um código que prova que não foi montado no Photoshop, explica isso em português. 'Código que prova que ninguém mexeu'. Agora: mandar pra SP Regula — e depois? Alguém me avisa se leram? Se não tiver prazo e resposta, é mais um buraco negro igual o 156.

TELA 5B verde 'Resolvido em 6 dias'. Bonito. Nunca vi isso acontecer, mas se acontecer eu posto também.

TELA 6 Privacidade. Escrevo 'minha sogra usa oxigênio, fica sozinha à noite no 42'. App tira e avisa. Achei bom que avisa, achei melhor ainda que não salva. Porque essa é exatamente a informação que eu NUNCA ia querer que vazasse: idosa sozinha, endereço. Mas aí 'gerado por LLM'... manda pra fora? Pra onde? Dizem que o LLM não recebe o dado original. Tem que provar. Mas pelo menos tiram antes.

DISPLAY DA PADARIA. Isso eu entendi na hora sem ler manual: 'APAGADO HÁ 34 NOITES / 17 VIZINHOS VIRAM'. Eu passo na padaria de manhã, compro pão, aperto o botão. Pronto. É tipo placar de futebol na rua, todo mundo vê, ninguém precisa baixar nada. Isso é o melhor da proposta. Medo: alguém da rua de baixo aperta 30 vezes de zoeira. Dizem que conta 1 por noite pra regra. Ok. Outro medo: a padaria fecha, a dona tira da tomada, roubam o aparelho. LED vermelho 'essa noite não conta' — bom ser honesto.

PÁGINA DO QR. Escaneio. Sem login, sem app. 'Você viu apagado hoje?' [Sim, apagado]. 'Contado.' Isso é perfeito pra mim, 5 segundos. 'Quer aviso? deixar WhatsApp (opcional)'. Aí eu paro. WhatsApp é meu número, meu número tá ligado ao meu CPF, meu CPF já vazou. Vou ler o rodapé 'Quem vê os dados' — se for um texto jurídico de 3 telas, não deixo. Se for 3 linhas: 'só o grupo da FIAP vê, não vendemos, apagamos em 90 dias, responsável: fulano', talvez deixo.

PLACAR DO FINALIZADO. '43% das podas finalizadas passaram de 30 dias em Itaquera contra 2% no Jabaquara.' ISSO. Isso é o que eu falo no TikTok sem ter número. Com número, a emissora dá matéria. Isso eu compartilho.

Resumo: consegui fazer o caminho até 'REABERTO PELA RUA', mas pela padaria e pelo QR foi muito mais fácil que pelo app. O app eu provavelmente desinstalava em uma semana, igual os outros.

### Banca FIAP simulada (nota 8)

**Memorável.** O "zero em 484.232". A equipe abriu o CSV do SP156, viu que lâmpada apagada não aparece na base e transformou esse buraco no argumento central: "a iluminação é o serviço mais cego da cidade, então a rua mede". Também vou lembrar da regra que credita o órgão quando ele acerta ("RESOLVIDO, CONFIRMADO em N dias") e do tubo de caneta preto colimando o LDR. É raro uma GS mudar de tese por causa do dado, e não o contrário.

**Já visto?** O tema é dos mais batidos: todo semestre aparecem dezenas de apps de zeladoria com mapa de pins, "reporte o buraco" e placar da prefeitura, e a Tela 1 (mapa com bolinhas coloridas) é igual a todas. A parte central é que é rara. Verificar o "finalizado" depois do fato, com série noturna offline, antifraude (heartbeat, sensor tampado) e confirmação humana, numa máquina de estados explícita, eu vi poucas vezes, e nunca tão bem amarrada ao dado público. Lembra o "How Dark is Your Block?" e a telegestão das concessionárias, e a equipe cita os dois com honestidade. Em ideia, uns 70% já vistos. Na contraprova, no achado de dados e na disciplina de corte, está acima da média.

**Perguntas difíceis.**
- Física na vitrine: a padaria está acesa à noite. O vidro reflete a luz interna, e o letreiro e a geladeira iluminam o tubo por trás. Vocês mediram, numa vitrine real, a diferença entre poste aceso e apagado com a padaria aberta? Se o LDR de referência também recebe a luz da própria luminária, como a diferença entre os dois LDRs separa 'rua escura' de 'padaria clara'? Me mostrem a série gravada, não o diagrama.
- Funil: para um caso reabrir, precisa de uma padaria parceira com o poste no campo de visão, alguém que ligou no 0800 e recebeu um 'finalizado', 3 noites escuras e 2 humanos. Quantos casos desses acontecem por ano no bairro de vocês? O poste do trajeto da Larissa está na frente de alguma padaria? Se a resposta for 'um ou dois por ano', o impacto mensurável vira anedota.
- O Ligue Ilume/SP Regula devolve mesmo um status de 'finalizado' ao cidadão, com número de protocolo consultável? Vocês ligaram para testar? O gatilho inteiro depende disso, e hoje ele é uma suposição.
- Integração do ML: o modelo prevê reincidência de buraco, poda, bueiro e guias. Nenhum dado dele entra na decisão da Vigília sobre iluminação. Fora o mapa ao lado no placar, onde o XGBoost muda o comportamento do sistema? Isso é integração ou dois projetos lado a lado?
- Vazamento no modelo: a chuva acumulada nos 7 dias depois da finalização é uma variável que vocês não conhecem no momento da previsão. Se a previsão é feita no 'finalizado', a feature não existe em produção. Como o PR-AUC fica sem ela?
- Validade do proxy: 'novo pedido do mesmo serviço no mesmo logradouro e número em 30 dias' pode ser outro buraco na mesma rua, um número aproximado, ou um vizinho reclamando de algo diferente. Vocês olharam manualmente uma amostra de, digamos, 50 pares e viram quantos são o mesmo problema voltando? Sem isso, 7% não prova 'finalizado falso'.
- '2 humanos distintos' com token anônimo por aparelho e página web sem login: eu abro a aba anônima duas vezes e sou dois vizinhos. Qual é o custo de forjar a regra, e por que a SP Regula deveria confiar nesse dossiê?
- A cadeia de hash fica no servidor de vocês. Quem impede vocês de reescrever a cadeia inteira? Há ancoragem externa (carimbo de tempo, publicação periódica do hash) ou o 'auditável' é só retórica?
- Faltam 14 dias e a validação foi com personas simuladas. Qual padaria real topou? Quem da SP Regula, da Ouvidoria ou de uma associação de moradores ouviu a proposta? 'Comece pelo território' é o primeiro critério do briefing.
- FastAPI + .NET + Postgres + Mosquitto + Expo + ESP32 + Nginx + LLM para 5 pessoas em 2 semanas: qual é o caminho mínimo que funciona ponta a ponta no dia 12, e o que vocês cortam se o dia 12 chegar sem ele?
- Por que um LLM para explicar 'tiramos seu CPF, o número da casa e uma informação de saúde', se ele só recebe a categoria? Um template faz isso de forma determinística e testável. Defendam o LLM ou admitam que ele está aí pela rubrica de Cyber.
- Rótulos inconsistentes: no item 10, 'Fiação e Postes' virou 'guias/calçada', e 'poda' e 'árvore' se alternam no achado de 43% x 2%. No slide, qual é exatamente o recorte, e o n de cada subprefeitura sustenta a comparação?

**Veredito.** Na banca, eu colocaria entre os 3 primeiros. Ainda não é campeã garantida.

O que sustenta a nota:
(1) Honestidade com o dado. Vocês baixaram o CSV, contaram linhas, corrigiram o nome da coluna, descobriram que iluminação não está na base e trocaram a pergunta de ML por uma que a base responde. É o que o briefing pede e quase ninguém faz.
(2) A tese tem formato próprio. É contraprova depois do "finalizado", não mais um canal de reclamação. A regra (finalizado + 3 noites + 2 humanos) é clara, testável e justa, porque também credita o órgão.
(3) O corte foi bem feito. Saíram a árvore, o MPU6050, o blur e o roteamento por IA. Ficou um serviço, um verificador e uma regra. Isso conversa com "comecem pequeno".
(4) Inclusão concreta: botão a 90 cm, QR sem login, um toque no almoço, nada de porcentagem para a Dona Cida.
(5) O mapa de disciplinas é coerente. A máquina de estados em C# e os testes "só sensor não reabre" e "sensor tampado invalida a noite" são critérios de aceite de verdade.

O que impede a nota máxima:
(a) A física da vitrine é o ponto que derruba a demo se ninguém testar antes. Uma padaria acesa atrás de um vidro é o pior ambiente possível para um LDR. Sem uma noite real gravada numa vitrine de verdade, o "colimador" é só uma hipótese.
(b) O funil de casos é estreito. A solução é elegante para um evento raro, e o indicador de impacto não vai mostrar nada em 2 semanas. Precisam dizer isso e mostrar como o caso escala (mais hospedeiros, mais serviços com verificação humana).
(c) O ML é bom, mas fica ao lado do sistema, não dentro dele. Além disso, a chuva pós-finalização vaza informação do futuro, e o proxy de reincidência ainda não foi validado com amostra manual.
(d) A validação foi simulada, com 14 dias para a entrega. Uma padaria real e uma ligação real para o 0800 valem mais que três slides de persona.
(e) A antifraude humana (token por aparelho) e o hash sem ancoragem externa enfraquecem justamente o que diferencia a proposta: a prova.
(f) O risco de execução é alto: dois backends mais um LLM para cinco pessoas.

Para chegar a 9 ou 10:
- Gravem uma noite real numa vitrine real e coloquem o gráfico no pitch.
- Contem uma história verdadeira: um poste, um protocolo, uma ligação gravada para o Ligue Ilume.
- Mostrem uma amostra manual validando o proxy de reincidência.
- Tirem a chuva futura do modelo, ou usem previsão de chuva no lugar.
- Ancorem o hash publicamente.
- Num pitch de 4 minutos, cortem a explicação da arquitetura e fiquem com a frase "zero em 484 mil, por isso a rua mede". Essa frase ganha a sala.

## Totem da Viela: o aviso sem nome que sobe, a rota que desce

**Conceito refinado.** CONCEITO REVISTO (v2, com os fixes dos 4 juízes e as objeções de Josefa, Sandra, Edileuza e Cléber)

Uma frase: num ponto com dono na viela (mercadinho, igreja ou escola), uma caixa com três botões de desenho recebe o aviso sem nome. Um líder do NUPDEC confirma o aviso antes de ele chegar à Defesa Civil. Quando a agente decide, a caixa toca sirene e fala, com o nome da rua, por onde sair e qual abrigo está de fato aberto.

Território e insight (precisa de fonte e de conversa real): Brasilândia/Capadócia, Zona Norte de SP. A Agência Pública (jan/2024) contou 185.634 moradias em área de risco no GeoSampa e registrou moradores da Capadócia que saíram com medo e depois voltaram porque não tinham para onde ir. Em 2026 a Defesa Civil de SP declarou que, entre dezembro e março, morre uma pessoa a cada 3 dias nas chuvas. O medo de remoção é hipótese forte, mas ainda não validada com morador real (é a hipótese mais arriscada, abaixo). A frase "três dias antes" sai do pitch até ter fonte IPT/CEMADEN; no lugar entra "a informação existia antes; agora ela tem um botão".

O QUE MUDOU E POR QUÊ
1. A caixa vai para um lugar com dono, não para um poste solto (Sandra, Josefa, Cléber, pre-mortem: vandalismo, trote, manutenção). O dono do comércio ou o zelador da igreja é o "guardião da caixa". Para o pitch, a manutenção fica assim: o NUPDEC é dono do equipamento e a Defesa Civil da subprefeitura dá o aval. A caixa tem bateria 18650 e placa solar pequena, e o tempo de autonomia vem escrito nela (Josefa, Edileuza, Cléber: "quando cai a luz é quando mais precisa").
2. Antitrote em três camadas (Edileuza, Cléber): segurar o botão por 3 s (criança que só bate não conta), no máximo 1 aperto por botão a cada 10 min, e só gerar alerta com corroboração (2 tipos de sinal diferentes, ou aperto + chuva acima do limiar, ou aperto + leitura de selo piorando). Uma leitura isolada não move nada.
3. Filtro humano local antes da Defesa Civil (Sandra: "é o filtro de boato que eu não tenho"). O aperto corroborado aparece primeiro no app do líder do NUPDEC, que vai ao local e marca "confirmei / não achei nada". Só depois vira cartão na mesa da Sandra. Se o líder não responder em 30 min com chuva forte, o cartão sobe mesmo assim, marcado "não confirmado".
4. Retorno visível para quem apertou (Edileuza, Cléber: "senão vira o 156"). A caixa tem um placar de LED/segmentos: "avisos esta semana: 4 · vistoria: QUI · feita: SIM". Ninguém é identificado e todo mundo vê que alguém viu.
5. Selo numerado sem sensor (viabilidade, Sandra). O MPU6050 sai do projeto. O selo é um adesivo com régua milimetrada que o voluntário do NUPDEC fotografa toda semana e cuja abertura em mm ele digita no app. O número do selo é ligado ao SETOR e ao PONTO, nunca ao endereço nem a nome. Ele só é colado com a presença do presidente da associação e com um texto falado e escrito explicando a finalidade. Sendo honestos: o sistema não pode prometer que a prefeitura nunca vai usar a informação. Por isso ele guarda o mínimo (setor, selo, mm, data), e o painel nunca mostra selos ao público (Sandra: "vira desvalorização de casa e uso político").
6. Descida do alerta sem depender de celular nem de app (Josefa, Edileuza, Cléber). A própria caixa toca sirene e fala em voz alta, com o nome da viela ("saia pela Escadaria Alta, a Escola Estadual está aberta"). O alerta ao celular usa o canal que já existe e é autenticado, o Defesa Civil Alerta/cell broadcast (a Sandra dispara pelo sistema dela, e nós só geramos o texto sugerido). O app Expo fica só para quem tem papel: líder do NUPDEC, responsável pelo abrigo e vizinho guardião.
7. A rota nunca é automática (Sandra, pre-mortem: "se a escadaria for a que está cedendo, a seta mata gente"). A Sandra escolhe a rota no momento, entre 2 ou 3 rotas já cadastradas com o NUPDEC. O vermelho nunca acende sem uma decisão humana autenticada. Depois da invasão de junho/2026, que mandou alerta falso a celulares de vários estados, a caixa só obedece comando de SAIR assinado (HMAC com chave do console, nonce e validade de 5 min). Sem rede, a caixa no máximo sobe sozinha para amarelo.
8. Ninguém fica para trás, sem expor nome (Sandra, Edileuza). A lista de quem precisa de ajuda para sair (acamado, eletrodependente, idoso sozinho) fica com a UBS/ACS, fora do sistema. O sistema guarda só um código opaco por pessoa (P-07) e uma contagem por setor. Com SAIR, o vizinho guardião e a ACS recebem "P-07 precisa de ajuda no setor 3", e só a ACS sabe quem é P-07. O guardião marca "já saiu". É aqui que entra o motor de PII do Cyber: quando o líder ou a ACS digita texto livre ("Josefa da casa 32, CPF 123..., mãe acamada"), o texto é redigido antes de qualquer gravação e vira "1 pessoa com prioridade, setor 3".
9. Abrigo só aparece se estiver de fato aberto (Josefa: "já fiquei na porta com a Emily no colo"). O responsável pela chave (escola ou igreja) faz check-in no app. Sem check-in nas últimas 2 h, o abrigo não entra na voz da caixa.
10. O escopo cai para o núcleo (os 4 juízes). Saem o pipeline de áudio do WhatsApp com Whisper, o sentinela MPU6050, o segundo e o terceiro uso de LLM e o "87%" sem lastro. O único uso de LLM é gerar o cartão do setor e a explicação ao cidadão ("tirei o nome da sua vizinha antes de mandar"), sempre depois do taint.
11. Adaptável a outros territórios (Edileuza, Cléber), mas só como fala de escala no pitch. Os pictogramas são configuráveis por território: várzea ("água subindo na rua", "água entrando em casa") ou árvore e fio baixo. O MVP é só encosta, na Brasilândia.
12. Diferencial em relação ao que existe (desejabilidade): 40199, cell broadcast e sirenes só levam o alerta de cima para baixo. O Totem é o canal de SUBIDA anônimo, com filtro comunitário, deduplicação por selo e retorno visível. A descida reaproveita o que já existe.

ML DEFENSÁVEL (com os fixes de viabilidade e pre-mortem, depois da verificação dos dados)
Pergunta: "com o que a estação do Mirante de Santana mede agora (chuva das últimas 1/3/24/72/96 h, pressão e sua queda em 3 h, umidade, ponto de orvalho, rajada, hora, mês), o acumulado de 72 h vai CRUZAR o limiar de alerta nas próximas 3 horas?" O rótulo é calculado sobre a própria série INMET, com o limiar como parâmetro citado (PPDC/CGE). Isso evita o rótulo raro de deslizamento: verifiquei que o Atlas tem só 15 registros de movimento de massa na capital entre 1991 e 2025, nenhum depois de 2020. Usar variáveis de pressão e umidade reduz a crítica de "é só persistência". O modelo é XGBoost com 12 combinações de max_depth, learning_rate e n_estimators, TimeSeriesSplit no treino, scale_pos_weight, recall e PR-AUC de treino contra teste (teste = 2024-2026). O baseline obrigatório fica ao lado: a regra "alerta quando o acumulado de 72 h já passou de 80% do limiar". A frase para o cidadão fica: "em X de cada 10 vezes em que a chuva acumulada chegou ao nível de alerta, o modelo avisou 3 horas antes; a regra atual avisou em Y". Os eventos do Atlas na capital e na RMSP vizinha (Caieiras, Franco da Rocha, Mairiporã, Francisco Morato etc.) servem só como checagem qualitativa: "quantos dos registros reais o modelo teria antecipado".

DISCIPLINAS (mesmo fluxo)
- ESP32/Wokwi: um único circuito com 3 botões (segurar 3 s), potenciômetro como pluviômetro, sensor de umidade do solo, semáforo, 2 LEDs de seta, buzzer, display de placar, buffer offline e verificação HMAC do comando SAIR.
- SOA: API de Ingestão (Totem, Aperto, Selo, Leitura, Setor) com migrações, Swagger e testes de deduplicação e antitrote.
- C#: Web API .NET de Decisão (Ocorrência, Cartão, Decisão com State pattern MONITORAR/PREPARAR/SAIR em que só humano autenticado promove a SAIR, Rota, Abrigo, CheckIn), EF Core + LINQ (apertos corroborados por setor, abrigos com check-in < 2 h) e async no push MQTT. Recomendação: as duas APIs em .NET na mesma solução, com bounded contexts separados e conversando por HTTP. Se o professor de SOA quiser outra linguagem, a Ingestão migra sem afetar a Decisão.
- Cyber: PII no texto livre de líder e ACS (CPF com dígito verificador, nome, número de casa, termos de saúde). O taint bloqueia identidade, saúde e local exato em voz da caixa, log, prompt de LLM e export. LLM explicador. Dashboard Risco Cidadão com k por setor e alerta quando o setor tem menos de 5 casas, caso em que agrega com o vizinho.
- Mobile: app Expo por papel (líder, abrigo, guardião), com fila offline em SQLite.
- Linux: provisionamento por Shell Script, systemd de monitoramento, auditoria assinada de cada SAIR e teste de falha (matar o broker no meio da chuva e ver a caixa ir para amarelo local).
- Testing (Azure Boards): "vermelho nunca acende sem decisão assinada", "aperto de menos de 3 s não conta", "apertos repetidos em 10 min contam uma vez", "abrigo sem check-in não entra na voz", "CPF nunca aparece em log nem em prompt".

DEMO WOW (60 s, 3 atos, todos com a caixa como heroína)
Ato 1: alguém da banca segura "água barrenta" por 3 s no Wokwi enquanto o potenciômetro marca 34 mm/h. O app do líder vibra e o líder toca "confirmei".
Ato 2: a mesa da Sandra mostra um cartão: "Viela 3: 2 sinais confirmados, Selo 14 abriu 3 mm na semana, chuva de 72 h cruza o limiar em até 3 h (modelo) / regra atual ainda não". A Sandra escolhe "Escadaria Alta" e assina SAIR.
Ato 3: a caixa fica vermelha, a seta muda, o buzzer toca e a voz fala a rua. O guardião recebe "P-07 precisa de ajuda". O Wi-Fi cai e a caixa segue no amarelo local. A PII redigida aparece no vídeo de Cyber, não no palco.

INDICADOR DE IMPACTO (ficha)
(1) Tempo entre o primeiro sinal e a decisão da Defesa Civil. (2) Avisos por noite caindo de 15-18 soltos para N cartões deduplicados. (3) % de moradores que, num teste com o protótipo, apontam a rota e o abrigo certos. (4) % de avisos com retorno exibido no placar em até 7 dias.

ODS: 11.5 (perdas em desastres), 13.1 (resiliência climática), 10.2 (inclusão de quem não lê nem tem smartphone), 3.d (alerta precoce de riscos à saúde) e 9.1 (infraestrutura resiliente).

Nota de método: a "rodada 2" de personas abaixo também é simulada. Como subagente, não tive acesso a uma ferramenta de agent team para criar agentes separados, então as personas foram encenadas aqui com base nos testes anteriores. Não substitui entrevista real, que o briefing exige ("uma pessoa real que convive com o problema"). Reação simulada à v2: Josefa sobe para 4/5 (sirene e voz com o nome da rua, e "não preciso de celular"), mas ainda pergunta se o selo serve para remoção. Sandra sobe para 4/5 (filtro do NUPDEC, rota escolhida por ela, comando assinado) e pergunta quem paga a bateria. Edileuza fica em 3/5 (a lista fica com a UBS e o P-07 a convence; a várzea fica para depois). Cléber fica em 3/5 (o placar o convence; "se acertar, eu faço o vídeo").

### Lean Canvas

- **problem.** 1) Quem mora em encosta R3/R4 vê os sinais (trinca abrindo, porta emperrando, água barrenta), mas não avisa por medo de ser identificado ou removido, e porque o 156 e o 199 'não dão retorno'. 2) A Defesa Civil recebe avisos soltos, duplicados e sem filtro (15-18 por noite na persona), sem saber o que é boato. 3) Quando há ordem de saída, ela não chega a quem não tem celular, não sabe ler ou está sem luz, a rota pode estar bloqueada e o abrigo anunciado pode estar trancado. Alternativas atuais: fita crepe na trinca, áudio no grupo de WhatsApp, 199/156, SMS 40199 e cell broadcast (só descida), sirenes comunitárias (só descida, sem relato).
- **customerSegments.** Usuários: moradores de setores R3/R4 com baixa alfabetização digital (Dona Josefa), líderes e voluntários do NUPDEC (seu Valdir), ACS e lideranças comunitárias (Edileuza), responsáveis por abrigos (escola, igreja). Cliente pagador/adotante: Defesa Civil municipal (COMDEC/subprefeitura) e estadual (PPDC). Early adopter: um NUPDEC já ativo, com um setor que já treina rota de fuga (a Sandra ofereceria isso).
- **uvp.** O aviso sobe sem nome e a rota desce sem celular. Três botões num lugar de confiança, um vizinho que confirma, uma pessoa da Defesa Civil que decide e uma caixa que fala o nome da sua rua e só anuncia abrigo que está de fato aberto.
- **solution.** 1) Caixa ESP32 com 3 pictogramas (segurar 3 s), pluviômetro, placar de retorno, sirene e voz, seta e bateria/solar, que funciona offline até o amarelo. 2) Selo numerado com régua fotografado semanalmente pelo NUPDEC (selo ligado a setor, nunca a endereço). 3) App do líder: confirmar aperto, ler selo, check-in de abrigo, guardião. 4) Mesa de Decisão: um cartão por setor, rota escolhida pela agente e SAIR assinado. 5) Motor de PII e taint no texto livre, com lista de prioridade em código opaco (P-07) mantida pela UBS. 6) Nowcast INMET que diz se o acumulado de 72 h vai cruzar o limiar nas próximas 3 h, comparado com a regra atual.
- **channels.** Defesa Civil da subprefeitura (Freguesia/Brasilândia) e PPDC estadual; NUPDECs e associações de moradores; UBS/ACS (Estratégia Saúde da Família); igrejas e o comércio local que hospeda a caixa; Cemaden Educação e escolas estaduais (abrigos e escolas-piloto); vídeo de resultado feito pela comunidade (Cléber oferece TikTok depois de ver funcionar).
- **revenue.** Sem cobrança do cidadão. Caminhos de financiamento para a escala, a validar: orçamento de prevenção da Defesa Civil municipal e estadual (PPDC); editais de inovação cívica e clima (ex.: programas municipais de inovação, FAPESP/PIPE em versão empresa, fundos climáticos); emendas parlamentares para NUPDEC; patrocínio de operadora ou concessionária de energia como contrapartida social. Modelo: kit caixa + software como serviço público, com licença aberta para os NUPDECs.
- **costs.** Estimativas não verificadas, a cotar: peças da caixa (ESP32, botões, display, buzzer, alto-falante, 18650, painel solar de 5 W, gabinete IP65) na faixa de algumas centenas de reais por unidade. Selos e réguas impressos: centavos por unidade. Nuvem: VM Linux pequena com broker MQTT. Conectividade: chip M2M ou Wi-Fi do comércio. O custo real é humano: treinar o NUPDEC, manter a bateria e a caixa e fazer as vistorias semanais dos selos.
- **keyMetrics.** Impacto: tempo entre o primeiro sinal e a decisão; avisos por noite, de 15-18 soltos para N cartões; % de moradores que acertam rota e abrigo no teste; % de avisos com retorno no placar em até 7 dias. Saúde do canal: apertos corroborados / apertos totais (taxa de trote); % de apertos confirmados pelo líder em até 30 min; horas de caixa online e em bateria. ML: recall e PR-AUC no teste 2024-2026 contra o baseline da regra atual.
- **unfairAdvantage.** Confiança construída no território, que não se copia com software: o NUPDEC, a ACS e o dono do comércio como guardiões, mais o desenho de anonimato e minimização de dados feito com eles (lista de prioridade fora do sistema, selo ligado a setor). Também o comando SAIR assinado e a regra de decisão humana, que respondem diretamente ao incidente de alerta falso de junho/2026.

**Hipótese mais arriscada.** Moradores de setores R3/R4 vão de fato apertar um botão num lugar público (o mercadinho ou a igreja da viela, na frente dos vizinhos) para relatar um sinal na própria casa ou perto dela, apesar do medo de remoção. E um líder do NUPDEC vai confirmar esses apertos em tempo útil. Se o anonimato percebido não basta (todo mundo se vê na viela) ou se ninguém aceita ser o filtro, o canal de subida fica mudo e o projeto vira só mais um alerta de cima para baixo, que já existe (40199, cell broadcast, sirenes). Riscos secundários, mais baratos de checar: a Defesa Civil local aceitar um piloto, e o abrigo ter quem faça check-in de madrugada.

**Experimento.** EXPERIMENTO "CAIXA DE PAPELÃO" (2-3 dias, custo menor que R$ 50)

Dia 1 (preparação, 3 h): montar uma caixa de papelão com 3 pictogramas grandes (trinca, porta, água barrenta) e, em cada um, uma fenda onde a pessoa deposita uma ficha colorida (o "aperto"). Ao lado, um placar escrito à mão ("avisos esta semana: _ / alguém foi ver: _"). Imprimir 5 selos de papel com régua. Escolher uma comunidade em encosta perto de algum aluno (Brasilândia/Capadócia de preferência, ou outra área R3/R4 conhecida da equipe) e combinar com um comércio, igreja ou associação para hospedar a caixa por 48 h. Mandar uma mensagem para a Defesa Civil da subprefeitura ou para um NUPDEC pedindo 20 minutos de conversa.

Dias 1-3 (campo):
(a) Caixa no ponto por 48 h, com um cartaz em linguagem simples: "Viu trinca nova, porta emperrando ou água barrenta? Coloque uma ficha. Não precisa dizer seu nome. Um vizinho do NUPDEC vai olhar." Contar as fichas por tipo e por horário, e anotar quantas parecem trote (criança, punhado de fichas de uma vez).
(b) Concierge do filtro: um líder ou voluntário topa receber no WhatsApp a foto da caixa duas vezes por dia e responder "fui ver / não fui". Medir o tempo de resposta.
(c) 6-8 entrevistas de 15 min com moradores no próprio ponto (roteiro abaixo), mostrando a caixa e um selo de papel e tocando no celular um áudio gravado: "Saia pela Escadaria Alta, a Escola Estadual está aberta." Pedir para a pessoa apontar no papel por onde sairia.
(d) 1 conversa com alguém da Defesa Civil, NUPDEC ou ACS, mostrando um cartão de setor impresso e a tela "escolher rota e assinar SAIR".

Critérios de sucesso (definidos antes de ir a campo):
- Pelo menos 5 de 8 moradores dizem que usariam a caixa NAQUELE local na frente dos vizinhos (se o local for o problema, testar outro local é o próximo passo, não desistir).
- Pelo menos 3 fichas não-trote em 48 h, ou pelo menos 1 morador que conte um sinal real que viu e não relatou.
- O dono do ponto aceita guardar a caixa por uma temporada de chuva.
- Pelo menos 1 líder do NUPDEC ou ACS aceita ser o filtro e responde em menos de 2 h no concierge.
- Pelo menos 6 de 8 entendem o áudio e apontam a rota certa sem ler.
- No máximo 2 de 8 associam o selo a remoção depois da explicação. Se mais pessoas associarem, o selo sai do MVP e a deduplicação passa a ser só por setor e ponto.
- A pessoa da Defesa Civil diz que usaria o cartão no lugar dos avisos soltos (nota ≥ 4/5) e que aceitaria escolher a rota.

Regra de decisão: se o anonimato percebido falhar (menos de 5 de 8), mudar o ponto (ex.: dentro da UBS ou da igreja) e testar de novo antes de escrever código de ingestão. O que for aprendido entra no README, no pitch e como evidência das 6 perguntas obrigatórias. Não registrar nome nem foto de rosto dos entrevistados, só idade aproximada, papel e falas anonimizadas.

**Roteiro para entrevista real.** ROTEIRO (15 min, mostrando a caixa de papelão, um selo de papel e o áudio; pedir consentimento verbal antes e não anotar nome nem endereço)

Para MORADORES (Dona Josefa):
1. "Me conta da última vez que a senhora viu alguma coisa estranha na casa ou na viela na época de chuva: trinca, porta emperrando, água barrenta. O que fez? Contou para quem?" (fatos passados, não opinião)
2. "Quando pensa em avisar a Defesa Civil ou a prefeitura sobre isso, o que passa pela sua cabeça? Já aconteceu alguma coisa com a senhora ou com algum vizinho depois de avisar?" (medo de remoção, experiência com código pintado na parede, 156 sem retorno)
3. [Mostrar a caixa] "Se esta caixa estivesse aqui no [mercadinho/igreja], a senhora colocaria uma ficha na frente dos vizinhos? Em que lugar da viela se sentiria mais à vontade? Onde não colocaria de jeito nenhum?"
4. [Mostrar o selo] "O pessoal do NUPDEC cola este adesivo com régua na trinca e tira foto toda semana. O que a senhora acha? O que precisaria acontecer para deixar colar? Quem teria que estar junto?"
5. [Tocar o áudio de madrugada] "Imagine que são 3 da manhã, chovendo e faltando luz. Como esse aviso chegaria até a senhora dentro de casa? Por onde a senhora sairia? Me mostra no papel." (testa sirene/voz, rota, abrigo)
6. "Tem alguém aqui perto que não conseguiria sair sozinho? Quem costuma ajudar essa pessoa hoje?" (sem pedir nome; testa o modelo vizinho guardião/ACS)
7. "O que faria a senhora parar de confiar nessa caixa?" (alarme falso, abrigo fechado, caixa quebrada, remoção)

Para DEFESA CIVIL / NUPDEC (Sandra, seu Valdir) e ACS (Edileuza), trocar as perguntas 3 a 6 por:
a. "Numa noite de chuva forte, como chegam os avisos até você hoje? Quantos? O que você faz com um aviso que não dá para confirmar?"
b. [Mostrar o cartão de setor impresso] "Se isso chegasse no lugar dos avisos soltos, o que falta para você decidir? O que sobra?"
c. "Quem confirmaria um aviso no local em 30 minutos de madrugada? O NUPDEC faria isso?"
d. "Você aceitaria escolher a rota de saída entre duas ou três cadastradas? Qual a sua responsabilidade legal nisso?"
e. "Quem cuidaria da caixa depois de dezembro? O que te faria desistir de pôr o nome da Defesa Civil nela?"
f. (ACS) "Como você sabe hoje quem é acamado ou eletrodependente na sua microárea? Topa que essa lista fique só com a UBS e o sistema conheça só um código?"

Encerramento: "Posso voltar na semana que vem para mostrar como ficou?" (vale como teste de retorno e de vínculo).

**Dataset (verificado: sim).** VERIFICADO DE FATO (abri os arquivos via HTTP, com range requests e leitura em streaming, sem guardar nada no projeto):

1) INMET, dados históricos de estações automáticas (FONTE PRINCIPAL DO ML). Página: https://portal.inmet.gov.br/dadoshistoricos. Um ZIP por ano de 2000 a 2026 (o de 2026 vai "até 31/10/2026"). URL exemplo: https://portal.inmet.gov.br/uploads/dadoshistoricos/2024.zip (HTTP 200, 102.772.199 bytes; o servidor exige User-Agent de navegador). Dentro de cada ZIP há um CSV por estação. Confirmei INMET_SE_SP_A701_SAO PAULO - MIRANTE_01-01-2024_A_31-12-2024.CSV (812 KB descompactado) e também A771 Interlagos. Formato: CSV com separador ';', decimal com vírgula, codificação latin1 e 8 linhas de cabeçalho de metadados (REGIAO SE, UF SP, ESTACAO SAO PAULO - MIRANTE, WMO A701, lat -23,4964, lon -46,6200, altitude 785,64 m, fundação 25/07/06). As colunas, uma linha por hora, são: Data (AAAA/MM/DD); Hora UTC (0000 UTC); PRECIPITAÇÃO TOTAL, HORÁRIO (mm); PRESSAO ATMOSFERICA AO NIVEL DA ESTACAO (mB), com MAX e MIN na hora anterior; RADIACAO GLOBAL (Kj/m²); TEMPERATURA DO AR - BULBO SECO (°C); TEMPERATURA DO PONTO DE ORVALHO; TEMPERATURA MÁX/MÍN e ORVALHO MÁX/MÍN na hora anterior; UMIDADE REL. MÁX/MÍN e UMIDADE RELATIVA HORÁRIA (%); VENTO DIREÇÃO (gr), RAJADA MÁXIMA (m/s) e VELOCIDADE HORÁRIA (m/s). Período útil da A701: 2006 a out/2026, cerca de 20 anos horários, o bastante para TimeSeriesSplit. A estação fica a cerca de 6-8 km da Brasilândia (estimativa minha pela coordenada). Responde à pergunta de ML revista? SIM: o alvo "acumulado de 72 h cruza o limiar nas próximas 3 h" é calculado sobre a própria série, e as features vêm das mesmas colunas (acumulados, pressão e tendência, umidade, orvalho, rajada, hora, mês). Cuidados: valores ausentes (campos vazios ou -9999), hora em UTC (converter para BRT) e o limiar (usar valor documentado do PPDC/CGE, com citação).

2) Atlas Digital de Desastres no Brasil, MIDR (VALIDAÇÃO QUALITATIVA, NÃO RÓTULO). https://atlasdigital.mdr.gov.br/paginas/downloads.xhtml → BD_Atlas_1991_2025_v1.1_2026.08.06_Consolidado.csv (86 MB, ';', latin1). As colunas confirmadas incluem Protocolo_S2iD, Nome_Municipio, Sigla_UF, Data_Registro, Data_Evento, Cod_Cobrade, descricao_tipologia, grupo_de_desastre, Cod_IBGE_Mun, Setores Censitários, Status, DH_MORTOS, DH_DESABRIGADOS, DH_DESALOJADOS, DM_Uni Habita Danificadas/Destruidas etc. Contagem feita por mim: São Paulo capital (IBGE 3550308) tem só 15 registros de COBRADE 11321 (Movimento de Massa) entre 1991 e 2025, o último antes de 2021. Os anos com registros vão de 1993 a 2020, com o último em 2020. O estado de SP tem 251 registros de 11321 no total, 29 desde 2014. Nos 8 municípios da RMSP vizinhos à Zona Norte que testei, são 31. CONCLUSÃO: os juízes que sugeriram usar o S2iD/Atlas como rótulo de deslizamento estão errados para a escala de bairro ou cidade. A classe é rara demais, então o Atlas serve apenas para a checagem "quantos desses eventos o modelo teria antecipado".

3) CEMADEN, pluviômetros e geotécnicas (NÃO VERIFICADO). Não consegui abrir uma série histórica do CEMADEN. A busca indica download via mapa interativo, com pedidos agendados disponíveis por 30 dias, e um webservice pouco documentado. Também não confirmei que exista estação geotécnica com umidade do solo perto da Brasilândia. Por isso o CEMADEN e a umidade do solo saem do ML obrigatório e ficam como trabalho futuro. O sensor de umidade do solo continua só no ESP32 (simulado no Wokwi, o que é permitido no IoT).

4) GeoSampa, setores de risco geológico (NÃO VERIFICADO diretamente). A Agência Pública (jan/2024) usou dados do GeoSampa (185.634 moradias em áreas de risco), o que indica que a camada existe, mas não abri o shapefile. Uso só para escolher o setor-piloto e para o mapa do pitch, não para o modelo.

Resumo: dataset principal verificado (INMET A701, CSV horário de 2006 a 2026, colunas confirmadas). Dataset de rótulo de deslizamento verificado e reprovado para treino (15 eventos na capital). CEMADEN não verificado.

**Concorrentes.** BRASIL
- Defesa Civil Alerta / cell broadcast (MIDR + Anatel + operadoras, nacional desde dez/2024; 805 alertas emitidos em 2025 segundo a Teletime) e o antigo SMS 40199. Fazem só a descida, de cima para baixo, sem relato do morador. Em junho/2026 uma invasão disparou alerta falso a celulares de vários estados, inclusive SP (Olhar Digital), o que reforça nosso comando SAIR assinado e a decisão humana autenticada. Diferença: somos o canal de subida anônimo e usamos o cell broadcast como canal de descida em vez de concorrer com ele.
- Sirenes comunitárias do Rio (desde 2011, Geo-Rio, com cerca de 1.800 agentes de saúde e 300 líderes treinados) e sirenes pontuais em SP. Também só descem. Sandra relata sirene desligada por ser tomada como trote. Diferença: a caixa fala a rua e o abrigo aberto, e recebe relato.
- Cemaden Educação e os pluviômetros comunitários: medem chuva com escolas e voluntários, sem canal de relato de sinal na casa e sem decisão. Diferença: são complementares e podem ser parceiros (escola como abrigo e ponto da caixa).
- NUPDEC e PPDC (Plano Preventivo de Defesa Civil de SP): a estrutura humana que já existe. Não é concorrente, é o nosso canal. Hoje funciona por WhatsApp, sem deduplicação nem filtro registrado.
- SP156 / 199 e apps de zeladoria (Colab etc.): pedem identificação e não dão retorno visível. Cléber desistiu de três pedidos "finalizados". Diferença: anonimato por construção, mais o placar público de retorno.
- Totens de segurança de Porto Alegre (botão que liga ao CEIC): mostram que o totem urbano com botão é viável e que trote é real, a ponto de terem aviso de que trote é crime. São de segurança, não de risco geológico, e não são anônimos.
- Redes comunitárias de WhatsApp (ex.: Alerta Santa Marta no Rio, e o grupo da Edileuza): informais, sem deduplicação e com risco de vazar dados pessoais.

MUNDO
- Pahiro Alert (Plan International, Nepal): app de alerta e resposta a deslizamento. Depende de smartphone e é sobretudo de descida. Diferença: não exigimos celular de quem relata.
- Sistemas comunitários de alerta precoce no Nepal (Dolakha, 2018, 495 pessoas de 117 famílias evacuadas segundo a Plan) e Landslide-EVO (oeste do Nepal, relatos por SMS). Provam que o relato comunitário funciona, mas usam celular e identificação.
- Medellín (Colômbia, Univ. de Edimburgo): moradores de assentamento informal treinados para reconhecer sinais precoces e relatar por app. É o mais próximo do nosso insight, mas depende de app e não trata do medo de identificação.
- Smart Landslide Information System (HKUST, Hong Kong): sensores e modelo, sem relato cidadão.
- Ushahidi / FixMyStreet: crowdsourcing geral por web e app, com identificação opcional e sem hardware offline nem decisão humana com protocolo.

Posicionamento: não encontrei, nas buscas desta rodada, nada que combine relato anônimo físico sem celular, filtro comunitário, deduplicação por selo, retorno público e descida por voz local com decisão humana assinada. O insight do medo de remoção é o nosso território próprio, e a tecnologia de descida a gente reaproveita.

Fontes consultadas: portal.inmet.gov.br/dadoshistoricos; atlasdigital.mdr.gov.br/paginas/downloads.xhtml; apublica.org/2024/01/sao-paulo-tem-quase-200-mil-moradias-em-area-de-risco-de-deslizamento-de-terra; apublica.org (Posicionamento Defesa Civil 21/01/2026, 'uma pessoa morre a cada 3 dias'); teletime.com.br/08/01/2026/defesa-civil-e-abr-telecom-emitiram-805-alertas-de-risco-em-2025; olhardigital.com.br/2026/06/20/seguranca/policia-federal-vai-investigar-falso-alerta-enviado-pela-defesa-civil; jb.com.br (sirene Rio 2011); canalrural.com.br (sistema de alerta Rio); prefeitura.poa.br/smseg (totens de segurança); plan-international.org/nepal/what-we-do/emergency-response/pahiro-alert; cahss.ed.ac.uk (app Medellín); ui.adsabs.harvard.edu (Landslide EVO); hkjcdpri.org.hk/node/450; plataforma-clima-ambiente-readthedocs.readthedocs.io/en/latest/cemaden_data.html.

### Protótipo de baixa fidelidade

PROTÓTIPO DE BAIXA FIDELIDADE DO MVP (leia como se estivesse usando)

=== A. A CAIXA (ESP32/Wokwi), no mercadinho do seu Zé, na Viela 3 ===
Tela física em repouso: 3 botões grandes com desenho (TRINCA NA PAREDE / PORTA EMPERRANDO / ÁGUA BARRENTA). Embaixo, o semáforo com o VERDE aceso. Acima, o placar: "AVISOS ESTA SEMANA: 4 · VISTORIA: QUI · FEITA: SIM". Na lateral, adesivo com "Funciona 3 dias sem luz" e "Não precisa dizer seu nome".
Passo A1: Dona Josefa SEGURA "TRINCA" por 3 s. O botão pisca 3 vezes e a voz diz baixinho "Recebido. Um vizinho vai olhar." O placar vai para 5. (Toque rápido não conta: a criança que bate no botão não gera nada. Segundo aperto do mesmo botão em menos de 10 min: pisca, mas não conta.)
Passo A2 (bastidor): a caixa publica pelo MQTT {totem: V3, botão: trinca, ts, chuva_mm_h: 34, umidade: 71}. Sem rede, guarda no buffer. Se houver 2 sinais diferentes, ou sinal + chuva acima do limiar, o semáforo vai sozinho para AMARELO e o display mostra "ATENÇÃO". Vermelho, nunca sozinha.
Passo A3 (depois da decisão SAIR): a luz fica VERMELHA, a seta da "ESCADARIA ALTA" acende e o buzzer toca 3 vezes. A voz repete a cada 2 min: "Atenção, Viela 3. Saia agora pela Escadaria Alta. A Escola Estadual está aberta." Se a escola não fez check-in, a voz diz só "Saia pela Escadaria Alta e siga o vizinho do NUPDEC", sem citar abrigo.
Passo A4 (queda de Wi-Fi): o display mostra "SEM REDE · AVISOS GUARDADOS: 3". O amarelo local continua e o vermelho só vem com comando assinado recebido.

=== B. APP EXPO "VIELA", só para quem tem papel (líder do NUPDEC, abrigo, guardião). Ícones grandes, alto contraste, botão de ouvir em toda tela ===
B1 Tela inicial (seu Valdir, líder): "Olá, Viela 3" e três cartões grandes: [!] "2 avisos para conferir", [régua] "Ler selos da semana", [casa] "Abrigos". A fila offline mostra "3 itens aguardando internet" quando não há sinal (SQLite).
B2 "Aviso para conferir": "TRINCA + ÁGUA BARRENTA · Viela 3 · há 12 min · chuva 34 mm/h". Botões: [FUI VER, É VERDADE] [FUI VER, NÃO ACHEI NADA] [NÃO CONSIGO IR]. Campo opcional "Observação (não escreva nomes)". Se ele digitar "a Josefa da casa 32, CPF 123.456.789-09, a mãe é acamada", o app mostra antes de enviar: "Tiramos nome, CPF e número da casa para proteger sua vizinha. Vai ficar assim: '1 pessoa com prioridade de saída, setor 3'." Botão [OK, ENVIAR].
B3 "Ler selo": [câmera] tirar foto do Selo 14, depois "Quantos milímetros a trinca está abrindo na régua?" com teclado numérico grande (ex.: 7). O app mostra "Semana passada: 4 mm" e a seta vermelha "piorou 3 mm". Não há campo de endereço.
B4 "Abrigo" (dona Cida, que tem a chave da escola): botão gigante [ESTOU AQUI E A ESCOLA ESTÁ ABERTA], com "Vagas aproximadas: 40" e um contador "Seu check-in vale por 2 h". Notificação a cada 2 h durante PREPARAR/SAIR: "A escola continua aberta?"
B5 "Guardião" (vizinho do idoso, visão de quem recebe): ao SAIR, toca o alarme do celular com o texto "P-07 precisa de ajuda para sair · Setor 3". Botões [JÁ SAIU] [PRECISO DE AJUDA]. O app não sabe quem é P-07: só a ACS, na UBS, tem essa lista no caderno ou no e-SUS.

=== C. MESA DE DECISÃO (web simples consumindo a API .NET), Sandra na Defesa Civil, de madrugada ===
C1 Login com usuário e senha mais segundo fator (é quem assina SAIR).
C2 "Setores agora": lista de cartões, um por setor, com cor (verde, amarelo, vermelho). No topo, "17 avisos recebidos → 3 cartões". Cada cartão diz, por exemplo: "VIELA 3 · 2 sinais CONFIRMADOS pelo NUPDEC · Selo 14: +3 mm na semana · Chuva 72 h: 61 mm. Modelo: cruza o limiar em até 3 h. Regra atual: ainda não. · Prioridades no setor: 2 (códigos)". Abaixo, o texto do LLM depois do taint: "Dois vizinhos confirmaram trinca e água barrenta perto da escadaria. Nenhum dado pessoal foi incluído."
C3 Detalhe do cartão: linha do tempo do Selo 14 (4, 4, 4, 7 mm) e a lista de apertos por hora, sem identificação. Selo "não confirmado" aparece em cinza.
C4 Decisão: três botões [MONITORAR] [PREPARAR] [SAIR]. Ao clicar SAIR, abre "Escolha a rota" com 2 ou 3 rotas já cadastradas com o NUPDEC (Escadaria Alta → Escola Estadual, check-in 14 min atrás; Rua de Baixo → Igreja, SEM check-in, apagada). Ela escolhe, confirma com o segundo fator e vê "Comando assinado enviado à caixa V3 · válido por 5 min". O painel também gera um "Texto sugerido para o Defesa Civil Alerta", para ela colar no sistema oficial. Não disparamos cell broadcast.
C5 Auditoria: "SAIR · Viela 3 · Sandra R. M. · 03:12 · rota Escadaria Alta · hash do comando". O log nunca contém nome, CPF ou casa de morador (teste automático).
C6 Dashboard "Risco Cidadão" (Cyber): para cada caixa, k de anonimato do setor (aviso "setor com menos de 5 casas: agregando com o Setor 4"), tentativas de PII bloqueadas por destino (voz da caixa, log, prompt, export) e explicações enviadas ao líder.

=== D. ONDE ENTRA CADA COISA NO FLUXO DA JOSEFA ===
Josefa vê a trinca (fita crepe) → segura o botão na caixa do mercadinho (A1, anônimo) → seu Valdir confirma e lê o selo (B2/B3) → Sandra vê um cartão e escolhe a rota (C2-C4) → a caixa fica vermelha, a seta acende e a voz fala a rua (A3); o celular do neto recebe o cell broadcast oficial; o guardião busca a vizinha acamada (B5); a escola está aberta porque a dona Cida fez check-in (B4) → no dia seguinte, o placar mostra "vistoria: feita" (retorno) → se cair a internet, a caixa segue no amarelo e guarda os avisos (A4).

=== E. COMO TESTAR ESTE PROTÓTIPO EM PAPEL ===
A caixa vira papelão com fichas. As telas B e C viram 9 folhas A4 desenhadas à mão (ou Figma de baixa fidelidade). O áudio da voz é gravado no celular. Pedir ao morador que "use" A1 e A3 e diga em voz alta o que entendeu, e à Defesa Civil ou NUPDEC que "decida" em C2-C4. Medir: entendeu sem ajuda? apontou a rota certa? confiaria? (critérios no experimento).

### Think-aloud das personas

**Josefa Maria da Conceição (Dona Zefa), diarista nordestina, mora em área de risco R4 em encosta e cria dois netos** (SUS 58, concluiu: sim, indicaria 6/10). Travou em: 1) Primeiro toque rápido no botão: não aconteceu nada, achei que tava quebrado. Ninguém avisa que precisa segurar 3 segundos, e não tem desenho de mão segurando nem uma voz dizendo "segure". 2) O placar e os adesivos são escritos, eu não leio isso, tive que pedir pra lerem. 3) A luz amarela: não sei o que fazer quando ela acende (arrumar a sacola? acordar os meninos? sair?). 4) A caixa fica dentro do mercadinho, que fecha de noite, e é de madrugada que a trinca abre e a água desce. 5) Não sei se a buzina e a voz chegam no fundo do barranco, onde eu moro, com chuva batendo na telha. 6) A voz manda pra escola, mas eu combinei com a comadre Lúcia. Não sei se posso ir pra outro lugar. Precisa mudar: 1) A caixa tem que ficar do lado de fora, com acesso a qualquer hora, inclusive de madrugada, e não dentro de um mercadinho que fecha. Ou ter mais de uma caixa perto das casas do barranco. 2) Ensinar a segurar o botão sem depender de leitura: desenho de mão segurando, luz que vai enchendo enquanto segura, voz dizendo "continue segurando" quando o toque for rápido. 3) Tudo que está escrito (placar, adesivos, ATENÇÃO) precisa de voz: um botão de "ouvir" na caixa que fale os avisos da semana, a vistoria e que ninguém precisa dar o nome. 4) Dizer com a voz o que fazer no AMARELO: "deixe a sacola perto da porta, fique atenta, não durma no quarto do barranco". 5) A sirene tem que chegar no fundo do barranco, então testem lá embaixo, com chuva. Se precisar, uma segunda luz ou buzina mais embaixo. 6) A voz do vermelho tem que aceitar que a gente vá pra casa de parente no plano: "saia pela Escadaria Alta. Se não tiver pra onde ir, a escola está aberta". 7) Retorno falado sobre o que eu avisei, e uma explicação clara, dada por alguém em quem a gente confia (associação, ACS, igreja), de que sair na emergência não quer dizer perder a casa.

> Vixe, deixa eu ver isso direito. Os meninos da faculdade trouxeram uma caixa de papelão com uns botão desenhado. Disseram que vai ficar no mercadinho do seu Zé, na Viela 3.

[Caixa em repouso] Eu olho e o que eu vejo primeiro é o desenho. Tem um da parede rachada, um da porta e um da água suja. Isso aí eu entendo sem ler, porque é o que eu vejo lá em casa. O farol verde também entendo, é que nem semáforo. Agora o placar lá em cima, "AVISOS ESTA SEMANA: 4 · VISTORIA: QUI · FEITA: SIM", isso eu não ia ler não. É letra miúda e é muita coisa. "Vistoria qui" é o quê, quinta? Precisei pedir pro menino ler. O adesivo de "não precisa dizer seu nome" eu também não ia ler sozinha. Se alguém me falar isso com a boca, aí sim me dá sossego.

[A1, apertar TRINCA] Semana passada mesmo eu vi trinca nova em cima da janela do quarto dos meninos e passei fita crepe pra ver se abre. Então eu vou lá e aperto o da parede. Bati uma vez rápido e não aconteceu nada. Fiquei achando que tava quebrado. Só depois a moça disse que tem que segurar três segundos. Ninguém me falou isso! Se eu vou lá às seis da manhã correndo pro ônibus, eu bato e vou embora achando que avisei. Aí segurei, piscou e a voz falou "Recebido. Um vizinho vai olhar." Gostei da voz, porque essa eu escuto. Mas "um vizinho" é quem? Se for o seu Valdir, tudo bem, eu confio nele. Se for gente da prefeitura vindo marcar minha casa, aí eu não aperto mais.

Outra coisa. O mercadinho do seu Zé abre sete horas e fecha nove da noite. A trinca e a água barrenta aparecem é de madrugada, quando chove. É uma da manhã, eu tô acordada enchendo balde e ouvindo a laje estalar. Vou descer a viela no escuro, com chuva, deixando os dois meninos sozinhos, pra apertar um botão no mercadinho? Não vou não. A caixa tem que ficar pra fora, onde dá pra chegar a qualquer hora.

[A2, amarelo sozinho] Me explicaram que se juntar dois avisos ou muita chuva, a luz fica amarela sozinha e aparece "ATENÇÃO". Isso eu acho bom, porque é todo mundo vendo junto. Mas amarelo quer dizer o quê pra mim? Arrumo a sacola? Acordo os meninos? Vou pra casa da Lúcia? Ninguém me disse o que fazer no amarelo. É que nem o código que a Defesa Civil pintou na minha parede: mostraram e não explicaram.

[A3, vermelho e voz] Agora isso aí sim. Luz vermelha, buzina três vezes, a seta mostrando a Escadaria Alta e a voz falando "Atenção, Viela 3, saia agora pela Escadaria Alta, a Escola Estadual está aberta." Isso eu entendo! Falou o nome da minha viela, falou por onde passar, que não é a viela que desmorona, e falou pra onde ir. É bem o que o SMS nunca faz. Mas eu moro lá embaixo no barranco. Daqui de casa eu escuto essa buzina? Com chuva batendo na telha, eu duvido. A sirene tem que ser alta e chegar no fundo do barranco, senão quem mora perto do mercadinho sai e eu fico.

E a escola. Eu combinei de ir pra casa da comadre Lúcia, que mora no plano. Se a voz manda pra escola, vou pra escola ou pra Lúcia? Acho que vou pra Lúcia mesmo. Pra mim tem que valer "saia pela escada", o lugar que eu vou é eu que sei.

Também queria saber se é alarme de verdade. Ano passado o vizinho gritou que o barranco ia cair, eu tirei os meninos da cama três horas da manhã, molhei todo mundo, e não caiu nada. A Emily chorou, e no outro dia o Kauã dormiu na aula. Se essa caixa tocar à toa duas vezes, na terceira eu não saio.

[B, app] Isso não é pra mim. O moço disse que é só pro seu Valdir, pra dona Cida da escola, pro vizinho que cuida do idoso. Melhor assim, porque meu celular não cabe mais nada e eu não ia mexer. Gostei de uma coisa que me contaram: se o seu Valdir escrever meu nome no celular, o aparelho apaga e só fica "1 pessoa com prioridade". Isso me dá mais confiança, porque o que eu tenho medo é de lista com meu nome indo parar na prefeitura.

[C, mesa da Defesa Civil] Essa parte da moça de madrugada eu só escutei explicarem. A Sandra lá olha um cartão e aperta "SAIR". Quer dizer que quem manda eu sair é a Defesa Civil? Aí já me dá um frio na barriga. E depois que eu saio, quem garante que eu volto? Quem garante que a casa não vai ser interditada e demolida? Isso o protótipo não fala em nenhum lugar.

[D, retorno] Gostei de uma coisa: no outro dia o placar mostra "vistoria feita". Eu nunca soube se alguém viu o que eu reclamei. Mas tinha que falar com a voz, não com letrinha. E queria que alguém falasse comigo da minha trinca: "olhamos, abriu tanto, faz isso".

No fim eu consegui apertar o botão, mas só depois que me explicaram que tem que segurar. E na hora do vermelho eu entendi a voz.

**Sandra Regina Mota, agente de Defesa Civil da subprefeitura, responsável pelos NUPDECs e pelas vistorias em áreas de risco** (SUS 66, concluiu: sim, indicaria 6/10). Travou em: 1) No cartão C2, travei no "Modelo: cruza o limiar em até 3 h / Regra atual: ainda não". São duas orientações contraditórias e eu não sei de onde vem o modelo nem quem responde se ele errar. 2) Na escolha da rota em C4, a rota sugerida (Escadaria Alta) passa perto do local do próprio aviso, e o sistema não me alertou sobre isso. 3) Depois de assinar o SAIR, fiquei sem saber se a caixa realmente recebeu e executou: aparece "enviado", não "confirmado". 4) Em B2, hesitei porque o botão "FUI VER" empurra o voluntário pra área de risco no meio da chuva, e eu não posso orientar isso. 5) No placar público da caixa, a contagem de avisos expõe o setor e pode desvalorizar as casas ou virar uso político. Precisa mudar: 1) Confirmação de execução da caixa (ack): trocar "comando enviado" por "caixa V3 confirmou: vermelho aceso", com alarme pra mim se a caixa não responder em 5 min. 2) Alerta automático quando a rota escolhida passa pelo ponto do aviso ou pelo setor afetado. 3) Tirar a contagem de avisos do placar público e deixar só "vistoria: feita", que gera confiança sem estigmatizar a viela. 4) O amarelo público precisa vir com uma instrução clara ("prepare-se, ainda não saia"). 5) Mostrar o modelo como apoio, com incerteza e fonte do limiar, nunca como uma segunda ordem. 6) Em B2, colocar o aviso "só olhe se for seguro" e transformar "prioridade de saída" num contador estruturado, em vez de só apagar o texto. 7) Mesa de decisão que funcione no celular, fora da rede da prefeitura. 8) Status de resposta dos guardiões (P-07 sem resposta há X min) e pelo menos duas pessoas com papel de abrigo por escola. 9) Um plano escrito de quem assume a manutenção depois da entrega (subprefeitura, ONG, NUPDEC), antes de instalar qualquer caixa.

> Tá, deixa eu ler isso como se fosse uma noite de chuva de verdade, porque é aí que vai dar certo ou errado.

A CAIXA: três botões com desenho, trinca, porta emperrando e água barrenta. Gostei, porque são exatamente os sinais que eu ensino na reunião da igreja. A Josefa segura 3 segundos, pisca, a voz fala "um vizinho vai olhar". Isso é bom: a pessoa sabe que alguém recebeu. No WhatsApp ela manda áudio às 2h da manhã e ninguém responde. Agora, o placar "AVISOS ESTA SEMANA: 4". Paro aqui. Esse número fica pendurado no mercadinho do seu Zé, à vista de todo mundo, inclusive de quem está vendendo casa ou de vereador fazendo campanha. Semana passada um corretor me ligou perguntando se o setor tal "era R4". Esse placar vira prova de que a viela é perigosa. Eu deixaria o "vistoria: feita" (isso me ajuda com a desconfiança, o pessoal acha que a Defesa Civil só aparece pra remover) e tiraria a contagem de avisos do display público.

A2, amarelo sozinho com dois sinais ou sinal mais chuva. "Vermelho nunca sozinha" é a frase que me fez continuar lendo. Mas pensa: a caixa acende ATENÇÃO numa viela às 23h. Em dez minutos eu tenho trinta mensagens perguntando "é pra sair?". O amarelo público precisa vir com uma frase do que fazer ("prepare documentos, fique atento, ainda não saia"), senão vira pânico ou, pior, vira costume e na terceira vez ninguém liga.

A3, a voz: "Saia agora pela Escadaria Alta, a Escola Estadual está aberta." Isso eu quero. É a última milha que o cell broadcast não tem, porque o cell broadcast chega pro neto mas não chega pra dona que não tem celular. E o detalhe de não citar o abrigo sem check-in é exatamente o tipo de coisa que eu já vi dar errado: mandaram o povo pra uma escola fechada em 2023, e as famílias ficaram na chuva no portão.

A4, sem rede: segue amarelo, guarda os avisos. Ok. "Funciona 3 dias sem luz": quero ver isso testado, não só escrito no adesivo.

O APP DO NUPDEC: B2, "FUI VER, É VERDADE". Aqui tenho um problema sério. Vocês estão pedindo pro seu Valdir, que tem 63 anos, sair na chuva forte e ir até a encosta conferir uma trinca. Eu não posso orientar voluntário a entrar em área de risco durante evento. Se ele escorrega na escadaria, de quem é a culpa? Precisa de um aviso claro: "olhe só se for seguro, não entre em casa com trinca nova", e o botão "NÃO CONSIGO IR" não pode parecer falha dele. O filtro de nome e CPF na observação é bom. Mas "1 pessoa com prioridade de saída, setor 3" é uma informação que eu preciso, e ela some do texto. Tem que virar um contador estruturado, não só ser apagada.

B3, régua no selo. Quem instalou o selo? Se for a minha equipe, ótimo, é o que eu faço com fissurômetro. Se for o estudante, não confio na leitura. E o voluntário lendo 7 mm na chuva, de lanterna... Precisa da foto obrigatória pra eu conferir de manhã. "Selo não confirmado em cinza" ajuda.

B4, dona Cida e o check-in de 2 horas. Na prática, a dona Cida dorme. A chave da escola na mão de uma pessoa só é ponto único de falha. Precisa de pelo menos duas pessoas com papel de abrigo.

B5, guardião com P-07 e a lista com a ACS. Essa é a parte mais honesta do protótipo, porque é exatamente o meu problema da planilha de acamados no celular pessoal. Mas a ACS não trabalha de madrugada. Se o guardião está viajando e ninguém responde, quem fica sabendo? Eu preciso ver no painel "P-07: sem resposta há 15 min".

A MESA, que é a minha parte. C1, login com segundo fator: ok, desde que funcione no celular. Meu notebook da prefeitura não abre nada fora da rede interna, e de madrugada eu estou em casa ou no carro. Se isso for só web pra desktop, eu não uso.

C2, "17 avisos → 3 cartões". Isso é o que eu peço há anos. O cartão da Viela 3 tem o que eu olho: confirmação do NUPDEC, selo, chuva acumulada de 72 h. Aí vem "Modelo: cruza o limiar em até 3 h. Regra atual: ainda não." Travo. São duas respostas diferentes na mesma tela, às 3h da manhã. Quem calibrou esse modelo? Com que dado? Se eu sigo a regra, não tiro ninguém e desliza, aparece no processo que "o modelo avisou". Se eu sigo o modelo e não acontece nada, perco credibilidade com a comunidade. Preciso saber de onde vem o limiar (é o do CEMADEN? do plano de contingência?) e o modelo tem que aparecer como apoio, com o grau de incerteza, não como uma segunda voz que manda.

C3, linha do tempo 4, 4, 4, 7 mm. Bom, é visual e rápido.

C4, eu clico em PREPARAR primeiro, depois em SAIR. Escolho a rota: Escadaria Alta → Escola, com check-in de 14 min. A da Igreja aparece apagada. Correto. Mas quem me garante que a Escadaria Alta não é justamente onde está a trinca? O cartão diz "trinca e água barrenta perto da escadaria"! Se a rota passa pelo ponto do aviso, o sistema tem que me alertar em vermelho na hora de escolher. Confirmo com o segundo fator: "Comando assinado enviado à caixa V3, válido por 5 min." Enviado não é recebido. Eu preciso de "CAIXA V3 CONFIRMOU: vermelho aceso, voz tocando". Se não confirmar em 5 min, eu ligo pro Valdir. Isso tem que estar na tela. O "texto sugerido pro Defesa Civil Alerta" é ótimo, porque me economiza digitar na pressa, e vocês não dispararem cell broadcast é o certo.

C5, auditoria com meu nome, horário e hash. Tudo bem, eu assino o que eu decido. Mas aí também precisa registrar o que eu vi quando decidi: quais dados estavam no cartão naquela hora. Se não, me julgam depois com informação que eu não tinha.

C6, Risco Cidadão, k de anonimato. Isso é pro pessoal de TI e pro jurídico. Eu não olho de madrugada, mas gosto que exista pra mostrar à procuradoria.

**Edileuza Pereira Lima, agente comunitária de saúde da UBS e liderança comunitária que administra o grupo de WhatsApp do bairro** (SUS 55, concluiu: não, indicaria 6/10). Travou em: No B5 (guardião) e no 'Prioridades no setor: 2 (códigos)' do C2. O protótipo diz que só a ACS tem a lista do P-07, mas não tem nenhuma tela para mim: não sei como cadastro o código, como ligo o código ao vizinho guardião, onde isso fica guardado, quem vê e quem atualiza quando o idoso muda, morre ou é internado. Também travei na caixa dentro do mercadinho: de madrugada ele está fechado, e quem vai apertar? E na contradição sobre o cell broadcast: no D o neto recebe, no C4 'não disparamos'. Precisa mudar: 1) Desenhar a tela da ACS, ou um fluxo combinado com a UBS, para montar a lista de prioridades e casar cada código com um guardião, com poucos cliques, sem senha que expira, e sem tirar dado do e-SUS. A hora gasta tem que contar como trabalho reconhecido, não virar meta. 2) Avisar o guardião também por SMS, ligação ou WhatsApp, não só pelo app. 3) Incluir os eletrodependentes (oxigênio) na prioridade de saída e em apagão junto com a enchente. 4) Pôr a caixa num lugar que funcione 24 h, ou ter um plano para a madrugada. 5) Corrigir a promessa do cell broadcast: deixar explícito que depende da Defesa Civil colar o texto. 6) Usar o grupo de WhatsApp do bairro como canal oficial de retorno ('vistoria feita'), com aviso fixado em áudio, que é onde o povo realmente está.

> Tá, deixa eu ver isso aqui com calma, que eu tô entre uma visita e outra. Começando pela caixa (A). Três botões com desenho: TRINCA, PORTA EMPERRANDO, ÁGUA BARRENTA. Isso eu entendo de cara, e a Josefa também entenderia, porque ela não lê direito, mas desenho ela pega. Segurar 3 segundos... olha, idoso com artrite segura sim, mas tem que ter alguém explicando no primeiro dia, senão ela dá um toque, não acontece nada e ela acha que quebrou. Eu colaria um adesivo bem grande escrito 'SEGURE ATÉ PISCAR', com áudio também. A voz dizer 'Um vizinho vai olhar' é bonito, mas é uma promessa. Se ninguém for olhar, no segundo aviso a Josefa não aperta mais. É igual ao SP156: você reclama, ninguém volta, você desiste. O placar 'VISTORIA: QUI · FEITA: SIM' é a parte que mais me agradou, porque é o retorno que a gente nunca tem.

Agora, uma coisa que me pegou: a caixa fica no mercadinho do seu Zé. O seu Zé fecha às 21h. A água desce na Viela 3 às três da manhã, que é quando a Sandra tá decidindo lá no painel. A caixa fica do lado de dentro ou de fora? Se for de fora, tem que aguentar chuva, e na primeira semana já vão tentar arrancar. Se for de dentro, de madrugada ninguém aperta nada. E 'Funciona 3 dias sem luz' é ótimo. Em dezembro do ano passado, no apagão, eu passei a noite ligando para a Enel por causa da dona Cecília, que usa concentrador. Se a caixa aguenta e a dona Cecília não aguenta, tem alguma coisa faltando aí.

Passo A3, caixa vermelha falando 'Saia pela Escadaria Alta, a escola está aberta': isso é claro, gostei que não cita abrigo se a escola não abriu. Já vi gente ir para a igreja que estava fechada e ficar na chuva.

App B: eu não sou líder do NUPDEC, então em tese eu nem tenho esse app. B2, a parte de tirar nome e CPF da observação, eu achei muito bom. Já vi print de prontuário rodando no grupo e quase morri. Se o seu Valdir escrever 'a Josefa da casa 32' e o app limpar, me protege também. B3, da régua do selo, não é comigo, mas é simples, só número.

B5, guardião. Aqui eu parei. 'P-07 precisa de ajuda para sair. O app não sabe quem é P-07: só a ACS, na UBS, tem essa lista no caderno ou no e-SUS.' Gente, a ACS sou eu. Então quem vai ligar o P-07 ao vizinho guardião? Eu. Com que tempo? Ninguém me perguntou. E tem mais: se o guardião sabe que P-07 é a dona Josefa, o código não protege nada. Se ele não sabe, ele recebe o alarme às três da manhã e vai buscar quem? Eu já faço isso no meu caderno: os nove que moram sozinhos e o telefone de um vizinho de cada um. O caderno funciona porque eu conheço o vizinho. Esse 'P-07' só funciona se eu fizer o casamento na mão, e aí vira mais uma planilha minha, sem hora reconhecida pela UBS. E o e-SUS não deixa eu tirar lista de paciente para mandar para app de faculdade, nem pode. Se vazar, quem responde sou eu.

Outra coisa: e quem não tem smartphone? Metade dos vizinhos que cuidam de idoso aqui tem celular de botão ou um Android velho sem memória. Alarme no app não chega neles. Tem que ter SMS ou ligação, ou mandar no WhatsApp, que é onde a gente vive.

Painel C da Sandra: não é para mim, mas li. '17 avisos → 3 cartões' é bom. 'Prioridades no setor: 2 (códigos)': de onde saíram esses dois? De mim de novo. E vi uma contradição: no D está escrito 'o celular do neto recebe o cell broadcast oficial', mas no C4 está 'Não disparamos cell broadcast', só um texto para a Sandra colar. Então o neto só recebe se a Sandra lembrar de colar às três da manhã. Isso tem que ficar claro, senão o morador acha que está coberto e não está.

Se a minha tarefa era entender como o meu povo prioritário sai na hora do SAIR, eu entendi o caminho, mas o pedaço que depende de mim é o que está menos desenhado.

**Cléber Andrade dos Reis, motorista de aplicativo que já desistiu do SP156 e de apps 'cidadãos' e desconfia de tecnologia do governo** (SUS 58, concluiu: sim, indicaria 5/10). Travou em: 1) No A1: eu daria um toque rápido e iria embora achando que tinha mandado. Não fica claro que precisa segurar 3 segundos, e o toque rápido não dá nenhum retorno dizendo "segure mais". 2) No placar "VISTORIA: FEITA: SIM": travei porque não sei quem marca isso. Se for a própria prefeitura, é igual ao "finalizado" do SP156. 3) No A3: não entendi como eu fico sabendo dentro de casa, com chuva e janela fechada, se a caixa está no mercadinho. E o alerta no celular depende da Sandra colar o texto no sistema oficial, então não sei se ele chega. 4) No B5: fiquei sem saber quem sabe que P-07 é a minha sogra, onde isso fica guardado e quem apaga. 5) Não achei botão nenhum para o meu problema real, árvore em cima do fio e apagão. Precisa mudar: 1) Placar com contestação do morador: além de "FEITA: SIM", um botão "NÃO VEIO NINGUÉM" na própria caixa, e o placar mostrando os dois números. Sem isso vira mais um "finalizado". 2) O toque curto tem que dar retorno ("segure o botão") e o desenho tem que dizer SEGURE, senão o apressado acha que mandou. 3) Alerta que chega dentro de casa: o A3 tem que ser alto, e precisa de um plano B se o cell broadcast não for disparado (SMS, ou sirene de verdade). Não dá pra depender de passar na frente do mercadinho. 4) Cartaz de privacidade colado na caixa e entregue pra família do P-07: quem sabe o código, onde fica guardado, como sai da lista. Linguagem de gente, não termo de uso. 5) Dizer quem mantém a caixa depois da GS (bateria, conserto, servidor), com nome e telefone. 6) Se puder, botão para queda de árvore e fio, e aviso de apagão para família com eletrodependente. É isso que tira gente da UPA em Itaquera.

> Tá, deixa eu ver isso aí. Primeira coisa que eu olho: não tem app pra baixar. Já ganhou dez segundos de mim, porque eu tenho quatro apps desinstalados no histórico do Play Store. Quatro, irmão.

TELA A, a caixa no mercadinho do seu Zé. Três botões grandes com desenho: TRINCA, PORTA EMPERRANDO, ÁGUA BARRENTA. Beleza, entendi na hora, é pra barranco, deslizamento. Só que vou ser sincero: lá em casa, em Itaquera, o meu problema foi árvore e fio. Cadê o botão "ÁRVORE TORTA EM CIMA DO FIO"? Se eu passo nessa caixa e não tem o meu problema, viro as costas. Mas tudo bem, finjo que moro na Viela 3.

Adesivo "Não precisa dizer seu nome". Isso aí me pegou, não vou mentir. Em 2024 abriram conta no meu CPF. Se não pede CPF nem selfie, eu aperto. "Funciona 3 dias sem luz": eu fiquei 52 horas sem luz em dezembro, então quero ver isso funcionando de verdade. Mas quem troca a bateria? O seu Zé? E se o seu Zé fechar o mercadinho às 20h, a caixa fica do lado de dentro ou do lado de fora? Chuva forte é de madrugada. Isso aí ninguém explicou.

A1: segurar TRINCA 3 segundos. Eu sou impaciente, ia dar um toque rápido e ir embora achando que mandei. O desenho tem que dizer "SEGURE", senão eu bato e saio. Aí pisca 3 vezes e a voz diz "Recebido. Um vizinho vai olhar." Olha, "um vizinho vai olhar" é melhor do que "protocolo 2025-blablabla finalizado". Mas é a mesma coisa que o SP156 me falou três vezes: recebido. Pra mim o que vale é o placar: "AVISOS ESTA SEMANA: 4 · VISTORIA: QUI · FEITA: SIM". Isso eu gostei de verdade. É a primeira vez que vejo alguém mostrar na rua, na cara de todo mundo, se fizeram ou não. Só que eu vou perguntar: quem marca "FEITA: SIM"? Se for a própria Defesa Civil apertando o botão, é igual ao SP156 marcando "finalizado" sem ter vindo ninguém. Eu quero que o morador possa dizer "não veio ninguém". Senão o SIM vira mentira pública.

A2 é bastidor, não me interessa muito. Entendi que fica amarelo sozinho se tiver chuva e dois avisos. Ok. "Vermelho nunca sozinha". Aqui fiquei com o pé atrás: se a Sandra estiver dormindo às 3 da manhã, ou o login dela der problema no segundo fator, o vermelho não vem e a encosta vem? Quem garante que tem alguém na mesa de madrugada? Já liguei pra Enel às 2h e fiquei 40 minutos na musiquinha.

A3: luz vermelha, seta da Escadaria Alta, buzzer, voz dizendo "Saia agora pela Escadaria Alta. A Escola Estadual está aberta." Isso eu entendo sem ajuda, e minha sogra de 80 anos também entende. É simples. Só que se eu estiver dentro de casa, com a janela fechada e chovendo forte, eu não escuto uma caixa no mercadinho. A voz baixinha do A1 tudo bem, mas no A3 tem que ser alto. E a minha sogra não sai de casa pra ver a seta. Quem tem que saber é eu, no celular. Vocês falam que o cell broadcast é o oficial, que vocês só sugerem o texto. Então na prática, se a Sandra não colar o texto no sistema, meu celular não toca. Entendi direito?

A4: "SEM REDE · AVISOS GUARDADOS: 3". Gostei que mostra. Pelo menos não finge que mandou.

TELA B, o app só pra líder. Eu não sou líder, não vou baixar mesmo. Olhei por curiosidade. B2 com "FUI VER, É VERDADE / NÃO ACHEI NADA / NÃO CONSIGO IR" é claro. O "não consigo ir" é honesto, gostei. Agora, B2 na parte do texto: o app apaga nome, CPF e número da casa. Bom. Mas o seu Valdir sabe quem é a Josefa, a rua inteira sabe. O app esconder no sistema não muda o fato de que o líder sabe. Para mim isso tá certo, é vizinho. O meu medo é outro: é o dado ficar num servidor e vazar.

B5, guardião: "P-07 precisa de ajuda para sair". Isso aqui é o meu caso, a Dona Iracema com o concentrador. Eu tenho medo de cadastrar ela como acamada ou eletrodependente e algum golpista saber que tem idosa sozinha em casa de dia, enquanto eu tô rodando e a Patrícia tá no serviço. Aqui vocês dizem que só a ACS da UBS tem a lista, no caderno. Isso me deixa mais tranquilo do que uma lista num app. Mas e se a ACS tirar férias ou mudar de posto? O vizinho guardião recebe "P-07" e precisa saber quem é P-07, senão não serve pra nada. Então em algum lugar alguém sabe que P-07 é a minha sogra. Quero ver isso escrito num papel que eu possa ler: quem sabe, onde fica guardado, quem apaga.

E outra coisa: a minha sogra não precisa só de ajuda pra sair. Ela precisa de energia pro concentrador. O apagão foi o meu problema maior, não a encosta. Nada aqui me avisa que a luz vai cair ou quanto tempo vai durar.

TELA C, mesa da Defesa Civil. Isso não é pra mim, mas eu li. "17 avisos recebidos → 3 cartões". "Modelo: cruza o limiar em até 3h. Regra atual: ainda não." Isso me preocupa: tem uma máquina dizendo que vai dar ruim e a regra dizendo que não. Quem decide? Se der ruim e ela não apertar, a culpa é de quem? A auditoria C5 com nome dela, hora e rota, essa eu gostei. Pelo menos dá pra cobrar depois. No meu caso da árvore ninguém assinou nada, ninguém tem nome.

C4, rota da igreja aparecendo apagada porque não teve check-in: inteligente, não manda o povo pra porta fechada. Já aconteceu na enchente do Jardim Pantanal de mandarem o povo pra lugar fechado, eu vi no grupo.

D, o fluxo inteiro: entendi. Mas são umas sete pessoas que têm que fazer a parte delas: Josefa, seu Valdir, dona Cida, Sandra, ACS, guardião, seu Zé. Se uma falhar de madrugada, o negócio trava no amarelo. É um projeto que depende de gente que é voluntária. Quem segura isso daqui a um ano, quando vocês se formarem?

Resumo do que eu fiz: consegui fazer o A1 de cabeça, entendi o A3 e saberia pra onde ir pela seta. Mas fui procurar o defeito e achei: a voz que não se escuta de dentro de casa, o "FEITA: SIM" que pode ser mentira, a dependência da Sandra acordada, e o apagão, que é o que me fodeu de verdade, não aparece.

### Banca FIAP simulada (nota 7)

**Memorável.** A frase "o aviso sobe sem nome e a rota desce sem celular" e o detalhe do abrigo que só entra na voz da caixa se alguém com a chave fez check-in nas últimas 2 h. É a única parte do pitch que nasce de uma dor concreta (a mãe na porta trancada com a criança no colo) e vira regra de sistema testável. Também vou lembrar da honestidade de ter aberto o Atlas, contado 15 eventos na capital e descartado o rótulo de deslizamento. Isso é raro em GS.

**Já visto?** No tema, muito parecido: todo semestre vejo umas dez propostas de alerta de encosta ou enchente, quase todas um app com mapa e push. Na arquitetura, é diferente. O canal de subida físico e anônimo, com filtro humano do NUPDEC, decisão humana assinada e a descida reaproveitando o cell broadcast em vez de competir com ele, eu vi pouquíssimas vezes. O que já vi muito é o resto: dashboard de risco, "LLM que gera texto", XGBoost sobre INMET e o placar de "retorno ao cidadão". A ideia central se destaca, mas o pacote ao redor é o de sempre.

**Perguntas difíceis.**
- Vocês dizem que o insight é o medo de remoção, mas ele ainda não foi validado com nenhum morador real. As personas, inclusive a 'rodada 2', são simuladas. O briefing pede uma pessoa real que convive com o problema. Faltando 14 dias, com quem vocês vão falar, quando, e o que muda no projeto se a Josefa real disser que não tem medo de remoção, e sim de não ser levada a sério?
- O Selo 14 está colado na parede de uma casa específica. O NUPDEC e a Defesa Civil sabem que 'Selo 14 abriu 7 mm' quer dizer 'a casa da Dona Fulana está trincando'. Ligar o selo ao 'ponto' em vez do endereço não muda isso. Isso não é um cadastro de casas condenáveis, que é exatamente o que vocês dizem que o morador teme? Como vocês respondem à própria pergunta da Josefa, 'o selo serve para remoção?'.
- A caixa fica no mercadinho do seu Zé, numa viela onde todo mundo se conhece. Ele, ou quem estiver no balcão, vê quem segurou o botão por 3 segundos. Em que sentido esse aviso é anônimo? Vocês testaram isso com alguém?
- O alvo do modelo é 'o acumulado de 72 h vai cruzar o limiar nas próximas 3 h'. Na prática isso é prever se vai chover forte nas próximas 3 h com uma estação a 6-8 km. Qual é o valor exato do limiar, com citação do PPDC/CGE para a capital? Quantos eventos positivos independentes existem em 20 anos depois de tirar as horas consecutivas do mesmo evento? Como vocês evitam vazamento entre janelas sobrepostas no TimeSeriesSplit (gap)?
- Se o modelo acerta, ele acertou chuva, não deslizamento. No cartão da Sandra está escrito 'Modelo: cruza o limiar em até 3 h'. Como garantir que ninguém (a agente, a banca, o cidadão) leia isso como 'probabilidade de deslizamento'? E se o XGBoost empatar com a regra dos 80%, qual é a conclusão para o cidadão?
- Na demo, a caixa 'fala o nome da rua'. O Wokwi simula buzzer, mas não toca voz sintetizada nem arquivo de áudio de forma confiável. O que exatamente vai rodar no vídeo de IoT, e o que vai ser edição? Se a voz não for real, vocês vão dizer isso no pitch?
- O comando SAIR tem validade de 5 minutos e nonce. Como o ESP32 sabe a hora certa sem NTP quando a rede está instável? Onde fica a chave HMAC na caixa? O que impede alguém que abre o gabinete no mercadinho de extrair a chave e assinar comandos para todas as caixas? Tem uma chave por caixa?
- A corroboração exige dois tipos de sinal, ou sinal mais chuva acima do limiar. A trinca que abre devagar, em semana seca, que é justamente o sinal precoce que vocês querem captar, nunca vira alerta. Isso foi uma decisão consciente? Onde está registrada?
- São duas APIs .NET, uma mesa web com segundo fator, um app Expo com três papéis e fila offline, ESP32 com HMAC, LLM, motor de PII com taint, dashboard, infraestrutura Linux e ML, tudo para cinco pessoas em duas semanas. Se só 60% ficar pronto em 19/11, qual fluxo de ponta a ponta vocês garantem funcionando, e o que cortam primeiro?
- O LLM gera o texto do cartão para a Sandra. Isso cabe num template de string com três variáveis. O que o LLM faz aqui que um template não faz? E se ele alucinar 'dois vizinhos confirmaram' quando só um confirmou, quem responde?
- Os números '15-18 avisos por noite', 'invasão de junho/2026' e 'uma pessoa morre a cada 3 dias' estão no pitch. Quais têm fonte primária que eu possa abrir agora, e quais vieram de persona simulada? O indicador 2 parte de uma linha de base que é ficção?
- Vocês listam cinco ODS. Defendam o 3.d e o 9.1 em uma frase cada, ou tirem.

**Veredito.** É uma das propostas mais maduras que vi neste tema, e por isso exijo mais dela. Dou 7 na fase atual, com teto real de 9 se resolverem três coisas até 19/11.

O que está muito bom:
(1) O desenho do sistema responde ao problema, e não à tecnologia. Subida anônima, filtro humano local, decisão humana autenticada e descida pelo canal oficial que já existe formam um posicionamento claro diante do 40199 e do cell broadcast.
(2) Os princípios de segurança são concretos e viram testes no Azure Boards: "vermelho nunca acende sem decisão assinada", "CPF nunca aparece em log nem em prompt". Isso é integração real entre Cyber, C#, IoT e QA, não um capítulo por disciplina.
(3) A honestidade com os dados. Abriram o INMET, abriram o Atlas, mostraram que o rótulo de deslizamento não se sustenta e puseram um baseline ao lado do modelo. Poucos grupos fazem isso.
(4) Fecharam detalhes que normalmente ninguém fecha: a lista de prioridade fica fora do sistema (P-07 com a ACS), o abrigo só é anunciado com check-in, a rota nunca é automática e a caixa cai para amarelo local sem rede.

O que me impede de dar nota de prêmio hoje:
(1) Falta a pessoa real. Todo o refinamento da v2 foi feito contra personas simuladas, e vocês mesmos admitem isso. O briefing é explícito, e a banca vai perguntar. Sem uma conversa gravada (com consentimento e sem identificação) com um morador da Brasilândia e, de preferência, com alguém de NUPDEC ou Defesa Civil, o "insight do medo de remoção" é hipótese de sala de aula. Façam o teste em papel da seção E com 5 pessoas reais e tragam o número do indicador 3. Isso vale mais que qualquer feature.
(2) Há uma contradição interna. O selo numerado cria exatamente o registro de casas trincadas que a premissa diz que o morador teme. Ou vocês enfrentam isso de frente (selo só agregado por setor na mesa, mapa selo→casa só no caderno do NUPDEC, fora do sistema, como fizeram com o P-07), ou tiram o selo do MVP. O mesmo vale para o anonimato físico no balcão do mercadinho, que precisa de resposta.
(3) O ML está tecnicamente correto, mas fraco na ligação com o problema. Prever que a chuva acumulada cruza um limiar é nowcast de chuva, e com poucos eventos independentes. Citem o limiar, deduplicquem eventos, usem gap no split e mostrem a matriz de confusão por evento, não por hora. Se empatar com a regra, digam isso com orgulho: "a regra atual já é boa; o valor está na subida do sinal humano". E tirem do cartão qualquer redação que soe como probabilidade de deslizamento.

Riscos de execução:
- O escopo ainda é grande para duas semanas.
- A voz "falando a rua" provavelmente não roda no Wokwi. Não prometam no palco o que vai ser edição de vídeo.
- O LLM está a um passo de ser enfeite. Ele só se justifica na explicação ao cidadão sobre o que foi redigido, que é o que a rubrica de Cyber pede, e não no texto do cartão.
- Reduzam os ODS para 11.5, 13.1 e 10.2.

Para o pitch de 4 minutos:
- Uma persona real, uma noite de chuva, os 3 atos da caixa, um número medido no teste com moradores e um slide de "o que aprendemos e mudamos".
- Nada de listar os 12 itens de "o que mudou".

Se voltarem com a entrevista real, o selo resolvido e a demo de ponta a ponta funcionando de verdade (botão, confirmação do líder, cartão, SAIR assinado, caixa vermelha, queda de rede e amarelo local), essa equipe está na disputa pelo prêmio.
