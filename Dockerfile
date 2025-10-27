# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# runtime - USE A IMAGEM RUNTIME, NÃO ASPNET
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app .

# O Render injeta a variável PORT em runtime.
# Usamos 'sh -c' para expandir ${PORT} em tempo de execução.
EXPOSE 10000
CMD ["dotnet", "SimpleFetchApi.dll"]