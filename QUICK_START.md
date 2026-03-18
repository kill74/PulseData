# Quick Reference for PulseData Development

## Start Development Immediately

```bash
# Clone and get running in 3 commands
git clone <repo-url>
cd PulseData
docker-compose up -d && make docker-logs

# In another terminal
cd src/PulseData.API && dotnet watch run
```

## Service URLs

| Service    | URL                     | Credentials                         |
| ---------- | ----------------------- | ----------------------------------- |
| API        | `http://localhost:5001` | —                                   |
| pgAdmin    | `http://localhost:8080` | `admin@pulsedata.local` / `admin`   |
| PostgreSQL | `localhost:5432`        | `pulsedata_user` / `pulsedata_pass` |

## Essential Make Commands

```bash
make help                              # Overview of all commands
make docker-up                         # Start database + pgAdmin
make docker-logs                       # Watch all service logs
make db-connect                        # Connect to database shell
make db-exec QUERY="SELECT * FROM..."  # Run SQL query
make docker-clean                      # Clean everything (⚠️ deletes data)
```

## Common Workflows

### Add New API Endpoint

1. **Add repository method** → `src/PulseData.Infrastructure/Repositories/*.cs`
2. **Add interface method** → `src/PulseData.Core/Interfaces/IRepositories.cs`
3. **Create controller endpoint** → `src/PulseData.API/Controllers/*.cs`

### Modify Database Schema

1. Create migration file: `sql/005_feature_name.sql`
2. Include `IF NOT EXISTS` checks
3. Test: `psql -h localhost -U pulsedata_user -d pulsedata -f sql/005_*.sql`
4. Restart containers if needed: `docker-compose restart`

### Debug API Issue

```bash
# 1. Check logs
make api-logs

# 2. Verify database connection
make db-connect
SELECT COUNT(*) FROM orders;

# 3. Test endpoint with curl
curl -v http://localhost:5001/api/orders

# 4. Set breakpoint in VS Code and press F5
```

## Project Structure

```
src/
├── PulseData.Core/         ← Domain models & interfaces
├── PulseData.Infrastructure/ ← Data access & repositories
└── PulseData.API/          ← REST API & controllers

sql/
├── 001_create_schema.sql   ← Tables & indexes
├── 002_seed_data.sql       ← Initial data
├── 003_views.sql           ← Reporting views
└── 004_stored_procedures.sql ← Business logic
```

## Key Files

| File                 | Purpose                       |
| -------------------- | ----------------------------- |
| `docker-compose.yml` | Service orchestration         |
| `Dockerfile`         | API container definition      |
| `.env.example`       | Environment variable template |
| `Makefile`           | Development command shortcuts |

## Configuration

- **Connection String** → `.env` or `appsettings.json`
- **Logging Level** → `src/PulseData.API/appsettings.json`
- **Environment** → Set `ASPNETCORE_ENVIRONMENT` variable

## Testing Endpoints

```bash
# Using curl
curl -X GET http://localhost:5001/api/orders

# Using httpie (better formatting)
http GET http://localhost:5001/api/orders limit==10

# Using VS Code REST Client extension (.http files)
GET http://localhost:5001/api/analytics/top-products
Content-Type: application/json
```

## Troubleshooting

| Problem                | Solution                                        |
| ---------------------- | ----------------------------------------------- |
| Port 5432 in use       | `make docker-clean` then `docker-compose up -d` |
| Can't connect to DB    | `make db-connect` to verify, check `.env`       |
| API won't start        | Check logs: `make api-logs`                     |
| Hot reload not working | Kill process: `Ctrl+C`, then `dotnet watch run` |

## Documentation

- **[README.md](README.md)** — Project overview & quick start
- **[DOCKER.md](DOCKER.md)** — Docker setup, all commands, production guide
- **[DEVELOPMENT.md](DEVELOPMENT.md)** — IDE setup, detailed workflows, testing, debugging
- **[docs/architecture.md](docs/architecture.md)** — System design

## Stack Overview

| Component        | Technology   | Version |
| ---------------- | ------------ | ------- |
| Language         | C#           | .NET 8  |
| API Framework    | ASP.NET Core | 8.0     |
| Database         | PostgreSQL   | 16      |
| ORM              | Dapper       | Latest  |
| Containerization | Docker       | Latest  |

## Useful Commands

```bash
# Build everything
dotnet build

# Run tests (when available)
dotnet test

# Format code
dotnet format

# Show all Docker containers
docker-compose ps

# Read API logs in real-time
docker-compose logs -f api --tail=50

# Connect via psql
psql postgresql://pulsedata_user:pulsedata_pass@localhost:5432/pulsedata

# Backup database
make backup-db

# View database size
make db-exec QUERY="SELECT pg_size_pretty(pg_database_size('pulsedata'));"
```

---

**For detailed information on any topic, refer to the full documentation files listed above.**

📤 Loading 1240 orders into database...
✓ Loaded 1240 orders
✅ Pipeline complete | Loaded: 1240 | Failed: 10 | Duration: 2847ms

````

## 🔒 Security Features

### Exception Handling (Now Working!)

All unhandled exceptions are caught and returned as JSON:

```json
{
  "error": "An unexpected error occurred.",
  "detail": "Detailed message (dev only)",
  "traceId": "0HN4QKDNC5ECT:00000001"
}
````

### CORS Configuration

**Development:** Allows localhost on ports 3000, 5173, 5174 (typical frontend ports)
**Production:** Configure allowed origins in `appsettings.Production.json`:

```json
{
  "Cors": {
    "AllowedOrigins": ["https://app.example.com", "https://www.example.com"],
    "AllowedMethods": ["GET", "POST"],
    "AllowCredentials": true
  }
}
```

### JWT Authentication (Optional)

To enable JWT authentication, configure appsettings:

```json
{
  "Jwt": {
    "Issuer": "https://your-auth-server.com",
    "Audience": "pulsedata-api",
    "SecretKey": "your-secret-key",
    "ExpirationMinutes": 60
  }
}
```

Then add `[Authorize]` to controllers:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // Requires valid JWT token
public class AnalyticsController : ControllerBase
{
    [HttpGet("sales-summary")]
    public async Task<IActionResult> GetMonthlySummary()
    {
        // Only accessible with valid JWT
    }
}
```

## 📋 Configuration Files

### Development (`appsettings.Development.json`)

- CORS: allows localhost
- Logging: Debug level (verbose)
- JWT: configured but not enforced
- Request logging: enabled

### Production (`appsettings.Production.json`)

- CORS: restrict to configured origins only
- Logging: Information level only
- JWT: should use secrets manager
- Request logging: disabled
- Connection strings: use environment variables

## 📊 ETL Improvements

### Performance

- **Before:** 30+ seconds for 1000 records
- **After:** 2-3 seconds for 1000 records
- **Speedup:** ~10-15x faster ✨

### Error Handling

Errors now include row numbers and context:

```
[Row 15] Quantity must be > 0 (got -5)
[Row 42] Invalid status 'pending' (typo with trailing space)
```

### Status Validation

The ETL now uses the `OrderStatus` enum instead of hardcoded strings:

```csharp
// Automatically validates against: pending, confirmed, shipped, delivered, cancelled, refunded
if (!OrderStatusExtensions.TryParse(statusString, out var status))
{
    // Invalid status
}
```

## 🔧 Customization

### Change CORS for Development

Edit `appsettings.Development.json`:

```json
{
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:8080"]
  }
}
```

### Adjust Logging Level

Edit `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information", // Change to Debug, Trace, etc.
      "Microsoft": "Warning",
      "System": "Warning"
    }
  }
}
```

### Enable Request Logging on Production

Add to `appsettings.Production.json`:

```json
{
  "Logging": {
    "LogRequestBody": true,
    "LogResponseBody": false
  }
}
```

## 🐛 Debugging

### API Exceptions

Unhandled exceptions are now properly formatted:

```bash
# Before: HTML response, stack trace visible
# After: JSON response, user-friendly message
curl https://localhost:7000/api/analytics/invalid-endpoint
# Response:
# {
#   "error": "An unexpected error occurred.",
#   "detail": "Resource not found",
#   "traceId": "0HN4QKDNC5ECT:00000001"
# }
```

### ETL Debugging

Run with debug logging:

```bash
# In code: _logger.LogDebug("message {variable}", value)
# Shows detailed information about lookups, batch sizes, etc.

dotnet build -c Debug
dotnet run
```

### Request Logging

Check the console output during development:

```
→ GET /api/analytics/sales-summary | Query: ?startDate=2024-01-01
← 200 | Duration: 245ms
```

## 📦 Dependencies

The upgrade uses only built-in .NET dependencies:

- `Microsoft.Extensions.Logging` (built-in)
- `Microsoft.Extensions.Configuration` (built-in)
- `Microsoft.Extensions.DependencyInjection` (built-in)

No new NuGet packages needed! Ready for Serilog when you need it.

## 🚀 Next Steps

1. **Test the improvements locally** — Run the API and ETL
2. **Review UPGRADE_GUIDE.md** — Comprehensive documentation of all changes
3. **Enable JWT** — Configure in production
4. **Add tests** — See recommended tests in UPGRADE_GUIDE.md
5. **Deploy to production** — Use the deployment checklist in UPGRADE_GUIDE.md

## 🆘 Troubleshooting

### "Address already in use" error

The API expects ports 7000 (HTTPS) or 5000 (HTTP). Kill existing processes:

```bash
# Linux/Mac
lsof -ti:7000 | xargs kill -9

# Windows
netstat -ano | findstr :7000
taskkill /PID <PID> /F
```

### "Connection string not found" error

Make sure `appsettings.json` has a valid PostgreSQL connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PulseData;User Id=postgres;Password=..."
  }
}
```

### JWT Authorization not working

Make sure you're sending the token in the Authorization header:

```bash
curl -H "Authorization: Bearer <your-jwt-token>" https://localhost:7000/api/analytics/sales-summary
```

## 📚 Resources

- [UPGRADE_GUIDE.md](./UPGRADE_GUIDE.md) — Complete upgrade documentation
- [docs/architecture.md](./docs/architecture.md) — System architecture
- [README.md](./README.md) — Project overview

---

**Questions?** Check UPGRADE_GUIDE.md or see the inline code comments for details.
