# =================================================================
# STAGE 1: Compilação e Publicação (SDK .NET 10.0)
# =================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. Copia arquivos de projeto (.csproj) individualmente para otimizar o cache de camadas do Docker
COPY ["MercadoBonsai.Web/MercadoBonsai.Web.csproj", "MercadoBonsai.Web/"]
COPY ["MercadoBonsai.Domain/MercadoBonsai.Domain.csproj", "MercadoBonsai.Domain/"]
COPY ["MercadoBonsai.Infrastructure/MercadoBonsai.Infrastructure.csproj", "MercadoBonsai.Infrastructure/"]

# 2. Restaura dependências do NuGet
RUN dotnet restore "MercadoBonsai.Web/MercadoBonsai.Web.csproj"

# 3. Copia todo o código fonte e realiza o Publish de Release
COPY . .
WORKDIR "/src/MercadoBonsai.Web"
RUN dotnet publish "MercadoBonsai.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =================================================================
# STAGE 2: Imagem de Execução Final (Runtime ASP.NET 10.0)
# =================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Configura a porta padrão do ASP.NET Core no container (Porta 8080)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Copia os artefatos compilados da etapa de build
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MercadoBonsai.Web.dll"]
