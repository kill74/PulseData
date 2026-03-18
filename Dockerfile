# Multi-stage build for production-ready Docker image
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy solution and project files
COPY ["src/PulseData.Core/PulseData.Core.csproj", "src/PulseData.Core/"]
COPY ["src/PulseData.Infrastructure/PulseData.Infrastructure.csproj", "src/PulseData.Infrastructure/"]
COPY ["src/PulseData.API/PulseData.API.csproj", "src/PulseData.API/"]

# Restore dependencies
RUN dotnet restore "src/PulseData.API/PulseData.API.csproj"

# Copy entire source
COPY . .

# Build application
WORKDIR "/src/src/PulseData.API"
RUN dotnet build "PulseData.API.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "PulseData.API.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app

# Install curl for health checks
RUN apk add --no-cache curl

# Copy published application
COPY --from=publish /app/publish .

# Create non-root user for security
RUN addgroup -g 1000 appuser && adduser -D -u 1000 -G appuser appuser && chown -R appuser:appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8000/api/health/live || exit 1

# Expose port
EXPOSE 8000
ENV ASPNETCORE_URLS=http://+:8000
ENV ASPNETCORE_ENVIRONMENT=Production

# Run application
ENTRYPOINT ["dotnet", "PulseData.API.dll"]
