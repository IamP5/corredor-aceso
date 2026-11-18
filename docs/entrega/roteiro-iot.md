# Roteiro do vídeo de Physical Computing (até 4 minutos)

Roteiro de gravação de uso da equipe. Saiu de `iot/README.md`.

Antes de gravar, prepare:
- o projeto público do Wokwi aberto, com a casa de teste e `SILENCIO_DEMO_S 90`;
- o [painel](https://iamp5.github.io/corredor-aceso/painel/) aberto ao lado;
- a tela dividida: Wokwi à esquerda, com o monitor serial visível, e painel à direita.

Esta entrega não usa a API nem a VM. Se o Wi-Fi do Wokwi mostrar "IoT Gateway ... closed the connection",
reinicie a simulação antes de gravar.

| Tempo | Na tela | Fala |
|---|---|---|
| 0:00 a 0:20 | Foto ou desenho do corredor. Depois, o circuito no Wokwi. | "Dona Efigênia mora sozinha e usa concentrador de oxigênio. Se ela cair, ninguém sabe. Este aparelho fica no corredor. Ela não precisa usar celular nem lembrar de nada." |
| 0:20 a 0:55 | Aponta cada peça do circuito. | "O sensor de presença acende a luz de caminho. O DHT22 mede o calor dentro de casa. O potenciômetro faz o papel do sensor de tensão da tomada do concentrador. O pingente, um MPU6050, sente o impacto de um tombo. O sensor ultrassônico fica no teto e mede a altura do que está embaixo: se tem alguém deitado no chão, ele percebe. A tela, o LED âmbar e o buzzer falam com ela. O botão verde é o 'estou bem'." |
| 0:55 a 1:20 | Inicia a simulação. Serial: `[pecas] tela ok, pingente ok`, `[mqtt] conectado`. O painel muda para "Tudo bem" e mostra os sensores. | "O aparelho publica por MQTT num broker público. Do outro lado, este painel é o que a agente de saúde vê: a situação da casa, os sensores e a linha do tempo. Só o token da casa trafega, nunca o nome." |
| 1:20 a 1:40 | Clica no PIR. A luz acende. A linha do tempo do painel mostra "Passou no corredor", e o último sinal de vida vira "há 0 s". | "Ela passa, a luz acende, e essa passagem é o sinal de vida do dia." |
| 1:40 a 2:25 | MPU6050: Z de 1 g para 2 g. A tela mostra "CAIU?", e o buzzer bipa. O painel mostra "Possível queda". Espera os 15 s: a tela mostra "QUEDA!", e o painel fica vermelho com "Queda avisada". Volta Z para 1 g e aperta o botão. | "Num tombo, o pingente sente o impacto. Antes de chamar ajuda, o aparelho pergunta: caiu? Se ela não responde em 15 segundos, o aviso de queda chega na hora para a agente." |
| 2:25 a 2:50 | HC-SR04 em 220 cm. Comenta enquanto espera e mostra no painel "alguém deitado no chão". Depois de 30 s, "CAIU?". Aperta o botão: "cancelada". Volta para 400 cm. | "E se ela cair sem o pingente? O sensor de teto vê alguém deitado no corredor. Aqui ela respondeu que está bem, e nenhum aviso sai." |
| 2:50 a 3:15 | Gira o potenciômetro para 0 V. Âmbar pisca rápido, buzzer toca, tela "ENERGIA!". O painel fica vermelho com "Falta de energia". Volta a tensão. | "Se a luz da rua cai, o concentrador para. O alarme toca na casa, mesmo sem internet, e a agente vê no painel." |
| 3:15 a 3:40 | No painel, clica em "Declarar dia crítico". O âmbar acende, a tela mostra "BEBA AGUA". Muda o DHT22 para 35 °C: "Casa quente". | "No dia crítico de calor, a UBS manda o recado pelo painel. O aparelho acende o âmbar e a tela pede: beba água. Se a casa passa de 34 graus, ele avisa sozinho." |
| 3:40 a 4:00 | Painel com a linha do tempo cheia. | "O ESP32 cuida da casa sozinho e conta tudo para quem cuida dela. Os mesmos eventos MQTT entram no Motor do Corredor Aceso, que ordena o radar da microárea." |
