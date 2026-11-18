# Roteiro do vídeo de Mobile Development (até 3 minutos)

Roteiro de gravação de uso da equipe. Saiu de `app/README.md`.

| Tempo | Na tela | Fala |
|---|---|---|
| 0:00 a 0:20 | Tela de entrada no iPhone. | "Dia de calor extremo na Zona Leste. A Maria é agente comunitária de saúde e tem 40 idosos na microárea. Ela não consegue bater em todas as portas. O Corredor Aceso mostra a quem ir primeiro." |
| 0:20 a 0:45 | Toca em Entrar. Tela do dia crítico. | "O modelo previu dia crítico. O app diz quantas casas estão urgentes, quantas estão com aparelho sem sinal e quantas deram sinal hoje. Verde só aparece com prova de vida." |
| 0:45 a 1:15 | Radar. Rola pelos cards. | "O radar mostra só quem precisa de visita. Cada card diz o motivo em palavras, não só pela cor: sem energia numa casa com concentrador de oxigênio, ninguém passou no corredor, aparelho sem sinal. O cinza nunca vira verde." |
| 1:15 a 1:40 | Abre a casa de Benedito. Mostra vizinho e linha do tempo. | "Na casa, ela vê o vizinho de confiança, liga com um toque e confere a linha do tempo das últimas 24 horas. O nome só existe aqui, no celular. O servidor do radar só conhece um código aleatório." |
| 1:40 a 2:10 | Liga o modo avião. Registra "Estava fora". Mostra "Ainda não enviado". Desliga o modo avião. Mostra "Enviado". | "Na rua, a internet cai. O radar continua na tela e a visita fica guardada no celular. Quando a rede volta, o app manda sozinho, com o mesmo código, e o sistema não registra a visita duas vezes." |
| 2:10 a 2:35 | Mensagem para o grupo: "Ele mora sozinho e toma diurético". Bloqueada. Toca em "Usar esta versão". | "Ela quer pedir ajuda no grupo do bairro. O app barra a mensagem que diz que ele mora sozinho e fala da saúde, explica por quê e oferece uma versão segura." |
| 2:35 a 3:00 | Encerrar o turno. Mostra o indicador. | "No fim do dia, ela encerra o turno e vê o indicador: quantos idosos deram sinal de vida até as 10h. A meta é 100%. Os dados deste vídeo são simulados." |

Para gravar no iPhone sem a API, rode o app sem `EXPO_PUBLIC_API_URL`. O mock lê o estado da rede do `NetInfo` e falha como sem internet quando o celular está desconectado. Isso está coberto pelo teste do mock com rede caída, mas o modo avião no iPhone não foi gravado: ensaie esse trecho antes do vídeo. Com o modo avião, o Expo Go também perde a conexão com o computador, então abra o app antes e não recarregue no meio.
