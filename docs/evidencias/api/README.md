# Evidências do Swagger

Prints da documentação automática da API, tirados em 17/11/2026 com o Chrome sem janela, contra a API que `bash tools/demo.sh subir` deixou rodando em `http://localhost:5080`, no ambiente `Development`. O script que tira os prints é o `tools/demo/swagger.mjs`.

| Arquivo | Mostra |
|---|---|
| [01-swagger-visao-geral.png](01-swagger-visao-geral.png) | `/swagger` até a lista de schemas: as 21 rotas dos três módulos, agrupadas por Motor, Cadastro e Guarda, e o botão **Authorize** do token da ACS |
| [02-swagger-cadastro.png](02-swagger-cadastro.png) | `POST /api/idosos` do Cadastro aberta: a descrição da rota, o corpo do pedido e as respostas 201 e 422 |
| [03-swagger-motor.png](03-swagger-motor.png) | `GET /api/radar` do Motor aberta: o parâmetro `microarea` e o formato da resposta 200 |

O documento OpenAPI que a página lê fica em `/openapi/v1.json`. O teste `Swagger_publica_o_documento_openapi`, em `api/tests/Corredor.Api.Testes/HostTestes.cs`, confere que ele é publicado.

Para repetir:

```bash
bash tools/demo.sh subir
CORREDOR_API_URL=http://localhost:5080 node tools/demo/swagger.mjs docs/evidencias/api
bash tools/demo.sh parar --tudo
```
