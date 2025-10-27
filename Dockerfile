# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# runtime - USE A IMAGEM RUNTIME, N�O ASPNET
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app .

# O Render injeta a vari�vel PORT em runtime.
# Usamos 'sh -c' para expandir ${PORT} em tempo de execu��o.
EXPOSE 10000
CMD ["dotnet", "SimpleFetchApi.dll"]