# Development Guide for PulseData

This guide provides comprehensive information for developers working on the PulseData project.

## Table of Contents

1. [Development Environment Setup](#development-environment-setup)
2. [Project Structure](#project-structure)
3. [Running the Project](#running-the-project)
4. [Code Organization](#code-organization)
5. [Database Development](#database-development)
6. [API Development](#api-development)
7. [ETL Pipeline Development](#etl-pipeline-development)
8. [Testing](#testing)
9. [Debugging](#debugging)
10. [Common Tasks](#common-tasks)

---

## Development Environment Setup

### Install Required Tools

```bash
# macOS with Homebrew
brew install dotnet docker

# Ubuntu/Debian
sudo apt-get install dotnet-sdk-8.0 docker.io docker-compose

# Windows
# Use chocolatey or download from official websites
choco install dotnet-sdk docker-desktop
```

### IDE Setup

**Visual Studio Code (Recommended for this project)**

1. Install extensions:
   - C# Dev Kit (ms-dotnettools.csharp)
   - C# Extensions (kreativ-software.csharp-extension-pack)
   - Docker (ms-azuretools.vscode-docker)
   - SQL Tools (mtxr.sqltools)
   - PostgreSQL (mtxr.sqltools-driver-pg)

2. Open workspace:
   ```bash
   code PulseData.sln
   ```

**Visual Studio 2022**

- Open `PulseData.sln` directly
- SQL Server Object Explorer can connect to PostgreSQL with the right connection string

### Clone and Initialize

```bash
git clone https://github.com/yourusername/PulseData.git
cd PulseData

# Create environment file
cp .env.example .env

# Start infrastructure
docker-compose up -d

# Restore NuGet packages
dotnet restore
```

---

## Project Structure

```
PulseData/
├── src/
│   ├── PulseData.Core/                 # Shared domain logic
│   │   ├── DTOs/                       # Data Transfer Objects
│   │   ├── Interfaces/                 # Contracts for repositories
│   │   └── Models/
│   │       └── Entities.cs             # Domain entities
│   │
│   ├── PulseData.Infrastructure/       # Data access layer
│   │   ├── Data/
│   │   │   └── DbConnectionFactory.cs  # Connection management
│   │   └── Repositories/               # Repository implementations
│   │       ├── AnalyticsRepository.cs
│   │       ├── CustomerRepository.cs
│   │       └── OrderRepository.cs
│   │
│   ├── PulseData.API/                  # REST API
│   │   ├── Controllers/                # API endpoints
│   │   │   ├── AnalyticsController.cs
│   │   │   ├── CustomersAndProductsController.cs
│   │   │   └── OrdersController.cs
│   │   ├── Middleware/                 # Request/response middleware
│   │   │   └── GlobalExceptionMiddleware.cs
│   │   ├── Program.cs                  # Startup & DI configuration
│   │   └── appsettings.json
│   │
│   └── PulseData.ETL/                  # Data pipeline
│       ├── Models/
│       │   └── EtlModels.cs
│       ├── Pipeline/
│       │   └── OrderEtlPipeline.cs
│       ├── sample_orders.csv
│       └── Program.cs
│
├── sql/                                # Database schema & procedures
│   ├── 001_create_schema.sql          # Tables, indexes, constraints
│   ├── 002_seed_data.sql              # Initial/test data
│   ├── 003_views.sql                  # Reporting views
│   └── 004_stored_procedures.sql      # Business logic in SQL
│
├── docker-compose.yml                  # Service orchestration
├── Dockerfile                          # API container image
└── Makefile                            # Development commands
```

### Dependency Graph

```
PulseData.API
    ├── PulseData.Infrastructure
    │   └── PulseData.Core
    └── PulseData.Core

PulseData.ETL
    └── PulseData.Core
```

---

## Running the Project

### Start Infrastructure Only

```bash
# Start PostgreSQL and pgAdmin
docker-compose up -d

# Verify services are running
docker-compose ps
```

### Option 1: Run Everything Locally

```bash
# Terminal 1: Database
docker-compose up -d

# Terminal 2: ETL Pipeline
cd src/PulseData.ETL
dotnet run

# Terminal 3: API
cd src/PulseData.API
dotnet run
```

Then access:

- API: `https://localhost:5001`
- Swagger: `https://localhost:5001/swagger/index.html`
- pgAdmin: `http://localhost:8080`

### Option 2: Run Everything in Containers

```bash
docker-compose --profile with-api up -d

# View logs
docker-compose logs -f api
```

Then access:

- API: `http://localhost:8000`
- pgAdmin: `http://localhost:8080`

### Option 3: Mixed Setup

```bash
# Start infrastructure
docker-compose up -d

# Terminal in src/PulseData.API
cd src/PulseData.API
dotnet watch run  # Hot reload enabled
```

---

## Code Organization

### Naming Conventions

- **Classes**: PascalCase (`OrderRepository`, `AnalyticsController`)
- **Methods**: PascalCase (`GetTopProducts()`, `CalculateRevenue()`)
- **Properties**: PascalCase (`OrderId`, `CustomerName`)
- **Private fields**: camelCase with underscore (`_logger`, `_repository`)
- **Constants**: UPPER_CASE (`DEFAULT_PAGE_SIZE = 50`)

### Architecture Principles

**Clean Architecture Layers:**

1. **Core** - Domain models, DTOs, interfaces (no external dependencies)
2. **Infrastructure** - Data access, repository implementations
3. **API** - Controllers, middleware, HTTP concerns

**Key Rules:**

- ✅ Controllers → Services/Repositories
- ✅ Repositories → Database only
- ❌ Don't: Controllers → Database directly
- ❌ Don't: Mix business logic with HTTP logic

### Dependency Injection

Configured in [src/PulseData.API/Program.cs](src/PulseData.API/Program.cs):

```csharp
builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
// etc.
```

---

## Database Development

### Connect to PostgreSQL

```bash
# Using psql (installed with PostgreSQL)
psql -h localhost -U pulsedata_user -d pulsedata

# Using Docker directly
docker exec -it pulsedata_db psql -U pulsedata_user -d pulsedata

# Using Make
make db-connect
```

### Run SQL Scripts

```bash
# Via psql
psql -h localhost -U pulsedata_user -d pulsedata -f sql/001_create_schema.sql

# Via Docker
docker exec pulsedata_db psql -U pulsedata_user -d pulsedata < sql/001_create_schema.sql

# Via Make
make db-exec QUERY="SELECT * FROM customers LIMIT 5"
```

### View Database in pgAdmin

1. Open `http://localhost:8080`
2. Login: `admin@pulsedata.local` / `admin`
3. Servers → PulseData → Databases → pulsedata
4. Right-click tables to query or modify

### Modify Schema

**For new tables/columns:**

1. Create new SQL migration file: `005_add_new_feature.sql`
2. Include IF NOT EXISTS checks:
   ```sql
   CREATE TABLE IF NOT EXISTS new_table (
       id SERIAL PRIMARY KEY,
       name VARCHAR(255) NOT NULL
   );
   ```
3. Test locally: `psql -h localhost ... -f sql/005_add_new_feature.sql`
4. Add to docker-compose.yml volumes for auto-initialization
5. Update [Documentation](docs/architecture.md) if schema changes significantly

### Performance Optimization

```bash
# View active queries
make db-exec QUERY="SELECT pid, query FROM pg_stat_activity WHERE query != '<idle>';"

# Check table sizes
make db-exec QUERY="SELECT schemaname, tablename, pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) FROM pg_tables WHERE schemaname NOT IN ('pg_catalog', 'information_schema') ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;"

# Analyze query plans
make db-exec QUERY="EXPLAIN ANALYZE SELECT * FROM orders WHERE customer_id = 123;"
```

---

## API Development

### Project Structure

```
src/PulseData.API/
├── Controllers/          # REST endpoints
├── Middleware/           # Request/response processing
├── Properties/
│   └── launchSettings.json
├── appsettings.json      # Configuration
├── Program.cs            # Startup configuration
└── PulseData.API.csproj
```

### Adding a New Endpoint

1. Create repository method in `Infrastructure/Repositories/*.cs`:

   ```csharp
   public async Task<List<Order>> GetOrdersByStatus(string status)
   {
       var query = "SELECT * FROM orders WHERE status = @Status";
       return (await _connection.QueryAsync<Order>(query,
           new { Status = status })).ToList();
   }
   ```

2. Add interface in `Core/Interfaces/IRepositories.cs`:

   ```csharp
   Task<List<Order>> GetOrdersByStatus(string status);
   ```

3. Create/Update controller in `Controllers/OrdersController.cs`:
   ```csharp
   [HttpGet("by-status/{status}")]
   public async Task<ActionResult<List<OrderDto>>> GetByStatus(string status)
   {
       var orders = await _orderRepository.GetOrdersByStatus(status);
       return Ok(_mapper.Map<List<OrderDto>>(orders));
   }
   ```

### Configuration

Edit `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=pulsedata;User Id=pulsedata_user;Password=pulsedata_pass;Port=5432;"
  }
}
```

### Error Handling

Global exception middleware in `Middleware/GlobalExceptionMiddleware.cs` catches all unhandled exceptions and returns consistent error responses.

When throwing exceptions:

```csharp
if (!customer.Exists)
{
    throw new KeyNotFoundException($"Customer {id} not found");
}
```

### Testing Endpoints

```bash
# Using curl
curl http://localhost:5001/api/analytics/top-products

# Using httpie (prettier output)
http GET http://localhost:5001/api/analytics/top-products limit==10

# Using VS Code REST Client extension
# Create test.http in the project root
GET http://localhost:5001/api/orders
Accept: application/json
```

---

## ETL Pipeline Development

### Project Structure

```
src/PulseData.ETL/
├── Models/
│   └── EtlModels.cs      # Data mappings
├── Pipeline/
│   └── OrderEtlPipeline.cs # Main pipeline logic
├── sample_orders.csv
├── Program.cs
└── appsettings.json
```

### Pipeline Flow

```
CSV File
  ↓
Parse/Transform (OrderEtlPipeline)
  ↓
Data Validation
  ↓
Load to Database
  ↓
Log Results
```

### Modifying the Pipeline

1. Update input data model in `Models/EtlModels.cs`:

   ```csharp
   public class OrderImport
   {
       public string OrderId { get; set; }
       public string CustomerId { get; set; }
       public decimal Amount { get; set; }
       // Add new fields following this pattern
   }
   ```

2. Update transformation logic in `Pipeline/OrderEtlPipeline.cs`:

   ```csharp
   private static OrderDto TransformOrder(OrderImport import)
   {
       return new OrderDto
       {
           OrderId = int.Parse(import.OrderId),
           CustomerId = int.Parse(import.CustomerId),
           Amount = import.Amount,
           // Transform new fields
       };
   }
   ```

3. Run the pipeline:
   ```bash
   cd src/PulseData.ETL
   dotnet run
   ```

### Testing Data

```bash
# View sample data
head -20 src/PulseData.ETL/sample_orders.csv

# Create test data
cd src/PulseData.ETL
dotnet run < test_data.csv
```

---

## Testing

### Unit Testing

Current test projects:

```
tests/
└── PulseData.API.Tests/
```

Run API health endpoint tests:

```bash
dotnet test ./tests/PulseData.API.Tests/PulseData.API.Tests.csproj
```

Current coverage in `PulseData.API.Tests`:

- Liveness returns 200 with healthy payload
- Readiness returns 200 when dependencies are healthy
- Readiness returns 408 when dependency check times out
- Readiness returns 503 when dependency is unavailable

Integration coverage in `PulseData.API.Tests` (WebApplicationFactory):

- End-to-end validation of `/api/health/live` status and payload
- End-to-end validation of `/api/health/ready` timeout path (`408`)
- End-to-end validation of `/api/health/ready` dependency failure path (`503`)

### CI Validation

GitHub Actions workflow: `.github/workflows/ci.yml`

The CI pipeline validates:

- API restore and build
- Test project restore and build
- Health endpoint unit test execution on every push/PR to `main`

### Manual Testing Checklist

- [ ] Database schema creation succeeds
- [ ] ETL pipeline runs without errors
- [ ] All API endpoints return expected status codes
- [ ] Pagination works correctly
- [ ] Error handling returns proper error messages
- [ ] Connection pooling works under load

---

## Debugging

### VS Code Debugging

1. Install C# Dev Kit extension
2. Set breakpoints by clicking line numbers
3. Press `F5` to start debugging
4. Use Debug Console to execute commands

### Debug Configuration

`.vscode/launch.json` (auto-generated or manual):

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/PulseData.API/bin/Debug/net8.0/PulseData.API.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/PulseData.API",
      "stopAtEntry": false,
      "serverReadyAction": {
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)",
        "uriFormat": "{0}",
        "action": "openExternally"
      }
    }
  ]
}
```

### Logging

Configure in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "PulseData": "Debug"
    }
  }
}
```

Then inject `ILogger<T>`:

```csharp
public class OrderRepository
{
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(ILogger<OrderRepository> logger)
    {
        _logger = logger;
    }

    public async Task<Order> GetOrder(int id)
    {
        _logger.LogInformation("Fetching order {OrderId}", id);
        // ...
    }
}
```

---

## Common Tasks

### Add a New NuGet Package

```bash
cd src/PulseData.API
dotnet add package PackageName --version 1.0.0
```

### Update All Dependencies

```bash
dotnet list package --outdated
dotnet package update --interactive
```

### Clean Build

```bash
dotnet clean
dotnet build
```

### Format Code

```bash
# Install dotnet format
dotnet tool install -g dotnet-format

# Format entire solution
dotnet format
```

### Check for Security Vulnerabilities

```bash
# Install analyzer
dotnet add package SecurityCodeScan

# Run analysis
dotnet build /p:TreatWarningsAsErrors=true
```

### Generate API Documentation

```bash
# Enable XML documentation in .csproj
# Add to PropertyGroup:
# <GenerateDocumentationFile>true</GenerateDocumentationFile>

# Then run:
dotnet build
```

### Database Backup

```bash
make backup-db
# Creates: backup_YYYYMMDD_HHMMSS.sql
```

### Database Restore

```bash
make restore-db FILE=backup_20240101_120000.sql
```

---

## Troubleshooting

### Port Already in Use

```bash
# Find process using port
sudo lsof -i :5001

# Kill process
kill -9 <PID>

# Or change port in launchSettings.json
```

### Database Connection Failed

```bash
# Check container is running
docker ps | grep pulsedata

# Check logs
docker logs pulsedata_db

# Try reconnecting
make docker-down
docker-compose up -d
```

### NuGet Restore Issues

```bash
# Clear cache
dotnet nuget locals all --clear

# Restore
dotnet restore --force
```

### Entity Framework Commands

If using EF in the future:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## Resources

- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/fundamentals/)
- [ASP.NET Core API Best Practices](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Dapper Documentation](https://github.com/DapperLib/Dapper)
- [Docker Documentation](https://docs.docker.com/)

---

## Getting Help

1. Check documentation in `docs/` folder
2. Review similar code patterns in existing files
3. Check GitHub issues
4. Consult team members or open a discussion
