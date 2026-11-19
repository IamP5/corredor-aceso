// Uma disciplina = uma entrada de DISCIPLINAS. A página inteira sai daqui.
// Textos são HTML confiável escrito à mão; nada vem de fora.

const REPO = 'https://github.com/IamP5/corredor-aceso';
const arq = (caminho) => `${REPO}/blob/main/${caminho}`;
const pasta = (caminho) => `${REPO}/tree/main/${caminho}`;
const TOKEN = 'dev-acs-demo-01-so-no-notebook';
const API = 'http://localhost:5080';

const AMANHA = '$(date -v+1d +%F 2>/dev/null || date -d tomorrow +%F)';

const PASSOS_COMUNS = [
  { texto: 'Clone o repositório e entre na pasta.', cmd: 'git clone https://github.com/IamP5/corredor-aceso.git && cd corredor-aceso' },
  { texto: 'Suba tudo: banco, migrações, carga das 40 casas, API, simulador das 39 casas e app. Na primeira vez leva alguns minutos. Espere aparecer <code>corredor-api-1 ... Healthy</code>.', cmd: 'docker compose up --build' },
  { texto: 'Em outro terminal, leve o relógio da demo para as 08h de amanhã. O turno da ACS vai das 05h às 18h, e assim o roteiro funciona a qualquer hora.', cmd: `curl -s -X PUT ${API}/api/demo/relogio -H "Content-Type: application/json" -d "{\\"agora\\":\\"${AMANHA}T08:00:00-03:00\\"}"` },
  { texto: 'Declare o dia crítico, como o job de ML faz às 05h.', cmd: `curl -s -X PUT "${API}/api/dias-criticos/${AMANHA}" -H "Content-Type: application/json" -d '{"probabilidade":0.11,"motivo":"Noite quente","origem":"MANUAL"}'` },
  { texto: 'Espere uns 30 segundos e consulte o radar. Casas acamadas e com calor aparecem em <code>Vermelho</code>, aparelhos mudos em <code>Cinza</code>, e nenhuma casa fica verde sem sinal de vida.', cmd: `curl -s -H "Authorization: Bearer ${TOKEN}" "${API}/api/radar?microarea=3"` },
  { texto: 'No fim, pare tudo. O <code>-v</code> apaga o banco, e o próximo <code>up</code> começa do zero.', cmd: 'docker compose down -v' },
];

// Documentos que abrem no leitor, em página inteira: ?ler=<caminho>.
// O tools/montar-site.sh copia cada `leitura:` e confere os links e os desenhos de dentro.
const LEITURAS = [
  { grupo: 'Requisitos', docs: [
    { leitura: 'REQUISITOS.md', nome: 'Requisitos por disciplina', resumo: 'A ficha da CitizenTech e os requisitos de cada disciplina, com o local em que o projeto atende a cada um.' },
  ] },
  { grupo: 'Arquitetura', docs: [
    { leitura: 'docs/arquitetura/README.md', nome: 'Índice e desenhos', resumo: 'A ordem de leitura dos documentos e a lista dos desenhos.' },
    { leitura: 'docs/arquitetura/01-visao-geral.md', nome: '01 · Visão geral', resumo: 'O produto, os componentes, as decisões de projeto e as três cenas da demonstração.' },
    { leitura: 'docs/arquitetura/02-event-storming.md', nome: '02 · Event storming', resumo: 'Os eventos do domínio e os oito fluxos, passo a passo.' },
    { leitura: 'docs/arquitetura/03-modelo-de-dominio.md', nome: '03 · Modelo de domínio', resumo: 'Contextos, agregados, regras da situação da casa e contratos.' },
    { leitura: 'docs/arquitetura/04-modelo-de-dados.md', nome: '04 · Modelo de dados', resumo: 'Tabelas do Postgres, EF Core, retenção e SQLite do app.' },
    { leitura: 'docs/arquitetura/05-infra-e-entrega.md', nome: '05 · Infra e entrega', resumo: 'VM, provisionamento, CI, release e rollback.' },
  ] },
];
const DOCS = LEITURAS.flatMap((g) => g.docs.map((d) => ({ ...d, grupo: g.grupo })));
const ehLeitura = (c) => DOCS.some((d) => d.leitura === c);
const linkDoLeitor = (c, ancora = '') => `?ler=${c}${ancora}`;

const DISCIPLINAS = [
  {
    id: 'soa',
    requisitos: 'soa-e-web-services',
    nome: 'SOA e Web Services',
    entrega: 'Link do GitHub',
    total: '10 pontos',
    resumo: 'O módulo Cadastro é o único dono de dado pessoal: domínio próprio, schema próprio no Postgres, migrações e Swagger.',
    criterios: [
      { nome: 'Estruturação do domínio', pontos: '3,0', onde: `<a href="${pasta('api/src/Corredor.Cadastro/Dominio')}">Cadastro/Dominio</a>: <code>Idoso</code>, <code>Visita</code>, <code>AvisoAoVizinho</code>, <code>MensagemSaida</code>. Regra violada vira 422 ou 409.` },
      { nome: 'Conexão com banco de dados', pontos: '3,0', onde: `<a href="${arq('api/src/Corredor.Cadastro/Infra/CadastroDbContext.cs')}">CadastroDbContext</a>, schema <code>cadastro</code>, papel <code>corredor_cadastro</code> que não enxerga outros schemas.` },
      { nome: 'Migrações', pontos: '1,5', onde: `<a href="${pasta('api/src/Corredor.Cadastro/Infra/Migrations')}">Migrations</a>: <code>Esqueleto</code>, <code>Inicial</code>, <code>Gatilhos</code>, aplicadas duas vezes sem erro.` },
      { nome: 'Documentação dos endpoints', pontos: '1,5', onde: 'Swagger em <code>/swagger</code> e OpenAPI em <code>/openapi/v1.json</code>.' },
      { nome: 'Testes automatizados', pontos: '1,0', onde: `<a href="${pasta('api/tests/Corredor.Cadastro.Testes')}">Corredor.Cadastro.Testes</a>, com Postgres real pelo Testcontainers.` },
    ],
    passos: [
      { texto: 'Suba a solução do passo zero. O serviço <code>preparar</code> aplica as migrações com os papéis de cada módulo, como o deploy.', cmd: 'docker compose up --build' },
      { texto: `Abra <a href="${API}/swagger">${API}/swagger</a>. Clique em <strong>Authorize</strong> e cole o token da ACS. As rotas do Cadastro ficam agrupadas.`, cmd: TOKEN },
      { texto: 'Execute todos os testes. Este passo precisa do SDK do .NET 10 instalado. Em versões antigas do Docker, como o Desktop 4.22, prefixe o comando com <code>DOCKER_API_VERSION=1.43</code>.', cmd: 'dotnet test api/Corredor.slnx' },
    ],
    evidencias: [
      { nome: 'Print do Swagger: Cadastro', caminho: 'docs/evidencias/api/02-swagger-cadastro.png' },
      { nome: 'Prints do Swagger', caminho: 'docs/evidencias/api/' },
      { nome: 'Relatório da integração', caminho: 'docs/evidencias/integracao/RELATORIO.md' },
      { nome: 'README da API', caminho: 'api/README.md' },
    ],
  },
  {
    id: 'csharp',
    requisitos: 'c-software-development',
    nome: 'C# Software Development',
    entrega: 'Link do GitHub',
    total: '10 pontos',
    resumo: 'O Motor aplica as regras de contato em POO, fala com o Cadastro só por interfaces e consome o MQTT de forma assíncrona.',
    criterios: [
      { nome: 'POO, interfaces e injeção de dependência', pontos: '3,0', onde: `<a href="${arq('api/src/Corredor.Motor/Dominio/Regras.cs')}">Regras.cs</a>: a abstrata <code>RegraDeContato</code> e quatro regras. <a href="${arq('api/src/Corredor.Contratos/Portas.cs')}">Portas.cs</a> liga os módulos.` },
      { nome: 'Web API', pontos: '2,5', onde: `Minimal APIs em <code>api/src/*/Rotas/</code>, host em <a href="${arq('api/src/Corredor.Api/Program.cs')}">Program.cs</a>, com 201, 204, 401, 403, 404, 409 e 422.` },
      { nome: 'Entity Framework Core e LINQ', pontos: '1,5', onde: `<a href="${arq('api/src/Corredor.Motor/Infra/MotorDbContext.cs')}">MotorDbContext</a> e a ordem do radar em <a href="${arq('api/src/Corredor.Motor/Aplicacao/Consultas.cs')}">Consultas.cs</a>.` },
      { nome: 'Programação assíncrona', pontos: '1,0', onde: `Consumidor MQTT em <a href="${arq('api/src/Corredor.Motor/Infra/Mqtt.cs')}">Mqtt.cs</a> e <a href="${arq('api/src/Corredor.Motor/Infra/ServicosPeriodicos.cs')}">ServicosPeriodicos.cs</a>.` },
      { nome: 'Qualidade do código', pontos: '2,0', onde: `Nullable e aviso como erro em <a href="${arq('api/Directory.Build.props')}">Directory.Build.props</a>, um projeto xUnit por módulo e o <a href="${arq('.github/workflows/ci.yml')}">CI</a>.` },
    ],
    passos: [
      { texto: 'Compile a solução. Qualquer aviso interrompe o build. Este passo e o seguinte precisam do SDK do .NET 10 instalado.', cmd: 'dotnet build api/Corredor.slnx' },
      { texto: 'Execute os testes unitários e de integração de todos os módulos.', cmd: 'dotnet test api/Corredor.slnx' },
      { texto: 'Execute o passo zero completo e confira o radar ordenado, com vermelho antes de cinza.', cmd: `curl -s -H "Authorization: Bearer ${TOKEN}" "${API}/api/radar?microarea=3"` },
    ],
    evidencias: [
      { nome: 'Print do Swagger: Motor', caminho: 'docs/evidencias/api/03-swagger-motor.png' },
      { nome: 'Log da demo integrada', caminho: 'docs/evidencias/integracao/demo-completa.log' },
      { nome: 'Relatório da integração', caminho: 'docs/evidencias/integracao/RELATORIO.md' },
      { nome: 'README da API', caminho: 'api/README.md' },
    ],
  },
  {
    id: 'cyber',
    requisitos: 'cybersecurity',
    nome: 'Cybersecurity',
    entrega: 'PDF com a documentação e, separado, o ZIP ou o link do GitHub',
    total: '10 pontos',
    resumo: 'A Guarda mascara PII na entrada, bloqueia PII nas saídas, traduz o evento por LLM e mostra o farol Risco Cidadão. O PDF tem três partes: Source, Auditoria e Vídeo do Dashboard.',
    criterios: [
      { nome: 'Mapeamento de PII (motor de parsing)', pontos: '3,0', onde: `<a href="${arq('api/src/Corredor.Guarda/Rotas/InterceptadorDeEntrada.cs')}">InterceptadorDeEntrada.cs</a>, <a href="${arq('api/src/Corredor.Guarda/Dominio/MotorDeParsing.cs')}">MotorDeParsing.cs</a> e as regras em <a href="${arq('api/src/Corredor.Guarda/Regras/pii.json')}">pii.json</a>.` },
      { nome: 'Taint analysis', pontos: '3,0', onde: `<a href="${arq('api/src/Corredor.Guarda/Aplicacao/SaidaSegura.cs')}">SaidaSegura.cs</a>, <a href="${arq('api/src/Corredor.Guarda/Infra/ProvedorDeLogSeguro.cs')}">ProvedorDeLogSeguro.cs</a>, <a href="${arq('api/src/Corredor.Guarda/Rotas/FiltroDeResposta.cs')}">FiltroDeResposta.cs</a>.` },
      { nome: 'IA na remediação', pontos: '2,0', onde: `<a href="${arq('api/src/Corredor.Guarda/Aplicacao/Tradutor.cs')}">Tradutor.cs</a>: o evento técnico vira prompt, sem o dado, e o LLM responde em linguagem simples.` },
      { nome: 'Dashboard Risco Cidadão', pontos: '2,0', onde: `<a href="${arq('api/src/Corredor.Guarda/painel/index.html')}">painel/index.html</a>: farol verde, amarelo e vermelho com a mensagem traduzida.` },
    ],
    passos: [
      { texto: `Opcional: para a tradução vir do LLM, copie o <code>.env.example</code>, preencha <code>OPENROUTER_API_KEY</code> e suba a solução. A chave vem de uma conta no <a href="https://openrouter.ai">OpenRouter</a>, com créditos, em <a href="https://openrouter.ai/keys">openrouter.ai/keys</a>. O passo a passo está no <a href="${arq('README.md')}#chave-do-openrouter">README</a>.`, cmd: 'cp .env.example .env && docker compose up --build' },
      { texto: `Abra o painel em <a href="${API}/api/guarda/demo/painel">${API}/api/guarda/demo/painel</a> e mantenha a aba aberta. O farol inicia em verde.`, cmd: `open ${API}/api/guarda/demo/painel` },
      { texto: '<strong>Parsing de PII.</strong> Envie um CPF. A rota recebe <code>***.456.***-00</code>, e o log só tem a máscara.', cmd: `curl -s -X POST ${API}/api/guarda/demo/documento -H 'Content-Type: application/json' -H 'Authorization: Bearer ${TOKEN}' -d '{"documento":"123.456.789-00"}'` },
      { texto: '<strong>Taint, resposta sem autenticação.</strong> A rota que devolveria condições de saúde responde <code>403</code> sem token.', cmd: `curl -s -i ${API}/api/guarda/demo/condicoes` },
      { texto: '<strong>Taint, log.</strong> O nome sai do log da API como <code>[NOME]</code>.', cmd: `curl -s -X POST ${API}/api/guarda/demo/log-nome -H 'Content-Type: application/json' -H 'Authorization: Bearer ${TOKEN}' -d '{"nome":"Dona Aparecida"}' && docker compose logs api | grep -n 'NOME' | tail -3` },
      { texto: '<strong>Taint, canal inseguro e IA.</strong> A mensagem para o grupo do bairro sai <code>Bloqueada</code>, com explicação. Recarregue o painel: ele fica vermelho, com a frase traduzida.', cmd: `curl -s -X POST ${API}/api/guarda/demo/grupo-do-bairro -H 'Content-Type: application/json' -H 'Authorization: Bearer ${TOKEN}' -d '{"texto":"ele mora sozinho e toma diurético"}'` },
      { texto: 'Para reproduzir as quatro evidências, execute o roteiro da auditoria. Ele lê o log em arquivo, então roda com a API sem Docker, de <code>bash tools/demo.sh subir</code>.', cmd: `API=${API} LOG_DA_API=.demo/api.log bash docs/evidencias/cyber/reproduzir.sh` },
    ],
    evidencias: [
      { nome: 'Painel verde', caminho: 'docs/evidencias/cyber/painel-1-verde.png' },
      { nome: 'Painel amarelo', caminho: 'docs/evidencias/cyber/painel-2-amarelo.png' },
      { nome: 'Painel vermelho', caminho: 'docs/evidencias/cyber/painel-3-vermelho.png' },
      { nome: 'Auditoria: o que cada evidência prova', caminho: 'docs/evidencias/cyber/LEIA-ME.md' },
      { nome: 'CPF mascarado na entrada', caminho: 'docs/evidencias/cyber/evidencia-1-documento.txt' },
      { nome: 'Tradução pelo LLM', caminho: 'docs/evidencias/cyber/traducao-openrouter.txt' },
      { nome: 'Todos os arquivos da auditoria', caminho: 'docs/evidencias/cyber/' },
      { nome: 'Documentação de Cybersecurity', caminho: 'docs/entrega/cyber-documentacao.md' },
    ],
  },
  {
    id: 'ml',
    requisitos: 'ia-e-machine-learning',
    nome: 'IA e Machine Learning',
    entrega: 'Link do GitHub e vídeo de até 5 min',
    total: '10 pontos',
    resumo: 'Onze anos de INMET e SIH/DATASUS. O XGBoost foi testado contra a regra da noite quente e não a superou. O relatório registra esse resultado.',
    criterios: [
      { nome: 'Problema e dados reais', pontos: '2,0', onde: `<a href="${arq('ml/dados/README.md')}">ml/dados/README.md</a>: fonte, link, período e licença. Dados simulados não são aceitos.` },
      { nome: 'Escolha justificada do modelo', pontos: '1,0', onde: `<a href="${arq('ml/RELATORIO_ML.md')}">RELATORIO_ML.md</a>` },
      { nome: 'Preparação e divisão', pontos: '2,0', onde: `<a href="${arq('ml/corredor_aceso.ipynb')}">corredor_aceso.ipynb</a>` },
      { nome: 'Treino com parâmetros diferentes', pontos: '2,0', onde: '24 combinações de 3 hiperparâmetros, validação cruzada só no treino.' },
      { nome: 'Resultados em treino e teste', pontos: '2,0', onde: 'Notebook e relatório: PR-AUC 0,150 do XGBoost contra 0,130 do acaso.' },
      { nome: 'Conclusão para o cidadão', pontos: '1,0', onde: `<a href="${arq('ml/RELATORIO_ML.md')}">RELATORIO_ML.md</a>` },
    ],
    passos: [
      { texto: 'Monte a imagem com as dependências do notebook.', cmd: 'docker compose --profile ml build ml' },
      { texto: 'Execute o notebook do início ao fim. Ele roda sem baixar nada, a partir de <code>dados/processados/</code>.', cmd: 'docker compose --profile ml run --rm ml jupyter nbconvert --to notebook --execute --inplace corredor_aceso.ipynb' },
      { texto: 'Observe o job das 05h montar o pedido de dia crítico com o clima de 29/12/2025, sem enviar.', cmd: 'docker compose --profile ml run --rm ml python prever_hoje.py --dia-das-variaveis 2025-12-29 --seco' },
    ],
    evidencias: [
      { nome: 'Notebook executado', caminho: 'ml/corredor_aceso.ipynb' },
      { nome: 'Relatório de 2 a 4 páginas', caminho: 'ml/RELATORIO_ML.md' },
      { nome: 'Fonte e período dos dados', caminho: 'ml/dados/README.md' },
      { nome: 'README do ML', caminho: 'ml/README.md' },
    ],
  },
  {
    id: 'mobile',
    requisitos: 'mobile-development',
    nome: 'Mobile Development',
    entrega: 'Link do GitHub e vídeo de até 3 min',
    total: '6 critérios',
    resumo: 'App Expo da ACS com React Native, TypeScript e Expo Router. Mostra só quem precisa de visita, funciona sem internet e nunca exibe verde sem sinal de vida.',
    criterios: [
      { nome: 'Telas e navegação', pontos: '', onde: `Sete telas com Expo Router em <a href="${pasta('app/src/app')}">app/src/app</a>.` },
      { nome: 'Consumo de dados', pontos: '', onde: `Cliente tipado em <a href="${arq('app/src/api/cliente.ts')}">cliente.ts</a> e mock com os mesmos contratos.` },
      { nome: 'Banco de dados', pontos: '', onde: `expo-sqlite em <a href="${pasta('app/src/banco')}">app/src/banco</a>.` },
      { nome: 'Acessibilidade', pontos: '', onde: 'Rótulos, texto junto da cor e contraste de 4,5:1 medido por script.' },
      { nome: 'Aderência ao problema', pontos: '', onde: 'Radar de pendentes, caminho sem rede e casa cinza que não vira verde.' },
      { nome: 'Documentação', pontos: '', onde: `<a href="${arq('app/README.md')}">app/README.md</a> com a Ficha e prints.` },
      { nome: 'Vídeo de até 3 minutos', pontos: '', onde: '<a href="https://youtu.be/Jtd0-UYokfE">youtu.be/Jtd0-UYokfE</a>' },
    ],
    passos: [
      { texto: 'O passo zero já sobe o app no navegador, ligado à API.', cmd: 'docker compose up --build' },
      { texto: 'Abra <a href="http://localhost:8081">localhost:8081</a> e entre com o token da ACS e a microárea 3.', cmd: TOKEN },
      { texto: 'No celular, com o Node 22 instalado: instale o Expo Go e leia o QR code. Sem <code>EXPO_PUBLIC_API_URL</code>, o app usa o mock de 40 casas.', cmd: 'npm --prefix app ci && cd app && npx expo start' },
      { texto: 'Execute os testes e a medição de contraste. Também precisa do Node 22.', cmd: 'npm --prefix app test && npm --prefix app run contraste' },
    ],
    evidencias: [
      { nome: 'Radar', caminho: 'docs/evidencias/mobile/03-radar.png' },
      { nome: 'Visita sem internet', caminho: 'docs/evidencias/mobile/12-offline-visita-sem-internet.png' },
      { nome: 'Mensagem bloqueada', caminho: 'docs/evidencias/mobile/09-mensagem-bloqueada.png' },
      { nome: 'Todas as telas', caminho: 'docs/evidencias/mobile/' },
      { nome: 'Medição de contraste', caminho: 'docs/evidencias/mobile/contraste.txt' },
      { nome: 'README do app', caminho: 'app/README.md' },
    ],
  },
  {
    id: 'os',
    requisitos: 'operating-systems',
    nome: 'Operating Systems',
    entrega: 'Link do GitHub, vídeo de até 7 min e capturas de tela organizadas no repositório',
    total: '10 pontos',
    resumo: 'Infraestrutura e API validadas em VM Ubuntu 24.04 no Lima/VZ: provisionamento, limites padrão, falhas, watchdog e novo boot aprovados. <strong>A entrega ainda não está completa: faltam a demonstração visual do app da ACS consumindo a API da VM e o vídeo de até 7 minutos.</strong> A captura do painel Risco Cidadão é de Cybersecurity e não substitui essa demonstração. O Pages organiza as provas, mas sozinho não conclui a disciplina.',
    criterios: [
      { nome: 'Script de infra: hostname, rede, DNS e IP', pontos: '2', onde: `<a href="${arq('infra/01-rede.sh')}">01-rede.sh</a>. Registro na VM: <a href="${arq('docs/evidencias/os/vm/05-rede-e-dns.txt')}">05 · Rede e DNS com resolved ativo</a>.` },
      { nome: 'Script da aplicação', pontos: '2', onde: `<a href="${arq('infra/02-app.sh')}">02-app.sh</a>, <a href="${arq('infra/provisionar.sh')}">provisionar.sh</a> e <a href="${arq('infra/deploy.sh')}">deploy.sh</a>: dependências, usuários, diretórios, permissões, variáveis e serviço. Registros na VM: <a href="${arq('docs/evidencias/os/vm/01-provisionamento.txt')}">01 · Provisionamento</a> e <a href="${arq('docs/evidencias/os/vm/02-aplicacao.txt')}">02 · Aplicação</a>.` },
      { nome: 'Monitor com alertas de CPU, disco e memória', pontos: '2', onde: `<a href="${arq('infra/monitor.sh')}">monitor.sh</a>: verificações a cada 15 s, limites configuráveis e normalizações. Registro na VM: <a href="${arq('docs/evidencias/os/vm/07-monitor-e-alertas.txt')}">07 · Métricas e alertas</a>. Limites padrão: CPU 85%, RAM 85%, disco 80%.` },
      { nome: 'Monitor como serviço tolerante a falhas', pontos: '1', onde: `<a href="${arq('infra/03-monitor.sh')}">03-monitor.sh</a> e <a href="${arq('infra/systemd/corredor-monitor.service')}">corredor-monitor.service</a>: controle por systemctl, reinício e watchdog. Registros na VM: <a href="${arq('docs/evidencias/os/vm/08-recuperacao-e-watchdog.txt')}">08 · Recuperação</a>, <a href="${arq('docs/evidencias/os/vm/10-controle-e-erro.txt')}">10 · Controle</a> e <a href="${arq('docs/evidencias/os/vm/11-reinicializacao.txt')}">11 · Novo boot e início automático</a>.` },
      { nome: 'Logs de auditoria e de execução', pontos: '1', onde: `<a href="${arq('infra/lib/auditoria.sh')}">lib/auditoria.sh</a>: data, usuário, operação, resultado e saída completa. Registro na VM: <a href="${arq('docs/evidencias/os/vm/09-auditoria.txt')}">09 · Auditoria</a>, com erros controlados e resultados identificados.` },
      { nome: 'Funcionamento completo', pontos: '2', onde: `<a href="${arq('infra/teste-de-falha.sh')}">teste-de-falha.sh</a> e <a href="${arq('infra/verificar.sh')}">verificar.sh</a>. Registros na VM: <a href="${arq('docs/evidencias/os/vm/03-reprovisionamento.txt')}">03 · Zero mudanças</a> e <a href="${arq('docs/evidencias/os/vm/06-teste-de-falha.txt')}">06 · Seis cenários aprovados antes e depois do boot</a>. Serviços e API aprovados, e o app da ACS, sem mock, navegou contra a API da VM: <a href="${arq('docs/evidencias/os/vm/14-app-acs-integrado.txt')}">14 · App da ACS integrado</a>. O <a href="${arq('docs/evidencias/os/vm/README.md')}">relatório</a> delimita as provas; Multipass não foi executado.` },
    ],
    passos: [
      { texto: `No macOS, instale <a href="https://lima-vm.io/">Lima</a>, Git e SDK do .NET 10. Para demonstrar o cliente Web da ACS, use também Node 22.13 ou mais novo. Clone o repositório e entre na raiz. Os comandos abaixo rodam no notebook; <code>limactl shell</code> executa dentro da VM. Use um nome livre. A alternativa Multipass está no <a href="${arq('infra/README.md')}">guia</a>, mas não foi executada neste ensaio.`, cmd: 'limactl start --name=corredor-os-validacao --vm-type=vz --cpus=2 --memory=4 --disk=20 --plain --mount-none --containerd=none --tty=false template:ubuntu-24.04' },
      { texto: 'Busque a main dentro da VM e execute a entrada única de provisionamento Bash. O clone não inclui alterações locais ainda não publicadas. Sem release, a API fica pendente até o deploy do próximo passo.', cmd: 'limactl shell --workdir=/ corredor-os-validacao -- sudo git clone --depth 1 https://github.com/IamP5/corredor-aceso.git /opt/corredor/repo\nlimactl shell --workdir=/ corredor-os-validacao -- sudo bash /opt/corredor/repo/infra/provisionar.sh' },
      { texto: 'Na raiz do repositório no notebook, gere o pacote e implante sem depender de GitHub Release. Estes comandos são para Mac Apple Silicon; em Mac x64, troque todas as ocorrências de <code>linux-arm64</code> por <code>linux-x64</code>.', cmd: 'bash tools/empacotar.sh v0.0.1-vm linux-arm64\nlimactl copy dist/corredor-linux-arm64.tar.gz corredor-os-validacao:/tmp/corredor-linux-arm64.tar.gz\nlimactl shell --workdir=/ corredor-os-validacao -- sudo corredor deploy --arquivo /tmp/corredor-linux-arm64.tar.gz\nlimactl shell --workdir=/ corredor-os-validacao -- sudo corredor semear' },
      { texto: 'Confira o estado e a versão da aplicação pelo Nginx. A saúde fica na porta interna 5000; o Nginx esconde <code>/healthz</code> com 404 de propósito.', cmd: 'limactl shell --workdir=/ corredor-os-validacao -- sudo corredor status\nlimactl shell --workdir=/ corredor-os-validacao -- curl -fsS http://api.corredor.internal/api/versao' },
      { texto: 'Em outro terminal, abra este encaminhamento SSH local e mantenha-o ativo. A API ficará em <code>http://127.0.0.1:18080</code>, pelo Nginx da VM, sem publicação na internet. A rota <code>/painel/</code> é somente o painel de Cyber; ela não é o app da ACS.', cmd: 'ssh -F "$HOME/.lima/corredor-os-validacao/ssh.config" -o ExitOnForwardFailure=yes -L 127.0.0.1:18080:127.0.0.1:80 -N lima-corredor-os-validacao' },
      { texto: 'Na raiz do repositório, em mais um terminal, prepare o app da ACS apontando explicitamente para a API da VM. Abra <code>http://127.0.0.1:18081</code>, use o código de desenvolvimento documentado e a microárea 3. Valide o dia e o radar e registre as requisições HTTP 200 para <code>:18080/api/radar</code> e <code>/api/lista-de-nomes</code>. O frontend Web roda no notebook; o backend, Postgres e Nginx rodam na VM. <strong>Essa navegação e suas capturas ainda não foram comprovadas neste ensaio.</strong>', cmd: 'npm --prefix app ci\ncd app && EXPO_PUBLIC_API_URL=http://127.0.0.1:18080 EXPO_NO_TELEMETRY=1 npx expo start --localhost --port 18081' },
      { texto: 'No terminal original, consulte métricas, simule falha do monitor e carga de CPU, RAM e disco e consulte os registros. Rode somente nesta VM dedicada. Os cenários devem terminar sem FALHOU ou PULADO. <code>systemctl stop</code> é uma parada administrativa; o cenário monitor mata o processo para comprovar recuperação automática.', cmd: 'limactl shell --workdir=/ corredor-os-validacao -- sudo corredor logs monitor\nlimactl shell --workdir=/ corredor-os-validacao -- sudo corredor teste-de-falha monitor carga memoria disco\nlimactl shell --workdir=/ corredor-os-validacao -- sudo corredor logs alertas\nlimactl shell --workdir=/ corredor-os-validacao -- sudo corredor logs auditoria' },
      { texto: `Feche o encaminhamento com Ctrl+C, reinicie a VM e confira os serviços sem start manual. Para repetir todos os detalhes do ensaio, consulte o <a href="${arq('docs/evidencias/os/vm/README.md')}">relatório e reprodução</a>. Para o vídeo de até 7 min, use o <a href="${arq('docs/entrega/roteiro-os.md')}">roteiro</a>.`, cmd: 'limactl stop corredor-os-validacao\nlimactl start --tty=false corredor-os-validacao\nlimactl shell --workdir=/ corredor-os-validacao -- systemctl is-enabled corredor-api corredor-monitor\nlimactl shell --workdir=/ corredor-os-validacao -- sudo corredor status' },
    ],
    evidencias: [
      { nome: 'VM real · relatório, capturas e reprodução', caminho: 'docs/evidencias/os/vm/README.md' },
      { nome: 'App da ACS · radar contra a API da VM', caminho: 'docs/evidencias/os/vm/acs/03-radar.png' },
      { nome: 'App da ACS · visita na fila com a API parada', caminho: 'docs/evidencias/os/vm/acs/08-fila-api-fora.png' },
      { nome: 'App da ACS · fila enviada depois que a API voltou', caminho: 'docs/evidencias/os/vm/acs/09-fila-enviada.png' },
      { nome: '14 · App da ACS integrado à API da VM', caminho: 'docs/evidencias/os/vm/14-app-acs-integrado.txt' },
      { nome: 'Painel de Cyber na VM · captura auxiliar, não é o app ACS', caminho: 'docs/evidencias/os/vm/01-aplicacao.png' },
      { nome: '01 · Provisionamento na VM', caminho: 'docs/evidencias/os/vm/01-provisionamento.txt' },
      { nome: '02 · Aplicação, migrações e carga', caminho: 'docs/evidencias/os/vm/02-aplicacao.txt' },
      { nome: '03 · Reexecução sem mudanças', caminho: 'docs/evidencias/os/vm/03-reprovisionamento.txt' },
      { nome: '04 · Serviços, usuários e permissões', caminho: 'docs/evidencias/os/vm/04-servicos-e-permissoes.txt' },
      { nome: '05 · Hostname, rede, IP e DNS', caminho: 'docs/evidencias/os/vm/05-rede-e-dns.txt' },
      { nome: '06 · Testes de falha antes e depois do boot', caminho: 'docs/evidencias/os/vm/06-teste-de-falha.txt' },
      { nome: '07 · Monitor, métricas e alertas', caminho: 'docs/evidencias/os/vm/07-monitor-e-alertas.txt' },
      { nome: '08 · Recuperação e watchdog', caminho: 'docs/evidencias/os/vm/08-recuperacao-e-watchdog.txt' },
      { nome: '09 · Auditoria e logs de execução', caminho: 'docs/evidencias/os/vm/09-auditoria.txt' },
      { nome: '10 · Controle do serviço e erro registrado', caminho: 'docs/evidencias/os/vm/10-controle-e-erro.txt' },
      { nome: '11 · Reinicialização e início automático', caminho: 'docs/evidencias/os/vm/11-reinicializacao.txt' },
      { nome: 'Todos os registros e capturas da VM', caminho: 'docs/evidencias/os/vm/' },
      { nome: 'App da ACS · execução e integração com API', caminho: 'app/README.md' },
      { nome: 'Índice geral e histórico em contêiner', caminho: 'docs/evidencias/os/README.md' },
      { nome: 'Instalação e operação na VM', caminho: 'infra/README.md' },
      { nome: 'Roteiro do vídeo · gravação pendente', caminho: 'docs/entrega/roteiro-os.md' },
    ],
  },
  {
    id: 'iot',
    requisitos: 'physical-computing-iot-e-iob',
    nome: 'Physical Computing',
    entrega: 'Link do Wokwi, vídeo de até 4 min e integrantes.txt',
    total: 'IoT e IoB',
    resumo: 'Um ESP32 no corredor cuida da casa sozinho: luz de caminho, luz de emergência na falta de energia, aviso de calor, pedido de toque e detecção de queda pelo pingente. Tudo chega por MQTT ao painel web da agente de saúde. Sem rede, o nó guarda os eventos e reenvia com a hora original.',
    criterios: [
      { nome: 'Circuito ESP32 com sensores', pontos: '', onde: `<a href="${arq('iot/no/diagram.json')}">diagram.json</a> e <a href="${arq('iot/no/no.ino')}">no.ino</a>: presença, DHT22, tensão da tomada, pingente MPU6050, tela OLED, botão, LEDs e buzzer.` },
      { nome: 'Integrado à solução', pontos: '', onde: 'O nó publica os eventos do contrato MQTT do Corredor Aceso. O <a href="painel/">painel web</a> assina o mesmo broker e é a visão da ACS sobre a casa: situação, sensores, linha do tempo e os comandos de dia crítico, pedido de toque e encerrar alertas.' },
      { nome: 'Link do projeto no Wokwi', pontos: '', onde: '<a href="https://wokwi.com/projects/481069519743904769">wokwi.com/projects/481069519743904769</a>' },
      { nome: 'Painel web via MQTT', pontos: '', onde: '<a href="painel/">iamp5.github.io/corredor-aceso/painel</a>' },
      { nome: 'Vídeo de até 4 minutos', pontos: '', onde: '<a href="https://youtu.be/XH_M3QyMZsQ">youtu.be/XH_M3QyMZsQ</a>' },
      { nome: 'Nomes dos integrantes', pontos: '', onde: `<a href="${arq('integrantes.txt')}">integrantes.txt</a>` },
    ],
    passos: [
      { texto: 'Abra o projeto no Wokwi e, ao lado, o <a href="painel/">painel</a>. Inicie a simulação: o serial mostra <code>[pecas] tela ok, pingente ok</code> e <code>[mqtt] conectado</code>, e o painel muda para <strong>Tudo bem</strong>. Se o Wi-Fi do Wokwi fechar a conexão, reinicie a simulação.', cmd: 'https://wokwi.com/projects/481069519743904769' },
      { texto: 'Clique no PIR e em <em>Simulate motion</em>: a luz de caminho acende e o painel registra <strong>Passou no corredor</strong> como sinal de vida.', cmd: '' },
      { texto: 'Queda pelo pingente: no MPU6050, leve a aceleração Z de 1 para 2 g. A tela mostra <strong>QUEDA?</strong> com a contagem e o buzzer bipa. Sem toque no botão em 15 s, sai <code>QUEDA</code> e o painel fica vermelho. O botão verde cancela ou encerra o alarme.', cmd: '' },
      { texto: 'Gire o potenciômetro para baixo de 100 V: a luz de emergência acende e o painel mostra <strong>Sem energia</strong>. Ponha o DHT22 em 35 °C: a tela mostra <strong>CALOR</strong>. No painel, <strong>Declarar dia crítico</strong> acende o âmbar no Wokwi, e <strong>Encerrar alertas</strong> apaga.', cmd: '' },
      { texto: 'Desligue a chave de Wi-Fi, gere eventos e religue: o buffer reenvia com a hora original, e o painel marca cada um como guardado sem rede.', cmd: '' },
    ],
    evidencias: [
      { nome: 'Circuito no Wokwi, rodando', caminho: 'docs/evidencias/iot/final-2026-11-18/00-circuito-no-wokwi.png' },
      { nome: 'Painel: tudo bem', caminho: 'docs/evidencias/iot/final-2026-11-18/painel-01-tudo-bem.png' },
      { nome: 'Painel: passou no corredor', caminho: 'docs/evidencias/iot/final-2026-11-18/painel-02-passou-no-corredor.png' },
      { nome: 'Painel: possível queda', caminho: 'docs/evidencias/iot/final-2026-11-18/painel-03-possivel-queda.png' },
      { nome: 'Painel: queda avisada', caminho: 'docs/evidencias/iot/final-2026-11-18/painel-04-queda.png' },
      { nome: 'Painel: sem energia, luz de emergência', caminho: 'docs/evidencias/iot/final-2026-11-18/painel-05-sem-energia.png' },
      { nome: 'Painel: dia crítico', caminho: 'docs/evidencias/iot/final-2026-11-18/painel-06-dia-critico.png' },
      { nome: 'Painel: muito quente', caminho: 'docs/evidencias/iot/final-2026-11-18/painel-07-calor.png' },
      { nome: 'Painel: pedido de toque', caminho: 'docs/evidencias/iot/final-2026-11-18/painel-08-pedido-de-toque.png' },
      { nome: 'Painel: reenvio do buffer', caminho: 'docs/evidencias/iot/final-2026-11-18/painel-09-reenvio-do-buffer.png' },
      { nome: 'Serial da rodada', caminho: 'docs/evidencias/iot/final-2026-11-18/serial.txt' },
      { nome: 'Captura do broker', caminho: 'docs/evidencias/iot/final-2026-11-18/broker.txt' },
      { nome: 'Evidências do IoT', caminho: 'docs/evidencias/iot/README.md' },
      { nome: 'README do IoT', caminho: 'iot/README.md' },
    ],
  },
  {
    id: 'qa',
    requisitos: 'testing-compliance-e-quality-assurance',
    nome: 'Testing, Compliance e QA',
    entrega: 'PDF com o link do Azure Boards, prints e o link do pitch',
    total: '50% Features, 50% Test Cases',
    resumo: 'Features e Test Cases gerados a partir da arquitetura, com um conferidor que falha se alguma regra ou rota ficar sem caso positivo e negativo.',
    criterios: [
      { nome: 'Features com descrição e critérios de aceite', pontos: '50%', onde: `<a href="${arq('docs/qa/features.md')}">features.md</a>, critérios na forma dado, quando, então.` },
      { nome: 'Test Cases positivos e negativos', pontos: '50%', onde: `<a href="${arq('docs/qa/casos-de-teste.md')}">casos-de-teste.md</a>, com objetivo, preparação, entradas, saídas esperadas e passos.` },
      { nome: 'Professor como Admin no Azure Boards', pontos: '', onde: 'O link do projeto consta no PDF da disciplina.' },
      { nome: 'Vídeo de pitch de até 4 minutos', pontos: '', onde: '<a href="https://youtu.be/8zQQs5pIhz8">youtu.be/8zQQs5pIhz8</a>. O link também vai no PDF da disciplina.' },
    ],
    passos: [
      { texto: 'Confira a cobertura. O script sai com 1 se alguma regra, rota ou caso da arquitetura ficar sem Feature e Test Case.', cmd: 'python3 docs/qa/ferramentas/conferir.py' },
      { texto: `Consulte a rastreabilidade entre regra, Feature, Test Case e teste automatizado em <a href="${arq('docs/qa/rastreabilidade.md')}">rastreabilidade.md</a>.`, cmd: '' },
      { texto: 'No Azure Boards, as Features e os Test Cases estão cadastrados a partir de <code>docs/qa/azure/</code>.', cmd: '' },
    ],
    evidencias: [
      { nome: 'README do QA', caminho: 'docs/qa/README.md' },
      { nome: 'Features', caminho: 'docs/qa/features.md' },
      { nome: 'Rastreabilidade', caminho: 'docs/qa/rastreabilidade.md' },
      { nome: 'Importação das Features no Azure', caminho: 'docs/qa/azure/features.csv' },
    ],
  },
];

// ---- Estado: o que o professor já conferiu, salvo no navegador.

const CHAVE = 'corredor-aceso:conferido';
const conferidos = new Set(JSON.parse(localStorage.getItem(CHAVE) || '[]'));
const salvar = () => localStorage.setItem(CHAVE, JSON.stringify([...conferidos]));

// ---- Renderização

const el = (tag, attrs = {}, html = '') => {
  const n = document.createElement(tag);
  for (const [k, v] of Object.entries(attrs)) n.setAttribute(k, v);
  n.innerHTML = html;
  return n;
};

function botaoCopiar(texto, rotulo) {
  const botao = el('button', { type: 'button', class: 'cmd__copiar', 'aria-label': rotulo }, 'Copiar');
  botao.addEventListener('click', async () => {
    await navigator.clipboard.writeText(texto);
    botao.textContent = 'Copiado';
    setTimeout(() => (botao.textContent = 'Copiar'), 1500);
  });
  return botao;
}

// <pre><code> com o texto já posto; o realce chega depois, se a linguagem for conhecida.
function blocoDeCodigo(texto, lingua, classe) {
  const pre = el('pre', classe ? { class: classe } : {});
  const code = el('code');
  code.textContent = texto;
  pre.append(code);
  colorir(code, lingua);
  return pre;
}

function blocoDeComando(cmd) {
  if (!cmd) return null;
  const caixa = el('div', { class: 'cmd' });
  caixa.append(blocoDeCodigo(cmd, 'bash'), botaoCopiar(cmd, 'Copiar comando'));
  return caixa;
}

function listaDePassos(passos, alvo) {
  for (const p of passos) {
    const li = el('li', {}, `<p>${p.texto}</p>`);
    const c = blocoDeComando(p.cmd);
    if (c) li.append(c);
    alvo.append(li);
  }
}

function progresso(d) {
  const feitos = d.criterios.filter((_, i) => conferidos.has(`${d.id}:${i}`)).length;
  return { feitos, total: d.criterios.length };
}

function atualizarProgresso(d) {
  const { feitos, total } = progresso(d);
  const pct = Math.round((feitos / total) * 100);
  document.querySelectorAll(`[data-progresso="${d.id}"]`).forEach((n) => {
    n.querySelector('.barra__cheia').style.width = `${pct}%`;
    n.querySelector('.barra__texto').textContent = `${feitos}/${total}`;
    n.classList.toggle('completo', feitos === total);
  });
}

const barra = (id) => `<span class="barra" data-progresso="${id}"><span class="barra__trilho"><span class="barra__cheia"></span></span><span class="barra__texto"></span></span>`;

function secao(d, n) {
  const s = el('section', { class: 'bloco disciplina', id: d.id, 'aria-labelledby': `${d.id}-titulo` });
  s.innerHTML = `
    <header class="bloco__cabeca">
      <p class="sobretitulo">${String(n).padStart(2, '0')} · ${d.total}</p>
      <h2 id="${d.id}-titulo">${d.nome}</h2>
      <p class="disciplina__resumo">${d.resumo}</p>
      <p class="entrega"><span>Entrega</span>${d.entrega}</p>
      <p class="disciplina__requisitos"><a href="${linkDoLeitor('REQUISITOS.md', `#${d.requisitos}`)}">Requisitos da disciplina <span aria-hidden="true">→</span></a></p>
    </header>
    <div class="colunas colunas--disciplina">
      <div>
        <h3>Critérios ${barra(d.id)}</h3>
        <ul class="criterios"></ul>
      </div>
      <div>
        <h3>Como validar</h3>
        <ol class="passos"></ol>
      </div>
    </div>
    <h3>Evidências</h3>
    <div class="evidencias"></div>`;

  const ul = s.querySelector('.criterios');
  d.criterios.forEach((c, i) => {
    const chave = `${d.id}:${i}`;
    const id = `chk-${d.id}-${i}`;
    const li = el('li', { class: 'criterio' });
    li.innerHTML = `
      <input type="checkbox" id="${id}" ${conferidos.has(chave) ? 'checked' : ''}>
      <label for="${id}"><span class="criterio__nome">${c.nome}</span>${c.pontos ? `<span class="criterio__pontos">${c.pontos}</span>` : ''}</label>
      <p class="criterio__onde">${c.onde}</p>`;
    li.querySelector('input').addEventListener('change', (e) => {
      e.target.checked ? conferidos.add(chave) : conferidos.delete(chave);
      salvar();
      atualizarProgresso(d);
    });
    ul.append(li);
  });

  listaDePassos(d.passos, s.querySelector('.passos'));

  const ev = s.querySelector('.evidencias');
  for (const e of d.evidencias) ev.append(cartao(e));
  return s;
}

// ---- Gaveta de evidências
// Todo link para um arquivo ou pasta do repositório abre aqui. A árvore do repositório vem da API do GitHub,
// uma vez por sessão. O conteúdo vem da cópia em repo/ que o tools/montar-site.sh faz, quando o arquivo
// foi copiado, e do raw.githubusercontent.com quando não foi. O botão "Ver no GitHub" leva ao arquivo de verdade.

const RAIZ = 'repo/';
const RAW = 'https://raw.githubusercontent.com/IamP5/corredor-aceso/main/';
// O raw serve SVG como text/plain, que o <img> não desenha; o jsDelivr serve com o tipo certo.
const CDN = 'https://cdn.jsdelivr.net/gh/IamP5/corredor-aceso@main/';
const ARVORE = 'https://api.github.com/repos/IamP5/corredor-aceso/git/trees/main?recursive=1';
const fonte = (c) => (c.endsWith('/') ? pasta(c.slice(0, -1)) : arq(c));
const nomeDoArquivo = (c) => c.replace(/\/$/, '').split('/').pop();

// Um desenho da arquitetura é um .mmd com o SVG escuro ao lado. O link pode apontar o .mmd ou o .svg.
const ehDiagrama = (c) => /^docs\/arquitetura\/diagramas\/.+\.(mmd|svg)$/.test(c);
const svgDoDiagrama = (c) => c.replace(/(\.escuro)?\.(mmd|svg)$/, '.escuro.svg');

const TIPOS = {
  diagrama: { rotulo: 'Desenho', exts: [] },
  imagem: { rotulo: 'Imagem', exts: ['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg'] },
  markdown: { rotulo: 'Documento', exts: ['md'] },
  notebook: { rotulo: 'Notebook', exts: ['ipynb'] },
  tabela: { rotulo: 'CSV', exts: ['csv'] },
  binario: { rotulo: 'Arquivo', exts: ['woff2', 'woff', 'ttf', 'otf', 'pdf', 'zip', 'gz', 'ico'] },
  texto: { rotulo: 'Código', exts: [] },
};
function tipoDe(c) {
  if (c.endsWith('/')) return 'pasta';
  if (ehDiagrama(c)) return 'diagrama';
  const ext = c.split('.').pop().toLowerCase();
  return Object.keys(TIPOS).find((t) => TIPOS[t].exts.includes(ext)) || 'texto';
}
const rotuloDe = (t) => (t === 'pasta' ? 'Pasta' : TIPOS[t].rotulo);

let locaisP, arvoreP;
const lerLocais = () => (locaisP ??= fetch(`${RAIZ}indice.json`).then((r) => r.json()).then((l) => new Set(l), () => new Set()));
// Todos os arquivos do repositório. A API sem login aceita 60 pedidos por hora, então a lista fica na sessão.
// Sem a API, sobra o que foi copiado.
const lerIndice = () => (arvoreP ??= (async () => {
  try {
    const salva = sessionStorage.getItem(ARVORE);
    if (salva) return JSON.parse(salva);
  } catch {}
  try {
    const r = await fetch(ARVORE, { headers: { Accept: 'application/vnd.github+json' } });
    if (!r.ok) throw new Error(`${r.status}`);
    const lista = (await r.json()).tree.filter((x) => x.type === 'blob').map((x) => x.path);
    try { sessionStorage.setItem(ARVORE, JSON.stringify(lista)); } catch {}
    return lista;
  } catch {
    return [...await lerLocais()];
  }
})());
const urlDe = async (c) => ((await lerLocais()).has(c) ? RAIZ : RAW) + c;
const urlDaImagem = (c, locais) => (locais.has(c) ? RAIZ : c.endsWith('.svg') ? CDN : RAW) + c;
const scripts = new Map();
const carregarScript = (src, global) => {
  if (!scripts.has(src)) scripts.set(src, new Promise((ok, erro) => {
    const s = document.createElement('script');
    s.src = src;
    s.onload = () => ok(window[global]);
    s.onerror = erro;
    document.head.append(s);
  }));
  return scripts.get(src);
};
const carregarMarked = () => carregarScript('https://cdn.jsdelivr.net/npm/marked@18.2.1/lib/marked.umd.js', 'marked');

const LINGUA_DA_EXT = {
  sh: 'bash', cs: 'csharp', py: 'python', ino: 'cpp', h: 'cpp', json: 'json', sql: 'sql', yml: 'yaml', yaml: 'yaml',
  ts: 'typescript', tsx: 'typescript', js: 'javascript', mjs: 'javascript', cjs: 'javascript', css: 'css',
  html: 'xml', xml: 'xml', csproj: 'xml', props: 'xml', slnx: 'xml', svg: 'xml', md: 'markdown',
  toml: 'ini', conf: 'ini', service: 'ini', timer: 'ini', tf: 'ini', tfvars: 'ini', modelo: 'ini', padrao: 'ini',
  dockerfile: 'bash', example: 'bash',
};
const linguaDe = (c) => LINGUA_DA_EXT[nomeDoArquivo(c).split('.').pop().toLowerCase()];
// Acima disso o realce trava a aba por um tempo que não compensa (o package-lock.json, por exemplo).
const REALCE_MAXIMO = 200_000;
function colorir(code, lingua) {
  if (!lingua || code.textContent.length > REALCE_MAXIMO) return;
  carregarScript('https://cdn.jsdelivr.net/npm/@highlightjs/cdn-assets@11.11.1/highlight.min.js', 'hljs').then((hljs) => {
    if (!hljs.getLanguage(lingua)) return;
    code.innerHTML = hljs.highlight(code.textContent, { language: lingua, ignoreIllegals: true }).value;
    code.classList.add('hljs');
  }, () => {});
}
const lerTexto = async (c) => {
  const r = await fetch(await urlDe(c));
  if (!r.ok) throw new Error(`${r.status}`);
  return r.text();
};

function cartao(e, aoAbrir = () => gaveta.abrir(e.caminho, e.nome), locais = new Set([e.caminho])) {
  const tipo = tipoDe(e.caminho);
  const b = el('button', { type: 'button', class: tipo === 'imagem' ? 'evidencia evidencia--img' : 'evidencia' });
  b.innerHTML = tipo === 'imagem'
    ? `<img src="${urlDaImagem(e.caminho, locais)}" alt="" loading="lazy"><span>${e.nome}</span>`
    : `<span class="evidencia__texto"><span class="evidencia__tipo">${rotuloDe(tipo)}</span>${e.nome}</span><span class="seta" aria-hidden="true">→</span>`;
  b.addEventListener('click', aoAbrir);
  return b;
}

const RENDER = {
  async imagem(c, nome) {
    const fig = el('figure', { class: 'vis-imagem' }, `<img src="${urlDaImagem(c, await lerLocais())}" alt="${escapar(nome)}">`);
    fig.addEventListener('click', () => fig.classList.toggle('vis-imagem--real'));
    return fig;
  },

  async texto(c) {
    const t = (await lerTexto(c)).replace(/\n$/, '');
    const linhas = el('pre', { class: 'vis-codigo__linhas', 'aria-hidden': 'true' });
    linhas.textContent = t.split('\n').map((_, i) => i + 1).join('\n');
    // As linhas e o código rolam juntos para o lado; o botão de copiar fica parado no canto.
    const rolagem = el('div', { class: 'vis-codigo__rolagem' });
    rolagem.append(linhas, blocoDeCodigo(t, linguaDe(c)));
    const caixa = el('div', { class: 'cmd vis-codigo' });
    caixa.append(rolagem, botaoCopiar(t, 'Copiar o arquivo'));
    return caixa;
  },

  async binario(c) {
    return el('p', { class: 'gaveta__estado' }, `Este arquivo não é texto. <a href="${RAW}${c}" data-github>Baixar</a> ou abrir no GitHub.`);
  },

  markdown: (c) => documento(c),

  async diagrama(c, nome) {
    return figura(await lerTexto(svgDoDiagrama(c)), nome, fonte(svgDoDiagrama(c).replace('.escuro.svg', '.mmd')));
  },

  async tabela(c) {
    const linhas = lerCsv(await lerTexto(c));
    const LIMITE = 300;
    const [cab, ...resto] = linhas;
    const tab = el('table', { class: 'tabela vis-tabela' });
    tab.append(el('thead', {}, `<tr>${cab.map((x) => `<th>${escapar(x)}</th>`).join('')}</tr>`));
    const corpo = el('tbody');
    for (const l of resto.slice(0, LIMITE)) corpo.append(el('tr', {}, l.map((x) => `<td>${escapar(x)}</td>`).join('')));
    tab.append(corpo);
    const caixa = el('div', { class: 'tabela-rolagem' });
    caixa.append(tab);
    if (resto.length > LIMITE) caixa.append(el('p', { class: 'nota' }, `Mostrando ${LIMITE} de ${resto.length} linhas. O arquivo inteiro está no GitHub.`));
    return caixa;
  },

  async notebook(c) {
    const [nb, marked] = await Promise.all([lerTexto(c).then(JSON.parse), carregarMarked()]);
    const doc = el('article', { class: 'doc notebook' });
    const junto = (x) => (Array.isArray(x) ? x.join('') : x || '');
    for (const celula of nb.cells) {
      const fonteDaCelula = junto(celula.source);
      if (celula.cell_type === 'markdown') { doc.append(el('div', { class: 'nb-md' }, marked.parse(fonteDaCelula))); continue; }
      if (celula.cell_type !== 'code') continue;
      doc.append(blocoDeCodigo(fonteDaCelula, 'python', 'nb-entrada'));
      for (const o of celula.outputs || []) {
        const dados = o.data || {};
        if (o.output_type === 'stream') {
          const pre = el('pre', { class: 'nb-saida' });
          pre.textContent = junto(o.text);
          doc.append(pre);
        } else if (dados['image/png']) {
          doc.append(el('img', { class: 'nb-imagem', src: `data:image/png;base64,${junto(dados['image/png']).trim()}`, alt: 'Saída da célula' }));
        } else if (dados['text/html']) {
          doc.append(el('div', { class: 'nb-html tabela-rolagem' }, junto(dados['text/html'])));
        } else if (dados['text/plain']) {
          const pre = el('pre', { class: 'nb-saida' });
          pre.textContent = junto(dados['text/plain']);
          doc.append(pre);
        }
      }
    }
    return doc;
  },

  // Como no GitHub: só o primeiro nível, pastas antes de arquivos, cada uma abrindo por cima desta.
  async pasta(c) {
    const [arquivos, locais] = await Promise.all([lerIndice(), lerLocais()]);
    const filhos = new Set(arquivos.filter((x) => x.startsWith(c)).map((x) => x.slice(c.length).replace(/\/.*/s, '/')));
    if (!filhos.size) throw new Error('pasta vazia');
    const ordem = [...filhos].sort((a, b) => (b.endsWith('/') - a.endsWith('/')) || a.localeCompare(b, 'pt-BR'));
    const imagens = ordem.filter((n) => tipoDe(n) === 'imagem');
    const resto = ordem.filter((n) => tipoDe(n) !== 'imagem');
    const abrir = (n) => () => gaveta.abrir(c + n, n.replace(/\/$/, ''), true);
    const caixa = el('div', { class: 'vis-pasta' });
    if (resto.length) {
      const lista = el('ul', { class: 'vis-lista' });
      for (const n of resto) {
        const b = el('button', { type: 'button' }, `<span class="vis-lista__tipo">${rotuloDe(tipoDe(n))}</span><span class="vis-lista__nome">${escapar(n)}</span>`);
        b.addEventListener('click', abrir(n));
        lista.append(el('li'));
        lista.lastChild.append(b);
      }
      caixa.append(lista);
    }
    if (imagens.length) {
      const galeria = el('div', { class: 'evidencias vis-galeria' });
      for (const n of imagens) galeria.append(cartao({ caminho: c + n, nome: n }, abrir(n), locais));
      caixa.append(galeria);
    }
    return caixa;
  },
};

// Markdown do repositório virando página: desenhos no lugar das referências e links religados.
// No leitor, cada título ganha o mesmo id que o GitHub dá, para as âncoras dos .md valerem aqui.
async function documento(c, noLeitor = false) {
  const [md, marked, indice, locais] = await Promise.all([lerTexto(c), carregarMarked(), lerIndice(), lerLocais()]);
  // O <template> não baixa imagem, então a referência ao .mmd não vira pedido perdido antes de virar desenho.
  const molde = el('template', {}, marked.parse(md));
  const doc = molde.content;
  if (noLeitor) {
    const vistos = new Map();
    doc.querySelectorAll('h1, h2, h3, h4, h5, h6').forEach((h) => {
      const base = h.textContent.toLowerCase().trim().replace(/[^\p{L}\p{M}\p{N}\s_-]/gu, '').replace(/\s/g, '-');
      const n = vistos.get(base) ?? 0;
      vistos.set(base, n + 1);
      h.id = n ? `${base}-${n}` : base;
    });
  }
  const resolver = resolvedorDe(c);
  await Promise.all([
    ...[...doc.querySelectorAll('img[src$=".mmd"]')].map(async (img) => {
      const alvo = resolver(img.getAttribute('src'));
      const fig = await figura(await lerTexto(svgDoDiagrama(alvo)), img.alt, fonte(alvo));
      const p = img.parentElement;
      (p.tagName === 'P' && p.childNodes.length === 1 ? p : img).replaceWith(fig);
    }),
    desenharMermaid(doc),
  ]);
  for (const code of doc.querySelectorAll('pre > code')) {
    colorir(code, code.className.match(/language-(\S+)/)?.[1]);
    const caixa = el('div', { class: 'cmd' });
    code.parentElement.replaceWith(caixa);
    caixa.append(code.parentElement, botaoCopiar(code.textContent, 'Copiar código'));
  }
  ligarLinks(doc, c, indice, locais, noLeitor);
  const artigo = el('article', { class: 'doc' });
  artigo.append(doc);
  return artigo;
}

// Desenho na largura da coluna. Se ele é mais largo que ela, o botão troca para o tamanho real, com rolagem.
function figura(svg, legenda, linkDaFonte) {
  const fig = el('figure', { class: 'diagrama' });
  const tela = el('div', { class: 'diagrama__tela', tabindex: '0', role: 'img', 'aria-label': legenda }, svg);
  const botao = el('button', { type: 'button', class: 'diagrama__zoom', hidden: '' }, 'Tamanho real');
  const alternar = () => { botao.textContent = fig.classList.toggle('diagrama--real') ? 'Ajustar à largura' : 'Tamanho real'; };
  botao.addEventListener('click', alternar);
  tela.addEventListener('click', () => { if (!botao.hidden && !fig.classList.contains('diagrama--real')) alternar(); });
  new ResizeObserver(() => {
    const cabe = tela.querySelector('svg').width.baseVal.value <= tela.clientWidth;
    botao.hidden = cabe && !fig.classList.contains('diagrama--real');
    fig.classList.toggle('diagrama--amplia', !botao.hidden);
  }).observe(tela);
  const cap = el('figcaption', {}, `<span>${escapar(legenda)}</span>`);
  cap.append(botao);
  if (linkDaFonte) cap.append(el('a', { href: linkDaFonte, target: '_blank', rel: 'noopener' }, 'Fonte <span aria-hidden="true">↗</span>'));
  fig.append(tela, cap);
  return fig;
}

// Bloco ```mermaid escrito dentro do .md. O Mermaid só é baixado se a página tiver um.
let mermaidP;
let desenhados = 0;
async function desenharMermaid(doc) {
  const blocos = [...doc.querySelectorAll('pre > code.language-mermaid')];
  if (!blocos.length) return;
  try {
    for (const code of blocos) {
      const titulo = [...doc.querySelectorAll('h1, h2, h3, h4')].filter((h) => h.compareDocumentPosition(code) & Node.DOCUMENT_POSITION_FOLLOWING).pop();
      const fig = figura(await svgDoMermaid(code.textContent), titulo?.textContent ?? 'Diagrama', '');
      fig.dataset.mermaid = code.textContent;
      code.parentElement.replaceWith(fig);
    }
  } catch {
    // Sem rede ou com erro no desenho, o código do bloco continua na página.
  }
}

// O Mermaid pinta as cores dentro do SVG, então cada desenho sai com as do tema atual e é refeito na troca.
async function svgDoMermaid(fonte) {
  mermaidP ??= import('https://cdn.jsdelivr.net/npm/mermaid@11.4.1/dist/mermaid.esm.min.mjs').then((m) => m.default);
  const mermaid = await mermaidP;
  const cor = (nome) => getComputedStyle(document.documentElement).getPropertyValue(nome).trim();
  mermaid.initialize({
    startOnLoad: false,
    theme: 'base',
    themeVariables: {
      darkMode: document.documentElement.dataset.tema !== 'claro',
      background: cor('--preto-atomico'),
      primaryColor: cor('--preto-casual'),
      primaryTextColor: cor('--branco'),
      primaryBorderColor: cor('--ardosia'),
      lineColor: cor('--prata'),
      textColor: cor('--prata'),
      attributeBackgroundColorOdd: cor('--preto-atomico'),
      attributeBackgroundColorEven: cor('--preto-casual'),
      fontFamily: 'Roboto, system-ui, sans-serif',
    },
  });
  const { svg } = await mermaid.render(`mermaid-${desenhados++}`, fonte);
  const caixa = el('div', {}, svg);
  const desenho = caixa.querySelector('svg');
  const [, , w, h] = desenho.getAttribute('viewBox').split(/\s+/).map(Number);
  desenho.setAttribute('width', w);
  desenho.setAttribute('height', h);
  desenho.removeAttribute('style');
  return desenho.outerHTML;
}

async function redesenharMermaid() {
  for (const fig of document.querySelectorAll('.diagrama[data-mermaid]')) {
    try { fig.querySelector('.diagrama__tela').innerHTML = await svgDoMermaid(fig.dataset.mermaid); } catch {}
  }
}

// Tema: o <head> já aplicou a escolha salva ou a do sistema. O botão alterna; se a escolha
// coincide com a do sistema, ela não é salva, e a página volta a acompanhar o sistema.
const TEMA = 'corredor-aceso:tema';
const sistemaClaro = matchMedia('(prefers-color-scheme: light)');
const temaDoSistema = () => (sistemaClaro.matches ? 'claro' : 'escuro');
function aplicarTema(tema) {
  document.documentElement.dataset.tema = tema;
  const rotulo = tema === 'claro' ? 'Usar tema escuro' : 'Usar tema claro';
  const botao = document.getElementById('tema');
  botao.setAttribute('aria-label', rotulo);
  botao.title = rotulo;
  redesenharMermaid();
}
aplicarTema(document.documentElement.dataset.tema || temaDoSistema());
document.getElementById('tema').addEventListener('click', () => {
  const novo = document.documentElement.dataset.tema === 'claro' ? 'escuro' : 'claro';
  try { novo === temaDoSistema() ? localStorage.removeItem(TEMA) : localStorage.setItem(TEMA, novo); } catch {}
  aplicarTema(novo);
});
sistemaClaro.addEventListener('change', () => {
  let salvo = null;
  try { salvo = localStorage.getItem(TEMA); } catch {}
  if (!salvo) aplicarTema(temaDoSistema());
});

function escapar(t) {
  return t.replace(/[&<>"]/g, (ch) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[ch]);
}

function lerCsv(texto) {
  const linhas = [];
  let linha = [], campo = '', aspas = false;
  for (let i = 0; i < texto.length; i++) {
    const ch = texto[i];
    if (aspas) {
      if (ch === '"' && texto[i + 1] === '"') { campo += '"'; i++; }
      else if (ch === '"') aspas = false;
      else campo += ch;
    } else if (ch === '"') aspas = true;
    else if (ch === ',') { linha.push(campo); campo = ''; }
    else if (ch === '\n' || ch === '\r') {
      if (ch === '\r' && texto[i + 1] === '\n') i++;
      linha.push(campo); linhas.push(linha); linha = []; campo = '';
    } else campo += ch;
  }
  if (campo || linha.length) { linha.push(campo); linhas.push(linha); }
  return linhas;
}

const resolvedorDe = (origem) => {
  const pastaDaOrigem = origem.slice(0, origem.lastIndexOf('/') + 1);
  return (href) => decodeURIComponent(new URL(href, `https://x/${pastaDaOrigem}`).pathname.slice(1));
};

// Links relativos de um Markdown: documento do leitor abre no leitor; o resto aponta para o GitHub,
// e o clique num link do repositório abre a gaveta.
function ligarLinks(doc, origem, indice, locais, noLeitor = false) {
  const resolver = resolvedorDe(origem);
  const copiado = (c) => indice.includes(ehDiagrama(c) ? svgDoDiagrama(c) : c);
  const externo = (href) => /^([a-z]+:|\/\/)/i.test(href);

  doc.querySelectorAll('img[src]').forEach((img) => {
    const src = img.getAttribute('src');
    if (externo(src)) return;
    const c = resolver(src);
    img.src = urlDaImagem(c, locais);
  });

  doc.querySelectorAll('a[href]').forEach((a) => {
    const href = a.getAttribute('href');
    if (href.startsWith('#')) { if (!noLeitor) a.removeAttribute('href'); return; }
    if (externo(href)) { a.target = '_blank'; a.rel = 'noopener'; return; }
    const [parte, ancora] = href.split('#');
    const c = resolver(parte);
    if (ehLeitura(c)) { a.href = linkDoLeitor(c, ancora ? `#${ancora}` : ''); return; }
    const ehPasta = !copiado(c) && indice.some((x) => x.startsWith(`${c.replace(/\/$/, '')}/`));
    a.href = fonte(ehPasta ? `${c.replace(/\/$/, '')}/` : c);
  });
}

const gaveta = (() => {
  const dlg = document.getElementById('gaveta');
  const titulo = dlg.querySelector('#gaveta-titulo');
  const caminhoEl = dlg.querySelector('.gaveta__caminho');
  const corpo = dlg.querySelector('.gaveta__corpo');
  const linkFonte = dlg.querySelector('.gaveta__fonte');
  const voltar = dlg.querySelector('.gaveta__voltar');
  let pilha = [];
  let vez = 0;

  async function mostrar({ caminho, nome }) {
    const minha = ++vez;
    const tipo = tipoDe(caminho);
    titulo.textContent = nome;
    caminhoEl.replaceChildren(`${rotuloDe(tipo)} · `);
    const partes = caminho.replace(/\/$/, '').split('/');
    partes.forEach((parte, i) => {
      if (i) caminhoEl.append('/');
      const ultima = i === partes.length - 1;
      if (ultima) { caminhoEl.append(parte); return; }
      const b = el('button', { type: 'button' });
      b.textContent = parte;
      b.addEventListener('click', () => gaveta.abrir(`${partes.slice(0, i + 1).join('/')}/`, parte, true));
      caminhoEl.append(b);
    });
    linkFonte.href = fonte(caminho);
    voltar.hidden = pilha.length < 2;
    corpo.replaceChildren(el('p', { class: 'gaveta__estado' }, 'Carregando…'));
    corpo.scrollTop = 0;
    try {
      const n = await RENDER[tipo](caminho, nome);
      if (minha === vez) corpo.replaceChildren(n);
    } catch {
      if (minha === vez) corpo.replaceChildren(el('p', { class: 'gaveta__estado' }, `Não deu para abrir aqui. <a href="${fonte(caminho)}" target="_blank" rel="noopener" data-github>Ver no GitHub</a>.`));
    }
  }

  dlg.querySelector('.gaveta__fechar').addEventListener('click', () => dlg.close());
  dlg.addEventListener('click', (e) => { if (e.target === dlg) dlg.close(); });
  // O evento close chega depois; se a gaveta já reabriu, não apaga o que está nela.
  dlg.addEventListener('close', () => { if (!dlg.open) { pilha = []; corpo.replaceChildren(); } });
  voltar.addEventListener('click', () => { pilha.pop(); mostrar(pilha.at(-1)); });

  // Um clique comum num link do repositório abre a gaveta; Cmd, Ctrl, Shift ou o botão do meio seguem para o GitHub,
  // como em qualquer link. Os links marcados com data-github são os que levam ao GitHub de propósito.
  const DO_REPO = new RegExp(`^${REPO.replace(/[.]/g, '\\.')}/(blob|tree)/main/([^?#]+)`);
  document.addEventListener('click', (ev) => {
    if (ev.defaultPrevented || ev.button !== 0 || ev.metaKey || ev.ctrlKey || ev.shiftKey || ev.altKey) return;
    const a = ev.target.closest('a[href]');
    if (!a || a.hasAttribute('data-github')) return;
    const m = a.href.match(DO_REPO);
    if (!m) return;
    ev.preventDefault();
    const caminho = decodeURIComponent(m[2]).replace(/\/$/, '') + (m[1] === 'tree' ? '/' : '');
    gaveta.abrir(caminho, nomeDoArquivo(caminho), dlg.contains(a));
  });

  return {
    abrir(caminho, nome, empilhar = false) {
      if (!empilhar) pilha = [];
      pilha.push({ caminho, nome });
      if (!dlg.open) dlg.showModal();
      mostrar(pilha.at(-1));
    },
  };
})();

// ---- Indicadores de navegação: o link do topo marca a página aberta, e os menus da página
// seguem a rolagem, marcando a seção em que o leitor está.

function marcarTopo(caminho) {
  for (const a of document.querySelectorAll('.topo__links a')) {
    const alvo = new URL(a.href, location.href).searchParams.get('ler');
    if (!alvo || !caminho) continue;
    const pasta = alvo.slice(0, alvo.lastIndexOf('/') + 1);
    if (alvo === caminho || (pasta && caminho.startsWith(pasta))) a.setAttribute('aria-current', 'page');
  }
}

// Cada link aponta para #id. A seção atual é a última cujo topo já passou da linha logo abaixo
// das barras fixas; no fim da página, a última. O menu rola sozinho para o link marcado ficar à vista.
function seguirRolagem(links, rolavel) {
  const pares = links.map((a) => [a, document.getElementById(decodeURIComponent(a.hash.slice(1)))]).filter(([, s]) => s);
  if (!pares.length) return;
  let atual = null;
  const linha = () => parseFloat(getComputedStyle(document.documentElement).scrollPaddingTop) + 16;
  const atualizar = () => {
    const y = linha();
    const noFim = innerHeight + scrollY >= document.documentElement.scrollHeight - 4;
    let ativo = null;
    for (const [a, s] of pares) if (s.getBoundingClientRect().top <= y) ativo = a;
    if (noFim && pares.at(-1)[1].getBoundingClientRect().top < innerHeight) ativo = pares.at(-1)[0];
    if (ativo === atual) return;
    atual?.removeAttribute('aria-current');
    atual = ativo;
    if (!ativo) return;
    ativo.setAttribute('aria-current', 'location');
    if (rolavel) {
      const c = rolavel.getBoundingClientRect(), r = ativo.getBoundingClientRect();
      if (r.left < c.left || r.right > c.right) rolavel.scrollBy({ left: r.left - c.left - (c.width - r.width) / 2, behavior: 'smooth' });
      if (r.top < c.top || r.bottom > c.bottom) rolavel.scrollBy({ top: r.top - c.top - (c.height - r.height) / 2, behavior: 'smooth' });
    }
  };
  let pedido = 0;
  const agendar = () => { if (!pedido) pedido = requestAnimationFrame(() => { pedido = 0; atualizar(); }); };
  addEventListener('scroll', agendar, { passive: true });
  addEventListener('resize', agendar);
  // Imagens e desenhos que carregam depois mudam a altura das seções.
  new ResizeObserver(agendar).observe(document.body);
  atualizar();
}

// ---- Leitor: um documento de LEITURAS em página inteira, com o menu dos documentos e o índice da página.

async function leitor(caminho) {
  const raiz = document.getElementById('leitor');
  document.getElementById('conteudo').hidden = true;
  raiz.hidden = false;
  document.querySelector('.pular').href = '#leitor';
  marcarTopo(caminho);

  const i = DOCS.findIndex((d) => d.leitura === caminho);
  const atual = DOCS[i];
  const menu = LEITURAS.map((g) => `
    <p class="leitor__grupo">${g.grupo}</p>
    <ul>${g.docs.map((d) => `<li><a href="${linkDoLeitor(d.leitura)}"${d.leitura === caminho ? ' aria-current="page"' : ''}>${d.nome}</a></li>`).join('')}</ul>`).join('');
  raiz.innerHTML = `
    <aside class="leitor__lado">
      <a class="leitor__voltar" href="./">← Guia de avaliação</a>
      <details class="leitor__menu"><summary>Documentos</summary><nav aria-label="Documentos">${menu}</nav></details>
      <details class="leitor__menu leitor__indice" hidden><summary>Nesta página</summary><nav aria-label="Nesta página"><ul></ul></nav></details>
    </aside>
    <div class="leitor__texto">
      <header class="leitor__cabeca">
        <p class="sobretitulo">${atual ? `${atual.grupo} · ${atual.leitura}` : 'Documento'}</p>
        ${atual ? `<a class="leitor__fonte" href="${fonte(atual.leitura)}" target="_blank" rel="noopener" data-github>Ver no GitHub <span aria-hidden="true">↗</span></a>` : ''}
      </header>
      <div class="leitor__corpo"><p class="gaveta__estado">Carregando…</p></div>
    </div>`;
  // Na tela larga os dois menus ficam abertos ao lado do texto; na estreita, fechados em cima dele.
  const largo = matchMedia('(min-width: 901px)');
  const ajustarMenus = () => raiz.querySelectorAll('.leitor__menu').forEach((m) => { m.open = largo.matches; });
  ajustarMenus();
  largo.addEventListener('change', ajustarMenus);
  const corpo = raiz.querySelector('.leitor__corpo');
  if (!atual) {
    corpo.innerHTML = '<p class="gaveta__estado">Este documento não está no guia. Escolha um no menu.</p>';
    return;
  }
  document.title = `${atual.nome} · Corredor Aceso`;

  let doc;
  try {
    doc = await documento(caminho, true);
  } catch {
    corpo.innerHTML = `<p class="gaveta__estado">Não deu para abrir aqui. <a href="${fonte(caminho)}" target="_blank" rel="noopener" data-github>Ver no GitHub</a>.</p>`;
    return;
  }
  corpo.replaceChildren(doc);

  const vizinhos = el('nav', { class: 'leitor__vizinhos', 'aria-label': 'Documento anterior e próximo' });
  for (const [d, rotulo] of [[DOCS[i - 1], '← Anterior'], [DOCS[i + 1], 'Próximo →']]) {
    vizinhos.append(d ? el('a', { href: linkDoLeitor(d.leitura) }, `<span>${rotulo}</span>${d.nome}`) : el('span'));
  }
  corpo.append(vizinhos);

  const titulos = [...doc.querySelectorAll('h2')];
  const indice = raiz.querySelector('.leitor__indice');
  indice.hidden = !titulos.length;
  indice.querySelector('ul').append(...titulos.map((h) => el('li', {}, `<a href="#${h.id}">${escapar(h.textContent)}</a>`)));
  indice.addEventListener('click', (e) => { if (e.target.closest('a') && !largo.matches) indice.open = false; });

  if (location.hash.length > 1) document.getElementById(decodeURIComponent(location.hash.slice(1)))?.scrollIntoView({ behavior: 'instant' });
  seguirRolagem([...indice.querySelectorAll('a')], raiz.querySelector('.leitor__lado'));
}

function montar() {
  listaDePassos(PASSOS_COMUNS, document.getElementById('passos-comuns'));

  const nav = document.getElementById('lista-disciplinas');
  nav.append(el('li', {}, '<a href="#ficha">Ficha</a>'));
  nav.append(el('li', {}, '<a href="#comecar">Passo zero</a>'));
  for (const d of DISCIPLINAS) nav.append(el('li', {}, `<a href="#${d.id}">${d.nome}${barra(d.id)}</a>`));
  nav.append(el('li', {}, '<a href="#documentos">Documentos</a>'));

  const docs = document.getElementById('lista-documentos');
  for (const d of DOCS) {
    docs.append(el('a', { class: 'evidencia', href: linkDoLeitor(d.leitura) },
      `<span class="evidencia__texto"><span class="evidencia__tipo">${d.grupo}</span>${d.nome}<span class="evidencia__resumo">${d.resumo}</span></span><span class="seta" aria-hidden="true">→</span>`));
  }

  const alvo = document.getElementById('secoes');
  DISCIPLINAS.forEach((d, i) => alvo.append(secao(d, i + 1)));
  DISCIPLINAS.forEach(atualizarProgresso);
  seguirRolagem([...nav.querySelectorAll('a')], nav);
}

const ler = new URLSearchParams(location.search).get('ler');
ler ? leitor(ler) : montar();
