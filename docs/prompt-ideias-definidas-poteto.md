# Prompt: ideias já definidas → discovery, entrevistas, Lean e vídeos pitch (poteto mode)

Use quando a equipe já escolheu as ideias. O Claude não inventa ideias novas: ele testa o problema de cada uma, entrevista personas, refina a solução, valida com Lean e produz um vídeo pitch de até 4:00 por ideia.

Rode num projeto ou pasta nova. Tudo o que é reaproveitado fica neste repo e é referenciado por caminho absoluto:

| O quê | Caminho |
|---|---|
| Script do Workflow (ideias definidas) | `kit/workflow-ideias-definidas.js` |
| Contrato de produção dos vídeos | `kit/PLAYBOOK.md` |
| Modelo da tabela de vozes ElevenLabs | `kit/VOICES.md` |
| Pesquisa sobre vídeo com Claude (swarm) | `pitch/research/` |
| Pipeline Remotion de referência | `pitch/totem-da-viela/video/` (tools/ e script/scenes.json) |
| Skills oficiais do Remotion | `~/.claude/plugins/cache/remotion/remotion/4.0.524/skills` |
| Skill do poteto mode | `~/.claude/plugins/cache/cursor-plugins/pstack/0.15.25/skills/poteto-mode/SKILL.md` |

Cole o bloco abaixo num chat novo do Claude Code, com a pasta nova como diretório de trabalho. Troque o que está entre `<...>`.

---

```text
/pstack:poteto-mode

Contexto
- Desafio: <nome do desafio>. Fontes: <caminho do PDF> e <URL>. Só você (o chat principal) lê as fontes; os agentes leem docs/desafio.md.
- Equipe de <N> pessoas, entrega em <data>. Hoje é <data>.
- Ideias já definidas pela equipe (não crie ideias novas; pode refinar ou pivotar a solução dentro do mesmo problema):
  1. <Nome>: <one-liner>. Problema: <frase>. Cidadão: <quem>. Lugar: <cidade/bairro>. Notas: <opcional>
  2. <...>
- Vídeos: um pitch de até 4:00 para <cada ideia | as N primeiras do ranking>, com Working Backwards, para impressionar banca, imprensa e investidores.
- Eu autorizo a orquestração multiagente (Workflow e subagentes) e o gasto de créditos ElevenLabs descrito abaixo.

Kit reaproveitado (não copie conteúdo de outros projetos além do que está aqui):
- Workflow: kit/workflow-ideias-definidas.js
- Contrato dos vídeos: kit/PLAYBOOK.md
- Vozes: kit/VOICES.md
- Pesquisa de vídeo: pitch/research/
- Pipeline Remotion de referência: pitch/totem-da-viela/video/ (copiar só tools/ e o formato de script/scenes.json; nunca cenas, tema, textos ou identidade visual)

Parte A. Discovery das ideias (Workflow)
1. Leia as fontes e escreva docs/desafio.md com: o desafio, o território, os cidadãos possíveis, os requisitos da solução, a regra de dados pessoais, o prazo, o tamanho da equipe e o formato do vídeo pitch.
2. Copie o script do Workflow para docs/discovery/workflow-ideias-definidas.js e rode com Workflow(scriptPath) passando args = { desafio: "<caminho absoluto de docs/desafio.md>", ideas: [ { name, oneLiner, problem, citizen, place, notes } ] }. Fases: Pesquisa do problema de cada ideia (evidência e dado público real), Personas (3 a 4 por ideia, mais um cético), Entrevistas Mom Test sobre o PROBLEMA sem mostrar a solução (5 porquês, mapa de empatia, JTBD), Definir (o problema se confirma? contradições, sinal de pivô), Refinar (ideia revista com as falas), Convergir (juízes: disrupção 0,25, viabilidade no prazo 0,20, desejabilidade 0,15, pre-mortem 0,10, reação das personas 0,20, potencial de pitch 0,10), Teste de conceito, Lean (todas as ideias), Teste de protótipo com banca simulada, Decidir (ranking e dossiê por ideia).
3. Se eu pedir mudança no meio, pare, edite o script mantendo o prefixo e retome com resumeFromRunId.
4. Verifique você mesmo a principal evidência e o dado público de cada ideia (abrir a fonte ou baixar o dado). Diga o que foi verificado e o que só um agente afirmou.
5. Exporte para docs/discovery/<AAAA-MM-DD>/:
   - 00-ranking.md: posição, nota, veredito do problema (confirmado, parcial, não confirmado), one-liner, ponto forte, risco, e ideia original vs refinada.
   - ideias/<slug>.md, um arquivo autocontido por ideia (é o único que o orquestrador do vídeo lê): original vs refinada e por quê, ficha, tecnologia mínima, juízes, falas das personas, Lean Canvas, experimento, protótipo, perguntas da banca, números com fonte e status de verificação, o "uau" de 20 s, o personagem e o número herói.
   - 02-personas.md, 03-entrevistas.md, raw.json.
6. Se alguma ideia tiver o problema "não confirmado", diga isso com clareza no ranking e na resposta, e não faça vídeo dela a não ser que eu peça. Mostre o ranking numa mensagem curta e siga direto para a Parte B, sem esperar resposta.

Parte B. Vídeos pitch
7. Copie o kit para pitch/: PLAYBOOK.md e VOICES.md. Leia a pesquisa em pitch/research/ só quando uma regra do PLAYBOOK precisar da fonte; não rode um novo swarm.
8. Preencha pitch/VOICES.md: a ideia nº 1 do ranking usa eleven_v4; as outras usam eleven_multilingual_v2; uma voz pt-BR diferente por vídeo, escolhida com creative_list_voices pelo tom da história. Antes de despachar, gere UMA frase de teste com a voz da nº 1, baixe e meça com ffprobe. Se o total previsto passar de 20 mil créditos, use estimate_only e me avise.
9. Despache um orquestrador por vídeo, todos numa única mensagem (Agent, general-purpose, model opus, effort high). Cada brief traz: o caminho do SKILL.md do poteto mode, "siga pitch/PLAYBOOK.md à risca, sem editar", a linha dela em pitch/VOICES.md, o escopo de escrita pitch/<slug>/ e nada mais, o arquivo docs/discovery/<AAAA-MM-DD>/ideias/<slug>.md, o pipeline de referência (copiar tools/, não cenas), o ângulo sugerido (o "uau" do dossiê) e as fraquezas que o pitch precisa responder de frente. O relatório final traz o checklist do done predicate com ffprobe e ebur128 colados, a tabela do revisor, o ângulo, o número herói com a linha da fonte, a referência visual, os caminhos e o que falta para o time humano, com veredito VERIFIED, NOT VERIFIED ou INCONCLUSIVE.
10. Enquanto rodam, não faça o trabalho deles. Quando cada um terminar, confira você mesmo no arquivo real:
    - ffprobe: 1920x1080, stream de áudio, duração até 240 s, nos três arquivos (pitch, PressCut, InvestorCut).
    - ebur128: entre -15 e -13 LUFS.
    - Narração ElevenLabs de verdade: md5 de public/vo/*.wav diferente de public/vo-scratch/*.wav.
    - Abra out/review/sheet.png: identidade visual própria, "persona ilustrativa", "dados simulados" e "Narração gerada por IA (ElevenLabs)" no end card.
    - Toda citação externa na tela confirmada ou marcada UNVERIFIED.
    Se algo falhar, despache um orquestrador NOVO com o brief original, o relatório anterior e o defeito exato.
11. Copie os vídeos aprovados e as legendas .srt para ~/Downloads/pitch-videos-<nome-do-projeto>/, numerados pela posição no ranking.

Regras
- Nada de dado pessoal real. Personas, entrevistas, SUS e banca são simulados e isso tem que estar escrito nos arquivos e na tela.
- Nenhum logo real, nenhuma imitação de pessoa ou órgão real, nenhum vídeo gerado por IA de pessoas.
- Nenhum número inventado: todo número na tela aponta para arquivo:linha ou fonte pública em NUMBERS.md.
- Termine com um plano de validação real por ideia (1 pessoa de verdade, experimento Lean barato, data de decisão).
- Responda em português. Na resposta final quero: o ranking com nota e veredito do problema, o que mudou em cada ideia por causa das entrevistas, o caminho e a duração de cada vídeo com as medidas que VOCÊ conferiu, os créditos ElevenLabs gastos, e o que o time humano ainda precisa fazer (ouvir a narração inteira, nomes e rostos no slide do time, URL do repo e QR, música opcional, licença do Remotion, validação com gente real).
```

---

## Por que o prompt é assim

- **Ideias entram prontas, o problema não.** O Workflow começa pesquisando e entrevistando sobre o problema de cada ideia, sem mostrar a solução às personas (Mom Test). Assim a entrevista pode derrubar a premissa em vez de só confirmar a ideia.
- **Refinar em vez de idear.** A fase Idear do discovery aberto foi trocada por Definir + Refinar: a solução pode mudar, o problema e o cidadão ficam. O dossiê mostra original vs refinada e as falas que motivaram a mudança.
- **Veredito do problema.** Cada ideia sai com "confirmado, parcial ou não confirmado". Ideia com problema não confirmado não ganha vídeo por padrão, porque pitch bonito de problema fraco é o pior resultado.
- **Kit por caminho absoluto.** O projeto novo não precisa copiar a pesquisa nem os vídeos antigos. Só PLAYBOOK e VOICES são copiados, porque são preenchidos por rodada.
- **Copiar as ferramentas, não as cenas.** O pipeline do Totem da Viela é a referência mais completa (scenes.json como fonte única, lint de rótulos de honestidade, final.sh que roda tudo). As cenas e o tema dele não entram, para cada vídeo ter identidade própria.
- **Lições dos três vídeos já feitos, agora no PLAYBOOK do kit:**
  - Transição por escurecimento até o fundo, dentro do silêncio da narração. O crossfade empilhava o texto de duas cenas.
  - Revisão com barra fixa e no máximo 4 rodadas. Cada nota 2 precisa citar a regra violada. Um vídeo levou 10 rodadas porque cada revisor novo inventava 2s novos.
  - Roteiro mirando 3:45, não 3:50. As vozes ElevenLabs leem mais devagar, e cada correção de texto custa uma geração.
  - Cortes de imprensa e investidor renderizados, não só marcados.
  - Checar a licença do Remotion antes de publicar.

## Custo de referência (medido nesta sessão)

- Discovery aberto anterior: 138 agentes, cerca de 32 minutos e 10 milhões de tokens. Com ideias definidas, o Workflow é menor: sem a fase de lentes, sem os 30+ conceitos e com personas proporcionais ao número de ideias.
- Um orquestrador de vídeo: de 530 mil a 605 mil tokens, de 80 a 127 minutos, de 189 a 244 chamadas de ferramenta.
- ElevenLabs: de 2.650 a 3.700 créditos por vídeo de 4 minutos, cerca de 1 crédito por caractere.
