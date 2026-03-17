-- =============================================================
-- PulseData Stored Procedures & Functions
-- 004_stored_procedures.sql
-- =============================================================

-- -------------------------------------------------------------
-- get_sales_by_period(start_date, end_date)
-- Returns daily revenue aggregates for a given date range.
-- Compatible syntax with minor changes also works in Oracle.
-- -------------------------------------------------------------
CREATE OR REPLACE FUNCTION get_sales_by_period(
    p_start_date TIMESTAMPTZ,
    p_end_date   TIMESTAMPTZ
)
RETURNS TABLE (
    sale_date       DATE,
    order_count     BIGINT,
    revenue         NUMERIC,
    avg_order_value NUMERIC
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        placed_at::DATE                     AS sale_date,
        COUNT(*)                            AS order_count,
        ROUND(SUM(total_amount), 2)         AS revenue,
        ROUND(AVG(total_amount), 2)         AS avg_order_value
    FROM orders
    WHERE placed_at BETWEEN p_start_date AND p_end_date
      AND status NOT IN ('cancelled', 'refunded')
    GROUP BY placed_at::DATE
    ORDER BY placed_at::DATE;
END;
$$;

COMMENT ON FUNCTION get_sales_by_period IS
    'Daily sales aggregates for a date range. Excludes cancelled and refunded orders.';

-- -------------------------------------------------------------
-- get_top_customers(p_limit, p_country)
-- Returns top customers by total spend.
-- Pass NULL for p_country to get global results.
-- -------------------------------------------------------------
CREATE OR REPLACE FUNCTION get_top_customers(
    p_limit   INT DEFAULT 10,
    p_country VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    customer_id   INT,
    full_name     TEXT,
    email         VARCHAR,
    country       VARCHAR,
    total_orders  BIGINT,
    total_spent   NUMERIC
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        c.id,
        c.first_name || ' ' || c.last_name,
        c.email,
        c.country,
        COUNT(o.id),
        ROUND(SUM(o.total_amount), 2)
    FROM customers c
    JOIN orders o ON o.customer_id = c.id
               AND o.status NOT IN ('cancelled', 'refunded')
    WHERE (p_country IS NULL OR c.country = p_country)
    GROUP BY c.id, c.first_name, c.last_name, c.email, c.country
    ORDER BY SUM(o.total_amount) DESC
    LIMIT p_limit;
END;
$$;

-- -------------------------------------------------------------
-- place_order(customer_id, items JSON)
-- Wraps order creation in a transaction.
-- Returns the new order ID.
-- -------------------------------------------------------------
CREATE OR REPLACE FUNCTION place_order(
    p_customer_id INT,
    p_items       JSONB   -- [{"product_id": 1, "quantity": 2}, ...]
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_order_id    INT;
    v_total       NUMERIC := 0;
    v_item        JSONB;
    v_product_id  INT;
    v_quantity    INT;
    v_unit_price  NUMERIC;
    v_stock       INT;
BEGIN
    -- Validate all items exist and have enough stock before touching anything
    FOR v_item IN SELECT * FROM jsonb_array_elements(p_items)
    LOOP
        v_product_id := (v_item->>'product_id')::INT;
        v_quantity   := (v_item->>'quantity')::INT;

        SELECT unit_price, stock
        INTO v_unit_price, v_stock
        FROM products
        WHERE id = v_product_id AND is_active = TRUE;

        IF NOT FOUND THEN
            RAISE EXCEPTION 'Product % not found or inactive', v_product_id;
        END IF;

        IF v_stock < v_quantity THEN
            RAISE EXCEPTION 'Insufficient stock for product % (requested: %, available: %)',
                v_product_id, v_quantity, v_stock;
        END IF;

        v_total := v_total + (v_unit_price * v_quantity);
    END LOOP;

    -- Create the order
    INSERT INTO orders (customer_id, status, total_amount)
    VALUES (p_customer_id, 'pending', v_total)
    RETURNING id INTO v_order_id;

    -- Insert line items and decrement stock
    FOR v_item IN SELECT * FROM jsonb_array_elements(p_items)
    LOOP
        v_product_id := (v_item->>'product_id')::INT;
        v_quantity   := (v_item->>'quantity')::INT;

        SELECT unit_price INTO v_unit_price
        FROM products WHERE id = v_product_id;

        INSERT INTO order_items (order_id, product_id, quantity, unit_price)
        VALUES (v_order_id, v_product_id, v_quantity, v_unit_price);

        UPDATE products
        SET stock = stock - v_quantity
        WHERE id = v_product_id;
    END LOOP;

    RETURN v_order_id;
END;
$$;

COMMENT ON FUNCTION place_order IS
    'Creates an order with line items in a single transaction. Validates stock before committing.';
