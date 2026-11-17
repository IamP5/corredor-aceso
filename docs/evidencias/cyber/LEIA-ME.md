# Evidências da Guarda para Cybersecurity

Capturas de 17/11/2026 contra a API rodando em `Development`, com um Postgres 16 descartável e a chave do OpenRouter lida do `.env` da raiz, que não entra no Git. O código está em `api/src/Corredor.Guarda` e os testes em `api/tests/Corredor.Guarda.Testes`.

| Arquivo | O que mostra | Ponto da rubrica |
|---|---|---|
| `evidencia-1-documento.txt` | `"documento": "123.456.789-00"` chega ao binder como `***.456.***-00`. O log tem só a máscara. O evento `MascaramentoNaEntrada` grava o tipo, sem o valor. | Parsing de PII |
| `evidencia-2-log-nome.txt` | Um log com `Idoso.Nome` sai como `Visita registrada para [NOME]` e registra um `BloqueioDeSaida` no canal `Log`. | Taint |
| `evidencia-3-rota-aberta.txt` | A rota sem token que devolveria condições de saúde responde 403 `RESPOSTA_BLOQUEADA`. Com token, a rota responde e o valor continua fora do JSON. | Taint |
| `evidencia-4-grupo-do-bairro.txt` | "ele mora sozinho e toma diurético" para o grupo do bairro sai `Bloqueada`, com a explicação, e o painel fica vermelho. | Taint e painel |
| `evidencia-integrada-cadastro.txt` | As evidências 1 e 4 pelas rotas reais do Cadastro: a linha de `cadastro.idoso` guarda `documento ***.456.***-00` com nome e endereço inteiros, e `cadastro.mensagem_saida` guarda só o texto mascarado. | Parsing de PII e taint |
| `traducao-openrouter.txt` | A mensagem de sistema, o prompt de um evento (sem o dado), as quatro chamadas com resposta 200 e as traduções gravadas. | IA na remediação |
| `painel-1-verde.png`, `painel-2-amarelo.png`, `painel-3-vermelho.png` | O painel Risco Cidadão nos três estados: sem evento, depois do mascaramento na entrada, e depois das saídas bloqueadas de severidade 4. | Painel |

## Como repetir

1. Suba a API em `Development` com o banco migrado (`npm run dev:deps` e `npm run dev:api`, como no `api/README.md`).
2. Rode o roteiro. Ele grava os quatro arquivos `evidencia-*.txt` nesta pasta:

   ```bash
   API=http://localhost:5000 LOG_DA_API=<saída da API> PSQL="psql <conexão do banco>" bash docs/evidencias/cyber/reproduzir.sh
   ```

3. Abra o painel em `http://localhost:5000/api/guarda/demo/painel`. Na VM, o Nginx serve a mesma página em `/painel/`.

As rotas `/api/guarda/demo/*` só existem em `Development`. Elas reproduzem cada evidência sem depender das rotas dos outros módulos.

## Limites que o PDF deve dizer

- O `evidencia-integrada-cadastro.txt` saiu de uma junção local das branches da Guarda, do Cadastro e do Motor feita só para a captura. A junção oficial é do orquestrador.
- Na captura, o Motor rodou sem broker MQTT. Nenhuma evidência da Guarda depende dele.
- A regra de nome no texto livre só acha nome depois de tratamento, como "Dona Aparecida". Um nome solto num texto livre não é achado pelas regras. O nome do cadastro é protegido pelo rótulo do campo, que não depende de regra.
- O interceptador lê corpo JSON e texto. Query string e cabeçalho não passam pelo `MotorDeParsing`.
