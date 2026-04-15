# PulseData | E-Commerce Analytics Platform

PulseData is a production-style backend analytics project built with .NET 8, PostgreSQL, and a practical ETL pipeline.

This repository was designed to feel like real engineering work: ingest data, model it well, expose useful analytics, and keep operations healthy with tests and CI.

## What You Get

- Clean architecture split: Core, Infrastructure, API, ETL
- SQL-first analytics with Dapper (performance and clarity)
- Liveness/readiness health endpoints for orchestration
- Docker-based local stack
- Automated unit and integration tests for health behavior
- CI pipeline on push and pull request to main

## 5-Minute Start

```bash
git clone https://github.com/kill74/PulseData.git
cd PulseData

# Start PostgreSQL + pgAdmin
docker-compose up -d

# Run ETL (loads sample orders)
cd src/PulseData.ETL
dotnet run

# Run API
cd ../PulseData.API
dotnet run
```

Expected services:

- PostgreSQL: localhost:5432
- pgAdmin: [http://localhost:8080](http://localhost:8080)
- API (local): [https://localhost:5001](https://localhost:5001) or [http://localhost:5000](http://localhost:5000)

## Full Docker Mode

```bash
docker-compose --profile with-api up -d
```

Expected services:

- PostgreSQL: localhost:5432
- pgAdmin: [http://localhost:8080](http://localhost:8080)
- API (container): [http://localhost:8000](http://localhost:8000)

## Verify It Works

```bash
# Liveness
curl http://localhost:5000/api/health/live

# Readiness
curl http://localhost:5000/api/health/ready
```

Readiness semantics:

- 200 when dependencies are healthy
- 408 when a dependency check times out
- 503 when a required dependency is unavailable

Note: health endpoints are anonymous by design to support probes from load balancers and orchestrators.

## API Surface

### Health

| Method | Endpoint          | Purpose              |
| ------ | ----------------- | -------------------- |
| GET    | /api/health/live  | Process liveness     |
| GET    | /api/health/ready | Dependency readiness |

### Analytics

| Method | Endpoint                                                               |
| ------ | ---------------------------------------------------------------------- |
| GET    | /api/analytics/sales-summary                                           |
| GET    | /api/analytics/top-products?limit=10                                   |
| GET    | /api/analytics/customer-stats?limit=20                                 |
| GET    | /api/analytics/category-performance                                    |
| GET    | /api/analytics/sales-by-period?startDate=2026-01-01&endDate=2026-01-31 |

### Orders

| Method | Endpoint                                      |
| ------ | --------------------------------------------- |
| GET    | /api/orders?page=1&pageSize=20&status=shipped |
| GET    | /api/orders/{id}                              |
| GET    | /api/orders/by-customer/{customerId}          |

### Customers

| Method | Endpoint                          |
| ------ | --------------------------------- |
| GET    | /api/customers?page=1&pageSize=20 |
| GET    | /api/customers/{id}               |

### Products

| Method | Endpoint                                         |
| ------ | ------------------------------------------------ |
| GET    | /api/products?page=1&pageSize=20&activeOnly=true |
| GET    | /api/products/{id}                               |
| GET    | /api/products/by-category/{categoryId}           |

## Testing

Run the API test suite:

```bash
dotnet test ./tests/PulseData.API.Tests/PulseData.API.Tests.csproj
```

Current coverage includes:

- Unit behavior tests for HealthController
- Integration HTTP tests using WebApplicationFactory:
  - /api/health/live
  - /api/health/ready timeout path (408)
  - /api/health/ready dependency failure path (503)

## CI

Workflow: .github/workflows/ci.yml

CI validates on push and pull request to main:

- API restore + build
- Test project restore + build
- Health endpoint test execution

## Repository Layout

```text
PulseData/
  src/
    PulseData.Core/
    PulseData.Infrastructure/
    PulseData.API/
    PulseData.ETL/
  sql/
  tests/
    PulseData.API.Tests/
  .github/workflows/
  docker-compose.yml
  Makefile
```

## Useful Commands

```bash
# Reliable API build
dotnet build ./src/PulseData.API/PulseData.API.csproj

# Run health-related tests
dotnet test ./tests/PulseData.API.Tests/PulseData.API.Tests.csproj

# Start infra only
docker-compose up -d

# Start full stack
docker-compose --profile with-api up -d

# API logs (container mode)
docker-compose logs -f api
```

## Documentation

- [QUICK_START.md](QUICK_START.md)
- [DEVELOPMENT.md](DEVELOPMENT.md)
- [DOCKER.md](DOCKER.md)
- [docs/architecture.md](docs/architecture.md)

## License

MIT
