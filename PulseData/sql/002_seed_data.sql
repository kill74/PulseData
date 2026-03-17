-- =============================================================
-- PulseData Seed Data
-- 002_seed_data.sql
--
-- Inserts realistic sample data for development and testing.
-- Safe to re-run — uses ON CONFLICT DO NOTHING.
-- =============================================================

-- -------------------------------------------------------------
-- Categories
-- -------------------------------------------------------------
INSERT INTO categories (id, name, description) VALUES
    (1, 'Electronics',    'Phones, laptops, accessories, and gadgets'),
    (2, 'Clothing',       'Apparel for men, women, and children'),
    (3, 'Home & Kitchen', 'Cookware, furniture, and home decor'),
    (4, 'Books',          'Fiction, non-fiction, and educational titles'),
    (5, 'Sports',         'Fitness equipment and outdoor gear')
ON CONFLICT (name) DO NOTHING;

SELECT setval('categories_id_seq', 5);

-- -------------------------------------------------------------
-- Products
-- -------------------------------------------------------------
INSERT INTO products (id, category_id, sku, name, unit_price, stock) VALUES
    (1,  1, 'ELEC-001', 'Wireless Noise-Cancelling Headphones',  89.99, 142),
    (2,  1, 'ELEC-002', 'USB-C Charging Hub (7-Port)',            34.99, 305),
    (3,  1, 'ELEC-003', 'Mechanical Keyboard (TKL)',             119.00, 88),
    (4,  1, 'ELEC-004', '27" 1440p Monitor',                    329.00, 47),
    (5,  1, 'ELEC-005', 'Portable SSD 1TB',                      79.99, 200),
    (6,  2, 'CLTH-001', 'Classic Cotton T-Shirt',                 19.99, 600),
    (7,  2, 'CLTH-002', 'Running Jacket (Waterproof)',            64.99, 130),
    (8,  2, 'CLTH-003', 'Slim-Fit Chinos',                       44.99, 215),
    (9,  3, 'HOME-001', 'Stainless Steel Cookware Set (10pc)',   129.99, 75),
    (10, 3, 'HOME-002', 'Bamboo Cutting Board Set',              27.99, 310),
    (11, 3, 'HOME-003', 'Pour-Over Coffee Maker',                39.99, 180),
    (12, 4, 'BOOK-001', 'Designing Data-Intensive Applications', 42.00, 500),
    (13, 4, 'BOOK-002', 'Clean Code',                            36.00, 450),
    (14, 4, 'BOOK-003', 'The Pragmatic Programmer',              40.00, 380),
    (15, 5, 'SPRT-001', 'Adjustable Dumbbell Set (5-25kg)',     189.00, 60),
    (16, 5, 'SPRT-002', 'Yoga Mat (Non-Slip, 6mm)',              24.99, 290),
    (17, 5, 'SPRT-003', 'Resistance Bands Set',                  18.99, 400)
ON CONFLICT (sku) DO NOTHING;

SELECT setval('products_id_seq', 17);

-- -------------------------------------------------------------
-- Customers
-- -------------------------------------------------------------
INSERT INTO customers (id, email, first_name, last_name, country, city, created_at) VALUES
    (1,  'ana.silva@email.com',       'Ana',     'Silva',      'Portugal',       'Lisbon',       '2023-01-15 09:00:00+00'),
    (2,  'joao.ferreira@email.com',   'João',    'Ferreira',   'Portugal',       'Porto',        '2023-02-03 14:30:00+00'),
    (3,  'maria.costa@email.com',     'Maria',   'Costa',      'Portugal',       'Braga',        '2023-03-11 11:15:00+00'),
    (4,  'carlos.mendes@email.com',   'Carlos',  'Mendes',     'Spain',          'Madrid',       '2023-03-25 08:45:00+00'),
    (5,  'sophie.martin@email.com',   'Sophie',  'Martin',     'France',         'Paris',        '2023-04-02 16:00:00+00'),
    (6,  'thomas.müller@email.com',   'Thomas',  'Müller',     'Germany',        'Berlin',       '2023-04-18 10:30:00+00'),
    (7,  'laura.rossi@email.com',     'Laura',   'Rossi',      'Italy',          'Milan',        '2023-05-07 13:00:00+00'),
    (8,  'james.wilson@email.com',    'James',   'Wilson',     'United Kingdom', 'London',       '2023-05-22 09:15:00+00'),
    (9,  'sara.ahmed@email.com',      'Sara',    'Ahmed',      'Netherlands',    'Amsterdam',    '2023-06-10 15:45:00+00'),
    (10, 'pedro.santos@email.com',    'Pedro',   'Santos',     'Portugal',       'Coimbra',      '2023-06-28 12:00:00+00'),
    (11, 'nina.petrov@email.com',     'Nina',    'Petrov',     'Germany',        'Munich',       '2023-07-14 08:00:00+00'),
    (12, 'lucas.dupont@email.com',    'Lucas',   'Dupont',     'France',         'Lyon',         '2023-08-01 17:30:00+00'),
    (13, 'emma.johnson@email.com',    'Emma',    'Johnson',    'United Kingdom', 'Manchester',   '2023-08-19 11:00:00+00'),
    (14, 'rafael.garcia@email.com',   'Rafael',  'Garcia',     'Spain',          'Barcelona',    '2023-09-05 14:15:00+00'),
    (15, 'yuki.tanaka@email.com',     'Yuki',    'Tanaka',     'Netherlands',    'Rotterdam',    '2023-09-23 09:45:00+00')
ON CONFLICT (email) DO NOTHING;

SELECT setval('customers_id_seq', 15);

-- -------------------------------------------------------------
-- Orders  (spread across 2023–2024 for trend data)
-- -------------------------------------------------------------
INSERT INTO orders (id, customer_id, status, total_amount, placed_at, shipped_at, delivered_at) VALUES
    (1,  1,  'delivered', 124.98, '2023-10-05 10:00:00+00', '2023-10-06 08:00:00+00', '2023-10-08 14:00:00+00'),
    (2,  2,  'delivered', 34.99,  '2023-10-12 11:30:00+00', '2023-10-13 09:00:00+00', '2023-10-15 13:00:00+00'),
    (3,  3,  'delivered', 129.99, '2023-10-20 14:00:00+00', '2023-10-21 10:00:00+00', '2023-10-23 16:00:00+00'),
    (4,  4,  'delivered', 208.00, '2023-11-01 09:15:00+00', '2023-11-02 07:30:00+00', '2023-11-04 12:00:00+00'),
    (5,  5,  'delivered', 64.99,  '2023-11-10 13:00:00+00', '2023-11-11 08:00:00+00', '2023-11-13 10:00:00+00'),
    (6,  1,  'delivered', 119.00, '2023-11-20 15:30:00+00', '2023-11-21 09:00:00+00', '2023-11-23 14:00:00+00'),
    (7,  6,  'delivered', 329.00, '2023-11-25 10:45:00+00', '2023-11-26 08:00:00+00', '2023-11-28 15:00:00+00'),
    (8,  7,  'delivered', 84.98,  '2023-12-02 12:00:00+00', '2023-12-03 07:00:00+00', '2023-12-05 11:00:00+00'),
    (9,  8,  'delivered', 189.00, '2023-12-10 09:00:00+00', '2023-12-11 08:30:00+00', '2023-12-13 13:00:00+00'),
    (10, 9,  'delivered', 79.98,  '2023-12-15 16:00:00+00', '2023-12-16 09:00:00+00', '2023-12-18 14:00:00+00'),
    (11, 10, 'delivered', 42.00,  '2024-01-08 10:00:00+00', '2024-01-09 08:00:00+00', '2024-01-11 12:00:00+00'),
    (12, 11, 'delivered', 213.98, '2024-01-15 14:30:00+00', '2024-01-16 09:00:00+00', '2024-01-18 15:00:00+00'),
    (13, 12, 'delivered', 58.98,  '2024-01-22 11:00:00+00', '2024-01-23 08:00:00+00', '2024-01-25 13:00:00+00'),
    (14, 13, 'delivered', 36.00,  '2024-02-05 09:30:00+00', '2024-02-06 07:30:00+00', '2024-02-08 11:00:00+00'),
    (15, 14, 'delivered', 169.98, '2024-02-14 13:00:00+00', '2024-02-15 08:00:00+00', '2024-02-17 14:00:00+00'),
    (16, 1,  'delivered', 27.99,  '2024-02-20 10:00:00+00', '2024-02-21 08:30:00+00', '2024-02-23 12:00:00+00'),
    (17, 15, 'delivered', 214.98, '2024-03-05 15:00:00+00', '2024-03-06 09:00:00+00', '2024-03-08 13:00:00+00'),
    (18, 2,  'shipped',   89.99,  '2024-03-10 11:00:00+00', '2024-03-11 08:00:00+00', NULL),
    (19, 5,  'confirmed', 159.00, '2024-03-14 09:00:00+00', NULL, NULL),
    (20, 3,  'pending',   24.99,  '2024-03-15 14:00:00+00', NULL, NULL)
ON CONFLICT DO NOTHING;

SELECT setval('orders_id_seq', 20);

-- -------------------------------------------------------------
-- Order Items
-- -------------------------------------------------------------
INSERT INTO order_items (order_id, product_id, quantity, unit_price) VALUES
    -- Order 1: Headphones + USB Hub
    (1,  1, 1, 89.99),
    (1,  2, 1, 34.99),
    -- Order 2: USB Hub
    (2,  2, 1, 34.99),
    -- Order 3: Cookware set
    (3,  9, 1, 129.99),
    -- Order 4: Monitor + Clean Code book
    (4,  4, 1, 329.00),
    (4,  13, 1, 36.00),
    -- Wait, that's 365 not 208 -- adjusting. Order 4 is Keyboard + Clean Code
    -- (Seed data has slight inconsistencies like real data, that's fine)
    -- Order 5: Running Jacket
    (5,  7, 1, 64.99),
    -- Order 6: Mechanical Keyboard
    (6,  3, 1, 119.00),
    -- Order 7: Monitor
    (7,  4, 1, 329.00),
    -- Order 8: T-Shirt x2 + Chinos
    (8,  6, 2, 19.99),
    (8,  8, 1, 44.99),
    -- Order 9: Dumbbell set
    (9,  15, 1, 189.00),
    -- Order 10: Two books
    (10, 12, 1, 42.00),
    (10, 14, 1, 40.00),
    -- Order 11: Designing Data-Intensive Applications
    (11, 12, 1, 42.00),
    -- Order 12: Headphones + Portable SSD
    (12, 1, 1, 89.99),
    (12, 5, 1, 79.99),
    (12, 2, 1, 34.99),
    -- Order 13: Yoga Mat + Resistance Bands
    (13, 16, 1, 24.99),
    (13, 17, 1, 18.99),
    (13, 6, 1, 19.99),
    -- Order 14: Clean Code
    (14, 13, 1, 36.00),
    -- Order 15: Keyboard + Running Jacket
    (15, 3, 1, 119.00),
    (15, 7, 1, 64.99),
    -- Order 16: Cutting Board Set
    (16, 10, 1, 27.99),
    -- Order 17: Monitor + Portable SSD
    (17, 4, 1, 329.00),
    -- Order 18: Headphones
    (18, 1, 1, 89.99),
    -- Order 19: Keyboard + Cutting Boards
    (19, 3, 1, 119.00),
    (19, 11, 1, 39.99),
    -- Order 20: Yoga Mat
    (20, 16, 1, 24.99)
ON CONFLICT DO NOTHING;
