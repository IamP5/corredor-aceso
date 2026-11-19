# Roteiro do vídeo de Physical Computing (até 4 minutos)

Roteiro de gravação de uso da equipe. Saiu de `iot/README.md`.

Antes de gravar, prepare:
- o Wokwi no VS Code com `iot/no/wokwi.toml` (F1, "Wokwi: Select Config File") e o firmware compilado em
  `iot/no/build`. O gateway do VS Code é privado e não depende do Wi-Fi público do wokwi.com;
- o [painel](https://iamp5.github.io/corredor-aceso/painel/) aberto ao lado, já limpo com "Encerrar alertas";
- a tela dividida: simulador à esquerda, com o monitor serial visível, e painel à direita.

Esta entrega não usa a API nem a VM. O link entregue continua sendo o projeto público do Wokwi.

| Tempo | Na tela | Fala |
|---|---|---|
| 0:00 a 0:20 | Foto ou desenho do corredor. Depois, o circuito no Wokwi. | "Dona Efigênia mora sozinha e usa concentrador de oxigênio. Se ela cair, ninguém sabe. Este aparelho fica no corredor. Ela não precisa usar celular nem lembrar de nada." |
| 0:20 a 0:50 | Aponta cada peça do circuito. | "O sensor de presença acende a luz de caminho. O DHT22 mede o calor dentro de casa. O potenciômetro faz o papel do sensor de tensão da tomada do concentrador. O pingente, um MPU6050, sente o impacto de um tombo. A tela, o LED âmbar e o buzzer mostram o que está acontecendo. O botão verde é o 'estou bem'." |
| 0:50 a 1:10 | Inicia a simulação. Serial: `[pecas] tela ok, pingente ok`, `[mqtt] conectado`. O painel muda para "Tudo bem" e mostra os sensores. | "O aparelho publica por MQTT num broker público. Do outro lado, este painel é o que a agente de saúde vê: a situação da casa, os sensores e a linha do tempo. Só o token da casa trafega, nunca o nome." |
| 1:10 a 1:30 | Clica no PIR. A luz branca acende. A linha do tempo do painel mostra "Passou no corredor", e o último sinal de vida vira "há 0 s". | "Ela passa, a luz acende, e essa passagem é o sinal de vida do dia." |
| 1:30 a 2:15 | MPU6050: Z de 1 g para 2 g. A tela mostra "QUEDA?" com a contagem, e o buzzer bipa. O painel mostra "Possível queda". Espera os 15 s: a tela mostra "QUEDA", e o painel fica vermelho com "Queda". Volta Z para 1 g e segura o botão meio segundo. | "Num tombo, o pingente sente o impacto. O aparelho espera 15 segundos: se ninguém aperta o botão, o aviso de queda chega na hora para a agente." |
| 2:15 a 2:40 | Gira o potenciômetro para 0 V. A luz branca acende e fica acesa, a tela mostra "SEM LUZ". O painel mostra "Sem energia". Volta a tensão: a luz apaga. | "Acabou a energia. A luz de emergência acende sozinha, sem internet, e a agente vê no painel que o concentrador ficou sem tomada." |
| 2:40 a 3:10 | No painel, clica em "Declarar dia crítico". O âmbar acende, a tela mostra "DIA QUENTE". Muda o DHT22 para 35 °C: tela "CALOR", painel "Muito quente". | "No dia crítico de calor, a UBS declara pelo painel e o âmbar acende na casa. Se a casa passa de 34 graus, o aparelho mostra sozinho." |
| 3:10 a 3:40 | Desliga a chave de Wi-Fi, clica no PIR, religa. O serial mostra `guardado` e depois o reenvio. O painel marca o evento como "guardado sem rede". | "Sem internet, nada se perde: o aparelho guarda os eventos e reenvia com a hora certa quando a rede volta." |
| 3:40 a 4:00 | DHT22 de volta a 24 °C. No painel, clica em "Encerrar alertas": o âmbar apaga e o painel volta a "Tudo bem". | "Resolvido, a agente encerra os alertas. O ESP32 cuida da casa sozinho e conta tudo para quem cuida dela." |
