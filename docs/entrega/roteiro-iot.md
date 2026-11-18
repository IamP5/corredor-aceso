# Roteiro do vídeo de Physical Computing (até 4 minutos)

Roteiro de gravação de uso da equipe. Saiu de `iot/README.md`.

Antes de gravar: VM local de pé com a última versão, simulador parado, relógio de demo às 08h00
(`PUT /api/demo/relogio`), projeto do Wokwi com o prefixo da equipe e o token da casa 1, app aberto no
radar da microárea 3. Tela dividida: Wokwi à esquerda, app ou Swagger à direita, monitor serial visível.

| Tempo | Na tela | Fala |
|---|---|---|
| 0:00 a 0:25 | Foto ou desenho do corredor. Depois, o circuito no Wokwi. | "Dona Efigênia mora sozinha. Se ela cair, ninguém sabe. Este aparelho fica no corredor. Ela não precisa usar celular nem lembrar de nada." |
| 0:25 a 0:55 | Aponta cada peça do circuito. | "O sensor de presença acende a luz de caminho. O DHT22 mede o calor dentro da casa. O potenciômetro faz o papel do sensor de tensão da tomada, porque ela usa concentrador de oxigênio. O botão grande é o 'estou bem'. O LED âmbar e o buzzer são o recado da UBS." |
| 0:55 a 1:30 | Inicia a simulação. Serial: `[mqtt] conectado`, a `TELEMETRIA` inicial. Clica no PIR. A luz acende. Serial mostra o `MOVIMENTO`. | "Ela passa, a luz acende, e o aparelho manda este pacote. Só o token da casa, nunca o nome. A hora é a do fato, em UTC." |
| 1:30 a 1:55 | Liga o simulador na VM (`sudo systemctl start corredor-simulador`). Radar no app enche de casas. Abre a linha do tempo da casa 1 com o `MOVIMENTO`. | "As outras 39 casas da microárea vêm do simulador, com o mesmo contrato. O movimento da Dona Efigênia já está na linha do tempo dela." |
| 1:55 a 2:45 | Muda a chave do Wi-Fi. Serial: `Wi-Fi cortado`. Clica no PIR, aperta o botão. Serial: `sem rede: guardado (1 no buffer)`, `(2 no buffer)`. A luz acende mesmo assim. Volta a chave. Serial: `reenviados com a hora original` e os pacotes com `"bufferizado":true`. Linha do tempo mostra os dois eventos com a hora em que aconteceram, marcados como bufferizados. | "A internet da casa caiu. A luz continua. O aparelho guarda o que aconteceu. Quando a rede volta, ele manda tudo com a hora original, e o servidor grava cada evento uma vez só." |
| 2:45 a 3:15 | Declara o dia crítico (job de ML ou Swagger). O LED âmbar acende. Move o relógio para 14h30. Âmbar pisca, buzzer toca. Aperta o botão: buzzer para, serial mostra `BOTAO_BEM`. | "Num dia crítico de calor, a UBS acende o âmbar em todas as casas. Às 14h30, quem não deu sinal desde as 10h ouve o buzzer. Um toque e a casa volta ao verde." |
| 3:15 a 3:45 | Gira o potenciômetro abaixo de 100 V. Serial: `tensão cruzou 100 V`. Radar mostra a casa 1 vermelha com o motivo de energia. | "Se a luz da rua cai, o concentrador para. O aparelho avisa na hora, e a casa sobe para o topo do radar da ACS." |
| 3:45 a 4:00 | Radar com as casas silenciosas no topo. | "A ACS não liga para 40 casas. Ela vê só as que ficaram em silêncio." |
