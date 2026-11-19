// Configuração do nó do corredor. Os valores precisam bater com infra/config/corredor.conf.padrao.
#pragma once

// Rede do simulador Wokwi: aberta, sem senha, canal 6.
#define WIFI_SSID "Wokwi-GUEST"
#define WIFI_SENHA ""
#define WIFI_CANAL 6

// Broker público. A ponte do Mosquitto da VM traz <prefixo>/corredor/v1/... para corredor/v1/...
// MQTT_PUBLICO_HOST, MQTT_PUBLICO_PORTA e MQTT_PREFIXO_PUBLICO do corredor.conf.padrao.
#define MQTT_HOST "test.mosquitto.org"
#define MQTT_PORTA 1883
#define MQTT_PREFIXO "corredor-aceso-40028922"

// Token da casa: 32 hexadecimais minúsculos. O padrão é CASA_TESTE_TOKEN, a casa de teste que o
// seed do Motor ativa. Para a demo, troque pelo token da casa 1 em api/semente/tokens.local.json,
// só no projeto do wokwi.com. Token real nunca entra no Git: o broker é público.
#define CASA_TOKEN "fa11fa11fa11fa11fa11fa11fa11fa11"

// Servidores de hora. O ts do pacote é a hora do fato em UTC.
#define NTP_1 "pool.ntp.org"
#define NTP_2 "time.google.com"

// Reações locais, que funcionam sem a VM. Os limites são os do Motor (03-modelo-de-dominio.md).
// Fuso da casa, para a janela das 10h às 14h30: São Paulo, UTC-3, sem horário de verão.
#define FUSO_SEGUNDOS (-3 * 3600)
// Pedido de toque local: sem MOVIMENTO nem botão desde as 10h, o buzzer chama às 14h30.
// 1 para casa Sozinho, o perfil do nó do corredor. 0 deixa só o comando PEDIR_TOQUE do servidor.
#define PEDIDO_LOCAL_DE_TOQUE 1
#define INICIO_DA_JANELA_MIN (10 * 60)
#define HORA_DO_PEDIDO_MIN (14 * 60 + 30)
// Demo: acima de 0, troca a janela do relógio por N segundos sem sinal de vida. 0 é o real.
#define SILENCIO_DEMO_S 0

// Queda. O pingente (MPU6050) avisa um impacto. O sensor de teto (HC-SR04) mede a altura do que
// está embaixo dele: alguém deitado no chão por TEMPO_NO_CHAO_S também é queda. Antes de avisar,
// o aparelho mostra "QUEDA?" e bipa por CONTAGEM_DA_QUEDA_S. Um toque no botão cancela.
#define IMPACTO_G 2.0
#define ALTURA_DO_TETO_CM 250
#define ALTURA_DEITADO_CM 50
#define TEMPO_NO_CHAO_S 30
#define CONTAGEM_DA_QUEDA_S 15
