# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy csproj and restore dependencies
COPY src/TurnTable.Api/TurnTable.Api.csproj ./src/TurnTable.Api/
RUN dotnet restore ./src/TurnTable.Api/TurnTable.Api.csproj

# Copy everything else and build the release dll
COPY . ./
RUN dotnet publish ./src/TurnTable.Api/TurnTable.Api.csproj -c Release -o out

# 2. Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Hugging Face expects the container to run on port 7860
ENV ASPNETCORE_URLS=http://+:7860
EXPOSE 7860

ENTRYPOINT ["dotnet", "TurnTable.Api.dll"]