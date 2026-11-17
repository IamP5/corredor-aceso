// Configuração do nó do corredor. Os valores precisam bater com infra/config/corredor.conf.padrao.
#pragma once

// Rede do simulador Wokwi: aberta, sem senha, canal 6.
#define WIFI_SSID "Wokwi-GUEST"
#define WIFI_SENHA ""
#define WIFI_CANAL 6

// Broker público. A ponte do Mosquitto da VM traz <prefixo>/corredor/v1/... para corredor/v1/...
// MQTT_PUBLICO_HOST, MQTT_PUBLICO_PORTA e MQTT_PREFIXO_PUBLICO do corredor.conf.padrao.
#define MQTT_HOST "broker.hivemq.com"
#define MQTT_PORTA 1883
#define MQTT_PREFIXO "gs26-TROQUE-ISTO"

// Token da casa: 32 hexadecimais minúsculos. O padrão é CASA_TESTE_TOKEN, a casa de teste que o
// seed do Motor ativa. Para a demo, troque pelo token da casa 1 em api/semente/tokens.local.json,
// só no projeto do wokwi.com. Token real nunca entra no Git: o broker é público.
#define CASA_TOKEN "fa11fa11fa11fa11fa11fa11fa11fa11"

// Servidores de hora. O ts do pacote é a hora do fato em UTC.
#define NTP_1 "pool.ntp.org"
#define NTP_2 "time.google.com"
