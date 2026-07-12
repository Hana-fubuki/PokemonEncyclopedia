# Pokémon Encyclopedia

A comprehensive .NET Aspire-based web application for exploring Pokémon data with an interactive UI, real-time caching, and full Azure deployment support.

**Live Demo**: [pokémon-encyclopedia.azurewebsites.net](https://pokepedia-dev-web.azurecontainerapps.io) (when deployed)

## Features

- 🎨 **Interactive UI**: Light/dark theme toggle, interactive ability cards, evolution charts
- ⚡ **High Performance**: Multi-tier caching (Redis + in-memory) for API responses
- 🔄 **Real-time Updates**: Responsive Blazor components with streaming data
- 📊 **Advanced Filtering**: Filter Pokémon by type, rarity (legendary/mythical), region
- 🎯 **Detailed Information**: Comprehensive Pokémon stats, abilities, evolution chains
- 📈 **Production Ready**: Full observability via Application Insights + OpenTelemetry
- 🚀 **Cloud Deployment**: Single-click Azure deployment with Bicep/Terraform
- 💾 **Data Caching**: Automatic warmup on startup for ~1,025 Pokémon species

## Prerequisites

### Required
- **.NET 10 SDK** ([Download](https://dotnet.microsoft.com/en-us/download/dotnet/10.0))
- **.NET Aspire** (installed with .NET 10)
- **Docker Desktop** ([Download](https://www.docker.com/products/docker-desktop))
- **Git** ([Download](https://git-scm.com))

### For Azure Deployment (Optional)
- **Azure CLI** ([Install](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli))
- **Bicep CLI** (included with Azure CLI 2.20.0+)
- **Terraform** ([Download](https://www.terraform.io/downloads.html)) - only for Terraform deployments
- **Active Azure Subscription**

### Verify Installations
```bash
# Check .NET SDK
dotnet --version

# Check Docker
docker --version

# Check Aspire (optional - will auto-install when needed)
dotnet workload list
```

## Local Development Setup

### 1. Clone the Repository
```bash
git clone https://github.com/Hana-fubuki/PokemonEncyclopedia.git
cd PokemonEncyclopedia
```

### 2. Restore Dependencies
```bash
# Restore all NuGet packages
dotnet restore
```

### 3. Build the Solution
```bash
# Build in debug mode
dotnet build

# Or build with optimizations
dotnet build -c Release
```

## Running Locally

### Option 1: Run with Aspire Dashboard (Recommended for Development)

The Aspire Dashboard provides a unified view of all services, logs, and resource monitoring.

```bash
# Start the application with dashboard
dotnet run --project PokemonEncyclopedia.AppHost

# The dashboard will automatically open at: http://localhost:18629
```

**Dashboard provides:**
- Real-time service status and logs
- Resource monitoring (CPU, memory, container stats)
- Service dependencies and communication flow
- Health check status
- Direct access to service URLs

### Option 2: Run Individual Services

If you prefer to run services separately:

```bash
# Terminal 1: Start Docker containers (Redis, Cosmos DB Emulator)
docker-compose -f docker-compose.yml up -d

# Terminal 2: Start API Service
dotnet run --project PokemonEncyclopedia.ApiService

# Terminal 3: Start Web Service
dotnet run --project PokemonEncyclopedia.Web
```

Then access:
- **Web Application**: http://localhost:5173
- **API Service**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **Scalar API Docs**: http://localhost:5000/scalar

## Service Endpoints

When running locally with Aspire:

| Service | URL | Description |
|---------|-----|-------------|
| **Web Frontend** | http://localhost:5173 | Main Blazor application |
| **API Service** | http://localhost:5000 | RESTful API |
| **Swagger UI** | http://localhost:5000/swagger | API documentation (development only) |
| **Scalar Docs** | http://localhost:5000/scalar | Modern API documentation |
| **Hangfire Dashboard** | http://localhost:5000/hangfire | Background job monitoring |
| **Health Check** | http://localhost:5000/health | Service health status |
| **Aspire Dashboard** | http://localhost:18629 | Orchestration dashboard |

## Application Architecture

### Services

```
┌─────────────────────────────────────────────────────────────┐
│                   Docker Network (Local)                     │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │   Web App    │  │  API Service │  │   Redis      │       │
│  │  (Blazor)    │──│  (.NET 10)   │──│   Cache      │       │
│  │              │  │              │  │              │       │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
│                          │                                    │
│                          ▼                                    │
│                  ┌──────────────┐                            │
│                  │  Cosmos DB   │                            │
│                  │  Emulator    │                            │
│                  │  (Hangfire)  │                            │
│                  └──────────────┘                            │
│                                                               │
│  External APIs:                                              │
│  • PokéAPI (https://pokeapi.co)                             │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### Key Components

1. **PokemonEncyclopedia.Web** - Blazor interactive web frontend
   - Razor components with interactive server rendering
   - Real-time filtering and search
   - Theme persistence (light/dark mode)

2. **PokemonEncyclopedia.ApiService** - RESTful API backend
   - Pokémon data retrieval and caching
   - Ability/form/move endpoints
   - Hangfire background job orchestration
   - Health checks and monitoring

3. **PokemonEncyclopedia.Application** - Business logic
   - MediatR handlers for CQRS pattern
   - Domain models and entities

4. **PokemonEncyclopedia.Infrastructure** - Data access
   - Repository patterns
   - Redis cache implementation
   - Cosmos DB integration

5. **PokemonEncyclopedia.ServiceDefaults** - Shared configuration
   - OpenTelemetry setup
   - Service discovery
   - Health checks

6. **PokemonEncyclopedia.AppHost** - Aspire orchestration
   - Service composition
   - Resource configuration (Redis, Cosmos DB)
   - Environment variable management

## Common Development Tasks

### View API Documentation

**Swagger UI** (traditional):
```bash
# Navigate to http://localhost:5000/swagger
```

**Scalar** (modern, recommended):
```bash
# Navigate to http://localhost:5000/scalar
```

### Restart Services

```bash
# From Aspire Dashboard
# Click the service card and select "Restart"

# Or restart all from command line
dotnet run --project PokemonEncyclopedia.AppHost
```

### View Logs

```bash
# In Aspire Dashboard:
# - Click "Logs" tab
# - Select service from dropdown
# - View real-time logs

# Or via CLI:
docker logs <container-name>
```

### Access Redis Cache

```bash
# Connect to Redis
docker exec -it pokemonencyclopedia-redis redis-cli

# View cache keys
> KEYS *

# Get a value
> GET pokemon:1

# Clear cache
> FLUSHDB
```

### Access Cosmos DB Emulator

The Cosmos DB emulator runs on `https://localhost:8081` with the following credentials:
- **Endpoint**: https://localhost:8081
- **Key**: C2y6yDjf5/R+ob0N8UZrLJWJRY4IQstsSiP1ADAcgGQ=

To explore data:
```bash
# Cosmos DB Data Explorer opens at https://localhost:8081/_explorer/index.html
```

### Run Tests

```bash
# Run all tests
dotnet test

# Run specific project tests
dotnet test PokemonEncyclopedia.Tests

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run with code coverage
dotnet test /p:CollectCoverage=true
```

### Clean Up

```bash
# Stop all services
dotnet stop  # Only if using Aspire AppHost

# Stop Docker containers
docker-compose down

# Stop and remove all containers
docker-compose down -v

# Clean build artifacts
dotnet clean
```

## Data Caching & Warmup

On startup, the API service automatically:

1. **Caches all Pokémon** (~1,025 species) from PokéAPI
2. **Caches all Moves** (~900) with names and URLs
3. **Caches all Abilities** (~300+) for interactive cards
4. **Stores in Redis** with appropriate TTL (24 hours)
5. **Falls back to in-memory** cache if Redis unavailable

This warmup typically takes **15-30 seconds** on first run. Subsequent starts are much faster as Redis persists data.

**Warmup status**: Check the Aspire Dashboard or API logs for `Pokemon catalog warmup completed` message.

## Environment Variables

### Local Development (Auto-configured by Aspire)

| Variable | Value | Purpose |
|----------|-------|---------|
| `ASPNETCORE_ENVIRONMENT` | `Development` | Enable dev features (Swagger, detailed errors) |
| `DEPLOYMENT_MODE` | `local` | Use local Docker containers instead of Azure |
| `REDIS_ENDPOINT` | `localhost:6379` | Redis cache connection |
| `COSMOS_CONNECTION_STRING` | Cosmos DB emulator | Hangfire job backend |

### Production/Azure

See [`infra/README.md`](./infra/README.md) for Azure-specific configuration.

## Troubleshooting

### "Docker daemon is not running"

```bash
# Windows: Start Docker Desktop from Applications
# macOS: Open Docker.app from Applications
# Linux: Start Docker service
sudo systemctl start docker
```

### "Port already in use" Error

```bash
# Find process using port
netstat -ano | findstr :5000

# Kill the process (Windows, replace PID)
taskkill /PID <PID> /F

# Or use a different port
dotnet run --project PokemonEncyclopedia.Web -- --urls "http://localhost:5174"
```

### Redis Connection Failed

```bash
# Check if Redis container is running
docker ps | grep redis

# Check Redis logs
docker logs pokemonencyclopedia-redis

# Restart Redis
docker restart pokemonencyclopedia-redis

# Or rebuild from scratch
docker-compose down -v && docker-compose up -d
```

### Cosmos DB Emulator Not Starting

```bash
# Check if Cosmos emulator is running
docker ps | grep cosmos

# Check logs
docker logs pokemonencyclopedia-cosmos

# Increase available disk space (emulator needs ~1GB)
# Restart Docker and increase resource limits

# Access Cosmos Data Explorer (verify it's running)
# https://localhost:8081/_explorer/index.html
```

### Aspire Dashboard Won't Open

```bash
# Verify Aspire is running
# Check if port 18629 is in use
netstat -ano | findstr :18629

# Manually open dashboard
# http://localhost:18629

# Check AppHost logs for errors
dotnet run --project PokemonEncyclopedia.AppHost --verbose
```

### Slow Performance

- **Clear cache**: `docker exec -it pokemonencyclopedia-redis redis-cli FLUSHDB`
- **Restart services**: Stop and re-run AppHost
- **Check Docker resources**: Docker Desktop → Settings → Resources → increase allocated CPU/RAM
- **Monitor with Aspire**: Check "Resource" tab for CPU/memory usage

### "No such file or directory" on Linux

```bash
# Ensure compose file path is correct
docker-compose -f docker-compose.yml up -d

# Or create docker-compose.yml in project root if missing
# Copy from: https://github.com/Hana-fubuki/PokemonEncyclopedia/docker-compose.yml
```

## Development Workflow

### Adding a New Feature

1. **Create feature branch**:
   ```bash
   git checkout -b feature/my-feature
   ```

2. **Update domain models** in `PokemonEncyclopedia.Application`

3. **Add business logic** via MediatR handlers

4. **Update API endpoints** in `PokemonEncyclopedia.ApiService`

5. **Create UI components** in `PokemonEncyclopedia.Web`

6. **Test locally**:
   ```bash
   dotnet run --project PokemonEncyclopedia.AppHost
   ```

7. **Run tests**:
   ```bash
   dotnet test
   ```

8. **Push and create PR**:
   ```bash
   git push origin feature/my-feature
   ```

### Code Quality

- **Format code**: `dotnet format`
- **Run linters**: `dotnet analyzers` (via IDE)
- **Unit tests**: `dotnet test`
- **Integration tests**: See `PokemonEncyclopedia.Tests`

## Performance Tips

### For Local Development

1. **Use Release mode for benchmarking**:
   ```bash
   dotnet run -c Release --project PokemonEncyclopedia.AppHost
   ```

2. **Monitor with Aspire Dashboard**:
   - View resource usage per service
   - Identify slow endpoints
   - Check error rates

3. **Redis optimization**:
   - Monitor cache hit rate
   - Adjust TTL values as needed
   - Use `docker stats` to monitor memory

### For Production (Azure)

See [`infra/README.md`](./infra/README.md) for deployment and scaling recommendations.

## Deployment

### Local Deployment (Development)
```bash
dotnet run --project PokemonEncyclopedia.AppHost
```

### Azure Deployment

See [`infra/README.md`](./infra/README.md) for:
- Building container images
- Deploying with Bicep (default)
- Deploying with Terraform
- Configuring Application Insights
- Setting up CI/CD with GitHub Actions

### Using GitHub Actions

1. **Push to `main` branch**:
   ```bash
   git push origin main
   ```

2. **Bicep deployment** (automatic):
   - Workflow: `.github/workflows/deploy-bicep.yml`
   - Triggered on: code or `infra/bicep/` changes

3. **Terraform deployment** (manual):
   - Workflow: `.github/workflows/deploy-terraform.yml`
   - Trigger via: Actions tab → "Manual Deploy with Terraform" → Run workflow

## API Reference

### Pokémon Endpoints

```bash
# Get all Pokémon
GET /api/pokemon

# Get Pokémon by ID
GET /api/pokemon/:id

# Get Pokémon by name
GET /api/pokemon/:name

# Filter Pokémon
GET /api/pokemon?type=fire&legendary=true
```

### Ability Endpoints

```bash
# Get all abilities
GET /api/abilities

# Get ability by ID
GET /api/abilities/:id

# Get ability by name
GET /api/abilities/:name
```

### Move Endpoints

```bash
# Get all moves
GET /api/moves

# Get move by ID
GET /api/moves/:id
```

Full API documentation available at `/swagger` or `/scalar`

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support & Resources

- **PokéAPI**: https://pokeapi.co
- **.NET Aspire Docs**: https://learn.microsoft.com/aspire/
- **Blazor Docs**: https://learn.microsoft.com/aspnet/core/blazor/
- **Azure Container Apps**: https://docs.microsoft.com/azure/container-apps/
- **OpenTelemetry**: https://opentelemetry.io/

## Authors

- **Developer**: Josh
- **Infrastructure**: Copilot

## Acknowledgments

- PokéAPI for comprehensive Pokémon data
- .NET team for Aspire orchestration framework
- Azure community for deployment best practices
