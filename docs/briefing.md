# Briefing GS CitizenTech (FIAP 2026.2, Engenharia de Software, 3º ano Fev)

Fontes: PDF "GS CitizenTech - 3ES Fev - FIAP 2026" (40 slides) e https://experience.fiap.com.br/global-solution-citizen-tech.

## O desafio
- Escolher um problema real de uma cidade, perto da equipe, com uma pessoa real que convive com ele, e construir o protótipo de uma CitizenTech.
- "Comece pelo território, não pela tecnologia": observar, compreender, escutar.
- Uma única CitizenTech por equipe atravessa as 8 disciplinas. As partes conversam entre si.
- "O mais importante não é a complexidade da tecnologia, mas a profundidade com que você entende o problema e a capacidade de demonstrar resultados para as pessoas."
- "Comecem pequeno: uma solução simples que funciona vale mais que uma ideia enorme pela metade."
- Requisitos da solução: simples, segura, inclusiva, acessível, adequada à realidade; proteger dados; respeitar diferenças; continuar relevante fora de cenários ideais de infraestrutura e conectividade.
- Pontos de partida sugeridos: calor extremo, chuvas e encostas, trem/metrô/BRT, calçadas acessíveis, zeladoria do bairro, comércio local. Vale qualquer problema real da cidade.
- Áreas: mobilidade, segurança, saúde, educação, acessibilidade, moradia, energia, água, saneamento, produção de alimentos, conectividade, trabalho, emergências, serviços públicos, participação social.
- Cidadãos possíveis: metrópole, pequena cidade, produtor rural, estudante, idoso, PcD, trabalhador, empreendedor, agente público.

## Seis perguntas obrigatórias
1. Quem é o cidadão beneficiado? 2. Qual problema será resolvido? 3. Como a solução funcionará? 4. Quais tecnologias? 5. Quem participa do ecossistema? 6. Como o impacto será medido e ampliado?

## ODS (obrigatório identificar e explicar)
2, 3, 4, 9, 10, 11, 13.

## Ficha da CitizenTech (vai no README)
Nome, cidade e bairro, cidadão beneficiado, problema em uma frase, como funciona, o que cada parte faz, link do repo, indicador de impacto, ODS conectados.

## Combinados
Simples primeiro. Pensar em quem vai usar (contraste, linguagem simples). Repo organizado. IA como copiloto (prompts.md). Nada de dados pessoais reais (simulados ou públicos, com fonte). Todo mundo constrói.

## Regras e prazos
- Hoje: 05/11/2026. Entrega final: 19/11/2026 23h55 (cerca de 2 semanas). Equipe de até 5 alunos.
- Pitch (Prêmio melhor GS): vídeo de até 4 min no YouTube, enviado em Testing.

## Entregas por disciplina (a ideia precisa render bem em todas)
- SOA e Web Services: backend em qualquer linguagem. Domínio 3, banco de dados 3, migrações 1,5, Swagger 1,5, testes automatizados 1.
- Cybersecurity: motor de parsing de PII (CPF, saúde, senhas) 3, taint analysis bloqueando vazamentos 3, IA (LLM) traduzindo falhas técnicas em alertas claros ao cidadão 2, dashboard "Risco Cidadão" 2.
- C# Software Development: Web API .NET com POO 3, Web API 2,5, EF Core + LINQ 1,5, async/await 1, qualidade 2.
- IA & ML: pergunta de ML respondida com DADOS REAIS E PÚBLICOS (INMET, Metrô SP, Infosiga SP, SP156, IBGE etc). Dados simulados NÃO são aceitos. Um modelo (XGBoost, Random Forest, MLP, LSTM ou KMeans+PCA), 10+ combinações de 3 hiperparâmetros, CV no treino, métricas treino vs teste, conclusão para o cidadão.
- Mobile Development & IoT: app Expo (React Native + TypeScript, Expo Router), consome API ou mock, salva em banco, simples e acessível. Vídeo de até 3 min.
- Operating Systems: infra Linux provisionada por Shell Script hospedando uma app web da solução; monitoramento CPU/disco/memória como serviço systemd; logs de auditoria; teste de falha. Vídeo até 7 min.
- Physical Computing (IoT & IoB): circuito ESP32 no Wokwi com sensores, integrado à solução (não projeto paralelo). Vídeo até 4 min.
- Testing, Compliance & QA: features com critérios de aceite e test cases (positivos e negativos) no Azure Boards.

## Site FIAP (complementos)
- Boa proposta combina tecnologia, viabilidade e impacto. Pode ser app, plataforma, serviço, modelo de negócio ou combinação.
- Temas sugeridos extras: alertas precoces (enchente, deslizamento, incêndio, calor), zeladoria colaborativa, desperdício de água/energia/alimentos, tecnologia assistiva para idosos e PcD, saúde preventiva remota, assistentes digitais para direitos e benefícios, participação cidadã, conectividade em periferias, coordenação comunitária em emergências.
- Prêmios simbólicos (brindes, camiseta). Avaliação do prêmio pelo pitch.
