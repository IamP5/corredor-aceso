# API, migrações e carga da demo numa imagem só. Quem roda o compose não precisa do SDK do .NET.
# O estágio sdk compila e gera o SQL das migrações com tools/migracoes.sh, como o deploy da VM.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS sdk
WORKDIR /src
COPY global.json dotnet-tools.json .editorconfig ./
COPY infra/config/corredor.conf.padrao infra/config/
COPY tools/migracoes.sh tools/
COPY api/ api/
RUN dotnet publish api/src/Corredor.Api -c Release -o /saida/api --nologo \
 && bash tools/migracoes.sh /saida/migracoes

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends postgresql-client curl \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=sdk /saida/api ./
COPY --from=sdk /saida/migracoes /migracoes
COPY infra/config/bd-modulo.sql infra/config/corredor.conf.padrao /config/
COPY api/semente/casas.json /semente-original/casas.json
COPY tools/docker/preparar.sh /usr/local/bin/preparar
ENTRYPOINT ["dotnet", "Corredor.Api.dll"]
