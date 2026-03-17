# Architecture Notes

## Project Layers

### PulseData.Core
The innermost layer. Has zero external dependencies — just C# primitives and BCL types. Contains:
- **Models** — Plain entity classes (`Customer`, `Order`, `Product`, etc.)
- **DTOs** — What the API actually returns to callers. Kept separate from domain models so DB schema changes don't break the public contract.
- **Interfaces** — Repository contracts. The core doesn't know about PostgreSQL, Dapper, or anything else.

### PulseData.Infrastructure
Knows about the database. Implements the repository interfaces using Dapper and Npgsql. All SQL lives here — no query strings scattered around controllers.

Key choices:
- **Dapper over EF Core**: In data roles, reading and writing raw SQL is a daily task. Dapper keeps that visible.
- **DbConnectionFactory**: Returns a new open connection per call. Connections aren't thread-safe and shouldn't be reused across requests.
- **Raw SQL strings**: Using C# raw string literals (`"""..."""`) for multi-line SQL. Readable, no escaping needed.

### PulseData.API
ASP.NET Core Web API. Thin controllers — just validation, calling the repository, and returning the result. Business logic doesn't belong here.

- Swagger UI exposed at `/` in development
- GlobalExceptionMiddleware catches anything unhandled and returns clean JSON (no stack traces in prod)
- Pagination is handled via `PagedResult<T>` — includes total count and page metadata

### PulseData.ETL
Console application. Runs the Extract → Transform → Load pipeline for ingesting raw CSV data.

The ETL follows a **validate-all-before-loading** approach:
1. Read all rows from CSV
2. Validate every row (required fields, types, enum values)
3. Only then open a DB transaction and load
4. Log the run results to `etl_run_log`

This means a bad file fails loudly before touching the database.

---

## Database Design Decisions

### Snapshot pricing on order_items
`order_items.unit_price` stores the price at purchase time as a plain column. This means if a product's price changes later, historical orders stay correct. The `subtotal` is a `GENERATED ALWAYS AS` computed column — the DB handles it, not the application.

### Status enum as VARCHAR with CHECK constraint
Used `CHECK (status IN (...))` instead of a PostgreSQL `ENUM` type. ENUMs in PostgreSQL require an `ALTER TYPE` to add new values, which can be tricky in migrations. A CHECK constraint is easier to evolve.

### Indexes
Added only indexes that serve real query patterns:
- `idx_orders_analytics` — a covering index on `(placed_at, status, total_amount)` for the revenue views, which never need to hit the table
- `idx_customers_email` — unique lookups during ETL
- `idx_products_sku` — ETL product resolution

Avoided over-indexing. Each index has a write cost.

---

## SQL Patterns Used

| Pattern | Where |
|---|---|
| Window functions (`RANK`, `LAG`, `NTILE`) | Views |
| CTEs | `monthly_revenue_summary` view |
| Stored functions with OUT params | `get_sales_by_period`, `get_top_customers` |
| Transactional stored procedure | `place_order` |
| `GENERATED ALWAYS AS` computed columns | `order_items.subtotal` |
| `ON CONFLICT DO NOTHING` | ETL idempotent load |

---

## Oracle Compatibility Notes

The SQL in this project uses PostgreSQL syntax. Most of it is standard SQL-92/99 and would run on Oracle with minor changes:

| PostgreSQL | Oracle equivalent |
|---|---|
| `SERIAL` / `BIGSERIAL` | `GENERATED ALWAYS AS IDENTITY` |
| `TIMESTAMPTZ` | `TIMESTAMP WITH TIME ZONE` |
| `BOOLEAN` | `NUMBER(1)` or `CHAR(1)` |
| `LIMIT / OFFSET` | `FETCH FIRST n ROWS ONLY` / `OFFSET n ROWS` |
| `NOW()` | `SYSDATE` or `CURRENT_TIMESTAMP` |
| `$$` function body | `IS ... BEGIN ... END;` |
| `RETURNING id` | Not supported — use `RETURNING INTO` |

The views and analytical queries (CTEs, window functions, `RANK`, `LAG`, `NTILE`) are standard ANSI SQL and work on Oracle without changes.
