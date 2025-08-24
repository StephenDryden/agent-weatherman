# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY AgentWeatherman/AgentWeatherman.csproj AgentWeatherman/
RUN dotnet restore AgentWeatherman/AgentWeatherman.csproj

# Copy source code and build
COPY AgentWeatherman/ AgentWeatherman/
RUN dotnet build AgentWeatherman/AgentWeatherman.csproj -c Release -o /app/build

# Publish stage
RUN dotnet publish AgentWeatherman/AgentWeatherman.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

# Install additional dependencies if needed
RUN apt-get update && apt-get install -y \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Set environment variables
ENV DOTNET_ENVIRONMENT=Production
ENV WEATHERAGENT_AgentSettings__GitHubToken=""
ENV WEATHERAGENT_AgentSettings__McpServerUrl="ws://mcp-server:3000"

# Expose port (if needed for health checks)
EXPOSE 8080

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Set the entry point
ENTRYPOINT ["dotnet", "AgentWeatherman.dll"]