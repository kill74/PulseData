# PulseData — E-Commerce Analytics Platform

A full-stack data engineering project built with **C# / .NET 8**, **PostgreSQL**, and a clean ETL pipeline. Designed to simulate a real-world business intelligence system for an e-commerce operation.

---

## What it does

PulseData ingests raw sales data, stores it in a normalized relational database, and exposes a REST API with analytics endpoints — things like top-selling products, customer lifetime value, and monthly revenue trends.

The idea is to mirror what a junior data/backend engineer would actually work on at a company.

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

| Layer | Technology |
|---|---|
| Language | C# (.NET 8) |
| Database | PostgreSQL 16 |
| ORM / Query | Dapper (raw SQL, intentional) |
| API | ASP.NET Core Web API |
| Containerization | Docker + Docker Compose |
| SQL Compatibility | PostgreSQL syntax + Oracle-compatible queries noted |

---

## Project Structure

```
PulseData/
├── src/
│   ├── PulseData.Core/           # Domain models, interfaces, DTOs
│   ├── PulseData.Infrastructure/ # Repositories, DB connection factory
│   ├── PulseData.API/            # REST API controllers
│   └── PulseData.ETL/            # Extract → Transform → Load pipeline
├── sql/
│   ├── 001_create_schema.sql     # Tables, indexes, constraints
│   ├── 002_seed_data.sql         # Sample data for development
│   ├── 003_views.sql             # Reporting views
│   └── 004_stored_procedures.sql # Business logic in SQL
└── docs/
    └── architecture.md
```

---

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### 1. Clone and spin up the database

```bash
git clone https://github.com/yourusername/PulseData.git
cd PulseData

# Start PostgreSQL via Docker
docker-compose up -d
```

### 2. Run the SQL setup

```bash
# Connect to the database and run scripts in order
psql -h localhost -U pulsedata_user -d pulsedata -f sql/001_create_schema.sql
psql -h localhost -U pulsedata_user -d pulsedata -f sql/002_seed_data.sql
psql -h localhost -U pulsedata_user -d pulsedata -f sql/003_views.sql
psql -h localhost -U pulsedata_user -d pulsedata -f sql/004_stored_procedures.sql
```

### 3. Run the ETL pipeline

```bash
cd src/PulseData.ETL
dotnet run
```

### 4. Start the API

```bash
cd src/PulseData.API
dotnet run
```

API will be available at `https://localhost:5001`

---

## API Endpoints

### Analytics
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/analytics/sales-summary` | Revenue by period |
| GET | `/api/analytics/top-products?limit=10` | Best-selling products |
| GET | `/api/analytics/customer-stats` | LTV, churn, averages |
| GET | `/api/analytics/monthly-trends` | Month-over-month growth |

### Orders
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/orders` | Paginated order list |
| GET | `/api/orders/{id}` | Order detail with items |
| GET | `/api/orders/by-customer/{customerId}` | Customer order history |

### Products
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products` | Product catalog |
| GET | `/api/products/{id}` | Single product |
| GET | `/api/products/by-category/{categoryId}` | By category |

### Customers
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/customers` | Customer list |
| GET | `/api/customers/{id}` | Customer profile |

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

## Why Dapper over EF Core?

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

## License

MIT
