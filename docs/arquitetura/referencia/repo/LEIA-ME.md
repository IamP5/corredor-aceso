# Arquivos de referência da infra e da entrega

Esta pasta tem os 60 arquivos de `infra/`, `tools/`, `.github/` e da raiz do repositório do Corredor Aceso, nos caminhos finais. O desenho e os motivos estão em [05-infra-e-entrega.md](../../05-infra-e-entrega.md).

## Copiar para o repositório

Rode da raiz do repositório novo, com o Git limpo:

```bash
rsync -a --exclude LEIA-ME.md docs/arquitetura/referencia/repo/ ./
chmod +x infra/*.sh infra/bin/corredor infra/nuvem/preflight.sh tools/*.sh
git add -A && git update-index --chmod=+x infra/*.sh infra/bin/corredor infra/nuvem/preflight.sh tools/*.sh
```

Se a pasta `docs/` ainda não estiver no repositório, troque o caminho de origem pelo caminho onde esta pasta estiver.

Depois troque três valores:

1. `SUA-ORG/corredor-aceso` em `infra/config/corredor.conf.padrao` e em `infra/cloud-init/corredor.yaml`.
2. `MQTT_PREFIXO_PUBLICO` em `infra/config/corredor.conf.padrao`, com o mesmo valor no sketch do Wokwi.
3. Os nomes `@dono-...` em `.github/CODEOWNERS`.

## Checagens antes do primeiro PR

| Comando | O que confere | Precisa de |
|---|---|---|
| `npm run check:infra` | `bash -n` e `shellcheck` nos 20 scripts, CRLF e o repositório do cloud-init | `shellcheck` |
| `npm run check:terraform` | `terraform fmt -check` e `terraform validate` em `infra/nuvem/` | `terraform`. Passa mesmo sem chave em `infra/chaves/equipe/`. Só o `apply` exige uma |
| `npm run check:migracoes` | nenhuma migração do EF Core apaga ou renomeia tabela ou coluna até 20/11 | só Bash |
| `npm run check:api` | compila, gera o SQL das migrações e aplica duas vezes, como o dono de cada schema | .NET 10, `psql` e a variável `CORREDOR_BD_TESTE` |
| `npm run check:pacote` | monta o pacote x64 e confere o conteúdo | .NET 10 e a API com `Corredor.slnx` |
| `actionlint` | os três workflows | `actionlint` |

`check:api` e `check:pacote` só passam depois que `api/` existe. Até lá, o CI pula o job `api` e o check `ci-ok` continua verde.
