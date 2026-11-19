# Roteiro do vídeo de Physical Computing (até 4 minutos)

Roteiro de gravação de uso da equipe. Saiu de `iot/README.md`.

Antes de gravar:
- rode uma simulação só, no wokwi.com ou no VS Code. As duas usam o mesmo ID de cliente MQTT e se derrubam;
- no wokwi.com, o projeto público tem `SILENCIO_DEMO_S 90`: com 90 s sem passagem nem botão, o buzzer chama
  sozinho. O roteiro abaixo nunca passa de 70 s sem sinal de vida. Se tocar, aperte o botão;
- abra o [painel](https://iamp5.github.io/corredor-aceso/painel/) ao lado e clique em "Encerrar alertas";
- deixe o simulador à esquerda, com o monitor serial visível, e o painel à direita;
- segure o botão verde por meio segundo. Um clique rápido demais é ignorado.

Se o Wi-Fi do wokwi.com mostrar "IoT Gateway ... closed the connection", use o VS Code com
`iot/no/wokwi.toml`. Esta entrega não usa a API nem a VM.

| Tempo | Na tela | Fala |
|---|---|---|
| 0:00 a 0:20 | O circuito parado. | "Dona Efigênia mora sozinha e usa concentrador de oxigênio. Se ela cair, ninguém sabe. Este aparelho fica no corredor. Ela não precisa usar celular nem lembrar de nada." |
| 0:20 a 0:50 | Aponta cada peça. | "O sensor de presença acende a luz de caminho. O DHT22 mede o calor dentro de casa. O potenciômetro faz o papel do sensor de tensão da tomada do concentrador. O pingente, um MPU6050, sente o impacto de um tombo. A tela, o LED âmbar e o buzzer mostram o que está acontecendo. O botão verde é o 'estou bem'." |
| 0:50 a 1:10 | Inicia a simulação. Serial: `[mqtt] conectado`. A tela mostra "TUDO BEM", e o painel muda para "Tudo bem". | "O aparelho publica por MQTT num broker público. Este painel é o que a agente de saúde vê. Só o token da casa trafega, nunca o nome." |
| 1:10 a 1:25 | PIR, "Simulate motion". A luz branca acende. O painel mostra "Passou no corredor". | "Ela passa, a luz acende, e essa passagem é o sinal de vida do dia." |
| 1:25 a 2:10 | MPU6050: Z para 2 g. Tela "QUEDA?" com a contagem, buzzer bipando, painel "Possível queda". Espera 15 s: tela "QUEDA", painel vermelho. Z de volta para 1 g e segura o botão. | "Num tombo, o pingente sente o impacto. O aparelho espera 15 segundos. Ninguém apertou o botão, e o aviso de queda chega na hora para a agente." |
| 2:10 a 2:35 | Potenciômetro todo para a esquerda. A luz branca acende e fica, a tela mostra "SEM LUZ", o painel mostra "Sem energia". Volta o potenciômetro: a luz apaga. | "Acabou a energia. A luz de emergência acende sozinha, sem internet, e a agente vê que o concentrador ficou sem tomada." |
| 2:35 a 3:00 | Painel: "Declarar dia crítico". O âmbar acende, a tela mostra "DIA QUENTE". DHT22 acima de 34 °C: tela "CALOR", painel "Muito quente". DHT22 de volta para menos de 34 °C. | "Em dia crítico de calor, a UBS avisa pelo painel e o âmbar acende na casa. Se a casa passa de 34 graus, o aparelho mostra sozinho." |
| 3:00 a 3:15 | Painel: "Pedir um toque no botão". O buzzer bipa. Segura o botão: o painel mostra "Apertou o botão". | "A agente pode pedir um sinal. Um toque no botão responde que está tudo bem." |
| 3:15 a 3:45 | Chave do Wi-Fi para o outro lado. PIR. Chave de volta. Serial: `guardado (1 no buffer)` e depois `reenviados com a hora original`. Painel: "guardado sem rede, enviado depois". | "Sem internet, nada se perde. O aparelho guarda e reenvia com a hora certa quando a rede volta." |
| 3:45 a 4:00 | Painel: "Encerrar alertas". O âmbar apaga, o painel volta a "Tudo bem". | "Resolvido, a agente encerra os alertas. O ESP32 cuida da casa sozinho e conta tudo para quem cuida dela." |
