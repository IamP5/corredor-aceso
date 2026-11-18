# Caminho sem rede do app da ACS

Rodado em 17/11/2026 no alvo web do Expo, com o Chrome numa janela de 390 por 844 pontos. O app se comunicou por HTTP com o servidor mock (`npm run api:mock`), que responde nas rotas da seção 6.3 do modelo de domínio. Os dados são simulados.

O relógio do mock começa às 10h31 de um dia crítico. A hora da visita é a do celular, que estava perto de 00h50. Com a API real, o servidor soma o deslocamento do relógio de demo à hora da visita (seção 5.9 do modelo de domínio), e o app desconhece esse deslocamento.

## Caso 1: celular sem internet

O corte de rede foi feito com a emulação "Offline" do DevTools, que também avisa o `NetInfo` do app.

| Passo | O que aconteceu | Evidência |
|---|---|---|
| 1. Carregar o radar | `GET /api/lista-de-nomes`, `GET /api/radar` e `GET /api/avisos` responderam 200. O radar foi gravado no SQLite. | [03-radar.png](03-radar.png) |
| 2. Cortar a rede | O app mostrou "Sem internet". O radar continuou na tela, lido do SQLite. A linha do tempo, que não fica no celular, avisou que precisa de conexão. | [12-offline-visita-sem-internet.png](12-offline-visita-sem-internet.png) |
| 3. Registrar a visita | A ACS marcou "Estava bem" na casa de Neusa Ipê. O desfecho foi gravado com `estado = 'pendente'`, e o card mostrou "Ainda não enviado". | [13-offline-visita-na-fila.png](13-offline-visita-na-fila.png) |
| 4. Esperar | O app tentou o `POST /api/visitas` a cada 15 segundos, 11 vezes, todas com `net::ERR_INTERNET_DISCONNECTED`. Depois de 2 minutos, o título virou "Radar das 00h46, sem conexão". | [11-offline-radar-antigo.png](11-offline-radar-antigo.png) |
| 5. Voltar a rede | O `NetInfo` avisou a volta. O app mandou o desfecho, recebeu 201 e baixou o radar de novo. A casa saiu de "Precisam de visita" e entrou em "Já atendidas", com "Enviado". | [14-offline-visita-enviada.png](14-offline-visita-enviada.png) |

A tentativa que falhou e a que deu certo levaram o mesmo `id`, gerado no celular. É a chave de idempotência da visita:

```json
{"id":"963870e1-36a5-4a33-971f-d2a4ffa2b8c1","casaToken":"e7dcba79752d871f0f828493edc6bf28","ocorridaEm":"2026-11-17T00:46:37-03:00","desfecho":"EstavaBem","observacao":"Concentrador ligado na bateria. Energia voltou às 10h40."}
```

Resposta do envio que deu certo:

```json
{"id":"963870e1-36a5-4a33-971f-d2a4ffa2b8c1","registradaEm":"2026-11-17T10:33:53-03:00"}
```

Log do servidor mock. Nenhum `POST` chegou enquanto a rede estava cortada:

```text
2026-11-17T03:46:18.081Z GET /api/lista-de-nomes 200
2026-11-17T03:46:18.081Z GET /api/radar 200
2026-11-17T03:46:18.081Z GET /api/avisos 200
2026-11-17T03:48:59.554Z POST /api/visitas 201
2026-11-17T03:48:59.568Z GET /api/lista-de-nomes 200
2026-11-17T03:48:59.569Z GET /api/radar 200
2026-11-17T03:48:59.570Z GET /api/avisos 200
```

## Caso 2: internet ligada e API fora do ar

O servidor mock foi derrubado com a rede do celular ligada. Nesse caso o `NetInfo` não avisa nada, e quem reenvia é a tentativa a cada 15 segundos.

| Passo | O que aconteceu | Evidência |
|---|---|---|
| 1. Derrubar a API | O processo do servidor mock foi encerrado. | |
| 2. Registrar a visita | A ACS marcou "Estava fora" na casa de Benedito Quaresmeira. O card mostrou "Ainda não enviado". | [15-api-fora-visita-na-fila.png](15-api-fora-visita-na-fila.png) |
| 3. Subir a API | Na tentativa seguinte, o app mandou o desfecho e baixou o radar. "Estava fora" atende o alerta de silêncio, então a casa entrou em "Já atendidas". | [16-api-volta-visita-enviada.png](16-api-volta-visita-enviada.png) |

```text
mock da API em http://localhost:3001/api
2026-11-17T03:51:05.876Z POST /api/visitas 201
2026-11-17T03:51:05.885Z GET /api/lista-de-nomes 200
2026-11-17T03:51:05.885Z GET /api/radar 200
2026-11-17T03:51:05.885Z GET /api/avisos 200
```

## Como repetir

Da pasta `app/`, em dois terminais:

```bash
npm run api:mock -- 3001
```

```bash
EXPO_PUBLIC_API_URL=http://localhost:3001 npx expo start --web
```

No Chrome, abra o DevTools, carregue o radar, mude a rede para "Offline", registre uma visita e volte a rede para "No throttling".
