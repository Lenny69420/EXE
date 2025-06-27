# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Multi-stage build for development
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5052

# Install curl and DNS tools for health checks and debugging
RUN apt-get update && apt-get install -y curl dnsutils netcat-openbsd && rm -rf /var/lib/apt/lists/*

# Create directories for data protection keys
RUN mkdir -p /app/keys && chmod 755 /app/keys

# Set environment variables for container
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV ASPNETCORE_URLS=http://0.0.0.0:5052
ENV ASPNETCORE_ENVIRONMENT=Development

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["QuatBook.csproj", "."]
RUN dotnet restore "QuatBook.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "QuatBook.csproj" -c Debug -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "QuatBook.csproj" -c Debug -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user and set permissions
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Set environment variables for data protection
ENV ASPNETCORE_DATA_PROTECTION_KEY_DIRECTORY=/app/keys

ENTRYPOINT ["dotnet", "QuatBook.dll"]