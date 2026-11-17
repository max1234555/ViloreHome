# Збірка (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Web-prog_server/*.csproj ./Web-prog_server/
RUN dotnet restore Web-prog_server/*.csproj
COPY Web-prog_server/ ./Web-prog_server/
RUN dotnet publish Web-prog_server/*.csproj -c Release -o /out

# Рантайм (менший за розміром образ для запуску)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "Web-prog_server.dll"]