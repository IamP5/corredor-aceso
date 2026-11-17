# Prompt: discovery multiagente + vídeos pitch com poteto mode

Cole o bloco abaixo num chat novo do Claude Code, na raiz do repo. Troque o que está entre `<...>`.
O prompt reaproveita o que já existe no repo: `docs/briefing.md`, `docs/discovery/workflow-discovery.js`, a pesquisa em `pitch/research/`, o contrato `pitch/PLAYBOOK.md` e a tabela de vozes `pitch/VOICES.md`.

---

```text
/pstack:poteto-mode

Contexto
- Desafio: <nome do desafio, ex. GS CitizenTech FIAP 2026.2>.
- Fontes: <caminho do PDF> e <URL do site>. Se docs/briefing.md já existir, use ele e só confira se as fontes mudaram.
- Equipe de <N> pessoas, entrega em <data>. Hoje é <data>.
- Objetivo: um novo discovery que termine com as 5 MELHORES ideias ranqueadas, e um vídeo pitch de até 4:00 para cada uma, feito com Working Backwards para impressionar a banca, a imprensa e investidores.
- Ideias já exploradas (não repita, a não ser que o novo discovery mostre uma versão muito melhor e diga por quê): <Corredor Aceso, Vigília, Totem da Viela, ...>.
- Eu autorizo a orquestração multiagente (Workflow e subagentes) e o gasto de créditos ElevenLabs descrito abaixo.

Parte A. Discovery (Workflow)
1. Leia as fontes e consolide em docs/briefing.md: desafio, perguntas obrigatórias, temas, regras (privacidade, dados), prazos e as regras do vídeo pitch. Todos os agentes leem esse arquivo, não as fontes.
2. Rode um Workflow com Double Diamond + Lean. Parta de docs/discovery/workflow-discovery.js e adapte. Fases, nessa ordem:
   - Descobrir: problemas reais por 8 a 10 lentes (temas do edital, públicos, lugares da cidade). Cada problema vem com evidência de fonte pública.
   - Personas: 8 a 10 personas brasileiras específicas (idade, bairro, rotina, tecnologia que usa). Sempre um cético que odeia apps e uma avaliadora da banca. Cada persona é um agente separado com a ficha no prompt.
   - Entrevistas: Mom Test (passado concreto), 5 porquês, mapa de empatia, JTBD.
   - Definir: oportunidades "Como poderíamos..." ligadas a falas das entrevistas.
   - Idear: 30+ conceitos de ângulos diferentes (tecnologia, modelo de serviço, inversão do problema, analogia de outra área).
   - Convergir: juízes independentes com pesos explícitos (disrupção 0,25, viabilidade no prazo 0,20, desejabilidade 0,15, pre-mortem 0,10, reação das personas 0,20, potencial de pitch 0,10). Viabilidade = a equipe constrói um protótipo que funciona até a entrega. Potencial de pitch = existe um "uau" de causa e efeito que cabe em 20 segundos de tela, um personagem e UM número com fonte.
   - Os 5 primeiros viram finalistas. TODOS os 5 passam pelas fases seguintes (no run anterior só 3 passaram, e o vídeo da 4ª e 5ª ficaria sem base).
   - Teste de conceito: personas relevantes e o cético reagem a cada finalista.
   - Validar (Lean): Lean Canvas, hipótese mais arriscada, experimento barato do mundo real, e se o dataset público do ML existe e foi aberto de verdade.
   - Teste de protótipo: telas e fluxo em texto, tarefa com as personas, SUS estimado, onde travaram.
   - Decidir: banca simulada dá nota e faz as perguntas difíceis. Ranking final de 1 a 5 com o motivo de cada posição.
3. Schemas estruturados em todo agent(), pipeline() como padrão, log() de tudo que foi cortado. Se eu pedir mudança no meio, pare, edite o script mantendo o prefixo e retome com resumeFromRunId.
4. Verifique você mesmo a principal evidência e o dataset de cada um dos 5 (abrir a fonte ou baixar o dado). Diga o que foi verificado e o que só um agente afirmou.
5. Exporte para docs/discovery/<AAAA-MM-DD>/ (não sobrescreva o discovery anterior):
   - 00-ranking.md: os 5 com nota, ranking, one-liner, ponto forte, risco e por que cada um ficou nessa posição.
   - ideias/<slug>.md, UM arquivo autocontido por finalista: ficha do edital, respostas às perguntas obrigatórias, como funciona, arquitetura, notas dos juízes, reação das personas (com nomes e linhas), Lean Canvas, protótipo, banca, fraquezas apontadas, números com fonte e o "uau" do pitch. É o único arquivo que o orquestrador do vídeo precisa ler.
   - 02-personas.md, 03-entrevistas.md, 04-oportunidades.md, raw.json.
6. Mostre o ranking numa mensagem curta e siga direto para a Parte B, sem esperar resposta. Se eu interromper com outra ordem, refaça só o que mudou.

Parte B. Vídeos pitch (5 orquestradores)
7. Pesquisa: pitch/research/ já tem o swarm sobre prompts e práticas de motion/vídeo com Claude (Remotion, alternativas, storytelling e Working Backwards, áudio e legendas, prompting de motion). Reuse. Só rode um novo /swarm se os relatórios tiverem mais de 60 dias ou se eu pedir.
8. Vozes: atualize pitch/VOICES.md com os 5 novos slugs. A ideia nº 1 usa eleven_v4. As outras 4 usam eleven_multilingual_v2. Cada vídeo com uma voz pt-BR diferente, escolhida com creative_list_voices pelo tom da história. Antes de despachar, gere UMA frase de teste com a voz da nº 1, baixe e meça com ffprobe para provar que o conector funciona. Use estimate_only se o total previsto passar de 20 mil créditos e me avise.
9. Despache 5 orquestradores em paralelo, numa única mensagem (Agent, general-purpose, model opus, effort high), um por ideia. Cada brief traz: o caminho do SKILL.md do poteto-mode, "siga pitch/PLAYBOOK.md à risca, sem editar", a linha dela em pitch/VOICES.md, o escopo de escrita pitch/<slug>/ e nada mais, o arquivo docs/discovery/<AAAA-MM-DD>/ideias/<slug>.md, o ângulo sugerido (o "uau" do discovery) e as fraquezas que o pitch precisa responder de frente. O relatório final de cada um traz o checklist do done predicate com ffprobe e ebur128 colados, a tabela do revisor, o ângulo, o número herói com a linha da fonte, a referência visual, os caminhos e o que falta para o time humano. Veredito VERIFIED, NOT VERIFIED ou INCONCLUSIVE.
10. Enquanto rodam, não faça o trabalho deles. Quando cada um terminar, verifique você mesmo no arquivo real:
    - ffprobe: 1920x1080, stream de áudio, duração até 240 s.
    - ebur128: entre -15 e -13 LUFS.
    - A narração é ElevenLabs de verdade: md5 e duração de public/vo/*.wav diferentes de public/vo-scratch/*.wav.
    - Abra out/review/sheet.png e confira identidade visual, rótulos "persona ilustrativa" e "dados simulados" e o crédito "Narração gerada por IA (ElevenLabs)" no end card.
    - Toda citação externa na tela confirmada ou marcada UNVERIFIED.
    Se algo falhar, despache um orquestrador NOVO com o brief original, o relatório anterior e o defeito exato. Não reaproveite o agente antigo.

Regras
- Nada de dado pessoal real. Personas, entrevistas, SUS e banca são simulados e isso tem que estar escrito nos arquivos e na tela do vídeo.
- Nenhum logo real (FIAP, prefeitura, SUS), nenhuma imitação de pessoa ou órgão real, nenhum vídeo gerado por IA de pessoas.
- Nenhum número inventado: todo número na tela aponta para arquivo:linha ou fonte pública em NUMBERS.md.
- Termine com um plano de validação real por ideia (1 pessoa de verdade, experimento Lean barato, data de decisão).
- Responda em português. Na resposta final quero: o ranking dos 5 com nota, como funciona, ponto forte e risco de cada um, o caminho e a duração de cada vídeo com as medidas que VOCÊ conferiu, os créditos ElevenLabs gastos, e o que o time humano ainda precisa fazer (nomes e rostos no slide do time, ouvir a narração inteira, URL do repo e QR, validação com gente real, música opcional).
```

---

## Por que o prompt é assim

- **Personas desde o início.** No primeiro run elas entraram no meio e foi preciso parar e retomar.
- **Briefing único.** Os agentes leem docs/briefing.md em vez de cada um ler o PDF. Gasta menos e todos partem do mesmo resumo.
- **Pesos explícitos, agora com potencial de pitch.** Sem pesos, o juiz premia a ideia mais bonita. O critério de pitch entrou porque o "uau" de 20 segundos (a lista de 40 nomes que vira 3) foi o que deu força ao roteiro da vencedora.
- **Os 5 finalistas passam por Lean e protótipo.** No run anterior só os 3 primeiros passaram. Sem isso, o orquestrador do vídeo da 4ª e 5ª não teria Lean Canvas, banca nem fraquezas para responder.
- **Um arquivo por ideia.** Os arquivos de ranking e Lean do run anterior tinham 200 KB com as ideias misturadas, e cada orquestrador precisou receber faixas de linhas. Um `ideias/<slug>.md` autocontido deixa o brief curto e evita erro de faixa.
- **Pasta datada.** O discovery novo não apaga o anterior, e os vídeos já feitos continuam apontando para as fontes certas.
- **Pesquisa reaproveitada.** O swarm de 5 pesquisadores já está em pitch/research/ e foi destilado em pitch/PLAYBOOK.md. Refazer custa tempo sem ganho, a não ser que as ferramentas mudem.
- **Working Backwards antes do código.** Cada orquestrador escreve PR/FAQ, os três ângulos (jornalista, investidor, banca) e o roteiro de 10 batidas antes de abrir o Remotion. É isso que faz o vídeo falar com imprensa e investidor, não só com a banca.
- **Vozes numa tabela.** A regra "nº 1 em v4, o resto em multilingual v2, uma voz diferente por vídeo" fica em pitch/VOICES.md, e o PLAYBOOK só aponta para ela. Assim o contrato não muda a cada run.
- **Números falados por extenso só na voz.** O multilingual v2 lê melhor "quatrocentos e oitenta e quatro mil" do que "484.232". Os orquestradores mantêm um `spoken.py` que gera o texto falado, e as legendas continuam com dígitos.
- **Verificação no arquivo real pelo chat principal.** Os relatórios dos orquestradores vêm confiantes. O chat principal confere ffprobe, loudness, se a voz é mesmo ElevenLabs e a contact sheet antes de dizer que está pronto.
- **Citações externas com portão.** No Vigília, uma matéria de jornal entrou na tela sem ninguém confirmar. Agora o PLAYBOOK exige confirmar ou marcar UNVERIFIED.
- **Orquestrador novo para correções.** Retomar um agente antigo por interrupção perde instruções. Um agente novo com brief consolidado é mais confiável.

## Custo de referência

- Discovery anterior: 138 agentes, cerca de 32 minutos e cerca de 10 milhões de tokens de subagentes. Com os 5 finalistas passando por Lean e protótipo, espere um pouco mais. Para baratear, peça 6 personas e 20 conceitos.
- Swarm de pesquisa: 5 agentes, de 1,5 a 3 minutos cada, cerca de 100 mil tokens cada. Na Parte B ele é reaproveitado.
- Um orquestrador de vídeo (medido no Vigília): cerca de 530 mil tokens, 208 chamadas de ferramenta e 80 minutos, com 5 rodadas de revisor. Cinco em paralelo dão por volta de 2,7 milhões de tokens. Renders cheios de 4 minutos em paralelo pesam no notebook, por isso o PLAYBOOK pede rascunho em meia escala até o portão final.
- ElevenLabs: cerca de 1 crédito por caractere. Um roteiro de 4 minutos custou 3.147 créditos. Cinco vídeos devem ficar perto de 16 mil créditos, mais algum retrabalho de cenas cortadas.
