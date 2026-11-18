# Documentação de Cybersecurity

Este documento organiza a entrega de Cybersecurity do Corredor Aceso em três pontos: Source, Auditoria e Vídeo do Dashboard. O enunciado pede a documentação em PDF, com prints do código e das evidências, e o código em arquivo separado, por ZIP ou link do GitHub ([entregaveis.md](../entregaveis.md)). Os prints não mostram chave de API, token de casa nem nome de pessoa fora da carga simulada.

---

## Identificação

- Corredor Aceso, Global Solution CitizenTech, FIAP 2026, Engenharia de Software, 3º ano.
- Disciplina: Cybersecurity.
- Integrantes: Bruno Dominicheli (RM554981), Miguel Kapicius (RM556198) e Thiago Ferreira (RM555608).
- Repositório: https://github.com/IamP5/corredor-aceso

## 1. Contexto

A ACS cadastra idosos que moram sozinhos, com condições de saúde e remédios. Esse é o dado que mais atrai golpe e violência. O módulo Guarda tem três funções: detecta e mascara PII no que entra, impede que um texto com dado pessoal saia por um canal inseguro e traduz cada bloqueio para o cidadão com um LLM. Todo dado de pessoa no projeto é simulado.

## 2. Source

Os arquivos ficam em `api/src/Corredor.Guarda/`. Cada tabela aponta o arquivo e o que o trecho de código demonstra.

### 2.1 Parsing de PII

| Arquivo | O que o trecho mostra |
|---|---|
| `Regras/pii.json` | Uma regra por tipo de dado: padrão, validador, máscara e severidade de 1 a 5. CPF 5 com dígito válido e 4 só no formato, CNS 5, credencial 5, saúde 4, mora sozinho 4, RG 4, endereço 3, telefone 3, nome 3. |
| `MotorDeParsing.cs` | Aplica as regras a um texto ou a um JSON e devolve os achados com tipo, campo, valor mascarado e severidade, nunca o valor em claro. O validador de CPF confere os dígitos verificadores e só sobe a severidade. |
| `InterceptadorDeEntrada.cs` | Middleware que lê o corpo de toda requisição e mascara CPF, RG, CNS e credencial em qualquer campo antes do log e do banco. A regra de credencial casa com o nome inteiro da chave e nunca mascara `casaToken`. |

Nome e endereço não são mascarados na entrada, porque a lista de nomes do celular da ACS depende deles. O rótulo de dado pessoal é o mecanismo que impede a saída desses valores.

### 2.2 Taint analysis

| Arquivo | O que o trecho mostra |
|---|---|
| `TextoRotulado.cs` | Um texto com o conjunto de tipos de dado que carrega. O valor é `internal`. Juntar dois textos une os rótulos. `ToString()` devolve a máscara. |
| `PoliticaDeCanal` | O dicionário de canais: `GrupoDoBairro`, `PromptDoLlm`, `Mqtt`, `MensagemDeErro`, `RespostaSemAutenticacao` e `Vizinho` não aceitam rótulo; `Log` não aceita rótulo; `AppAutenticado` aceita nome, endereço e telefone; `Banco` aceita todos. |
| `SaidaSegura.cs` | A única porta por onde o valor sai. Consulta a política, devolve o veredito e grava `SaidaBloqueada` em todo bloqueio. |
| `ProvedorDeLogSeguro` | Decorador do logger: um argumento rotulado vai ao log como máscara e grava um bloqueio no canal `Log`. |
| `FiltroDeResposta.cs` e `TratadorDeErros.cs` | Bloqueio de texto rotulado em rota sem autenticação e troca de mensagem de exceção com dado pessoal por texto neutro. |
| `Corredor.Cadastro`, entidade `Idoso` | As fontes: `Nome`, `Condicoes`, `Medicamentos`, `Casa.Endereco` e os dados do vizinho têm o tipo `TextoRotulado`. O rótulo vem do campo. |

As fontes são os campos `TextoRotulado` e o corpo das requisições. Os sumidouros são o log, a resposta sem autenticação, a mensagem de erro, o MQTT, o grupo do bairro, o prompt do LLM e o vizinho. A política de canal liga os dois: a saída segura só entrega um texto rotulado a um canal que aceita os tipos de dado do rótulo.

### 2.3 IA na remediação

| Arquivo | O que o trecho mostra |
|---|---|
| `Tradutor.cs` | Monta o prompt só com tipo de dado, canal e módulo, e pede duas frases para um cidadão leigo de 60 anos. O prompt passa pela saída segura no canal `PromptDoLlm`. |
| `Tradutor.cs` ou a fila em memória | Roda depois da resposta HTTP. Sem resposta do LLM em 5 segundos, anexa a frase fixa do tipo de evento. |
| Configuração | A chave vem de `Llm__ApiKey`, no ambiente da VM. O print mostra a linha de configuração sem o valor. |

### 2.4 Dashboard Risco Cidadão

| Arquivo | O que o trecho mostra |
|---|---|
| `Farol` | Função pura: vermelho se existe saída bloqueada de severidade 4 ou 5 nas últimas 24 horas, amarelo com qualquer outro evento, verde sem evento. |
| `Painel.cs` e `painel/index.html` | `GET /api/painel` sem autenticação e a página que mostra o farol e a mensagem de cada evento. Nenhum campo carrega valor pessoal. |
| `EventoDeSeguranca` | O registro sem valor nem trecho do texto: tipo de dado, canal, módulo, rota modelo e severidade. A aplicação não altera nem apaga o evento. |

## 3. Auditoria

Cada evidência descreve o comando executado e o que a captura mostra. As quatro primeiras correspondem às da [visão geral](../arquitetura/01-visao-geral.md#arquivos-da-guarda), seção "Arquivos da Guarda". As demais vêm dos casos 38 a 42 do [modelo de domínio](../arquitetura/03-modelo-de-dominio.md) e dos Test Cases de [casos-de-teste.md](../qa/casos-de-teste.md), Features F23 a F26. As capturas registradas estão em [docs/evidencias/cyber/](../evidencias/cyber/LEIA-ME.md).

### 3.1 CPF mascarado na entrada

- Comando: `POST /api/idosos` com `"condicoes": "CPF 123.456.789-00 informado pela filha"` e nome e endereço da carga simulada.
- Log da API (`sudo corredor logs api`) com `***.456.***-00`.
- Linha da tabela `cadastro.idoso` com `***.456.***-00`, nome e endereço inteiros.
- Painel com um mascaramento no canal `Banco` e o farol amarelo.
- Caso do modelo 38.

### 3.2 Nome barrado no log

- Comando: o teste automatizado que grava `Idoso.Nome` no log (`dotnet test` em `Corredor.Guarda.Testes`).
- Saída do teste e a linha de log com `[NOME]`.
- Painel com um bloqueio no canal `Log`.
- Caso do modelo 40.

### 3.3 Resposta sem autenticação bloqueada

- Comando: chamar a rota sem autenticação que tentaria devolver `Idoso.Condicoes`.
- Resposta bloqueada.
- Evento `SaidaBloqueada` no canal `RespostaSemAutenticacao`.
- Caso do modelo 41.

### 3.4 Mensagem para o grupo do bairro

- Comando: no app, ou por `POST /api/mensagens/verificar` com `{"canal":"GrupoDoBairro","texto":"ele mora sozinho e toma diurético"}`.
- Resposta com `veredito` `Bloqueada`, `tiposDeDado` `MoraSozinho` e `Saude`, a explicação e a versão segura.
- Linha de `cadastro.mensagem_saida` só com o texto mascarado.
- Painel vermelho com a mensagem traduzida pelo LLM.
- Caso do modelo 39.

### 3.5 LLM fora do ar

- Comando: remover a chave do LLM, reiniciar a API e repetir a evidência 3.4.
- Painel com a frase fixa do tipo de evento.
- Caso do modelo 42.

### 3.6 O prompt e o painel não levam valor

- O prompt enviado ao LLM, extraído do log de depuração ou do teste, contém só tipo de dado, canal e módulo.
- O JSON de `GET /api/painel` não contém nome, CPF, endereço, telefone nem token.

### 3.7 Rastreabilidade com os Test Cases

| Evidência | Feature dos Test Cases |
|---|---|
| 3.1 | F23 |
| 3.2 | F24 |
| 3.3 | F24 |
| 3.4 | F25 |
| 3.5 | F26 |
| 3.6 | F26 |

## 4. Vídeo do Dashboard

O vídeo mostra, em sequência, o farol verde, o CPF mascarado com o farol amarelo, a mensagem bloqueada com o farol vermelho e a tradução, o evento sem valor, o log e a resposta sem autenticação, e a frase fixa sem o LLM.

## 5. Limitações

1. O token da casa viaja no nome do tópico MQTT por um broker público. Quem assina `corredor/v1/#` lê o token e pode publicar um movimento falso. O dano é limitado: token de 128 bits, pacote de token desconhecido descartado, e nenhum pacote muda perfil ou cadastro. A correção é assinar o pacote com um segredo por casa (seção 8.2 do modelo de domínio).
2. Nome e dados de saúde não são cifrados no banco nesta entrega. A chave ficaria na mesma VM. O caminho para cifrar é o conversor do EF Core (seção "Verificação realizada e limitações" de [04-modelo-de-dados.md](../arquitetura/04-modelo-de-dados.md#verificação-realizada-e-limitações)).
3. O SQLite do celular não é cifrado. A proteção é a área privada do app e o bloqueio do aparelho (seção 6.3 de 04-modelo-de-dados.md).

As limitações das capturas, como o alcance da regra de nome no texto livre, estão em [docs/evidencias/cyber/LEIA-ME.md](../evidencias/cyber/LEIA-ME.md).

## 6. Código

O código é entregue separado do PDF, pelo link https://github.com/IamP5/corredor-aceso ou pelo ZIP do repositório.
