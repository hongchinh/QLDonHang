-- Sample data: 100 customers (KH0001..KH0100) + 100 products (SP0001..SP0100).
-- Idempotent: skips rows whose code already exists.
-- Run against the dev/test DB, e.g.:
--   psql -h localhost -U postgres -d qldonhang_test -f backend/scripts/seed-sample-catalog.sql

BEGIN;

-- ============================================================================
-- Customers
-- ============================================================================
INSERT INTO customers (
    id, code, name, tax_code,
    company_address, default_shipping_address,
    contact_person, phone_number, email,
    "group", note, status,
    created_at, is_deleted
)
SELECT
    gen_random_uuid(),
    'KH' || lpad(i::text, 4, '0'),
    CASE grp
        WHEN 1 THEN org_prefix || ' ' || biz_name
        WHEN 2 THEN 'Đại lý ' || biz_name
        WHEN 3 THEN person_name
        WHEN 4 THEN 'Dự án ' || biz_name
    END,
    CASE WHEN grp IN (1, 2, 4)
         THEN lpad(((i * 9871 + 1000000) % 1000000000)::text, 10, '0')
         ELSE NULL END,
    CASE WHEN grp <> 3
         THEN street_num || ' ' || street || ', ' || ward || ', ' || district || ', ' || province
         ELSE NULL END,
    street_num || ' ' || street || ', ' || ward || ', ' || district || ', ' || province,
    person_name,
    '0' || (ARRAY['9', '8', '7', '3', '5'])[(i % 5) + 1]
         || lpad(((i * 7919 + 13) % 100000000)::text, 8, '0'),
    lower(replace(unaccent(biz_name), ' ', ''))
        || CASE grp
               WHEN 1 THEN '@company.vn'
               WHEN 2 THEN '@daily.vn'
               WHEN 3 THEN '.' || i::text || '@gmail.com'
               WHEN 4 THEN '@project.vn'
           END,
    grp,
    CASE WHEN i % 10 = 0 THEN 'Khách hàng VIP'
         WHEN i % 7  = 0 THEN 'Khách quen'
         ELSE NULL END,
    CASE WHEN i % 17 = 0 THEN 0 ELSE 1 END,
    now() - (i * interval '1 day'),
    false
FROM (
    SELECT
        i,
        CASE
            WHEN i % 10 IN (1, 2, 3, 4) THEN 1   -- Company (40%)
            WHEN i % 10 IN (5, 6)       THEN 2   -- Agent   (20%)
            WHEN i % 10 IN (7, 8, 9)    THEN 3   -- Retail  (30%)
            ELSE                              4   -- Project (10%)
        END AS grp,
        (ARRAY['Công ty TNHH', 'Công ty Cổ phần', 'Doanh nghiệp tư nhân', 'Tập đoàn'])[((i - 1) % 4) + 1] AS org_prefix,
        (ARRAY[
            'Tân Phát', 'An Khang', 'Hưng Thịnh', 'Phú Quý', 'Đại Lộc',
            'Minh Anh', 'Bảo Long', 'Hoàng Gia', 'Thiên Phú', 'Nam Sơn',
            'Đông Hải', 'Tây Bắc', 'Việt Hưng', 'Phương Đông', 'Toàn Cầu',
            'Á Châu', 'Hoa Mai', 'Sao Mai', 'Bình Minh', 'Hồng Hà',
            'Đại Việt', 'Trường Sơn', 'Cửu Long', 'Sông Đà', 'Thái Bình',
            'Anh Quân', 'Trung Dũng', 'Quang Minh', 'Đức Tài', 'Mai Linh'
        ])[((i - 1) % 30) + 1] AS biz_name,
        (ARRAY[
            'Nguyễn Văn An', 'Trần Thị Bình', 'Lê Hoàng Cường', 'Phạm Minh Đức', 'Hoàng Thị Em',
            'Vũ Quốc Phong', 'Đặng Thu Giang', 'Bùi Văn Hùng', 'Đỗ Thị Hoa', 'Ngô Hữu Khanh',
            'Dương Văn Lâm', 'Phan Thị Mai', 'Lý Quốc Nam', 'Trịnh Văn Oanh', 'Tô Thị Phương',
            'Hồ Đức Quân', 'Đinh Văn Rạng', 'Cao Thị Sương', 'Mai Văn Tâm', 'Lưu Thị Uyên'
        ])[((i - 1) % 20) + 1] AS person_name,
        ((i * 13) % 500 + 1)::text AS street_num,
        (ARRAY[
            'Nguyễn Trãi', 'Lê Lợi', 'Trần Hưng Đạo', 'Hai Bà Trưng', 'Lý Thường Kiệt',
            'Phan Đình Phùng', 'Nguyễn Huệ', 'Điện Biên Phủ', 'Cách Mạng Tháng 8', 'Võ Văn Kiệt'
        ])[((i - 1) % 10) + 1] AS street,
        'Phường ' || (((i * 3) % 20) + 1)::text AS ward,
        (ARRAY[
            'Quận 1', 'Quận 3', 'Quận 5', 'Quận 7', 'Quận 10',
            'Quận Bình Thạnh', 'Quận Tân Bình', 'Quận Phú Nhuận', 'TP. Thủ Đức', 'Quận Gò Vấp'
        ])[((i - 1) % 10) + 1] AS district,
        (ARRAY[
            'TP. Hồ Chí Minh', 'Hà Nội', 'Đà Nẵng', 'Hải Phòng', 'Cần Thơ',
            'Bình Dương', 'Đồng Nai', 'Long An', 'Bắc Ninh', 'Hưng Yên'
        ])[((i - 1) % 10) + 1] AS province
    FROM generate_series(1, 100) AS i
) src
WHERE NOT EXISTS (
    SELECT 1 FROM customers c WHERE c.code = 'KH' || lpad(src.i::text, 4, '0')
);

-- ============================================================================
-- Products
-- ============================================================================
INSERT INTO products (
    id, code, name, product_group_id, unit_id,
    length, width, thickness, density, specification,
    default_price, cost_price, default_tax_rate,
    note, status, pricing_mode,
    created_at, is_deleted
)
SELECT
    gen_random_uuid(),
    'SP' || lpad(src.i::text, 4, '0'),
    src.pg_name || ' ' || src.dim_spec,
    pg.id,
    u.id,
    src.len_val,
    src.wid_val,
    src.thk_val,
    src.density_val,
    src.dim_spec,
    src.base_price,
    round(src.base_price * 0.78, 2),
    10.00,
    CASE WHEN src.i % 11 = 0 THEN 'Hàng nhập khẩu'
         WHEN src.i % 13 = 0 THEN 'Hàng tồn kho'
         ELSE NULL END,
    CASE WHEN src.i % 19 = 0 THEN 0 ELSE 1 END,
    src.pricing_mode,
    now() - (src.i * interval '2 day'),
    false
FROM (
    SELECT
        i,
        (ARRAY['EPS', 'XPS', 'PE', 'CSN', 'THUNG', 'DAGEL', 'BK', 'BTT', 'VC', 'KHAC'])[((i - 1) % 10) + 1] AS pg_code,
        (ARRAY[
            'Tấm xốp EPS', 'Tấm xốp XPS', 'Tấm PE Foam', 'Cao su non', 'Thùng xốp',
            'Tấm da gel', 'Bông khoáng', 'Bông thủy tinh', 'Vận chuyển', 'Vật tư phụ'
        ])[((i - 1) % 10) + 1] AS pg_name,
        (ARRAY['TAM', 'TAM', 'TAM', 'TAM', 'THUNG', 'TAM', 'TAM', 'TAM', 'CHUYEN', 'CAI'])[((i - 1) % 10) + 1] AS unit_code,
        (ARRAY[1, 1, 1, 2, 1, 1, 2, 2, 1, 1])[((i - 1) % 10) + 1] AS pricing_mode,
        (ARRAY[1000, 1200, 1500, 2000, 2400, 3000])[((i - 1) % 6) + 1]::numeric AS len_val,
        (ARRAY[500, 600, 800, 1000, 1200])[((i - 1) % 5) + 1]::numeric AS wid_val,
        (ARRAY[5, 10, 15, 20, 25, 30, 40, 50, 75, 100])[((i - 1) % 10) + 1]::numeric AS thk_val,
        (ARRAY[15, 20, 25, 30, 35, 40])[((i - 1) % 6) + 1]::numeric AS density_val,
        (ARRAY[1000, 1200, 1500, 2000, 2400, 3000])[((i - 1) % 6) + 1]::text || 'x' ||
            (ARRAY[500, 600, 800, 1000, 1200])[((i - 1) % 5) + 1]::text || 'x' ||
            (ARRAY[5, 10, 15, 20, 25, 30, 40, 50, 75, 100])[((i - 1) % 10) + 1]::text || 'mm' AS dim_spec,
        (50000 + ((i * 4321) % 450000))::numeric(18, 2) AS base_price
    FROM generate_series(1, 100) AS i
) src
JOIN product_groups pg ON pg.code = src.pg_code
JOIN units          u  ON u.code  = src.unit_code
WHERE NOT EXISTS (
    SELECT 1 FROM products p WHERE p.code = 'SP' || lpad(src.i::text, 4, '0')
);

COMMIT;

-- Verify
SELECT 'customers' AS table_name, count(*) AS total FROM customers WHERE is_deleted = false
UNION ALL
SELECT 'products',                count(*)         FROM products  WHERE is_deleted = false;
