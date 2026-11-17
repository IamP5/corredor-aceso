# Contradições e lacunas achadas ao escrever o QA

Escrever um resultado esperado para cada regra obriga a ler cada regra ao pé da letra. Esta lista é o que essa leitura achou. Nos casos de teste, quando dois documentos discordam, vale o [modelo de domínio](../arquitetura/03-modelo-de-dominio.md), que é o mais recente e registra as próprias mudanças na seção 9.

Nada aqui foi corrigido nos documentos de arquitetura: o escopo desta entrega é `docs/qa/` e `docs/entrega/`. Cada linha diz quem precisa decidir.

## 1. Documentos de arquitetura que se contradizem

| # | Um documento diz | Outro documento diz | Como os casos tratam | Quem decide |
|---|---|---|---|---|
| 1 | [arquitetura.md](../arquitetura.md), Módulo Guarda: qualquer bloqueio de severidade 4 ou 5 dá vermelho; bloqueios até 3 dão amarelo. | 03 §4.4 e mudança 16 da §9: só uma `SaidaBloqueada` de severidade 4 ou 5 dá vermelho; qualquer outro evento dá amarelo, inclusive um mascaramento de CPF de severidade 5. | Seguem o 03. TC-F26-03 e TC-F24-08 provam a diferença. | Guarda. Atualizar arquitetura.md. |
| 2 | arquitetura.md: o interceptador troca por máscara todo campo com PII; o validador de CPF confere os dígitos. | 03 mudança 14 e §9.1 mudanças 10 e 11: só CPF, RG, CNS e credencial são mascarados; o CPF é mascarado pelo formato e o dígito válido só sobe a severidade. | Seguem o 03. | Guarda. |
| 3 | arquitetura.md, evidência 1 da auditoria: `POST` com o campo `"documento"`. | Nenhuma rota tem esse campo. O caso 38 do 03 usa `condicoes`. | Usam `condicoes`. O esqueleto do PDF de Cyber também. | Guarda. |
| 4 | arquitetura.md: a lista de nomes do app aceita nome e endereço. | 03 §4.4, `PoliticaDeCanal`: `AppAutenticado` aceita nome, endereço e telefone. | Seguem o 03. | Guarda. |
| 5 | arquitetura.md, Onde cada disciplina pontua: "`RegraDeContato` e cinco regras". | 03 §5.4: quatro regras na entrega; `IdaNoturnaSemVolta` está fora. | Cobrem as quatro. | Motor. O README de C# não pode prometer cinco. |
| 6 | arquitetura.md: um Postgres, dois schemas, dois `DbContext`. | 03 §2 e mudança 17: três schemas, com `guarda`. | Seguem o 03. | Atualizar arquitetura.md. |
| 7 | arquitetura.md e 03 §introdução: .NET 8. | 05 §1.3 e mudança 1 da §10: .NET 10. | Não afeta os casos. | O 03 ainda diz .NET 8 na abertura. |
| 8 | arquitetura.md, Contratos, MQTT: `PEDIR_TOQUE` toca o buzzer às 15h. | 03 §6.2, P12 e mudança 10 da §9: 14h30. | Seguem 14h30. | Atualizar arquitetura.md. |
| 9 | arquitetura.md, Modelo de dia crítico: na demo, o script roda com uma data histórica. | 03 mudança 20: `DiaCritico` só aceita a data de hoje; o script usa as variáveis da data histórica e envia a de hoje. | Seguem o 03 (TC-F19). | ML. |
| 10 | arquitetura.md, Infra Linux: `api.corredor.local`. | 05 mudança 2: `api.corredor.internal`. | Seguem o 05. | Atualizar arquitetura.md. |
| 11 | [02-event-storming.md](../arquitetura/02-event-storming.md), Fluxo 3, passo 1: logo depois da declaração das 05h, "40 casas `Aguardando`". | 03 §5.2, tabela do acamado em §5.8 e caso 16: a casa `Acamado` fica `Vermelho` desde 05h00 às 05h01. A carga tem 4 acamados. | Nenhum caso afirma "40 Aguardando". | Motor. O vídeo não pode mostrar 40 aguardando. |
| 12 | 03 §6.3: a página do painel fica em `/painel`. | 05 §4.2 e §6.4: `/painel/`. | Usam `/painel`. Diferença só de barra final. | Infra. |
| 13 | 05 §7, rubrica de OS: mostrar `/etc/corredor/api.env` "sem as senhas". | `infra/02-app.sh` grava a senha do banco nas linhas `ConnectionStrings__*` desse arquivo. | TC-F29-01 mostra o arquivo com um `sed` que esconde `Password=`. | Infra. Não mostre o arquivo cru no vídeo. |
| 14 | 04 §6.3: o desfecho recusado fica no card "Não enviado" até a ACS dispensar. | 04 §6.2: a casa revogada some do celular na sincronização seguinte, e a tela do radar lê de `radar_card`. Depois disso o desfecho recusado não tem card onde aparecer. | TC-F27-06 e TC-F27-07 observam antes da sincronização seguinte. | App. |
| 15 | 03 §4.3, invariante 7 do `Idoso`: a família só entra nos destinatários com `AvisarFamilia`. | 04 §2 e §8.1: não existe tabela de família, e `AvisarFamilia` não tem efeito na entrega. | A parte negativa é observável; a positiva só por teste de unidade (TC-F04-06 e TC-F04-07). | Cadastro. |
| 16 | 03 §6.3 lista os códigos de erro de cada rota. | 04 §7.2 traz códigos que o 03 não tem: 404 `IDOSO_DESCONHECIDO`, 403 `MICROAREA_DE_OUTRA_ACS`, 422 `CAMPO_INVALIDO`. Nenhum dos dois dá o código para o motivo do dia crítico com mais de 200 caracteres (invariante 3 do `DiaCritico`) nem o 401. | Os casos usam os códigos do 04 e anotam os que faltam. | Motor e Cadastro. |

## 2. O que os documentos não dizem

Os casos anotam a lacuna em vez de inventar o comportamento.

- **Guarda.** O 03 §4.4 diz que todo bloqueio da `SaidaSegura` grava `SaidaBloqueada`, mas não diz se o `TratadorDeErros`, o `FiltroDeResposta` e a publicação MQTT passam pela `SaidaSegura`. TC-F24-03, TC-F24-05 e TC-F24-08 esperam o evento nesses canais.
- **Guarda.** Uma requisição com dois achados gera um evento ou dois? TC-F23-04 espera um, com a lista dos tipos e a maior severidade, porque `Registrar` recebe uma lista.
- **Guarda.** O texto das frases fixas (a explicação de `POST /mensagens/verificar` e a frase de reserva de cada tipo de evento) não está escrito. A chave de configuração do endereço do LLM também não.
- **Motor.** Declarar de novo o mesmo dia publica `DIA_CRITICO` de novo? P12 exige dia crítico aberto para o pedido de toque? Os casos derivam que sim do fato de `PedirToque` ser comando do `DiaCritico`.
- **Motor.** `IntervaloDoExpurgo` é 1 hora também na demo (§5.9). Os casos de expurgo esperam até 1 hora. O vídeo não consegue mostrar o expurgo sem esperar.
- **Cadastro.** O código de erro da visita para um token que nunca existiu, o do `PUT /vizinho` sem `aceitouEm`, e se `POST /idosos` ignora ou recusa um campo `cpf` a mais.
- **Cadastro e app.** Nenhum documento diz como provocar 503 `MOTOR_INDISPONIVEL` de fora. TC-F27-03 para a API e recebe 5xx do Nginx.
- **App.** Como a sessão da ACS expira em 401 e o que `ultimo_erro` guarda num erro de rede.
- **Infra.** `cenario_api` e `cenario_monitor` de `infra/teste-de-falha.sh` conferem o PID novo e a saúde, não uma linha de volta no monitor. O critério da F29 foi escrito assim.

## 3. Entregáveis

| # | Ponto | Como este pacote trata |
|---|---|---|
| 17 | [entregaveis.md](../entregaveis.md) põe o PDF de Cyber em `docs/evidencias/cyber/`. O esqueleto desta entrega ficou em `docs/entrega/cyber-documentacao.md`. | O esqueleto diz para copiar para `docs/evidencias/cyber/` antes de preencher. |
| 18 | Nenhum documento dá limite de tempo para o vídeo do dashboard de Cyber. | O roteiro diz que não há limite e mira 3 minutos. |
| 19 | Mobile e Operating Systems pedem vídeo, mas a entrega é "link do GitHub" e não diz onde vai o link do vídeo. | O checklist põe o link no README da parte e no texto do envio. |
| 20 | O plano de [01-ideia-vencedora.md](../discovery/01-ideia-vencedora.md), seção 7, grava os vídeos em 18/11. [arquitetura.md](../arquitetura.md), ordem de construção, grava em 19/11 de manhã. | O checklist grava OS, Physical e Mobile em 18/11 e ML, Cyber e pitch em 19/11, pela ordem das dependências. |

## 4. Discovery desatualizado em relação ao modelo final

O roteiro de pitch e as respostas à banca da [01-ideia-vencedora.md](../discovery/01-ideia-vencedora.md), seções 8 e 9, são anteriores ao modelo de domínio. O [roteiros.md](../entrega/roteiros.md) já usa as regras finais. Quem for responder à banca precisa saber:

- A casa do concentrador fica **vermelha** quando o nó cala, não cinza (03 mudança 22). O cinza aparece na casa 10.
- O LED âmbar acende na declaração do dia crítico, e o buzzer toca às **14h30**, não às 15h (03 §6.2).
- O calor dentro de casa começa em **34 °C** por 2 horas, não 33 °C (03 §5.9).
- A ida noturna sem volta **não** entra na entrega (03 §5.4 e §8.4). Não responda que ela "roda toda noite".
- O nó do Wokwi publica num **broker público** com ponte para a VM (05 §1.11), ao contrário do "broker local na VM, nunca um público".
- A versão segura da mensagem é o texto fixo "Preciso de ajuda para uma visita agora. Quem puder, me chame no privado.", sem rua (03 §4.3).
- As entrevistas e os testes da descoberta foram simulados com personas (README). O pitch não pode apresentar a fala da idosa como entrevista real se ela não aconteceu.
