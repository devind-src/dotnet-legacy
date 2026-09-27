-- Restructure menu "Product" (2026-09-25):
--   1. Urutan submenu: Product > Master (1911-1915) lalu Product > Pricing & Fees (1921-1927).
--      Menu diurutkan berdasarkan menu_id (MenuService.GetMenuAsync), jadi menu_id ditukar.
--   2. URL bertingkat: /product/master/* dan /product/pricing-fees/*.
-- dashboard_role_menu ikut dipetakan ke menu_id baru. Jejak perpindahan dicatat di
-- dashboard_menu_move_bak (kunci = URL lama). Nama tabel tidak diubah.
-- Idempotent: baris yang URL-nya sudah baru tidak ikut terpetakan.

BEGIN;

CREATE TEMP TABLE product_menu_map (old_id int, new_id int, old_url varchar(255), new_url varchar(255)) ON COMMIT DROP;
INSERT INTO product_menu_map VALUES
  (1921, 1911, '/product-category',        '/product/master/category'),
  (1922, 1912, '/product-master',          '/product/master/product'),
  (1923, 1913, '/product-merchant',        '/product/master/merchant'),
  (1924, 1914, '/product-mapping',         '/product/master/mapping'),
  (1925, 1915, '/product-transfer',        '/product/master/transfer'),
  (1911, 1921, '/product-supplier-prices', '/product/pricing-fees/prepaid-pricing'),
  (1912, 1922, '/product-merchant-prices', '/product/pricing-fees/merchant-pricing'),
  (1913, 1923, '/product-fee',             '/product/pricing-fees/postpaid-fees'),
  (1914, 1924, '/product-fee-promo',       '/product/pricing-fees/promotions'),
  (1915, 1925, '/product-fee-additional',  '/product/pricing-fees/additional-fees'),
  (1916, 1926, '/product-qris-criteria',   '/product/pricing-fees/qris-fees'),
  (1917, 1927, '/product-fee-tiering',     '/product/pricing-fees/volume-tiering');

-- Hanya baris yang masih memakai id + URL lama.
DELETE FROM product_menu_map p
 WHERE NOT EXISTS (SELECT 1 FROM dashboard_menu m WHERE m.menu_id = p.old_id AND m.url = p.old_url);

INSERT INTO dashboard_menu_move_bak (url, old_menu_id, new_menu_id, old_level_1, old_level_2, old_level_3)
SELECT p.old_url, p.old_id, p.new_id, m.level_1, m.level_2, m.level_3
  FROM product_menu_map p JOIN dashboard_menu m ON m.menu_id = p.old_id
ON CONFLICT (url) DO NOTHING;

-- Dua tahap (offset sementara) supaya tukar menu_id tidak bentrok dengan PK.
UPDATE dashboard_menu m SET menu_id = p.new_id + 100000, url = p.new_url
  FROM product_menu_map p WHERE m.menu_id = p.old_id;
UPDATE dashboard_menu SET menu_id = menu_id - 100000 WHERE menu_id > 100000;

UPDATE dashboard_role_menu r SET menu_id = p.new_id + 100000
  FROM product_menu_map p WHERE r.menu_id = p.old_id;
UPDATE dashboard_role_menu SET menu_id = menu_id - 100000 WHERE menu_id > 100000;

COMMIT;
