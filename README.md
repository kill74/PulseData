# PulseData — E-Commerce Analytics Platform

A full-stack data engineering project built with **C# / .NET 8**, **PostgreSQL**, and a clean ETL pipeline. Designed to simulate a real-world business intelligence system for an e-commerce operation.

**Status**: Production-ready with structured logging, health checks, error handling, and Docker containerization.

---

## What It Does

PulseData ingests raw sales data, stores it in a normalized relational database, and exposes a REST API with analytics endpoints — things like top-selling products, customer lifetime value, and monthly revenue trends.

The idea is to mirror what a junior data/backend engineer would actually work on at a company.

### Key Features

- **Real-time Analytics** — Revenue summaries, top products, customer metrics
- **ETL Pipeline** — Automated data extraction, transformation, and loading from CSV
- **Health Checks** — Built-in service health monitoring and status endpoints
- **Structured Logging** — Serilog integration for comprehensive application insights
- **Error Handling** — Global exception middleware with proper error responses
- **Containerization** — Docker & Docker Compose for multi-environment deployments
- **Clean Architecture** — Separation of concerns with Core, Infrastructure, and API layers
- **Optimized Queries** — Dapper with raw SQL for performance-critical operations

```
Raw CSV / API data
      │
      ▼
  ETL Pipeline  ──────────────────▶  PostgreSQL Database
  (PulseData.ETL)                    (normalized schema)
                                            │
                                            ▼
                                     REST API Layer
                                    (PulseData.API)
                                            │
                                            ▼
                              Analytics Endpoints / Dashboard
```

---

## Tech Stack

| Layer            | Technology                                       |
| ---------------- | ------------------------------------------------ |
| Language         | C# (.NET 8)                                      |
| API Framework    | ASP.NET Core Web API with CORS support           |
| Database         | PostgreSQL 16                                    |
| ORM / Query      | Dapper (raw SQL, intentional)                    |
| Logging          | Serilog with structured logging                  |
| Containerization | Docker + Docker Compose                          |
| Error Handling   | Global exception middleware                      |
| Health Checks    | Custom HealthCheckService                        |
| SQL Features     | Window functions, CTEs, Views, Stored Procedures |

---

### Architecture Layers

```
Raw CSV / API data
      │
      ▼
  ETL Pipeline  ──────────────────▶  PostgreSQL Database
  (PulseData.ETL)                    (normalized schema)
  - Extract                          with views and
  - Transform                        stored procedures
  - Load
  - Batch optimization                      │
  - Error handling                          ▼
                                     REST API Layer
                                    (PulseData.API)
                                     - Controllers
                                     - Middleware
                                     - Health checks
                                            │
                                            ▼
                              Analytics Endpoints
                            (Customers, Orders, Products)
```

## Project Structure

```
PulseData/
├── src/
│   ├── PulseData.Core/                    # Domain layer
│   │   ├── DTOs/                          # Data Transfer Objects
│   │   ├── Interfaces/                    # Repository contracts
│   │   └── Models/                        # Domain entities
│   │
│   ├── PulseData.Infrastructure/          # Data access layer
│   │   ├── Data/                          # DB connection factory
│   │   └── Repositories/                  # Repository implementations
│   │
│   ├── PulseData.API/                     # Presentation layer
│   │   ├── Controllers/                   # REST endpoints
│   │   ├── Middleware/                    # Exception handling, logging
│   │   ├── Models/                        # API response models
│   │   ├── Services/                      # Health checks, utilities
│   │   └── Program.cs                     # Startup & DI configuration
│   │
│   └── PulseData.ETL/                     # ETL pipeline
│       ├── Pipeline/                      # ETL orchestration
│       ├── Models/                        # ETL data models
│       └── Program.cs                     # Entry point
│
├── sql/                                   # Database schema
│   ├── 001_create_schema.sql              # Tables, indexes
│   ├── 002_seed_data.sql                  # Sample data
│   ├── 003_views.sql                      # Reporting views
│   └── 004_stored_procedures.sql          # Business logic
│
├── docker-compose.yml                     # Service orchestration
├── Dockerfile                             # API container image
├── Makefile                               # Development commands
└── docs/
    └── architecture.md                    # System design details
```

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- `make` command (optional, for convenience commands)

### Quick Setup with Docker (Recommended)

#### Option 1: Database Only (Develop Locally)

```bash
# Clone the repository
git clone https://github.com/yourusername/PulseData.git
cd PulseData

# Start PostgreSQL and pgAdmin via Docker
docker-compose up -d

# Run the ETL pipeline locally
cd src/PulseData.ETL
dotnet run

# Run the API locally
cd ../PulseData.API
dotnet run
```

**Services:**

- PostgreSQL: `localhost:5432`
- pgAdmin: `http://localhost:8080`
- API: `http://localhost:5001`

#### Option 2: Full Docker Stack

```bash
# Start all services including containerized API
docker-compose --profile with-api up -d
```

**Services:**

- PostgreSQL: `localhost:5432`
- pgAdmin: `http://localhost:8080`
- API: `http://localhost:8000`

### Using Make Commands (Optional)

```bash
# Show all available commands
make help

# Start services
make docker-up        # Database + pgAdmin
make docker-up-full   # Full stack with API

# View logs
make docker-logs      # All services
make db-logs          # Database only
make api-logs         # API only

# Database access
make db-connect       # psql shell
make db-exec QUERY="SELECT * FROM customers LIMIT 5"

# Cleanup
make docker-clean     # Remove all containers and volumes
```

### Manual Setup (If Not Using Docker)

```bash
# 1. Set up PostgreSQL separately and create connection string in .env

# 2. Run SQL initialization scripts in order
psql -h localhost -U pulsedata_user -d pulsedata -f sql/001_create_schema.sql
psql -h localhost -U pulsedata_user -d pulsedata -f sql/002_seed_data.sql
psql -h localhost -U pulsedata_user -d pulsedata -f sql/003_views.sql
psql -h localhost -U pulsedata_user -d pulsedata -f sql/004_stored_procedures.sql

# 3. Run ETL pipeline
cd src/PulseData.ETL
dotnet run

# 4. Start API
cd ../PulseData.API
dotnet run
```

### Configuration

1. Copy environment template:

   ```bash
   cp .env.example .env
   ```

2. Edit `.env` with your settings (optional for default Docker setup)

3. For production deployments, see [DOCKER.md](DOCKER.md) for security considerations

---

## Documentation

- **[QUICK_START.md](QUICK_START.md)** — Quick reference with commands and troubleshooting
- **[DOCKER.md](DOCKER.md)** — Complete Docker & Docker Compose setup guide, production deployment
- **[DEVELOPMENT.md](DEVELOPMENT.md)** — Development workflow, IDE setup, debugging, best practices
- **[docs/architecture.md](docs/architecture.md)** — System design and data flow diagrams

## API Endpoints

### Health & Status

| Method | Endpoint            | Description                                  |
| ------ | ------------------- | -------------------------------------------- |
| GET    | `/api/health/live`  | Liveness check (is API running?)             |
| GET    | `/api/health/ready` | Readiness check (is API ready for requests?) |

### Analytics

| Method | Endpoint                               | Description                             |
| ------ | -------------------------------------- | --------------------------------------- |
| GET    | `/api/analytics/sales-summary`         | Revenue summary by period               |
| GET    | `/api/analytics/top-products?limit=10` | Best-selling products                   |
| GET    | `/api/analytics/customer-stats`        | Customer metrics (LTV, churn, averages) |
| GET    | `/api/analytics/monthly-trends`        | Month-over-month growth trends          |

### Orders

| Method | Endpoint                               | Description                         |
| ------ | -------------------------------------- | ----------------------------------- |
| GET    | `/api/orders`                          | Paginated order list with filtering |
| GET    | `/api/orders/{id}`                     | Order detail with line items        |
| GET    | `/api/orders/by-customer/{customerId}` | Customer order history              |

### Products & Customers

| Method | Endpoint                                 | Description                   |
| ------ | ---------------------------------------- | ----------------------------- |
| GET    | `/api/products`                          | Product catalog               |
| GET    | `/api/products/{id}`                     | Single product details        |
| GET    | `/api/products/by-category/{categoryId}` | Products filtered by category |
| GET    | `/api/customers`                         | Customer list                 |
| GET    | `/api/customers/{id}`                    | Customer profile and metrics  |

### Error Handling

All endpoints return consistent error responses. Example 404 response:

```json
{
  "error": "Not Found",
  "message": "Resource with ID 999 not found",
  "timestamp": "2024-03-18T10:30:45Z",
  "path": "/api/orders/999"
}
```

## ETL Pipeline

The `PulseData.ETL` project provides automated data ingestion and transformation:

### Features

- **Batch Processing** — Efficiently processes large CSV files with optimized queries
- **Error Handling** — Detailed error logging and partial success handling
- **Validation** — Data validation before database insertion
- **Logging** — Structured logging output with transaction summaries
- **Flexible Input** — Accepts CSV file path as argument or uses default sample data

### Usage

```bash
# Use default sample data
cd src/PulseData.ETL
dotnet run

# Load custom CSV file
dotnet run -- path/to/orders.csv
```

### Output Example

```
PulseData ETL Pipeline
CSV Path: sample_orders.csv
Starting pipeline...

Summary:
  Records read   : 1250
  Records loaded : 1248
  Records failed : 2
  Duration       : 3.45s

Errors (first 10):
  Row 567: Invalid customer ID format
  Row 892: Duplicate order ID
```

---

## Monitoring & Logging

### Health Checks

The API includes health check endpoints for Kubernetes/orchestration integration:

```bash
# Check if API is alive
curl http://localhost:5001/api/health/live

# Check if API is ready to serve requests
curl http://localhost:5001/api/health/ready
```

### Structured Logging

Serilog integration provides rich contextual logging:

```bash
# View API logs with timestamps and levels
docker-compose logs -f api

# View ETL pipeline logs with transaction details
docker-compose logs -f etl
```

---

## Key SQL Features

The `sql/` folder demonstrates real-world SQL patterns:

- **Window functions** — ranking, running totals, lag/lead for trend analysis
- **CTEs** — readable multi-step analytical queries
- **Stored procedures** — encapsulated business logic with parameters
- **Views** — pre-built reporting layers (`monthly_revenue_summary`, `customer_lifetime_value`)
- **Indexes** — covering indexes for common query patterns
- **Constraints** — proper foreign keys, check constraints, not-null rules

---

## Architecture & Best Practices

### Clean Architecture Layers

PulseData follows clean architecture principles with clear separation of concerns:

- **PulseData.Core** — Domain models, DTOs, and repository interfaces (no external dependencies)
- **PulseData.Infrastructure** — Data access, repository implementations, connection management
- **PulseData.API** — Controllers, middleware, HTTP-specific concerns
- **PulseData.ETL** — Data pipeline, extraction, transformation, loading

### Key Improvements

- **Global Exception Middleware** — Consistent error handling across all endpoints
- **Structured Logging** — Serilog for rich contextual logs
- **CORS & Security** — Configured cross-origin policies
- **Health Checks** — Liveness and readiness endpoints for orchestration
- **Batch Optimization** — Optimized ETL with efficient database insertion
- **Dependency Injection** — Proper IoC using Microsoft.Extensions
- **Status Enums** — Type-safe order status handling
- **Connection Pooling** — Optimized database connections

---

This project uses [Dapper](https://github.com/DapperLib/Dapper) for database access. When working in data-heavy roles, writing and optimizing raw SQL is a core skill. Dapper keeps that visible while still handling the boring parts (mapping, parameterization, connection management).

---

## Environment Variables

Copy `.env.example` to `.env` and fill in your values:

```
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DB=pulsedata
POSTGRES_USER=pulsedata_user
POSTGRES_PASSWORD=your_password
```

---

## Running in Different Environments

### Local Development

```bash
# Terminal 1: Start database and pgAdmin
make docker-up

# Terminal 2: Run API with hot reload
cd src/PulseData.API
dotnet watch run

# Terminal 3: Run ETL pipeline
cd src/PulseData.ETL
dotnet run
```

### Docker Containers

```bash
# Start only database (API runs locally)
docker-compose up -d

# OR start full stack (database + API in containers)
docker-compose --profile with-api up -d

# View logs
docker-compose logs -f
```

### Production Deployment

- Set `ASPNETCORE_ENVIRONMENT=Production`
- Use strong passwords in `.env`
- Enable HTTPS/TLS
- Configure database backups
- Set up monitoring and alerting
- See [DOCKER.md](DOCKER.md) for complete production guide

---

## Quick Reference

| Action              | Command                                 |
| ------------------- | --------------------------------------- |
| Start services      | `make docker-up`                        |
| View all logs       | `make docker-logs`                      |
| Connect to database | `make db-connect`                       |
| Run SQL query       | `make db-exec QUERY="SELECT * FROM..."` |
| Clean everything    | `make docker-clean`                     |
| Show all commands   | `make help`                             |

---

## License

MIT
