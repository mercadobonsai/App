# ETAPA 1: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_HTTP_PORTS=80

# ETAPA 2: Build / Compilação dos fontes .NET 10
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["MercadoBonsai.Web/MercadoBonsai.Web.csproj", "MercadoBonsai.Web/"]
COPY ["MercadoBonsai.Infrastructure/MercadoBonsai.Infrastructure.csproj", "MercadoBonsai.Infrastructure/"]
COPY ["MercadoBonsai.Domain/MercadoBonsai.Domain.csproj", "MercadoBonsai.Domain/"]
RUN dotnet restore "MercadoBonsai.Web/MercadoBonsai.Web.csproj"

COPY . .
WORKDIR "/src/MercadoBonsai.Web"
RUN dotnet build "MercadoBonsai.Web.csproj" -c Release -o /app/build

# ETAPA 3: Publish
FROM build AS publish
RUN dotnet publish "MercadoBonsai.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ETAPA 4: Imagem Final de Execução
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MercadoBonsai.Web.dll"]
