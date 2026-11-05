# Imagem da API AutoVia para o Railway.
#
# O contexto de build e a pasta "backend" (Root Directory configurado no
# servico do Railway), por isso todos os caminhos aqui sao relativos a ela.
#
# Build em dois estagios: o SDK compila e publica, a imagem final leva apenas
# o runtime do ASP.NET. Isso derruba a imagem de ~900 MB para ~220 MB, o que
# encurta o deploy e o cold start no Railway.

# ---------------------------------------------------------------------------
# Estagio 1 - compilacao
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Arquivos que valem para a solucao inteira: global.json fixa o SDK 10.0.100
# e Directory.Build.props define TargetFramework net10.0 para todos os projetos.
# Sem eles o restore resolve o framework errado.
COPY global.json Directory.Build.props ./

# Os .csproj entram antes do codigo de proposito: enquanto nenhuma dependencia
# muda, o Docker reaproveita a camada de restore e o deploy fica bem mais rapido.
COPY AutoVia.Api/AutoVia.Api.csproj                       AutoVia.Api/
COPY AutoVia.Communication/AutoVia.Communication.csproj   AutoVia.Communication/
COPY AutoVia.Core/AutoVia.Core.csproj                     AutoVia.Core/
COPY AutoVia.Helpers/AutoVia.Helpers.csproj               AutoVia.Helpers/
COPY AutoVia.Infrastructure/AutoVia.Infrastructure.csproj AutoVia.Infrastructure/
COPY AutoVia.Integrations/AutoVia.Integrations.csproj     AutoVia.Integrations/

RUN dotnet restore AutoVia.Api/AutoVia.Api.csproj

# Agora o codigo-fonte. AutoVia.Tests fica fora da imagem de proposito: a API
# em producao nao precisa dos testes, e copia-los invalidaria o cache a cada
# alteracao de teste.
COPY AutoVia.Api/            AutoVia.Api/
COPY AutoVia.Communication/  AutoVia.Communication/
COPY AutoVia.Core/           AutoVia.Core/
COPY AutoVia.Helpers/        AutoVia.Helpers/
COPY AutoVia.Infrastructure/ AutoVia.Infrastructure/
COPY AutoVia.Integrations/   AutoVia.Integrations/

RUN dotnet publish AutoVia.Api/AutoVia.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ---------------------------------------------------------------------------
# Estagio 2 - runtime
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

EXPOSE 8080

# O Railway injeta a porta na variavel PORT e nada no codigo da API le essa
# variavel. O bind precisa vir daqui, em forma de shell, porque a variavel so
# existe na hora em que o container sobe. Sem isso o Kestrel escuta na porta
# padrao, o healthcheck do Railway nao responde e o deploy e marcado como falho.
# O fallback 8080 mantem a imagem rodando igual fora do Railway.
ENTRYPOINT ["sh", "-c", "exec dotnet AutoVia.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
