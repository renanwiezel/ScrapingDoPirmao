# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "SimpleFetchApi/SimpleFetchApi.csproj"
RUN dotnet publish "SimpleFetchApi/SimpleFetchApi.csproj" -c Release -o /app

# runtime - VOLTA PARA ASPNET porque é uma web API
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .

EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000
CMD ["dotnet", "SimpleFetchApi.dll"]