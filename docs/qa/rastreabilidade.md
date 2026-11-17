# Rastreabilidade

Arquivo gerado por `docs/qa/ferramentas/gerar.py`. A conferência `docs/qa/ferramentas/conferir.py` lê este arquivo, as Features e os Test Cases e aponta qualquer regra ou rota sem cobertura.

A coluna "Teste automatizado" diz qual teste do repositório cobre o caso. "existe" quer dizer que o arquivo já está no repositório. "proposto" quer dizer que o código ainda não chegou e o nome é o que este documento pede ao módulo. Quando o arquivo existir, gere de novo e o status muda sozinho.

## Por Feature

| Feature | Test Case | Tipo | Regras e rotas | Teste automatizado |
|---|---|---|---|---|
| F01 | [TC-F01-01](casos-de-teste.md#tc-f01-01) | Positivo | `POST /idosos`, INV-Idoso-9, INV-Idoso-10 | `api/tests/Corredor.Cadastro.Testes/CadastrarIdosoTestes.cs::CorpoValido_Responde201_CasaSemToken` (proposto) |
| F01 | [TC-F01-02](casos-de-teste.md#tc-f01-02) | Negativo | `POST /idosos`, INV-Idoso-9 | `docs/arquitetura/referencia/dados/testes.sql::Idoso inv. 9: nome vazio` (existe) |
| F01 | [TC-F01-03](casos-de-teste.md#tc-f01-03) | Negativo | `POST /idosos`, INV-Idoso-9 | `docs/arquitetura/referencia/dados/testes.sql::Idoso inv. 9: endereco ausente` (existe) |
| F01 | [TC-F01-04](casos-de-teste.md#tc-f01-04) | Positivo | `POST /idosos`, IDEM-Cadastro-CadastrarIdoso | `api/tests/Corredor.Cadastro.Testes/CadastrarIdosoTestes.cs::CorpoRepetido_CriaDoisIdosos` (proposto) |
| F01 | [TC-F01-05](casos-de-teste.md#tc-f01-05) | Negativo | IDEM-Cadastro-CadastrarIdoso | `api/tests/Corredor.Cadastro.Testes/CargaDaDemoTestes.cs::SemearDuasVezes_MesmoBanco` (proposto) |
| F01 | [TC-F01-06](casos-de-teste.md#tc-f01-06) | Negativo | `POST /idosos`, INV-Idoso-10 | `docs/arquitetura/referencia/dados/testes.sql::caso 38: CPF em claro em condicoes nao entra no banco` (existe) |
| F02 | [TC-F02-01](casos-de-teste.md#tc-f02-01) | Positivo | `POST /idosos/{id}/consentimento`, INV-Idoso-1, INV-Idoso-3, INV-Idoso-4, NC-6, P1 | `api/tests/Corredor.Cadastro.Testes/ConsentimentoTestes.cs::Consentimento_GeraToken_AtivaCasaNoMotor` (proposto) |
| F02 | [TC-F02-02](casos-de-teste.md#tc-f02-02) | Negativo | INV-Idoso-1, NC-6, P1 | `api/tests/Corredor.Cadastro.Testes/ConsentimentoTestes.cs::Caso32_SemConsentimento_SemToken_ForaDaLista` (proposto) |
| F02 | [TC-F02-03](casos-de-teste.md#tc-f02-03) | Positivo | `POST /idosos/{id}/consentimento`, INV-Idoso-2, INV-Idoso-5, IDEM-Cadastro-RegistrarConsentimento, IDEM-Motor-AtivarCasa | `api/tests/Corredor.Cadastro.Testes/ConsentimentoTestes.cs::ConsentimentoVigente_DevolveMesmoToken` (proposto) |
| F02 | [TC-F02-04](casos-de-teste.md#tc-f02-04) | Negativo | `POST /idosos/{id}/consentimento`, INV-Idoso-2, INV-Idoso-5, IDEM-Cadastro-RegistrarConsentimento, IDEM-Motor-AtivarCasa | `api/tests/Corredor.Cadastro.Testes/ConsentimentoTestes.cs::MotorIndisponivel_503_RepetirAtivaComMesmoToken` (proposto) |
| F02 | [TC-F02-05](casos-de-teste.md#tc-f02-05) | Positivo | P22, IDEM-Motor-AtivarCasa, INV-CasaMonitorada-1 | `api/tests/Corredor.Cadastro.Testes/ReconciliacaoComMotorTestes.cs::AoIniciar_AtivaCasaAusente_SemTokenNovo` (proposto) |
| F02 | [TC-F02-06](casos-de-teste.md#tc-f02-06) | Negativo | P22, IDEM-Motor-AtivarCasa, INV-CasaMonitorada-1 | `api/tests/Corredor.Cadastro.Testes/ReconciliacaoComMotorTestes.cs::AoIniciar_IgnoraIdosoSemToken` (proposto) |
| F02 | [TC-F02-07](casos-de-teste.md#tc-f02-07) | Negativo | INV-Idoso-3, INV-Idoso-4 | `docs/arquitetura/referencia/dados/testes.sql::o mesmo token em dois idosos` (existe) |
| F03 | [TC-F03-01](casos-de-teste.md#tc-f03-01) | Positivo | `DELETE /idosos/{id}/consentimento`, INV-Idoso-8, P4, INV-CasaMonitorada-3, INV-Visita-6, IDEM-Cadastro-RevogarConsentimento, IDEM-Motor-DesativarCasa | `api/tests/Corredor.Cadastro.Testes/RevogacaoTestes.cs::Caso33_Revogar_ApagaTokenAvisos_DesativaCasa_MantemVisitas` (proposto) |
| F03 | [TC-F03-02](casos-de-teste.md#tc-f03-02) | Negativo | INV-CasaMonitorada-3 | `api/tests/Corredor.Motor.Testes/RegistrarPacoteTestes.cs::CasaDesativada_RecusaPacoteEConfirmacao` (proposto) |
| F03 | [TC-F03-03](casos-de-teste.md#tc-f03-03) | Negativo | `DELETE /idosos/{id}/consentimento`, P4, IDEM-Cadastro-RevogarConsentimento, IDEM-Motor-DesativarCasa, INV-Visita-6 | `api/tests/Corredor.Cadastro.Testes/RevogacaoTestes.cs::RevogarSemVigente_NaoFazNada` (proposto) |
| F03 | [TC-F03-04](casos-de-teste.md#tc-f03-04) | Negativo | INV-Idoso-8, INV-CasaMonitorada-3 | `docs/arquitetura/referencia/dados/testes.sql::Idoso inv. 2: revogado fica guardado e um novo vigente entra` (existe) |
| F03 | [TC-F03-05](casos-de-teste.md#tc-f03-05) | Negativo | INV-Visita-6 | `docs/arquitetura/referencia/dados/testes.sql::Visita inv. 6: apagar o idoso nao leva a visita junto` (existe) |
| F04 | [TC-F04-01](casos-de-teste.md#tc-f04-01) | Positivo | `PUT /idosos/{id}/vizinho`, INV-Idoso-6, IDEM-Cadastro-IndicarVizinho | `api/tests/Corredor.Cadastro.Testes/VizinhoTestes.cs::ComAvisarVizinho_Indica_204` (proposto) |
| F04 | [TC-F04-02](casos-de-teste.md#tc-f04-02) | Negativo | `PUT /idosos/{id}/vizinho`, INV-Idoso-6 | `api/tests/Corredor.Cadastro.Testes/VizinhoTestes.cs::SemAvisarVizinho_422AutorizacaoAusente` (proposto) |
| F04 | [TC-F04-03](casos-de-teste.md#tc-f04-03) | Negativo | INV-Idoso-6 | `docs/arquitetura/referencia/dados/testes.sql::Idoso inv. 6: vizinho sem a data em que aceitou` (existe) |
| F04 | [TC-F04-04](casos-de-teste.md#tc-f04-04) | Positivo | `PUT /idosos/{id}/vizinho`, IDEM-Cadastro-IndicarVizinho | `api/tests/Corredor.Cadastro.Testes/VizinhoTestes.cs::IndicarDeNovo_SubstituiVizinho` (proposto) |
| F04 | [TC-F04-05](casos-de-teste.md#tc-f04-05) | Negativo | IDEM-Cadastro-IndicarVizinho | `docs/arquitetura/referencia/dados/testes.sql::Idoso: no maximo um vizinho` (existe) |
| F04 | [TC-F04-06](casos-de-teste.md#tc-f04-06) | Positivo | INV-Idoso-7 | `api/tests/Corredor.Cadastro.Testes/IdosoTestes.cs::DestinatariosDoAviso_ComVizinho_TrazVizinho` (proposto) |
| F04 | [TC-F04-07](casos-de-teste.md#tc-f04-07) | Negativo | INV-Idoso-7 | `api/tests/Corredor.Cadastro.Testes/IdosoTestes.cs::Caso34_SemAvisarFamilia_FamiliaNaoAparece` (proposto) |
| F05 | [TC-F05-01](casos-de-teste.md#tc-f05-01) | Positivo | `PUT /idosos/{id}/perfil`, P21, IDEM-Motor-DefinirPerfil | `api/tests/Corredor.Cadastro.Testes/PerfilTestes.cs::TokenAtivo_TrocaPerfilNoMotor` (proposto) |
| F05 | [TC-F05-02](casos-de-teste.md#tc-f05-02) | Positivo | `PUT /idosos/{id}/perfil`, IDEM-Motor-DefinirPerfil, IDEM-Cadastro-IndicarVizinho | `api/tests/Corredor.Motor.Testes/MotorDeCasasTestes.cs::DefinirPerfil_UltimoValorVale` (proposto) |
| F05 | [TC-F05-03](casos-de-teste.md#tc-f05-03) | Negativo | `PUT /idosos/{id}/perfil`, P21 | `api/tests/Corredor.Cadastro.Testes/PerfilTestes.cs::SemToken_GravaPerfil_NaoChamaMotor` (proposto) |
| F05 | [TC-F05-04](casos-de-teste.md#tc-f05-04) | Negativo | P21, IDEM-Motor-DefinirPerfil | `api/tests/Corredor.Cadastro.Testes/PerfilTestes.cs::Revogado_NaoChamaMotor` (proposto) |
| F05 | [TC-F05-05](casos-de-teste.md#tc-f05-05) | Negativo | `PUT /idosos/{id}/perfil` | `api/tests/Corredor.Cadastro.Testes/PerfilTestes.cs::PerfilInvalido_422_NaoGrava` (proposto) |
| F06 | [TC-F06-01](casos-de-teste.md#tc-f06-01) | Positivo | `GET /lista-de-nomes?microarea={id}`, P3, CANAL-AppAutenticado | `api/tests/Corredor.Cadastro.Testes/ListaDeNomesTestes.cs::Autenticado_TrazTokenNomeEnderecoVizinho` (proposto) |
| F06 | [TC-F06-02](casos-de-teste.md#tc-f06-02) | Negativo | P3, CANAL-AppAutenticado | `api/tests/Corredor.Cadastro.Testes/ListaDeNomesTestes.cs::NaoTrazCondicoesNemMedicamentos` (proposto) |
| F06 | [TC-F06-03](casos-de-teste.md#tc-f06-03) | Negativo | `GET /lista-de-nomes?microarea={id}`, CANAL-AppAutenticado | `api/tests/Corredor.Cadastro.Testes/ListaDeNomesTestes.cs::SemAutenticacao_Recusa` (proposto) |
| F06 | [TC-F06-04](casos-de-teste.md#tc-f06-04) | Negativo | `GET /lista-de-nomes?microarea={id}`, P3 | `api/tests/Corredor.Cadastro.Testes/ListaDeNomesTestes.cs::SoCasasComTokenAtivo` (proposto) |
| F07 | [TC-F07-01](casos-de-teste.md#tc-f07-01) | Positivo | INV-EventoSensor-2, INV-EventoSensor-4, INV-EventoSensor-5, INV-CasaMonitorada-2 | `api/tests/Corredor.Motor.Testes/RegistrarPacoteTestes.cs::PacoteValido_GravaEvento_UltimoPacoteHoraDoServidor` (proposto) |
| F07 | [TC-F07-02](casos-de-teste.md#tc-f07-02) | Negativo | INV-EventoSensor-1, P6, NC-1, IDEM-Motor-RegistrarPacote | `docs/arquitetura/referencia/dados/testes.sql::caso 26: o mesmo pacote com ON CONFLICT DO NOTHING conta como sucesso` (existe) |
| F07 | [TC-F07-03](casos-de-teste.md#tc-f07-03) | Positivo | INV-EventoSensor-1, P6, NC-1, IDEM-Motor-RegistrarPacote | `docs/arquitetura/referencia/dados/testes.sql::caso 27: seq 1 de novo com outro ts` (existe) |
| F07 | [TC-F07-04](casos-de-teste.md#tc-f07-04) | Negativo | INV-EventoSensor-3, NC-1, INV-CasaMonitorada-2 | `api/tests/Corredor.Motor.Testes/RegistrarPacoteTestes.cs::Caso28_TsAdiantado_Recusado_UltimoPacoteNaoMuda` (proposto) |
| F07 | [TC-F07-05](casos-de-teste.md#tc-f07-05) | Positivo | INV-EventoSensor-3, INV-CasaMonitorada-2 | `api/tests/Corredor.Motor.Testes/RegistrarPacoteTestes.cs::TsDentroDaTolerancia_Aceito` (proposto) |
| F07 | [TC-F07-06](casos-de-teste.md#tc-f07-06) | Negativo | P2, INV-EventoSensor-4 | `docs/arquitetura/referencia/dados/testes.sql::EventoSensor inv. 4: evento de token desconhecido` (existe) |
| F07 | [TC-F07-07](casos-de-teste.md#tc-f07-07) | Positivo | P2, INV-EventoSensor-4 | `api/tests/Corredor.Motor.Testes/ConsumidorMqttTestes.cs::TokenDesconhecido_Descarta_SegueConsumindo` (proposto) |
| F07 | [TC-F07-08](casos-de-teste.md#tc-f07-08) | Negativo | INV-EventoSensor-2, INV-CasaMonitorada-2 | `docs/arquitetura/referencia/dados/testes.sql::EventoSensor inv. 2: heartbeat nunca vira linha` (existe) |
| F07 | [TC-F07-09](casos-de-teste.md#tc-f07-09) | Negativo | INV-EventoSensor-5 | `api/tests/Corredor.Motor.Testes/EventoSensorTestes.cs::LeituraForaDaFaixa_GravadaComoAusente` (proposto) |
| F08 | [TC-F08-01](casos-de-teste.md#tc-f08-01) | Positivo | SV-Sozinho, NC-5 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso02_SozinhoComMovimento_09h_Verde` (proposto) |
| F08 | [TC-F08-02](casos-de-teste.md#tc-f08-02) | Positivo | SV-Sozinho | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Sozinho_BotaoBem_ContaComoSinalDeVida` (proposto) |
| F08 | [TC-F08-03](casos-de-teste.md#tc-f08-03) | Negativo | SV-Coabitado, NC-5 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso24_CoabitadoComMovimentoEBotao_Aguardando` (proposto) |
| F08 | [TC-F08-04](casos-de-teste.md#tc-f08-04) | Positivo | SV-Coabitado, NC-5 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso25_CoabitadoComVisita_Verde` (proposto) |
| F08 | [TC-F08-05](casos-de-teste.md#tc-f08-05) | Negativo | SV-Acamado, NC-5 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso16_Acamado_MovimentoNaoConta` (proposto) |
| F08 | [TC-F08-06](casos-de-teste.md#tc-f08-06) | Positivo | SV-Acamado | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso16_Acamado_VisitaValeSoNaJanela` (proposto) |
| F08 | [TC-F08-07](casos-de-teste.md#tc-f08-07) | Negativo | SV-Sozinho, NC-5 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso01_SozinhoSemSinal_09h_Aguardando` (proposto) |
| F09 | [TC-F09-01](casos-de-teste.md#tc-f09-01) | Negativo | R2, P14, NC-2, TV-SIT-2 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso10_AparelhoMudo_Cinza` (proposto) |
| F09 | [TC-F09-02](casos-de-teste.md#tc-f09-02) | Negativo | R2, TV-SIT-2 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso15_NuncaMandouPacote_Cinza` (proposto) |
| F09 | [TC-F09-03](casos-de-teste.md#tc-f09-03) | Positivo | R2, P14, NC-2 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::SemHeartbeat_DentroDoSilencioMaximo_NaoDisparaRegra` (proposto) |
| F09 | [TC-F09-04](casos-de-teste.md#tc-f09-04) | Positivo | R2, P14, TV-SIT-2 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso13_NoVoltaComBuffer_OcorridoEmConta` (proposto) |
| F09 | [TC-F09-05](casos-de-teste.md#tc-f09-05) | Negativo | R2, TV-SIT-2 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso11e12_CinzaAtendidaSoNaJanela` (proposto) |
| F09 | [TC-F09-06](casos-de-teste.md#tc-f09-06) | Negativo | P14, NC-2, TV-SIT-2 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso14_MudoVoltaSemMovimento_VermelhoDesde10h` (proposto) |
| F10 | [TC-F10-01](casos-de-teste.md#tc-f10-01) | Negativo | R4, TV-SIT-4 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso03_SozinhoSemSinal_10h_Vermelho` (proposto) |
| F10 | [TC-F10-02](casos-de-teste.md#tc-f10-02) | Negativo | R4 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso05_MovimentoAntesDaAbertura_NaoVale` (proposto) |
| F10 | [TC-F10-03](casos-de-teste.md#tc-f10-03) | Positivo | R4, TV-SIT-4 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso04_MovimentoDepoisDoPrazo_Verde` (proposto) |
| F10 | [TC-F10-04](casos-de-teste.md#tc-f10-04) | Negativo | R4, TV-SIT-4 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso07_SoManha_Vermelho15h` (proposto) |
| F10 | [TC-F10-05](casos-de-teste.md#tc-f10-05) | Positivo | R4, P9 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso17_SaidaAvisada_SuspendeSilencio` (proposto) |
| F10 | [TC-F10-06](casos-de-teste.md#tc-f10-06) | Negativo | P9 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::SaidaAvisada_NaoSuspendeHeartbeat` (proposto) |
| F10 | [TC-F10-07](casos-de-teste.md#tc-f10-07) | Positivo | R4 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Exemplo58_SinalAntesDoPrazo_Aguardando` (proposto) |
| F10 | [TC-F10-08](casos-de-teste.md#tc-f10-08) | Negativo | R4, TV-SIT-4 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso16_Acamado_JanelaVenceNoInicio` (proposto) |
| F11 | [TC-F11-01](casos-de-teste.md#tc-f11-01) | Negativo | R3, TV-SIT-3 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso18_CalorInterno` (proposto) |
| F11 | [TC-F11-02](casos-de-teste.md#tc-f11-02) | Positivo | R3, TV-SIT-3 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Calor_AbaixoDoLimite_SemAlerta` (proposto) |
| F11 | [TC-F11-03](casos-de-teste.md#tc-f11-03) | Positivo | R3 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Calor_InterrupcaoRecomecaSequencia` (proposto) |
| F11 | [TC-F11-04](casos-de-teste.md#tc-f11-04) | Negativo | R3 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Calor_SemTurno_SemAlerta` (proposto) |
| F12 | [TC-F12-01](casos-de-teste.md#tc-f12-01) | Negativo | R1, NC-3, TV-SIT-1 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso19_EnergiaComTrava` (proposto) |
| F12 | [TC-F12-02](casos-de-teste.md#tc-f12-02) | Negativo | R1, TV-SIT-1 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso20_Eletrodependente61s_Vermelho` (proposto) |
| F12 | [TC-F12-03](casos-de-teste.md#tc-f12-03) | Positivo | R1, NC-3, TV-SIT-1 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso20_Eletrodependente59s_Aguardando` (proposto) |
| F12 | [TC-F12-04](casos-de-teste.md#tc-f12-04) | Positivo | R1 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Energia_CasaComum_SemAlerta` (proposto) |
| F12 | [TC-F12-05](casos-de-teste.md#tc-f12-05) | Positivo | R1, NC-3 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Energia_VisitaDepoisDaLeituraBaixa_SoltaTrava` (proposto) |
| F12 | [TC-F12-06](casos-de-teste.md#tc-f12-06) | Negativo | R1 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Energia_NuncaMandouPacote_Cinza` (proposto) |
| F12 | [TC-F12-07](casos-de-teste.md#tc-f12-07) | Negativo | R1, TV-SIT-1 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::OrdemDasRegras_EnergiaAntesDeCalor` (proposto) |
| F13 | [TC-F13-01](casos-de-teste.md#tc-f13-01) | Negativo | TV-SIT-6, NC-4 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso01_SozinhoSemSinal_09h_Aguardando` (proposto) |
| F13 | [TC-F13-02](casos-de-teste.md#tc-f13-02) | Positivo | TV-SIT-5, NC-4 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso08_VisitaEstavaBem_Verde` (proposto) |
| F13 | [TC-F13-03](casos-de-teste.md#tc-f13-03) | Negativo | TV-SIT-6, NC-4 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso06_SoManha_1230_Aguardando` (proposto) |
| F13 | [TC-F13-04](casos-de-teste.md#tc-f13-04) | Negativo | TV-SIT-5, TV-SIT-6, NC-4 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso09_VisitaNaoCobreOTurno` (proposto) |
| F13 | [TC-F13-05](casos-de-teste.md#tc-f13-05) | Negativo | TV-SIT-7, NC-4 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::DiaComum_SemVerde` (proposto) |
| F13 | [TC-F13-06](casos-de-teste.md#tc-f13-06) | Negativo | TV-SIT-7, P13 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::TurnoEncerrado_SoEnergiaEHeartbeat` (proposto) |
| F13 | [TC-F13-07](casos-de-teste.md#tc-f13-07) | Positivo | P13, TV-SIT-2 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::TurnoEncerrado_HeartbeatContinua` (proposto) |
| F13 | [TC-F13-08](casos-de-teste.md#tc-f13-08) | Positivo | P13 | `api/tests/Corredor.Motor.Testes/VigiaDoRadarTestes.cs::EncerraTurnoAs18h` (proposto) |
| F14 | [TC-F14-01](casos-de-teste.md#tc-f14-01) | Positivo | TV-ATD-1 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Atendimento_EnergiaAcsPessoaVista` (proposto) |
| F14 | [TC-F14-02](casos-de-teste.md#tc-f14-02) | Negativo | TV-ATD-2 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Atendimento_EnergiaVizinho_NaoAtende` (proposto) |
| F14 | [TC-F14-03](casos-de-teste.md#tc-f14-03) | Negativo | TV-ATD-3, TV-ATD-5 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Atendimento_AusenciaSoAtendeSilencio` (proposto) |
| F14 | [TC-F14-04](casos-de-teste.md#tc-f14-04) | Positivo | TV-ATD-4 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso23_CinzaAtendidaPeloVizinho` (proposto) |
| F14 | [TC-F14-05](casos-de-teste.md#tc-f14-05) | Positivo | TV-ATD-6 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Atendimento_CalorVizinho` (proposto) |
| F14 | [TC-F14-06](casos-de-teste.md#tc-f14-06) | Negativo | TV-ATD-7 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Atendimento_CalorAusencia_NaoAtende` (proposto) |
| F14 | [TC-F14-07](casos-de-teste.md#tc-f14-07) | Positivo | TV-ATD-8 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso22_EstavaFora_AtendeSoNaJanela` (proposto) |
| F14 | [TC-F14-08](casos-de-teste.md#tc-f14-08) | Positivo | TV-ATD-9 | `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Atendimento_SilencioPessoaVista_ViraSinalDeVida` (proposto) |
| F14 | [TC-F14-09](casos-de-teste.md#tc-f14-09) | Negativo | INV-CasaMonitorada-4, IDEM-Motor-Confirmar | `api/tests/Corredor.Motor.Testes/CasaMonitoradaTestes.cs::Confirmar_IdRepetido_Ignora` (proposto) |
| F14 | [TC-F14-10](casos-de-teste.md#tc-f14-10) | Positivo | INV-CasaMonitorada-4, IDEM-Motor-Confirmar | `api/tests/Corredor.Motor.Testes/CasaMonitoradaTestes.cs::Confirmar_IdsDiferentes_GuardaAmbas` (proposto) |
| F14 | [TC-F14-11](casos-de-teste.md#tc-f14-11) | Negativo | INV-CasaMonitorada-5 | `api/tests/Corredor.Motor.Testes/CasaMonitoradaTestes.cs::Confirmar_ForaDaJanelaDeTempo_Recusa` (proposto) |
| F14 | [TC-F14-12](casos-de-teste.md#tc-f14-12) | Positivo | INV-CasaMonitorada-5 | `api/tests/Corredor.Motor.Testes/CasaMonitoradaTestes.cs::Confirmar_DentroDaTolerancia_Aceita` (proposto) |
| F15 | [TC-F15-01](casos-de-teste.md#tc-f15-01) | Positivo | `GET /radar?microarea={id}` | `api/tests/Corredor.Motor.Testes/RadarTestes.cs::Pendentes_VermelhoAntesDeCinza_OrdemDaRegra_DesdeMaisAntigo` (proposto) |
| F15 | [TC-F15-02](casos-de-teste.md#tc-f15-02) | Positivo | `GET /radar?microarea={id}` | `api/tests/Corredor.Motor.Testes/RadarTestes.cs::Atendida_ForaDePendentes_NaoContaVerde` (proposto) |
| F15 | [TC-F15-03](casos-de-teste.md#tc-f15-03) | Positivo | `GET /radar?microarea={id}` | `api/tests/Corredor.Motor.Testes/RadarTestes.cs::Caso17_SaidaAvisada_AguardandoComMarca` (proposto) |
| F15 | [TC-F15-04](casos-de-teste.md#tc-f15-04) | Negativo | `GET /radar?microarea={id}` | `api/tests/Corredor.Motor.Testes/RadarTestes.cs::OutraMicroarea_403` (proposto) |
| F15 | [TC-F15-05](casos-de-teste.md#tc-f15-05) | Negativo | `GET /radar?microarea={id}` | `api/tests/Corredor.Motor.Testes/RadarTestes.cs::SoCasasAtivas_SemNome` (proposto) |
| F16 | [TC-F16-01](casos-de-teste.md#tc-f16-01) | Positivo | `GET /casas/{token}/linha-do-tempo` | `api/tests/Corredor.Motor.Testes/LinhaDoTempoTestes.cs::OrdemDeOcorridoEm_ComBufferizado` (proposto) |
| F16 | [TC-F16-02](casos-de-teste.md#tc-f16-02) | Negativo | `GET /casas/{token}/linha-do-tempo` | `api/tests/Corredor.Motor.Testes/LinhaDoTempoTestes.cs::SoUltimas24Horas` (proposto) |
| F16 | [TC-F16-03](casos-de-teste.md#tc-f16-03) | Negativo | `GET /casas/{token}/linha-do-tempo` | `api/tests/Corredor.Motor.Testes/LinhaDoTempoTestes.cs::CasaDesativada_404_IgualDesconhecida` (proposto) |
| F16 | [TC-F16-04](casos-de-teste.md#tc-f16-04) | Negativo | `GET /casas/{token}/linha-do-tempo`, INV-CasaMonitorada-6 | `api/tests/Corredor.Motor.Testes/LinhaDoTempoTestes.cs::SemDadoPessoalNemTextoLivre` (proposto) |
| F16 | [TC-F16-05](casos-de-teste.md#tc-f16-05) | Positivo | `GET /casas/{token}/linha-do-tempo`, INV-CasaMonitorada-6 | manual |
| F17 | [TC-F17-01](casos-de-teste.md#tc-f17-01) | Positivo | `POST /visitas`, DESF-EstavaBem, DESF-Encaminhada, P11, INV-Visita-4, INV-Visita-5 | `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::Caso08_EstavaBemEEncaminhada_ConfirmamPessoaVista` (proposto) |
| F17 | [TC-F17-02](casos-de-teste.md#tc-f17-02) | Negativo | DESF-NaoAbriu, NC-7, P11 | `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::Caso21_NaoAbriu_NaoChamaOMotor` (proposto) |
| F17 | [TC-F17-03](casos-de-teste.md#tc-f17-03) | Positivo | DESF-EstavaFora, NC-7, P11 | `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::Caso22_EstavaFora_AtendeSoNaJanela` (proposto) |
| F17 | [TC-F17-04](casos-de-teste.md#tc-f17-04) | Negativo | DESF-EstavaFora, NC-7 | `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::EstavaFora_NuncaESinalDeVida` (proposto) |
| F17 | [TC-F17-05](casos-de-teste.md#tc-f17-05) | Positivo | INV-Visita-1, DESF-NaoAbriu, DESF-EstavaBem, IDEM-Cadastro-RegistrarVisita, P11 | `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::Correcao_EOutraVisita` (proposto) |
| F17 | [TC-F17-06](casos-de-teste.md#tc-f17-06) | Positivo | `POST /visitas`, INV-Visita-2, P18, IDEM-Cadastro-RegistrarVisita | `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::Caso37_MesmoId_Devolve200` (proposto) |
| F17 | [TC-F17-07](casos-de-teste.md#tc-f17-07) | Negativo | `POST /visitas`, INV-Visita-1, INV-Visita-2, P18, IDEM-Cadastro-RegistrarVisita | `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::Caso37_MesmoIdOutroCorpo_NaoAltera` (proposto) |
| F17 | [TC-F17-08](casos-de-teste.md#tc-f17-08) | Negativo | `POST /visitas`, INV-Visita-4 | `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::TokenRevogado_Recusa422` (proposto) |
| F17 | [TC-F17-09](casos-de-teste.md#tc-f17-09) | Positivo | `POST /visitas`, INV-Visita-3, INV-Visita-7 | `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::DentroDaToleranciaEVisitaAntiga_Gravam` (proposto) |
| F17 | [TC-F17-10](casos-de-teste.md#tc-f17-10) | Negativo | `POST /visitas`, INV-Visita-3, INV-Visita-7 | `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::NoFuturoEAntiga_NaoConfirmam` (proposto) |
| F17 | [TC-F17-11](casos-de-teste.md#tc-f17-11) | Negativo | INV-Visita-5 | `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::ObservacaoComCpf_GravaMascarada` (proposto) |
| F17 | [TC-F17-12](casos-de-teste.md#tc-f17-12) | Negativo | DESF-EstavaBem, DESF-Encaminhada | `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::Caso09_PessoaVista_ValeSoNaJanela` (proposto) |
| F18 | [TC-F18-01](casos-de-teste.md#tc-f18-01) | Positivo | `GET /avisos?microarea={id}`, P10, P19, INV-AvisoAoVizinho-2, INV-AvisoAoVizinho-4, CANAL-Vizinho | `api/tests/Corredor.Cadastro.Testes/ReceptorDeContatoTestes.cs::PedidoDeContato_ComAutorizacaoEVizinho_RegistraAviso` (proposto) |
| F18 | [TC-F18-02](casos-de-teste.md#tc-f18-02) | Negativo | INV-AvisoAoVizinho-1, IDEM-Cadastro-PedidoDeContato, P19 | `api/tests/Corredor.Cadastro.Testes/ReceptorDeContatoTestes.cs::Caso35_PedidoRepetido_UmAviso` (proposto) |
| F18 | [TC-F18-03](casos-de-teste.md#tc-f18-03) | Positivo | `GET /avisos?microarea={id}`, P10, P19, INV-AvisoAoVizinho-1, IDEM-Cadastro-PedidoDeContato | `api/tests/Corredor.Cadastro.Testes/ReceptorDeContatoTestes.cs::EpisodioNovo_CriaOutroAviso` (proposto) |
| F18 | [TC-F18-04](casos-de-teste.md#tc-f18-04) | Negativo | INV-AvisoAoVizinho-2, P19 | `api/tests/Corredor.Cadastro.Testes/ReceptorDeContatoTestes.cs::Caso36_SemAutorizacaoOuSemVizinho_NenhumAviso` (proposto) |
| F18 | [TC-F18-05](casos-de-teste.md#tc-f18-05) | Negativo | P10 | `api/tests/Corredor.Motor.Testes/VigiaDoRadarTestes.cs::CinzaSemTurno_NaoPedeContato` (proposto) |
| F18 | [TC-F18-06](casos-de-teste.md#tc-f18-06) | Positivo | `POST /avisos/{id}/resposta`, `GET /avisos?microarea={id}`, P20, INV-AvisoAoVizinho-3, IDEM-Cadastro-RegistrarResposta | `api/tests/Corredor.Cadastro.Testes/AvisoAoVizinhoTestes.cs::EstaBem_ConfirmaPessoaVista` (proposto) |
| F18 | [TC-F18-07](casos-de-teste.md#tc-f18-07) | Negativo | `POST /avisos/{id}/resposta`, INV-AvisoAoVizinho-3, IDEM-Cadastro-RegistrarResposta | `api/tests/Corredor.Cadastro.Testes/AvisoAoVizinhoTestes.cs::RespostaDiferente_Recusa409` (proposto) |
| F18 | [TC-F18-08](casos-de-teste.md#tc-f18-08) | Negativo | P20 | `api/tests/Corredor.Cadastro.Testes/AvisoAoVizinhoTestes.cs::PrecisaDeAjuda_NaoChamaOMotor` (proposto) |
| F18 | [TC-F18-09](casos-de-teste.md#tc-f18-09) | Negativo | `GET /avisos?microarea={id}`, INV-AvisoAoVizinho-4, CANAL-Vizinho | `api/tests/Corredor.Cadastro.Testes/ReceptorDeContatoTestes.cs::Aviso_SemDadoPessoal` (proposto) |
| F19 | [TC-F19-01](casos-de-teste.md#tc-f19-01) | Positivo | `PUT /dias-criticos/{data}`, P8, INV-DiaCritico-1, INV-DiaCritico-2, INV-DiaCritico-3 | `api/tests/Corredor.Motor.Testes/DiaCriticoTestes.cs::Declarar_AbreTurnoEPublicaDiaCritico` (proposto) |
| F19 | [TC-F19-02](casos-de-teste.md#tc-f19-02) | Positivo | `PUT /dias-criticos/{data}`, INV-DiaCritico-4, INV-DiaCritico-5, IDEM-Motor-DeclararDiaCritico | `api/tests/Corredor.Motor.Testes/DiaCriticoTestes.cs::DeclararDeNovo_AtualizaPrevisao` (proposto) |
| F19 | [TC-F19-03](casos-de-teste.md#tc-f19-03) | Negativo | INV-DiaCritico-1, INV-DiaCritico-4, IDEM-Motor-DeclararDiaCritico | `api/tests/Corredor.Motor.Testes/DiaCriticoTestes.cs::Caso29_DeclararDuasVezes_UmTurno` (proposto) |
| F19 | [TC-F19-04](casos-de-teste.md#tc-f19-04) | Negativo | `PUT /dias-criticos/{data}`, INV-DiaCritico-2, INV-DiaCritico-3, P8 | `api/tests/Corredor.Motor.Testes/DiaCriticoTestes.cs::DeclaracaoInvalida_Recusa422` (proposto) |
| F19 | [TC-F19-05](casos-de-teste.md#tc-f19-05) | Positivo | `POST /dias-criticos/{data}/encerramento`, INV-DiaCritico-7, IDEM-Motor-EncerrarTurno | `api/tests/Corredor.Motor.Testes/DiaCriticoTestes.cs::Encerrar_GravaResumo` (proposto) |
| F19 | [TC-F19-06](casos-de-teste.md#tc-f19-06) | Negativo | `POST /dias-criticos/{data}/encerramento`, INV-DiaCritico-7, IDEM-Motor-EncerrarTurno | `api/tests/Corredor.Motor.Testes/DiaCriticoTestes.cs::EncerrarDeNovo_MesmoResumo` (proposto) |
| F19 | [TC-F19-07](casos-de-teste.md#tc-f19-07) | Negativo | `POST /dias-criticos/{data}/encerramento` | `api/tests/Corredor.Motor.Testes/DiaCriticoTestes.cs::EncerrarSemDiaCritico_Recusa404` (proposto) |
| F19 | [TC-F19-08](casos-de-teste.md#tc-f19-08) | Negativo | `PUT /dias-criticos/{data}`, INV-DiaCritico-5 | `api/tests/Corredor.Motor.Testes/DiaCriticoTestes.cs::DeclararDepoisDoEncerramento_Recusa409` (proposto) |
| F20 | [TC-F20-01](casos-de-teste.md#tc-f20-01) | Positivo | P12, INV-DiaCritico-6, IDEM-Motor-PedirToque | `api/tests/Corredor.Motor.Testes/VigiaDoRadarTestes.cs::Caso30_PedirToque_As14h30` (proposto) |
| F20 | [TC-F20-02](casos-de-teste.md#tc-f20-02) | Negativo | P12, INV-DiaCritico-6, IDEM-Motor-PedirToque | `api/tests/Corredor.Motor.Testes/VigiaDoRadarTestes.cs::Caso30_PedirToque_UmaVezPorDia` (proposto) |
| F20 | [TC-F20-03](casos-de-teste.md#tc-f20-03) | Negativo | P12 | `api/tests/Corredor.Motor.Testes/VigiaDoRadarTestes.cs::PedirToque_SoSozinhoSemSinalDesde10h` (proposto) |
| F20 | [TC-F20-04](casos-de-teste.md#tc-f20-04) | Negativo | P12 | `api/tests/Corredor.Motor.Testes/VigiaDoRadarTestes.cs::PedirToque_DiaComum_NaoPede` (proposto) |
| F21 | [TC-F21-01](casos-de-teste.md#tc-f21-01) | Positivo | `PUT /demo/relogio`, NC-10 | `api/tests/Corredor.Motor.Testes/RelogioDeDemoTestes.cs::Mover_GuardaDeslocamentoERadarUsaHoraNova` (proposto) |
| F21 | [TC-F21-02](casos-de-teste.md#tc-f21-02) | Positivo | `PUT /demo/relogio`, NC-10 | `api/tests/Corredor.Motor.Testes/TradutorDePacoteTestes.cs::RelogioDeslocado_SomaDeslocamentoAoTs` (proposto) |
| F21 | [TC-F21-03](casos-de-teste.md#tc-f21-03) | Negativo | `PUT /demo/relogio`, NC-10 | `api/tests/Corredor.Motor.Testes/TradutorDePacoteTestes.cs::RelogioDeslocado_TsAdiantado_Recusa` (proposto) |
| F21 | [TC-F21-04](casos-de-teste.md#tc-f21-04) | Negativo | `PUT /demo/relogio`, NC-10 | manual |
| F21 | [TC-F21-05](casos-de-teste.md#tc-f21-05) | Positivo | `PUT /demo/relogio`, NC-10 | `docs/arquitetura/referencia/dados/testes_expurgo.sql::reinicio da demo: sem eventos, confirmacoes, dia critico, avisos e eventos de seguranca, ultimo pacote vazio` (existe) |
| F22 | [TC-F22-01](casos-de-teste.md#tc-f22-01) | Positivo | P7, IDEM-Motor-Expurgo, INV-Visita-6 | `docs/arquitetura/referencia/dados/testes_expurgo.sql::leitura normal e movimento com mais de 24 h somem` (existe) |
| F22 | [TC-F22-02](casos-de-teste.md#tc-f22-02) | Negativo | P7 | `docs/arquitetura/referencia/dados/testes_expurgo.sql::caso 31: leitura baixa de 25 h sem confirmacao da ACS fica` (existe) |
| F22 | [TC-F22-03](casos-de-teste.md#tc-f22-03) | Positivo | P7, IDEM-Motor-Expurgo | `docs/arquitetura/referencia/dados/testes_expurgo.sql::leitura baixa com confirmacao da ACS depois dela some, mesmo com a confirmacao vencendo na mesma rodada` (existe) |
| F22 | [TC-F22-04](casos-de-teste.md#tc-f22-04) | Negativo | IDEM-Motor-Expurgo, P7 | `docs/arquitetura/referencia/dados/testes_expurgo.sql::rodar duas vezes deixa 3 eventos e 1 confirmacao` (existe) |
| F22 | [TC-F22-05](casos-de-teste.md#tc-f22-05) | Negativo | INV-Visita-6 | `api/tests/Corredor.Cadastro.Testes/RevogarConsentimentoTestes.cs::Caso33_Revogar_MantemVisitas` (proposto) |
| F23 | [TC-F23-01](casos-de-teste.md#tc-f23-01) | Positivo | P15, CANAL-Banco, INV-EventoDeSeguranca-2 | `api/tests/Corredor.Guarda.Testes/InterceptadorDeEntradaTestes.cs::Caso38_CpfEmCondicoes_MascaradoNoBancoENoLog` (proposto) |
| F23 | [TC-F23-02](casos-de-teste.md#tc-f23-02) | Positivo | P15, INV-EventoDeSeguranca-1, INV-EventoDeSeguranca-4 | `api/tests/Corredor.Guarda.Testes/InterceptadorDeEntradaTestes.cs::Mascaramento_RegistraEventoComTipoCanalModuloERota` (proposto) |
| F23 | [TC-F23-03](casos-de-teste.md#tc-f23-03) | Negativo | INV-EventoDeSeguranca-1, INV-EventoDeSeguranca-4 | `api/tests/Corredor.Guarda.Testes/InterceptadorDeEntradaTestes.cs::Evento_SemValorSemTrechoSemAcs` (proposto) |
| F23 | [TC-F23-04](casos-de-teste.md#tc-f23-04) | Positivo | P15, CANAL-Banco, INV-EventoDeSeguranca-2 | `api/tests/Corredor.Guarda.Testes/InterceptadorDeEntradaTestes.cs::ChaveSenha_MascaradaComoCredencial_SeveridadeMaior` (proposto) |
| F23 | [TC-F23-05](casos-de-teste.md#tc-f23-05) | Negativo | P15, CANAL-Banco | `api/tests/Corredor.Guarda.Testes/InterceptadorDeEntradaTestes.cs::CasaToken_NuncaMascarado` (proposto) |
| F23 | [TC-F23-06](casos-de-teste.md#tc-f23-06) | Negativo | P15, CANAL-Banco | `api/tests/Corredor.Guarda.Testes/InterceptadorDeEntradaTestes.cs::NomeEnderecoSaude_SoRotulo_SemMascaraSemEvento` (proposto) |
| F23 | [TC-F23-07](casos-de-teste.md#tc-f23-07) | Negativo | P15, INV-EventoDeSeguranca-2 | `api/tests/Corredor.Guarda.Testes/InterceptadorDeEntradaTestes.cs::SemAchado_NaoRegistraEvento` (proposto) |
| F23 | [TC-F23-08](casos-de-teste.md#tc-f23-08) | Positivo | IDEM-Guarda-Registrar, P15 | `api/tests/Corredor.Guarda.Testes/InterceptadorDeEntradaTestes.cs::MesmoCpfDuasVezes_DoisEventos` (proposto) |
| F23 | [TC-F23-09](casos-de-teste.md#tc-f23-09) | Negativo | IDEM-Guarda-Registrar, P15 | `api/tests/Corredor.Guarda.Testes/InterceptadorDeEntradaTestes.cs::VisitaRepetida_UmaVisitaDoisEventos` (proposto) |
| F24 | [TC-F24-01](casos-de-teste.md#tc-f24-01) | Negativo | CANAL-Log, NC-9 | `api/tests/Corredor.Guarda.Testes/ProvedorDeLogSeguroTestes.cs::Caso40_NomeNoLog_ViraMascaraEEvento` (proposto) |
| F24 | [TC-F24-02](casos-de-teste.md#tc-f24-02) | Positivo | CANAL-Log | `api/tests/Corredor.Guarda.Testes/ProvedorDeLogSeguroTestes.cs::TextoSemRotulo_SaiInteiroSemEvento` (proposto) |
| F24 | [TC-F24-03](casos-de-teste.md#tc-f24-03) | Negativo | CANAL-RespostaSemAutenticacao, NC-9 | `api/tests/Corredor.Guarda.Testes/FiltroDeRespostaTestes.cs::Caso41_RotaSemAutenticacao_CondicoesBloqueadas` (proposto) |
| F24 | [TC-F24-04](casos-de-teste.md#tc-f24-04) | Positivo | CANAL-RespostaSemAutenticacao | `api/tests/Corredor.Guarda.Testes/FiltroDeRespostaTestes.cs::RespostaSemRotulo_SaiInteira` (proposto) |
| F24 | [TC-F24-05](casos-de-teste.md#tc-f24-05) | Negativo | CANAL-MensagemDeErro, NC-9 | `api/tests/Corredor.Guarda.Testes/TratadorDeErrosTestes.cs::ExcecaoComNome_ViraTextoNeutro` (proposto) |
| F24 | [TC-F24-06](casos-de-teste.md#tc-f24-06) | Positivo | CANAL-MensagemDeErro | `api/tests/Corredor.Guarda.Testes/TratadorDeErrosTestes.cs::ErroDeDominioSemRotulo_SaiInteiro` (proposto) |
| F24 | [TC-F24-07](casos-de-teste.md#tc-f24-07) | Positivo | CANAL-Mqtt | `api/tests/Corredor.Guarda.Testes/SaidaSeguraTestes.cs::Mqtt_ComandoSemRotulo_Liberado` (proposto) |
| F24 | [TC-F24-08](casos-de-teste.md#tc-f24-08) | Negativo | CANAL-Mqtt, NC-9 | `api/tests/Corredor.Guarda.Testes/SaidaSeguraTestes.cs::Mqtt_TextoComEndereco_Bloqueado` (proposto) |
| F24 | [TC-F24-09](casos-de-teste.md#tc-f24-09) | Positivo | CANAL-PromptDoLlm | `api/tests/Corredor.Guarda.Testes/TradutorTestes.cs::PromptSemRotulo_ChegaAoLlm` (proposto) |
| F24 | [TC-F24-10](casos-de-teste.md#tc-f24-10) | Negativo | CANAL-PromptDoLlm, NC-9 | `api/tests/Corredor.Guarda.Testes/TradutorTestes.cs::PromptComRotulo_BloqueadoNaSaidaSegura` (proposto) |
| F24 | [TC-F24-11](casos-de-teste.md#tc-f24-11) | Positivo | INV-EventoDeSeguranca-5, NC-9 | `api/tests/Corredor.Guarda.Testes/EventoDeSegurancaTestes.cs::Evento_SobreviveAoReinicio_SemMudar` (proposto) |
| F24 | [TC-F24-12](casos-de-teste.md#tc-f24-12) | Negativo | INV-EventoDeSeguranca-5 | `api/tests/Corredor.Guarda.Testes/EventoDeSegurancaTestes.cs::Gatilho_RecusaDeleteEUpdate` (proposto) |
| F25 | [TC-F25-01](casos-de-teste.md#tc-f25-01) | Negativo | `POST /mensagens/verificar`, INV-MensagemSaida-2, INV-MensagemSaida-5, P16, NC-8, CANAL-GrupoDoBairro | `api/tests/Corredor.Cadastro.Testes/VerificarMensagemTestes.cs::Caso39_MoraSozinhoEDiuretico_Bloqueada` (proposto) |
| F25 | [TC-F25-02](casos-de-teste.md#tc-f25-02) | Negativo | `POST /mensagens/verificar`, P16, CANAL-GrupoDoBairro, NC-8 | `api/tests/Corredor.Cadastro.Testes/VerificarMensagemTestes.cs::MoraSozinhaFeminino_Bloqueada` (proposto) |
| F25 | [TC-F25-03](casos-de-teste.md#tc-f25-03) | Negativo | INV-MensagemSaida-3, NC-8 | `api/tests/Corredor.Cadastro.Testes/VerificarMensagemTestes.cs::Bloqueada_GravaSoTextoMascarado` (proposto) |
| F25 | [TC-F25-04](casos-de-teste.md#tc-f25-04) | Positivo | `POST /mensagens/verificar`, INV-MensagemSaida-1, INV-MensagemSaida-2, INV-MensagemSaida-3, P16, CANAL-GrupoDoBairro, NC-8 | `api/tests/Corredor.Cadastro.Testes/VerificarMensagemTestes.cs::VersaoSegura_Liberada` (proposto) |
| F25 | [TC-F25-05](casos-de-teste.md#tc-f25-05) | Positivo | `POST /mensagens/verificar`, INV-MensagemSaida-5, CANAL-GrupoDoBairro | manual |
| F25 | [TC-F25-06](casos-de-teste.md#tc-f25-06) | Negativo | `POST /mensagens/verificar`, INV-MensagemSaida-5, CANAL-GrupoDoBairro | `api/tests/Corredor.Cadastro.Testes/VerificarMensagemTestes.cs::VersaoSegura_TextoFixoSemRua` (proposto) |
| F25 | [TC-F25-07](casos-de-teste.md#tc-f25-07) | Negativo | `POST /mensagens/verificar`, INV-MensagemSaida-1, INV-MensagemSaida-5 | `api/tests/Corredor.Cadastro.Testes/VerificarMensagemTestes.cs::LlmFora_VereditoNaHora` (proposto) |
| F25 | [TC-F25-08](casos-de-teste.md#tc-f25-08) | Positivo | IDEM-Cadastro-CadastrarIdoso, INV-MensagemSaida-4, `POST /mensagens/verificar` | `api/tests/Corredor.Cadastro.Testes/VerificarMensagemTestes.cs::MesmoTextoDuasVezes_DuasMensagens` (proposto) |
| F25 | [TC-F25-09](casos-de-teste.md#tc-f25-09) | Negativo | INV-MensagemSaida-4, IDEM-Cadastro-CadastrarIdoso | `api/tests/Corredor.Cadastro.Testes/VerificarMensagemTestes.cs::NovaTentativa_NaoMudaVereditoAnterior` (proposto) |
| F25 | [TC-F25-10](casos-de-teste.md#tc-f25-10) | Negativo | CANAL-GrupoDoBairro, P16, INV-MensagemSaida-2, NC-8 | `api/tests/Corredor.Cadastro.Testes/VerificarMensagemTestes.cs::TelefoneNoGrupo_Bloqueado` (proposto) |
| F26 | [TC-F26-01](casos-de-teste.md#tc-f26-01) | Positivo | `GET /painel`, FAROL-Verde | `api/tests/Corredor.Guarda.Testes/FarolTestes.cs::SemEvento_Verde` (proposto) |
| F26 | [TC-F26-02](casos-de-teste.md#tc-f26-02) | Positivo | `GET /painel`, FAROL-Amarelo | `api/tests/Corredor.Guarda.Testes/FarolTestes.cs::Caso38_SoMascaramento_Amarelo` (proposto) |
| F26 | [TC-F26-03](casos-de-teste.md#tc-f26-03) | Negativo | FAROL-Vermelho, FAROL-Verde | `api/tests/Corredor.Guarda.Testes/FarolTestes.cs::MascaramentoSeveridade5EBloqueio3_NuncaVermelho` (proposto) |
| F26 | [TC-F26-04](casos-de-teste.md#tc-f26-04) | Positivo | `GET /painel`, FAROL-Vermelho, P17, INV-EventoDeSeguranca-3 | `api/tests/Corredor.Guarda.Testes/FarolTestes.cs::Caso39_BloqueioSeveridade4_Vermelho` (proposto) |
| F26 | [TC-F26-05](casos-de-teste.md#tc-f26-05) | Negativo | FAROL-Amarelo, FAROL-Vermelho | `api/tests/Corredor.Guarda.Testes/FarolTestes.cs::MascaramentoMaisBloqueio4_Vermelho` (proposto) |
| F26 | [TC-F26-06](casos-de-teste.md#tc-f26-06) | Negativo | P17, INV-EventoDeSeguranca-3 | `api/tests/Corredor.Guarda.Testes/TradutorTestes.cs::Caso42_LlmFora_FraseFixa` (proposto) |
| F26 | [TC-F26-07](casos-de-teste.md#tc-f26-07) | Negativo | INV-EventoDeSeguranca-3, P17 | `api/tests/Corredor.Guarda.Testes/TradutorTestes.cs::RespostaTardia_NaoTrocaFraseFixa` (proposto) |
| F26 | [TC-F26-08](casos-de-teste.md#tc-f26-08) | Negativo | P17 | `api/tests/Corredor.Guarda.Testes/TradutorTestes.cs::Prompt_SoTipoCanalModulo` (proposto) |
| F26 | [TC-F26-09](casos-de-teste.md#tc-f26-09) | Negativo | `GET /painel` | `api/tests/Corredor.Guarda.Testes/PainelTestes.cs::Painel_SemValorPessoal` (proposto) |
| F27 | [TC-F27-01](casos-de-teste.md#tc-f27-01) | Positivo | APP-FILA-1, APP-CACHE-5 | `app/src/banco/__tests__/fila.test.ts::resposta201_marcaEnviadoEApagaObservacao` (proposto) |
| F27 | [TC-F27-02](casos-de-teste.md#tc-f27-02) | Negativo | APP-FILA-1, APP-FILA-2, APP-CACHE-5, APP-CACHE-2 | `app/src/banco/__tests__/fila.test.ts::semRede_continuaPendenteESomaTentativa` (proposto) |
| F27 | [TC-F27-03](casos-de-teste.md#tc-f27-03) | Negativo | APP-FILA-2 | `app/src/banco/__tests__/fila.test.ts::resposta5xx_mesmoIdNaProximaTentativa` (proposto) |
| F27 | [TC-F27-04](casos-de-teste.md#tc-f27-04) | Negativo | APP-FILA-3 | `app/src/banco/__tests__/fila.test.ts::resposta401_paraAFilaSemMudarEstado` (proposto) |
| F27 | [TC-F27-05](casos-de-teste.md#tc-f27-05) | Positivo | APP-FILA-2, APP-FILA-3 | `app/src/banco/__tests__/fila.test.ts::enviaDoMaisAntigoAoMaisNovo_UmPorVez` (proposto) |
| F27 | [TC-F27-06](casos-de-teste.md#tc-f27-06) | Negativo | APP-FILA-4 | `app/src/banco/__tests__/fila.test.ts::resposta422_marcaRecusadoENaoTentaDeNovo` (proposto) |
| F27 | [TC-F27-07](casos-de-teste.md#tc-f27-07) | Positivo | APP-FILA-4, APP-FILA-1 | `app/src/banco/__tests__/fila.test.ts::recusadoNaoTravaAFila` (proposto) |
| F27 | [TC-F27-08](casos-de-teste.md#tc-f27-08) | Positivo | APP-CACHE-2, APP-CACHE-3, APP-CACHE-4, APP-CACHE-6 | `app/src/banco/__tests__/cache.test.ts::radarGravaAOrdemDoServidor` (proposto) |
| F27 | [TC-F27-09](casos-de-teste.md#tc-f27-09) | Positivo | APP-CACHE-1, APP-CACHE-3 | `app/src/banco/__tests__/cache.test.ts::trocarNaoMesclar_casaRevogadaSome` (proposto) |
| F27 | [TC-F27-10](casos-de-teste.md#tc-f27-10) | Negativo | APP-CACHE-3, APP-CACHE-1 | `app/src/banco/__tests__/cache.test.ts::respostaComErroNaoTocaOCache` (proposto) |
| F27 | [TC-F27-11](casos-de-teste.md#tc-f27-11) | Negativo | APP-CACHE-6 | `app/src/banco/__tests__/cache.test.ts::radarComMaisDe2MinutosSeDeclaraVelho` (proposto) |
| F27 | [TC-F27-12](casos-de-teste.md#tc-f27-12) | Negativo | APP-CACHE-4 | `app/src/banco/__tests__/cache.test.ts::cardSemNomeApareceComTexto` (proposto) |
| F28 | [TC-F28-01](casos-de-teste.md#tc-f28-01) | Positivo | P5 | manual |
| F28 | [TC-F28-02](casos-de-teste.md#tc-f28-02) | Negativo | P5 | manual |
| F28 | [TC-F28-03](casos-de-teste.md#tc-f28-03) | Negativo | P5 | manual |
| F28 | [TC-F28-04](casos-de-teste.md#tc-f28-04) | Positivo | P8 | manual |
| F28 | [TC-F28-05](casos-de-teste.md#tc-f28-05) | Negativo | P8 | manual |
| F28 | [TC-F28-06](casos-de-teste.md#tc-f28-06) | Positivo | P12 | manual |
| F28 | [TC-F28-07](casos-de-teste.md#tc-f28-07) | Negativo | P12 | manual |
| F29 | [TC-F29-01](casos-de-teste.md#tc-f29-01) | Positivo | INFRA-RUB-1, INFRA-RUB-2 | `infra/verificar.sh` (existe) |
| F29 | [TC-F29-02](casos-de-teste.md#tc-f29-02) | Positivo | INFRA-RUB-2, INFRA-RUB-6 | `infra/provisionar.sh` (existe) |
| F29 | [TC-F29-03](casos-de-teste.md#tc-f29-03) | Negativo | INFRA-RUB-1 | `infra/verificar.sh` (existe) |
| F29 | [TC-F29-04](casos-de-teste.md#tc-f29-04) | Negativo | INFRA-RUB-2 | manual |
| F29 | [TC-F29-05](casos-de-teste.md#tc-f29-05) | Positivo | INFRA-carga, INFRA-memoria, INFRA-disco, INFRA-broker, INFRA-api, INFRA-RUB-3 | `infra/verificar.sh` (existe) |
| F29 | [TC-F29-06](casos-de-teste.md#tc-f29-06) | Positivo | INFRA-monitor, INFRA-travado, INFRA-RUB-4 | `infra/verificar.sh` (existe) |
| F29 | [TC-F29-07](casos-de-teste.md#tc-f29-07) | Negativo | INFRA-broker, INFRA-RUB-6 | `infra/teste-de-falha.sh::cenario_broker` (existe) |
| F29 | [TC-F29-08](casos-de-teste.md#tc-f29-08) | Negativo | INFRA-api, INFRA-RUB-6 | `infra/teste-de-falha.sh::cenario_api` (existe) |
| F29 | [TC-F29-09](casos-de-teste.md#tc-f29-09) | Negativo | INFRA-monitor, INFRA-RUB-4 | `infra/teste-de-falha.sh::cenario_monitor` (existe) |
| F29 | [TC-F29-10](casos-de-teste.md#tc-f29-10) | Negativo | INFRA-travado, INFRA-RUB-4 | `infra/teste-de-falha.sh::cenario_travado` (existe) |
| F29 | [TC-F29-11](casos-de-teste.md#tc-f29-11) | Negativo | INFRA-carga, INFRA-RUB-3 | `infra/teste-de-falha.sh::cenario_carga` (existe) |
| F29 | [TC-F29-12](casos-de-teste.md#tc-f29-12) | Negativo | INFRA-memoria, INFRA-RUB-3 | `infra/teste-de-falha.sh::cenario_memoria` (existe) |
| F29 | [TC-F29-13](casos-de-teste.md#tc-f29-13) | Negativo | INFRA-disco, INFRA-RUB-3 | `infra/teste-de-falha.sh::cenario_disco` (existe) |
| F29 | [TC-F29-14](casos-de-teste.md#tc-f29-14) | Positivo | INFRA-RUB-5 | `infra/verificar.sh` (existe) |
| F29 | [TC-F29-15](casos-de-teste.md#tc-f29-15) | Negativo | INFRA-RUB-5 | manual |

## Por regra e por rota

Cada linha é um item que `docs/qa/ferramentas/registro.py` lê de `docs/arquitetura/`. Os casos do modelo (CT) precisam de um Test Case. Os outros precisam de uma Feature, um caso positivo e um negativo.

| Item | Origem | Features | Positivos | Negativos |
|---|---|---|---|---|
| `PUT /dias-criticos/{data}` | 03 §6.3 | F19 | TC-F19-01, TC-F19-02 | TC-F19-04, TC-F19-08 |
| `POST /dias-criticos/{data}/encerramento` | 03 §6.3 | F19 | TC-F19-05 | TC-F19-06, TC-F19-07 |
| `GET /radar?microarea={id}` | 03 §6.3 | F15 | TC-F15-01, TC-F15-02, TC-F15-03 | TC-F15-04, TC-F15-05 |
| `GET /casas/{token}/linha-do-tempo` | 03 §6.3 | F16 | TC-F16-01, TC-F16-05 | TC-F16-02, TC-F16-03, TC-F16-04 |
| `PUT /demo/relogio` | 03 §6.3 | F21 | TC-F21-01, TC-F21-02, TC-F21-05 | TC-F21-03, TC-F21-04 |
| `POST /idosos` | 03 §6.3 | F01 | TC-F01-01, TC-F01-04 | TC-F01-02, TC-F01-03, TC-F01-06 |
| `POST /idosos/{id}/consentimento` | 03 §6.3 | F02 | TC-F02-01, TC-F02-03 | TC-F02-04 |
| `DELETE /idosos/{id}/consentimento` | 03 §6.3 | F03 | TC-F03-01 | TC-F03-03 |
| `PUT /idosos/{id}/vizinho` | 03 §6.3 | F04 | TC-F04-01, TC-F04-04 | TC-F04-02 |
| `PUT /idosos/{id}/perfil` | 03 §6.3 | F05 | TC-F05-01, TC-F05-02 | TC-F05-03, TC-F05-05 |
| `GET /lista-de-nomes?microarea={id}` | 03 §6.3 | F06 | TC-F06-01 | TC-F06-03, TC-F06-04 |
| `POST /visitas` | 03 §6.3 | F17 | TC-F17-01, TC-F17-06, TC-F17-09 | TC-F17-07, TC-F17-08, TC-F17-10 |
| `POST /mensagens/verificar` | 03 §6.3 | F25 | TC-F25-04, TC-F25-05, TC-F25-08 | TC-F25-01, TC-F25-02, TC-F25-06, TC-F25-07 |
| `GET /avisos?microarea={id}` | 03 §6.3 | F18 | TC-F18-01, TC-F18-03, TC-F18-06 | TC-F18-09 |
| `POST /avisos/{id}/resposta` | 03 §6.3 | F18 | TC-F18-06 | TC-F18-07 |
| `GET /painel` | 03 §6.3 | F26 | TC-F26-01, TC-F26-02, TC-F26-04 | TC-F26-09 |
| R1 | 03 §5.4 | F12 | TC-F12-03, TC-F12-04, TC-F12-05 | TC-F12-01, TC-F12-02, TC-F12-06, TC-F12-07 |
| R2 | 03 §5.4 | F09 | TC-F09-03, TC-F09-04 | TC-F09-01, TC-F09-02, TC-F09-05 |
| R3 | 03 §5.4 | F11 | TC-F11-02, TC-F11-03 | TC-F11-01, TC-F11-04 |
| R4 | 03 §5.4 | F10 | TC-F10-03, TC-F10-05, TC-F10-07 | TC-F10-01, TC-F10-02, TC-F10-04, TC-F10-08 |
| SV-Sozinho | 03 §5.3 | F08 | TC-F08-01, TC-F08-02 | TC-F08-07 |
| SV-Acamado | 03 §5.3 | F08 | TC-F08-06 | TC-F08-05 |
| SV-Coabitado | 03 §5.3 | F08 | TC-F08-04 | TC-F08-03 |
| TV-SIT-1 | 03 §5.6 | F12 | TC-F12-03 | TC-F12-01, TC-F12-02, TC-F12-07 |
| TV-SIT-2 | 03 §5.6 | F09, F13 | TC-F09-04, TC-F13-07 | TC-F09-01, TC-F09-02, TC-F09-05, TC-F09-06 |
| TV-SIT-3 | 03 §5.6 | F11 | TC-F11-02 | TC-F11-01 |
| TV-SIT-4 | 03 §5.6 | F10 | TC-F10-03 | TC-F10-01, TC-F10-04, TC-F10-08 |
| TV-SIT-5 | 03 §5.6 | F13 | TC-F13-02 | TC-F13-04 |
| TV-SIT-6 | 03 §5.6 | F13 | - | TC-F13-01, TC-F13-03, TC-F13-04 |
| TV-SIT-7 | 03 §5.6 | F13 | - | TC-F13-05, TC-F13-06 |
| TV-ATD-1 | 03 §5.7 | F14 | TC-F14-01 | - |
| TV-ATD-2 | 03 §5.7 | F14 | - | TC-F14-02 |
| TV-ATD-3 | 03 §5.7 | F14 | - | TC-F14-03 |
| TV-ATD-4 | 03 §5.7 | F14 | TC-F14-04 | - |
| TV-ATD-5 | 03 §5.7 | F14 | - | TC-F14-03 |
| TV-ATD-6 | 03 §5.7 | F14 | TC-F14-05 | - |
| TV-ATD-7 | 03 §5.7 | F14 | - | TC-F14-06 |
| TV-ATD-8 | 03 §5.7 | F14 | TC-F14-07 | - |
| TV-ATD-9 | 03 §5.7 | F14 | TC-F14-08 | - |
| INV-CasaMonitorada-1 | 03 §4 | F02 | TC-F02-05 | TC-F02-06 |
| INV-CasaMonitorada-2 | 03 §4 | F07 | TC-F07-01, TC-F07-05 | TC-F07-04, TC-F07-08 |
| INV-CasaMonitorada-3 | 03 §4 | F03 | TC-F03-01 | TC-F03-02, TC-F03-04 |
| INV-CasaMonitorada-4 | 03 §4 | F14 | TC-F14-10 | TC-F14-09 |
| INV-CasaMonitorada-5 | 03 §4 | F14 | TC-F14-12 | TC-F14-11 |
| INV-CasaMonitorada-6 | 03 §4 | F16 | TC-F16-05 | TC-F16-04 |
| INV-EventoSensor-1 | 03 §4 | F07 | TC-F07-03 | TC-F07-02 |
| INV-EventoSensor-2 | 03 §4 | F07 | TC-F07-01 | TC-F07-08 |
| INV-EventoSensor-3 | 03 §4 | F07 | TC-F07-05 | TC-F07-04 |
| INV-EventoSensor-4 | 03 §4 | F07 | TC-F07-01, TC-F07-07 | TC-F07-06 |
| INV-EventoSensor-5 | 03 §4 | F07 | TC-F07-01 | TC-F07-09 |
| INV-DiaCritico-1 | 03 §4 | F19 | TC-F19-01 | TC-F19-03 |
| INV-DiaCritico-2 | 03 §4 | F19 | TC-F19-01 | TC-F19-04 |
| INV-DiaCritico-3 | 03 §4 | F19 | TC-F19-01 | TC-F19-04 |
| INV-DiaCritico-4 | 03 §4 | F19 | TC-F19-02 | TC-F19-03 |
| INV-DiaCritico-5 | 03 §4 | F19 | TC-F19-02 | TC-F19-08 |
| INV-DiaCritico-6 | 03 §4 | F20 | TC-F20-01 | TC-F20-02 |
| INV-DiaCritico-7 | 03 §4 | F19 | TC-F19-05 | TC-F19-06 |
| INV-Idoso-1 | 03 §4 | F02 | TC-F02-01 | TC-F02-02 |
| INV-Idoso-2 | 03 §4 | F02 | TC-F02-03 | TC-F02-04 |
| INV-Idoso-3 | 03 §4 | F02 | TC-F02-01 | TC-F02-07 |
| INV-Idoso-4 | 03 §4 | F02 | TC-F02-01 | TC-F02-07 |
| INV-Idoso-5 | 03 §4 | F02 | TC-F02-03 | TC-F02-04 |
| INV-Idoso-6 | 03 §4 | F04 | TC-F04-01 | TC-F04-02, TC-F04-03 |
| INV-Idoso-7 | 03 §4 | F04 | TC-F04-06 | TC-F04-07 |
| INV-Idoso-8 | 03 §4 | F03 | TC-F03-01 | TC-F03-04 |
| INV-Idoso-9 | 03 §4 | F01 | TC-F01-01 | TC-F01-02, TC-F01-03 |
| INV-Idoso-10 | 03 §4 | F01 | TC-F01-01 | TC-F01-06 |
| INV-Visita-1 | 03 §4 | F17 | TC-F17-05 | TC-F17-07 |
| INV-Visita-2 | 03 §4 | F17 | TC-F17-06 | TC-F17-07 |
| INV-Visita-3 | 03 §4 | F17 | TC-F17-09 | TC-F17-10 |
| INV-Visita-4 | 03 §4 | F17 | TC-F17-01 | TC-F17-08 |
| INV-Visita-5 | 03 §4 | F17 | TC-F17-01 | TC-F17-11 |
| INV-Visita-6 | 03 §4 | F03, F22 | TC-F03-01, TC-F22-01 | TC-F03-03, TC-F03-05, TC-F22-05 |
| INV-Visita-7 | 03 §4 | F17 | TC-F17-09 | TC-F17-10 |
| INV-MensagemSaida-1 | 03 §4 | F25 | TC-F25-04 | TC-F25-07 |
| INV-MensagemSaida-2 | 03 §4 | F25 | TC-F25-04 | TC-F25-01, TC-F25-10 |
| INV-MensagemSaida-3 | 03 §4 | F25 | TC-F25-04 | TC-F25-03 |
| INV-MensagemSaida-4 | 03 §4 | F25 | TC-F25-08 | TC-F25-09 |
| INV-MensagemSaida-5 | 03 §4 | F25 | TC-F25-05 | TC-F25-01, TC-F25-06, TC-F25-07 |
| INV-AvisoAoVizinho-1 | 03 §4 | F18 | TC-F18-03 | TC-F18-02 |
| INV-AvisoAoVizinho-2 | 03 §4 | F18 | TC-F18-01 | TC-F18-04 |
| INV-AvisoAoVizinho-3 | 03 §4 | F18 | TC-F18-06 | TC-F18-07 |
| INV-AvisoAoVizinho-4 | 03 §4 | F18 | TC-F18-01 | TC-F18-09 |
| INV-EventoDeSeguranca-1 | 03 §4 | F23 | TC-F23-02 | TC-F23-03 |
| INV-EventoDeSeguranca-2 | 03 §4 | F23 | TC-F23-01, TC-F23-04 | TC-F23-07 |
| INV-EventoDeSeguranca-3 | 03 §4 | F26 | TC-F26-04 | TC-F26-06, TC-F26-07 |
| INV-EventoDeSeguranca-4 | 03 §4 | F23 | TC-F23-02 | TC-F23-03 |
| INV-EventoDeSeguranca-5 | 03 §4 | F24 | TC-F24-11 | TC-F24-12 |
| DESF-EstavaBem | 03 §4.3 | F17 | TC-F17-01, TC-F17-05 | TC-F17-12 |
| DESF-Encaminhada | 03 §4.3 | F17 | TC-F17-01 | TC-F17-12 |
| DESF-EstavaFora | 03 §4.3 | F17 | TC-F17-03 | TC-F17-04 |
| DESF-NaoAbriu | 03 §4.3 | F17 | TC-F17-05 | TC-F17-02 |
| CANAL-GrupoDoBairro | 03 §4.4 | F25 | TC-F25-04, TC-F25-05 | TC-F25-01, TC-F25-02, TC-F25-06, TC-F25-10 |
| CANAL-PromptDoLlm | 03 §4.4 | F24 | TC-F24-09 | TC-F24-10 |
| CANAL-Mqtt | 03 §4.4 | F24 | TC-F24-07 | TC-F24-08 |
| CANAL-MensagemDeErro | 03 §4.4 | F24 | TC-F24-06 | TC-F24-05 |
| CANAL-RespostaSemAutenticacao | 03 §4.4 | F24 | TC-F24-04 | TC-F24-03 |
| CANAL-Vizinho | 03 §4.4 | F18 | TC-F18-01 | TC-F18-09 |
| CANAL-Log | 03 §4.4 | F24 | TC-F24-02 | TC-F24-01 |
| CANAL-AppAutenticado | 03 §4.4 | F06 | TC-F06-01 | TC-F06-02, TC-F06-03 |
| CANAL-Banco | 03 §4.4 | F23 | TC-F23-01, TC-F23-04 | TC-F23-05, TC-F23-06 |
| FAROL-Vermelho | 03 §4.4 | F26 | TC-F26-04 | TC-F26-03, TC-F26-05 |
| FAROL-Amarelo | 03 §4.4 | F26 | TC-F26-02 | TC-F26-05 |
| FAROL-Verde | 03 §4.4 | F26 | TC-F26-01 | TC-F26-03 |
| IDEM-Motor-RegistrarPacote | 03 §4.5 | F07 | TC-F07-03 | TC-F07-02 |
| IDEM-Motor-AtivarCasa | 03 §4.5 | F02 | TC-F02-03, TC-F02-05 | TC-F02-04, TC-F02-06 |
| IDEM-Motor-DefinirPerfil | 03 §4.5 | F05 | TC-F05-01, TC-F05-02 | TC-F05-04 |
| IDEM-Motor-Confirmar | 03 §4.5 | F14 | TC-F14-10 | TC-F14-09 |
| IDEM-Motor-DesativarCasa | 03 §4.5 | F03 | TC-F03-01 | TC-F03-03 |
| IDEM-Motor-DeclararDiaCritico | 03 §4.5 | F19 | TC-F19-02 | TC-F19-03 |
| IDEM-Motor-PedirToque | 03 §4.5 | F20 | TC-F20-01 | TC-F20-02 |
| IDEM-Motor-EncerrarTurno | 03 §4.5 | F19 | TC-F19-05 | TC-F19-06 |
| IDEM-Motor-Expurgo | 03 §4.5 | F22 | TC-F22-01, TC-F22-03 | TC-F22-04 |
| IDEM-Cadastro-RegistrarConsentimento | 03 §4.5 | F02 | TC-F02-03 | TC-F02-04 |
| IDEM-Cadastro-RevogarConsentimento | 03 §4.5 | F03 | TC-F03-01 | TC-F03-03 |
| IDEM-Cadastro-IndicarVizinho | 03 §4.5 | F04, F05 | TC-F04-01, TC-F04-04, TC-F05-02 | TC-F04-05 |
| IDEM-Cadastro-RegistrarVisita | 03 §4.5 | F17 | TC-F17-05, TC-F17-06 | TC-F17-07 |
| IDEM-Cadastro-PedidoDeContato | 03 §4.5 | F18 | TC-F18-03 | TC-F18-02 |
| IDEM-Cadastro-RegistrarResposta | 03 §4.5 | F18 | TC-F18-06 | TC-F18-07 |
| IDEM-Cadastro-CadastrarIdoso | 03 §4.5 | F01, F25 | TC-F01-04, TC-F25-08 | TC-F01-05, TC-F25-09 |
| IDEM-Guarda-Registrar | 03 §4.5 | F23 | TC-F23-08 | TC-F23-09 |
| NC-1 | 03 §8.3 | F07 | TC-F07-03 | TC-F07-02, TC-F07-04 |
| NC-2 | 03 §8.3 | F09 | TC-F09-03 | TC-F09-01, TC-F09-06 |
| NC-3 | 03 §8.3 | F12 | TC-F12-03, TC-F12-05 | TC-F12-01 |
| NC-4 | 03 §8.3 | F13 | TC-F13-02 | TC-F13-01, TC-F13-03, TC-F13-04, TC-F13-05 |
| NC-5 | 03 §8.3 | F08 | TC-F08-01, TC-F08-04 | TC-F08-03, TC-F08-05, TC-F08-07 |
| NC-6 | 03 §8.3 | F02 | TC-F02-01 | TC-F02-02 |
| NC-7 | 03 §8.3 | F17 | TC-F17-03 | TC-F17-02, TC-F17-04 |
| NC-8 | 03 §8.3 | F25 | TC-F25-04 | TC-F25-01, TC-F25-02, TC-F25-03, TC-F25-10 |
| NC-9 | 03 §8.3 | F24 | TC-F24-11 | TC-F24-01, TC-F24-03, TC-F24-05, TC-F24-08, TC-F24-10 |
| NC-10 | 03 §8.3 | F21 | TC-F21-01, TC-F21-02, TC-F21-05 | TC-F21-03, TC-F21-04 |
| P1 | 02 Políticas | F02 | TC-F02-01 | TC-F02-02 |
| P2 | 02 Políticas | F07 | TC-F07-07 | TC-F07-06 |
| P3 | 02 Políticas | F06 | TC-F06-01 | TC-F06-02, TC-F06-04 |
| P4 | 02 Políticas | F03 | TC-F03-01 | TC-F03-03 |
| P5 | 02 Políticas | F28 | TC-F28-01 | TC-F28-02, TC-F28-03 |
| P6 | 02 Políticas | F07 | TC-F07-03 | TC-F07-02 |
| P7 | 02 Políticas | F22 | TC-F22-01, TC-F22-03 | TC-F22-02, TC-F22-04 |
| P8 | 02 Políticas | F19, F28 | TC-F19-01, TC-F28-04 | TC-F19-04, TC-F28-05 |
| P9 | 02 Políticas | F10 | TC-F10-05 | TC-F10-06 |
| P10 | 02 Políticas | F18 | TC-F18-01, TC-F18-03 | TC-F18-05 |
| P11 | 02 Políticas | F17 | TC-F17-01, TC-F17-03, TC-F17-05 | TC-F17-02 |
| P12 | 02 Políticas | F20, F28 | TC-F20-01, TC-F28-06 | TC-F20-02, TC-F20-03, TC-F20-04, TC-F28-07 |
| P13 | 02 Políticas | F13 | TC-F13-07, TC-F13-08 | TC-F13-06 |
| P14 | 02 Políticas | F09 | TC-F09-03, TC-F09-04 | TC-F09-01, TC-F09-06 |
| P15 | 02 Políticas | F23 | TC-F23-01, TC-F23-02, TC-F23-04, TC-F23-08 | TC-F23-05, TC-F23-06, TC-F23-07, TC-F23-09 |
| P16 | 02 Políticas | F25 | TC-F25-04 | TC-F25-01, TC-F25-02, TC-F25-10 |
| P17 | 02 Políticas | F26 | TC-F26-04 | TC-F26-06, TC-F26-07, TC-F26-08 |
| P18 | 02 Políticas | F17 | TC-F17-06 | TC-F17-07 |
| P19 | 02 Políticas | F18 | TC-F18-01, TC-F18-03 | TC-F18-02, TC-F18-04 |
| P20 | 02 Políticas | F18 | TC-F18-06 | TC-F18-08 |
| P21 | 02 Políticas | F05 | TC-F05-01 | TC-F05-03, TC-F05-04 |
| P22 | 02 Políticas | F02 | TC-F02-05 | TC-F02-06 |
| APP-CACHE-1 | 04 §6.2 | F27 | TC-F27-09 | TC-F27-10 |
| APP-CACHE-2 | 04 §6.2 | F27 | TC-F27-08 | TC-F27-02 |
| APP-CACHE-3 | 04 §6.2 | F27 | TC-F27-08, TC-F27-09 | TC-F27-10 |
| APP-CACHE-4 | 04 §6.2 | F27 | TC-F27-08 | TC-F27-12 |
| APP-CACHE-5 | 04 §6.2 | F27 | TC-F27-01 | TC-F27-02 |
| APP-CACHE-6 | 04 §6.2 | F27 | TC-F27-08 | TC-F27-11 |
| APP-FILA-1 | 04 §6.3 | F27 | TC-F27-01, TC-F27-07 | TC-F27-02 |
| APP-FILA-2 | 04 §6.3 | F27 | TC-F27-05 | TC-F27-02, TC-F27-03 |
| APP-FILA-3 | 04 §6.3 | F27 | TC-F27-05 | TC-F27-04 |
| APP-FILA-4 | 04 §6.3 | F27 | TC-F27-07 | TC-F27-06 |
| INFRA-broker | 05 §4.5 | F29 | TC-F29-05 | TC-F29-07 |
| INFRA-api | 05 §4.5 | F29 | TC-F29-05 | TC-F29-08 |
| INFRA-monitor | 05 §4.5 | F29 | TC-F29-06 | TC-F29-09 |
| INFRA-carga | 05 §4.5 | F29 | TC-F29-05 | TC-F29-11 |
| INFRA-memoria | 05 §4.5 | F29 | TC-F29-05 | TC-F29-12 |
| INFRA-disco | 05 §4.5 | F29 | TC-F29-05 | TC-F29-13 |
| INFRA-travado | 05 §4.5 | F29 | TC-F29-06 | TC-F29-10 |
| INFRA-RUB-1 | 05 §7 | F29 | TC-F29-01 | TC-F29-03 |
| INFRA-RUB-2 | 05 §7 | F29 | TC-F29-01, TC-F29-02 | TC-F29-04 |
| INFRA-RUB-3 | 05 §7 | F29 | TC-F29-05 | TC-F29-11, TC-F29-12, TC-F29-13 |
| INFRA-RUB-4 | 05 §7 | F29 | TC-F29-06 | TC-F29-09, TC-F29-10 |
| INFRA-RUB-5 | 05 §7 | F29 | TC-F29-14 | TC-F29-15 |
| INFRA-RUB-6 | 05 §7 | F29 | TC-F29-02 | TC-F29-07, TC-F29-08 |
| CT-1 | 03 §7 | - | - | TC-F08-07, TC-F13-01 |
| CT-2 | 03 §7 | - | TC-F08-01 | - |
| CT-3 | 03 §7 | - | - | TC-F10-01 |
| CT-4 | 03 §7 | - | TC-F10-03 | - |
| CT-5 | 03 §7 | - | - | TC-F10-02 |
| CT-6 | 03 §7 | - | - | TC-F13-03 |
| CT-7 | 03 §7 | - | - | TC-F10-04 |
| CT-8 | 03 §7 | - | TC-F13-02, TC-F14-08, TC-F17-01 | - |
| CT-9 | 03 §7 | - | - | TC-F13-04, TC-F17-12 |
| CT-10 | 03 §7 | - | - | TC-F09-01 |
| CT-11 | 03 §7 | - | TC-F14-04 | TC-F09-05 |
| CT-12 | 03 §7 | - | - | TC-F09-05 |
| CT-13 | 03 §7 | - | TC-F09-04, TC-F16-01 | - |
| CT-14 | 03 §7 | - | - | TC-F09-06 |
| CT-15 | 03 §7 | - | TC-F02-01 | TC-F09-02 |
| CT-16 | 03 §7 | - | TC-F08-06 | TC-F08-05, TC-F10-08 |
| CT-17 | 03 §7 | - | TC-F10-05, TC-F15-03 | - |
| CT-18 | 03 §7 | - | - | TC-F11-01 |
| CT-19 | 03 §7 | - | TC-F14-01 | TC-F12-01, TC-F14-02 |
| CT-20 | 03 §7 | - | TC-F12-03 | TC-F12-02 |
| CT-21 | 03 §7 | - | - | TC-F17-02 |
| CT-22 | 03 §7 | - | TC-F14-07, TC-F15-02, TC-F17-03 | - |
| CT-23 | 03 §7 | - | TC-F14-04 | - |
| CT-24 | 03 §7 | - | - | TC-F08-03 |
| CT-25 | 03 §7 | - | TC-F08-04 | - |
| CT-26 | 03 §7 | - | - | TC-F07-02 |
| CT-27 | 03 §7 | - | TC-F07-03 | - |
| CT-28 | 03 §7 | - | - | TC-F07-04, TC-F21-03 |
| CT-29 | 03 §7 | - | - | TC-F19-03 |
| CT-30 | 03 §7 | - | TC-F20-01 | TC-F20-02 |
| CT-31 | 03 §7 | - | - | TC-F22-02, TC-F22-04 |
| CT-32 | 03 §7 | - | - | TC-F02-02, TC-F06-04 |
| CT-33 | 03 §7 | - | TC-F03-01 | TC-F22-05 |
| CT-34 | 03 §7 | - | - | TC-F04-07 |
| CT-35 | 03 §7 | - | - | TC-F18-02 |
| CT-36 | 03 §7 | - | - | TC-F18-04 |
| CT-37 | 03 §7 | - | TC-F17-06 | TC-F14-09, TC-F17-07 |
| CT-38 | 03 §7 | - | TC-F23-01, TC-F26-02 | TC-F01-06, TC-F26-03 |
| CT-39 | 03 §7 | - | TC-F26-04 | TC-F25-01 |
| CT-40 | 03 §7 | - | - | TC-F24-01, TC-F26-03 |
| CT-41 | 03 §7 | - | - | TC-F24-03 |
| CT-42 | 03 §7 | - | - | TC-F26-06 |
