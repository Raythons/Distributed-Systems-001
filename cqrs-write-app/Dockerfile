FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["cqrs-write-app.csproj", "."]
RUN dotnet restore "cqrs-write-app.csproj"
COPY . .
RUN dotnet build "cqrs-write-app.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "cqrs-write-app.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "cqrs-write-app.dll"]
