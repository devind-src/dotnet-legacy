-- Skema uji modul Routing: hanya tabel & kolom yang dibaca/ditulis SyncNet.Routing (SDK lama) dan
-- SyncNetPro.Routing. Tipe mengikuti entity SyncNetWebApi; tabel tanpa entity (tran_map, commitment,
-- volume) mengikuti query DbMgr. Dipakai generator golden (SDK lama) dan test SDK baru.
CREATE TABLE sw_nodes (node_id int PRIMARY KEY, node_name varchar(20) NOT NULL);
CREATE TABLE sw_product (product_code varchar(20) PRIMARY KEY, product_name varchar(100));
CREATE TABLE sw_routes_by_inst (inst_id varchar(20) NOT NULL, node_id int NOT NULL, notes varchar(200), fee_sharing int, lb_weight int NOT NULL DEFAULT 1);
CREATE TABLE sw_routes_by_inst_alt (id serial PRIMARY KEY, inst_id varchar(20) NOT NULL, node_id int NOT NULL, priority smallint NOT NULL,
    fee_sharing int, lb_weight int NOT NULL DEFAULT 1, status varchar(1) NOT NULL DEFAULT '1');
CREATE TABLE sw_routes_margin (inst_id varchar(20) NOT NULL, notes varchar(200), routing_mode varchar(20), static_node_id int);
CREATE TABLE sw_margin_supplier (id serial PRIMARY KEY, supplier_id varchar(20), biller_code varchar(20), denom int, harga_beli int, harga_jual int,
    margin int, status varchar(1), priority smallint, lb_weight int NOT NULL DEFAULT 1);
CREATE TABLE sw_margin_merchant (id serial PRIMARY KEY, merchant_id varchar(20), biller_code varchar(20), denom int, harga_jual int);
CREATE TABLE sw_fees (id bigserial PRIMARY KEY, product_id varchar(20), merchant_id varchar(20), submerchant_id varchar(20), fee_type varchar(1),
    routing_mode varchar(20), static_node_id int,
    fixed_fee int NOT NULL DEFAULT 0, fixed_fee_acq int NOT NULL DEFAULT 0, fixed_fee_mer int NOT NULL DEFAULT 0, fixed_fee_iss int NOT NULL DEFAULT 0,
    fixed_fee_bil int NOT NULL DEFAULT 0, fixed_fee_swt int NOT NULL DEFAULT 0,
    percent_fee numeric(9,4) NOT NULL DEFAULT 0, percent_fee_acq numeric(9,4) NOT NULL DEFAULT 0, percent_fee_mer numeric(9,4) NOT NULL DEFAULT 0,
    percent_fee_iss numeric(9,4) NOT NULL DEFAULT 0, percent_fee_bil numeric(9,4) NOT NULL DEFAULT 0, percent_fee_swt numeric(9,4) NOT NULL DEFAULT 0);
CREATE TABLE sw_routes_supplier_status (supplier_id varchar(20) PRIMARY KEY, status varchar(10) NOT NULL DEFAULT 'ACTIVE', last_rc_code varchar(10),
    consecutive_suspect_count int NOT NULL DEFAULT 0, consecutive_failed_count int NOT NULL DEFAULT 0, consecutive_pending_count int NOT NULL DEFAULT 0,
    consecutive_latency_count int NOT NULL DEFAULT 0, block_reason varchar(20), last_latency_ms int, retry_count int NOT NULL DEFAULT 0,
    last_tran_dt timestamp, blocked_since timestamp, blocked_until timestamp, updated_by varchar(50), updated_dt timestamp);
CREATE TABLE sw_routes_failover_config (id serial PRIMARY KEY, routing_type varchar(10) NOT NULL, inst_id varchar(20),
    rc_link_down varchar(100), rc_suspect varchar(100), max_consecutive_suspect int NOT NULL DEFAULT 3,
    link_down_cooldown_minutes int, suspect_cooldown_minutes int,
    rc_failed varchar(100), max_consecutive_failed int NOT NULL DEFAULT 3, failed_cooldown_minutes int,
    rc_pending varchar(100), max_consecutive_pending int NOT NULL DEFAULT 5, pending_cooldown_minutes int,
    latency_threshold_ms int, max_consecutive_latency int NOT NULL DEFAULT 3, latency_cooldown_minutes int, is_active varchar(1) NOT NULL DEFAULT '1');
CREATE TABLE sw_routes_failover_log (id serial PRIMARY KEY, routing_type varchar(10), inst_id varchar(20), denom int, trace_number varchar(20),
    from_supplier_id varchar(20), to_supplier_id varchar(20), rc_code varchar(10), reason varchar(20), latency_ms int, schedule_id int, created_dt timestamp);
CREATE TABLE sw_routes_tran_map (id bigserial PRIMARY KEY, merchant_id varchar(20), terminal_id varchar(20), refnum varchar(20), routing_type varchar(10),
    inst_id varchar(20), denom int, node_name varchar(20), inquiry_switch_key varchar(100), switch_key varchar(100), created_dt timestamp, updated_dt timestamp);
CREATE TABLE sw_trans_pg (switch_key varchar(100), dest_node varchar(20));
CREATE TABLE sw_routes_schedule (id serial PRIMARY KEY, rule_name varchar(100), rule_type varchar(10) NOT NULL, routing_type varchar(10), inst_id varchar(20),
    node_id int NOT NULL, priority smallint, recurrence varchar(10) NOT NULL DEFAULT 'ONCE', start_dt timestamp(0), end_dt timestamp(0),
    time_start time(0), time_end time(0), days_of_week varchar(20), days_of_month varchar(100), valid_from date, valid_until date, status varchar(1) NOT NULL DEFAULT '1');
CREATE TABLE sw_routes_commitment (id serial PRIMARY KEY, rule_name varchar(100), rule_type varchar(10) NOT NULL, routing_type varchar(10) NOT NULL,
    node_id int NOT NULL, inst_id varchar(20), metric varchar(10) NOT NULL, period_type varchar(10) NOT NULL, threshold_value numeric(18,2) NOT NULL,
    warn_pct smallint, valid_from date, valid_until date, created_dt timestamp, status varchar(1) NOT NULL DEFAULT '1');
CREATE TABLE sw_routes_commitment_config (id int PRIMARY KEY, is_active varchar(1) NOT NULL);
CREATE TABLE sw_routes_volume (volume_date date NOT NULL, routing_type varchar(10) NOT NULL, node_name varchar(20) NOT NULL, inst_id varchar(20) NOT NULL,
    tran_count bigint NOT NULL DEFAULT 0, tran_amount numeric(18,2) NOT NULL DEFAULT 0, updated_dt timestamp,
    PRIMARY KEY (volume_date, routing_type, node_name, inst_id));
CREATE TABLE sw_routes_commitment_log (id serial PRIMARY KEY, commitment_id int NOT NULL, rule_type varchar(10), routing_type varchar(10), node_name varchar(20),
    inst_id varchar(20), metric varchar(10), period_type varchar(10), period_start date NOT NULL, event varchar(20) NOT NULL,
    volume_value numeric(18,2), threshold_value numeric(18,2) NOT NULL, created_dt timestamp,
    CONSTRAINT uq_sw_routes_commitment_log_event UNIQUE (commitment_id, period_start, event, threshold_value));
