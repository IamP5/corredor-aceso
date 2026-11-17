# Esqueleto do PDF de Cybersecurity

O professor de Cybersecurity pede dois arquivos separados: **um PDF com a documentação** dos três pontos (Source, Auditoria e Vídeo do Dashboard), com prints do código e das evidências e o link do vídeo, e **um ZIP ou o link do GitHub** com o código ([arquitetura.md](../arquitetura.md), seção "Entrega" do Módulo Guarda; [entregaveis.md](../entregaveis.md)).

Como usar este esqueleto: copie para `docs/evidencias/cyber/documentacao.md`, troque cada caixa `[PRINT ...]` pelo print, preencha os campos entre colchetes e gere o PDF. Com o pandoc instalado:

```bash
pandoc docs/evidencias/cyber/documentacao.md -o corredor-aceso-cyber.pdf
```

Sem pandoc, abra o arquivo no VS Code com uma extensão de Markdown para PDF, ou cole no Google Docs e exporte. Os prints não podem mostrar chave de API, token de casa nem nome de outra pessoa que não seja da carga simulada.

Os critérios e os pesos: parsing de PII 3,0, taint analysis 3,0, IA na remediação 2,0, dashboard Risco Cidadão 2,0.

---

## Capa

- Corredor Aceso, Global Solution CitizenTech, FIAP 2026, Engenharia de Software, 3º ano.
- Disciplina: Cybersecurity.
- Integrantes: [nomes, como em `integrantes.txt`].
- Repositório: https://github.com/IamP5/corredor-aceso
- Vídeo do dashboard: [link].
- Data: 19/11/2026.

## 1. Contexto em um parágrafo

A ACS cadastra idosos que moram sozinhos, com condições de saúde e remédios. Esse é o dado que mais atrai golpe e violência. O módulo Guarda faz três coisas: detecta e mascara PII no que entra, impede que um texto com dado pessoal saia por um canal inseguro e traduz cada bloqueio para o cidadão com um LLM. Todo dado de pessoa no projeto é simulado.

## 2. Source

Um print por arquivo, com o trecho que prova o ponto. Os arquivos ficam em `api/src/Corredor.Guarda/`. Confira o caminho exato na junção das branches.

### 2.1 Parsing de PII (3,0)

| Print | Arquivo | O que o trecho mostra |
|---|---|---|
| [PRINT 2.1.a] | `Regras/pii.json` | Uma regra por tipo de dado: padrão, validador, máscara e severidade de 1 a 5. CPF 5 com dígito válido e 4 só no formato, CNS 5, credencial 5, saúde 4, mora sozinho 4, RG 4, endereço 3, telefone 3, nome 3. |
| [PRINT 2.1.b] | `MotorDeParsing.cs` | Aplica as regras a um texto ou a um JSON e devolve os achados com tipo, campo, valor mascarado e severidade, nunca o valor em claro. O validador de CPF confere os dígitos verificadores e só sobe a severidade. |
| [PRINT 2.1.c] | `InterceptadorDeEntrada.cs` | Middleware que lê o corpo de toda requisição e mascara CPF, RG, CNS e credencial em qualquer campo antes do log e do banco. A regra de credencial casa com o nome inteiro da chave e nunca mascara `casaToken`. |

Frase para o PDF: [explique em duas frases por que nome e endereço não são mascarados na entrada: a lista de nomes precisa deles, e o rótulo é que impede a saída. Fonte: mudança 14 da seção 9 do modelo de domínio].

### 2.2 Taint analysis (3,0)

| Print | Arquivo | O que o trecho mostra |
|---|---|---|
| [PRINT 2.2.a] | `TextoRotulado.cs` | Um texto com o conjunto de tipos de dado que carrega. O valor é `internal`. Juntar dois textos une os rótulos. `ToString()` devolve a máscara. |
| [PRINT 2.2.b] | `PoliticaDeCanal` | O dicionário de canais: `GrupoDoBairro`, `PromptDoLlm`, `Mqtt`, `MensagemDeErro`, `RespostaSemAutenticacao` e `Vizinho` não aceitam rótulo; `Log` não aceita rótulo; `AppAutenticado` aceita nome, endereço e telefone; `Banco` aceita todos. |
| [PRINT 2.2.c] | `SaidaSegura.cs` | A única porta por onde o valor sai. Consulta a política, devolve o veredito e grava `SaidaBloqueada` em todo bloqueio. |
| [PRINT 2.2.d] | `ProvedorDeLogSeguro` | Decorador do logger: um argumento rotulado vai ao log como máscara e grava um bloqueio no canal `Log`. |
| [PRINT 2.2.e] | `FiltroDeResposta.cs` e `TratadorDeErros.cs` | Bloqueio de texto rotulado em rota sem autenticação e troca de mensagem de exceção com dado pessoal por texto neutro. |
| [PRINT 2.2.f] | `Corredor.Cadastro`, entidade `Idoso` | As fontes: `Nome`, `Condicoes`, `Medicamentos`, `Casa.Endereco` e os dados do vizinho têm o tipo `TextoRotulado`. O rótulo vem do campo. |

Frase para o PDF: [fontes, sumidouros e a regra que liga os dois. Fontes: os campos `TextoRotulado` e o corpo das requisições. Sumidouros: log, resposta sem autenticação, mensagem de erro, MQTT, grupo do bairro, prompt do LLM, vizinho].

### 2.3 IA na remediação (2,0)

| Print | Arquivo | O que o trecho mostra |
|---|---|---|
| [PRINT 2.3.a] | `Tradutor.cs` | Monta o prompt só com tipo de dado, canal e módulo, e pede duas frases para um cidadão leigo de 60 anos. O prompt passa pela saída segura no canal `PromptDoLlm`. |
| [PRINT 2.3.b] | `Tradutor.cs` ou a fila em memória | Roda depois da resposta HTTP. Sem resposta do LLM em 5 segundos, anexa a frase fixa do tipo de evento. |
| [PRINT 2.3.c] | Configuração | A chave vem de `Llm__ApiKey`, no ambiente da VM. Mostre a linha de configuração sem o valor. |

### 2.4 Dashboard Risco Cidadão (2,0)

| Print | Arquivo | O que o trecho mostra |
|---|---|---|
| [PRINT 2.4.a] | `Farol` | Função pura: vermelho se existe saída bloqueada de severidade 4 ou 5 nas últimas 24 horas, amarelo com qualquer outro evento, verde sem evento. |
| [PRINT 2.4.b] | `Painel.cs` e `painel/index.html` | `GET /api/painel` sem autenticação e a página que mostra o farol e a mensagem de cada evento. Nenhum campo carrega valor pessoal. |
| [PRINT 2.4.c] | `EventoDeSeguranca` | O registro sem valor nem trecho do texto: tipo de dado, canal, módulo, rota modelo e severidade. A aplicação não altera nem apaga o evento. |

## 3. Auditoria

Cada evidência tem o comando, o print e a frase do resultado. As quatro primeiras são as de [arquitetura.md](../arquitetura.md), seção "Evidências para a auditoria". As outras vêm dos casos 38 a 42 do [modelo de domínio](../arquitetura/03-modelo-de-dominio.md) e dos Test Cases de [casos-de-teste.md](../qa/casos-de-teste.md), Features F23 a F26.

### 3.1 CPF mascarado na entrada

- Comando: `POST /api/idosos` com `"condicoes": "CPF 123.456.789-00 informado pela filha"` e nome e endereço da carga simulada.
- [PRINT 3.1.a] Log da API (`sudo corredor logs api`) com `***.456.***-00`.
- [PRINT 3.1.b] Linha da tabela `cadastro.idoso` com `***.456.***-00`, nome e endereço inteiros.
- [PRINT 3.1.c] Painel com um mascaramento no canal `Banco` e o farol amarelo.
- Resultado: [uma frase]. Caso do modelo 38.

### 3.2 Nome barrado no log

- Comando: o teste automatizado que grava `Idoso.Nome` no log (`dotnet test` em `Corredor.Guarda.Testes`).
- [PRINT 3.2.a] Saída do teste e a linha de log com `[NOME]`.
- [PRINT 3.2.b] Painel com um bloqueio no canal `Log`.
- Resultado: [uma frase]. Caso do modelo 40.

### 3.3 Resposta sem autenticação bloqueada

- Comando: chamar a rota sem autenticação que tentaria devolver `Idoso.Condicoes`.
- [PRINT 3.3.a] Resposta bloqueada.
- [PRINT 3.3.b] Evento `SaidaBloqueada` no canal `RespostaSemAutenticacao`.
- Resultado: [uma frase]. Caso do modelo 41.

### 3.4 Mensagem para o grupo do bairro

- Comando: no app, ou por `POST /api/mensagens/verificar` com `{"canal":"GrupoDoBairro","texto":"ele mora sozinho e toma diurético"}`.
- [PRINT 3.4.a] Resposta com `veredito` `Bloqueada`, `tiposDeDado` `MoraSozinho` e `Saude`, a explicação e a versão segura.
- [PRINT 3.4.b] Linha de `cadastro.mensagem_saida` só com o texto mascarado.
- [PRINT 3.4.c] Painel vermelho com a mensagem traduzida pelo LLM.
- Resultado: [uma frase]. Caso do modelo 39.

### 3.5 LLM fora do ar

- Comando: remover a chave do LLM, reiniciar a API e repetir a evidência 3.4.
- [PRINT 3.5] Painel com a frase fixa do tipo de evento.
- Resultado: [uma frase]. Caso do modelo 42.

### 3.6 O prompt e o painel não levam valor

- [PRINT 3.6.a] O prompt enviado ao LLM, tirado do log de depuração ou do teste: só tipo de dado, canal e módulo.
- [PRINT 3.6.b] O JSON de `GET /api/painel`: nenhum nome, CPF, endereço, telefone ou token.

### 3.7 Rastreabilidade com os Test Cases

Uma tabela curta com a evidência, o Test Case que a verifica e o resultado da execução no Azure Test Plans:

| Evidência | Test Case | Resultado |
|---|---|---|
| 3.1 | [id do caso em F23] | [Passed ou Failed] |
| 3.2 | [id do caso em F24] | |
| 3.3 | [id do caso em F24] | |
| 3.4 | [id do caso em F25] | |
| 3.5 | [id do caso em F26] | |
| 3.6 | [id do caso em F26] | |

## 4. Vídeo do Dashboard

- Link: [YouTube].
- Duração: [mm:ss].
- O que o vídeo mostra, com o tempo de cada cena: farol verde, CPF mascarado e farol amarelo, mensagem bloqueada e farol vermelho com a tradução, o evento sem valor, log e resposta sem autenticação, a frase fixa sem o LLM. O roteiro está em [roteiros.md](roteiros.md), seção "Dashboard de Cyber".

## 5. Limitações declaradas

Escreva as três, porque a banca pergunta:

1. O token da casa viaja no nome do tópico MQTT por um broker público. Quem assina `corredor/v1/#` lê o token e pode publicar um movimento falso. O dano é limitado: token de 128 bits, pacote de token desconhecido descartado, e nenhum pacote muda perfil ou cadastro. A correção é assinar o pacote com um segredo por casa (seção 8.4 do modelo de domínio).
2. Nome e dados de saúde não são cifrados no banco na entrega. A chave ficaria na mesma VM. O caminho para cifrar é o conversor do EF Core (seção 11.3 de [04-modelo-de-dados.md](../arquitetura/04-modelo-de-dados.md)).
3. O SQLite do celular não é cifrado. A proteção é a área privada do app e o bloqueio do aparelho (seção 6.3 de 04-modelo-de-dados.md).

## 6. Código

Entregue separado do PDF: o link https://github.com/IamP5/corredor-aceso ou o ZIP do repositório.
