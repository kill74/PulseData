-- =============================================================
-- PulseData Reporting Views
-- 003_views.sql
--
-- Pre-built views for common analytics queries.
-- These form the reporting layer that the API and BI tools consume.
-- =============================================================

-- -------------------------------------------------------------
-- monthly_revenue_summary
-- Monthly revenue broken down by order count and average order value.
-- Includes MoM growth via LAG window function.
-- -------------------------------------------------------------
CREATE OR REPLACE VIEW monthly_revenue_summary AS
WITH monthly AS (
    SELECT
        DATE_TRUNC('month', placed_at)  AS month,
        COUNT(*)                         AS order_count,
        SUM(total_amount)                AS revenue,
        AVG(total_amount)                AS avg_order_value
    FROM orders
    WHERE status NOT IN ('cancelled', 'refunded')
    GROUP BY DATE_TRUNC('month', placed_at)
)
SELECT
    month,
    order_count,
    ROUND(revenue, 2)                                               AS revenue,
    ROUND(avg_order_value, 2)                                       AS avg_order_value,
    LAG(revenue) OVER (ORDER BY month)                              AS prev_month_revenue,
    ROUND(
        (revenue - LAG(revenue) OVER (ORDER BY month))
        / NULLIF(LAG(revenue) OVER (ORDER BY month), 0) * 100,
        2
    )                                                               AS mom_growth_pct
FROM monthly
ORDER BY month;

COMMENT ON VIEW monthly_revenue_summary IS
    'Monthly revenue with order count, AOV, and month-over-month growth percentage.';

-- -------------------------------------------------------------
-- product_sales_ranking
-- All-time sales performance per product with ranking.
-- -------------------------------------------------------------
CREATE OR REPLACE VIEW product_sales_ranking AS
SELECT
    p.id                                                            AS product_id,
    p.sku,
    p.name                                                          AS product_name,
    c.name                                                          AS category,
    COUNT(DISTINCT oi.order_id)                                     AS times_ordered,
    SUM(oi.quantity)                                                AS units_sold,
    ROUND(SUM(oi.subtotal), 2)                                      AS total_revenue,
    RANK() OVER (ORDER BY SUM(oi.subtotal) DESC)                    AS revenue_rank,
    RANK() OVER (PARTITION BY c.id ORDER BY SUM(oi.subtotal) DESC)  AS rank_in_category
FROM products p
JOIN categories c         ON c.id = p.category_id
LEFT JOIN order_items oi  ON oi.product_id = p.id
LEFT JOIN orders o        ON o.id = oi.order_id
                         AND o.status NOT IN ('cancelled', 'refunded')
GROUP BY p.id, p.sku, p.name, c.id, c.name
ORDER BY revenue_rank;

COMMENT ON VIEW product_sales_ranking IS
    'Product sales performance with overall and per-category revenue ranking.';

-- -------------------------------------------------------------
-- customer_lifetime_value
-- LTV per customer with purchase frequency and recency.
-- -------------------------------------------------------------
CREATE OR REPLACE VIEW customer_lifetime_value AS
SELECT
    cu.id                                                           AS customer_id,
    cu.email,
    cu.first_name || ' ' || cu.last_name                           AS full_name,
    cu.country,
    COUNT(o.id)                                                     AS total_orders,
    ROUND(SUM(o.total_amount), 2)                                   AS lifetime_value,
    ROUND(AVG(o.total_amount), 2)                                   AS avg_order_value,
    MIN(o.placed_at)                                                AS first_order_at,
    MAX(o.placed_at)                                                AS last_order_at,
    -- Days since last order — useful for churn analysis
    EXTRACT(DAY FROM NOW() - MAX(o.placed_at))::INT                 AS days_since_last_order,
    NTILE(4) OVER (ORDER BY SUM(o.total_amount) DESC)               AS ltv_quartile
    -- Quartile 1 = top 25% customers by LTV
FROM customers cu
LEFT JOIN orders o ON o.customer_id = cu.id
               AND o.status NOT IN ('cancelled', 'refunded')
GROUP BY cu.id, cu.email, cu.first_name, cu.last_name, cu.country
ORDER BY lifetime_value DESC NULLS LAST;

COMMENT ON VIEW customer_lifetime_value IS
    'Customer LTV with recency, frequency, and LTV quartile segmentation.';

-- -------------------------------------------------------------
-- category_performance
-- Revenue and unit sales by category.
-- -------------------------------------------------------------
CREATE OR REPLACE VIEW category_performance AS
SELECT
    c.id                                AS category_id,
    c.name                              AS category,
    COUNT(DISTINCT o.id)                AS order_count,
    SUM(oi.quantity)                    AS units_sold,
    ROUND(SUM(oi.subtotal), 2)          AS total_revenue,
    ROUND(AVG(oi.unit_price), 2)        AS avg_unit_price,
    ROUND(
        SUM(oi.subtotal) / NULLIF(SUM(SUM(oi.subtotal)) OVER (), 0) * 100,
        2
    )                                   AS revenue_share_pct
FROM categories c
LEFT JOIN products p   ON p.category_id = c.id
LEFT JOIN order_items oi ON oi.product_id = p.id
LEFT JOIN orders o     ON o.id = oi.order_id
                      AND o.status NOT IN ('cancelled', 'refunded')
GROUP BY c.id, c.name
ORDER BY total_revenue DESC NULLS LAST;

COMMENT ON VIEW category_performance IS
    'Revenue contribution and unit sales broken down by product category.';
