-- =============================================================
-- PulseData Schema
-- 001_create_schema.sql
--
-- Creates all tables, indexes, and constraints for the
-- e-commerce analytics platform.
-- =============================================================

-- Clean up if re-running during development
DROP TABLE IF EXISTS order_items CASCADE;
DROP TABLE IF EXISTS orders CASCADE;
DROP TABLE IF EXISTS products CASCADE;
DROP TABLE IF EXISTS categories CASCADE;
DROP TABLE IF EXISTS customers CASCADE;
DROP TABLE IF EXISTS etl_run_log CASCADE;

-- -------------------------------------------------------------
-- customers
-- -------------------------------------------------------------
CREATE TABLE customers (
    id            SERIAL PRIMARY KEY,
    email         VARCHAR(255) NOT NULL UNIQUE,
    first_name    VARCHAR(100) NOT NULL,
    last_name     VARCHAR(100) NOT NULL,
    country       VARCHAR(100),
    city          VARCHAR(100),
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_active     BOOLEAN NOT NULL DEFAULT TRUE
);

-- Most queries filter by country or look up by email
CREATE INDEX idx_customers_country  ON customers(country);
CREATE INDEX idx_customers_email    ON customers(email);
CREATE INDEX idx_customers_created  ON customers(created_at);

COMMENT ON TABLE customers IS 'Registered customer accounts.';

-- -------------------------------------------------------------
-- categories
-- -------------------------------------------------------------
CREATE TABLE categories (
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- -------------------------------------------------------------
-- products
-- -------------------------------------------------------------
CREATE TABLE products (
    id           SERIAL PRIMARY KEY,
    category_id  INT NOT NULL REFERENCES categories(id),
    sku          VARCHAR(50) NOT NULL UNIQUE,
    name         VARCHAR(255) NOT NULL,
    description  TEXT,
    unit_price   NUMERIC(10, 2) NOT NULL CHECK (unit_price >= 0),
    stock        INT NOT NULL DEFAULT 0 CHECK (stock >= 0),
    is_active    BOOLEAN NOT NULL DEFAULT TRUE,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_products_category ON products(category_id);
CREATE INDEX idx_products_sku      ON products(sku);
CREATE INDEX idx_products_active   ON products(is_active);

COMMENT ON TABLE products IS 'Product catalog with pricing and stock levels.';

-- -------------------------------------------------------------
-- orders
-- -------------------------------------------------------------
CREATE TABLE orders (
    id              SERIAL PRIMARY KEY,
    customer_id     INT NOT NULL REFERENCES customers(id),
    status          VARCHAR(50) NOT NULL DEFAULT 'pending'
                        CHECK (status IN ('pending', 'confirmed', 'shipped', 'delivered', 'cancelled', 'refunded')),
    total_amount    NUMERIC(12, 2) NOT NULL CHECK (total_amount >= 0),
    currency        CHAR(3) NOT NULL DEFAULT 'USD',
    placed_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    shipped_at      TIMESTAMPTZ,
    delivered_at    TIMESTAMPTZ
);

CREATE INDEX idx_orders_customer    ON orders(customer_id);
CREATE INDEX idx_orders_status      ON orders(status);
CREATE INDEX idx_orders_placed_at   ON orders(placed_at);

-- Covering index for the most common analytics query (revenue by date)
CREATE INDEX idx_orders_analytics ON orders(placed_at, status, total_amount);

COMMENT ON TABLE orders IS 'Customer orders. Status transitions: pending → confirmed → shipped → delivered.';

-- -------------------------------------------------------------
-- order_items
-- -------------------------------------------------------------
CREATE TABLE order_items (
    id          SERIAL PRIMARY KEY,
    order_id    INT NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id  INT NOT NULL REFERENCES products(id),
    quantity    INT NOT NULL CHECK (quantity > 0),
    unit_price  NUMERIC(10, 2) NOT NULL CHECK (unit_price >= 0),
    -- We store unit_price at purchase time so historical data stays correct
    -- even if the product price changes later
    subtotal    NUMERIC(12, 2) GENERATED ALWAYS AS (quantity * unit_price) STORED
);

CREATE INDEX idx_order_items_order   ON order_items(order_id);
CREATE INDEX idx_order_items_product ON order_items(product_id);

COMMENT ON TABLE order_items IS 'Line items for each order. unit_price is snapshotted at purchase time.';

-- -------------------------------------------------------------
-- etl_run_log
-- Used by the ETL pipeline to track runs and avoid reprocessing
-- -------------------------------------------------------------
CREATE TABLE etl_run_log (
    id            SERIAL PRIMARY KEY,
    run_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    source_file   VARCHAR(500),
    records_read  INT NOT NULL DEFAULT 0,
    records_loaded INT NOT NULL DEFAULT 0,
    records_failed INT NOT NULL DEFAULT 0,
    status        VARCHAR(20) NOT NULL DEFAULT 'started'
                      CHECK (status IN ('started', 'completed', 'failed')),
    error_message TEXT,
    duration_ms   INT
);

COMMENT ON TABLE etl_run_log IS 'Audit trail for every ETL pipeline execution.';
