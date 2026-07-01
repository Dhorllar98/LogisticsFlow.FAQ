FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/LogisticsFlow.API/LogisticsFlow.API.csproj", "src/LogisticsFlow.API/"]
COPY ["src/LogisticsFlow.Application/LogisticsFlow.Application.csproj", "src/LogisticsFlow.Application/"]
COPY ["src/LogisticsFlow.Infrastructure/LogisticsFlow.Infrastructure.csproj", "src/LogisticsFlow.Infrastructure/"]
COPY ["src/LogisticsFlow.Domain/LogisticsFlow.Domain.csproj", "src/LogisticsFlow.Domain/"]

RUN dotnet restore "src/LogisticsFlow.API/LogisticsFlow.API.csproj"

COPY . .
WORKDIR "/src/src/LogisticsFlow.API"
RUN dotnet build "LogisticsFlow.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LogisticsFlow.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LogisticsFlow.API.dll"]