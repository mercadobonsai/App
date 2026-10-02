# Usa a imagem oficial do ASP.NET Core Runtime para execução (.NET 10)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_HTTP_PORTS=80

# Copia o conteúdo da pasta publish gerada para o container
COPY publish/ .

# Define o comando de inicialização apontando para a sua DLL principal
ENTRYPOINT ["dotnet", "MercadoBonsai.Web.dll"]
