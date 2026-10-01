-- Data skenario resolver (golden SDK lama & test SDK baru). Jam skenario: Senin 2026-09-28 10:00.
INSERT INTO sw_nodes VALUES (1,'BILLER_A'),(2,'BILLER_B'),(3,'BILLER_C'),(4,'BILLER_D'),(5,'SUP_X'),(6,'SUP_Y'),(7,'SUP_Z'),(8,'SUP_W');
INSERT INTO sw_product VALUES ('PLN','PLN Postpaid'),('BPJS','BPJS'),('PDAM','PDAM'),('TSEL10','Telkomsel 10K'),('XL10','XL 10K'),('ISAT10','Indosat 10K');

-- Bill payment: primary + alternate
INSERT INTO sw_routes_by_inst (inst_id,node_id,notes,fee_sharing,lb_weight) VALUES
  ('PLN',1,NULL,2500,1),('BPJS',4,NULL,1000,1),('PDAM',1,NULL,NULL,1),('TSEL10',5,'topup statis',NULL,1),('XL10',6,NULL,NULL,1);
INSERT INTO sw_routes_by_inst_alt (inst_id,node_id,priority,fee_sharing,lb_weight,status) VALUES
  ('PLN',2,2,3000,1,'1'),('PLN',3,3,3000,1,'1'),('PLN',1,4,9999,1,'1'),
  ('BPJS',2,2,1500,1,'0'),('PDAM',3,2,500,1,'1'),('ORPHAN',2,2,100,1,'1');

-- Topup: routing by margin
INSERT INTO sw_routes_margin (inst_id,notes,routing_mode,static_node_id) VALUES
  ('TSEL10',NULL,'BEST_PRICE',NULL),('XL10',NULL,'STATIC',7),('ISAT10',NULL,'PRIORITY',NULL);
INSERT INTO sw_margin_supplier (supplier_id,biller_code,denom,harga_beli,harga_jual,margin,status,priority,lb_weight) VALUES
  ('SUP_X','TSEL10',10000,9800,10500,700,'1',2,1),('SUP_Y','TSEL10',10000,9700,10500,800,'1',1,1),
  ('SUP_Z','TSEL10',10000,9900,10500,600,'1',NULL,1),('SUP_W','TSEL10',10000,9600,10500,900,'0',1,1),
  ('SUP_Y','XL10',10000,9500,10000,500,'1',NULL,1),('SUP_Z','XL10',10000,9600,10000,400,'1',NULL,1),
  ('SUP_X','ISAT10',10000,9700,10000,300,'1',3,1),('SUP_Y','ISAT10',10000,9800,10000,200,'1',1,1),('SUP_Z','ISAT10',10000,9750,10000,250,'1',2,1);
INSERT INTO sw_margin_merchant (merchant_id,biller_code,denom,harga_jual) VALUES ('M001','TSEL10',10000,10700);

-- Product Fees
INSERT INTO sw_fees (product_id,merchant_id,submerchant_id,fee_type,routing_mode,static_node_id,fixed_fee,fixed_fee_acq,fixed_fee_mer,fixed_fee_iss,fixed_fee_bil,fixed_fee_swt,
  percent_fee,percent_fee_acq,percent_fee_mer,percent_fee_iss,percent_fee_bil,percent_fee_swt) VALUES
  ('PLN',NULL,NULL,'0','PRIORITY',NULL,5000,1000,500,0,2000,1500,0,0,0,0,0,0),
  ('PLN','M001',NULL,'0','BEST_PRICE',NULL,6000,1000,1000,0,2500,1500,0,0,0,0,0,0),
  ('PLN','M001','S01','0','PRIORITY',NULL,6500,1000,1000,500,2500,1500,0,0,0,0,0,0),
  ('PLN','M002',NULL,'0',NULL,NULL,5500,1000,500,0,2500,1500,0,0,0,0,0,0),
  ('BPJS',NULL,NULL,'0','STATIC',4,2500,1000,0,0,1000,500,0,0,0,0,0,0),
  ('PDAM',NULL,NULL,'1',NULL,NULL,0,0,0,0,0,0,1.5,40,10,0,30,20),
  ('PDAM','M001',NULL,'1','STATIC',3,0,0,0,0,0,0,2.25,50,0,0,25,25);

-- Health check
INSERT INTO sw_routes_failover_config (routing_type,inst_id,rc_link_down,rc_suspect,max_consecutive_suspect,link_down_cooldown_minutes,suspect_cooldown_minutes,
  rc_failed,max_consecutive_failed,failed_cooldown_minutes,rc_pending,max_consecutive_pending,pending_cooldown_minutes,latency_threshold_ms,max_consecutive_latency,
  latency_cooldown_minutes,is_active) VALUES
  ('PRODUCT',NULL,'91,1091','68,1068',2,30,10,'05',2,10,'09',3,10,3000,2,5,'1'),
  ('MARGIN',NULL,'91,1091','68,1068',2,30,10,NULL,3,NULL,NULL,5,NULL,NULL,3,NULL,'1');
INSERT INTO sw_routes_supplier_status (supplier_id,status,consecutive_suspect_count,retry_count,block_reason,blocked_since,blocked_until,updated_dt) VALUES
  ('BILLER_B','SUSPECT',0,0,'TIMEOUT','2026-09-28 09:30:00','2026-09-28 11:00:00','2026-09-28 09:30:00'),
  ('SUP_Y','DOWN',0,0,'LINK_DOWN','2026-09-28 09:00:00',NULL,'2026-09-28 09:00:00');

-- Jadwal Routing
INSERT INTO sw_routes_schedule (rule_name,rule_type,routing_type,inst_id,node_id,priority,recurrence,time_start,time_end,days_of_week,status) VALUES
  ('C tutup pagi','CLOSED',NULL,NULL,3,NULL,'DAILY','09:00','12:00',NULL,'1'),
  ('D jam kerja','OPEN','PRODUCT',NULL,4,NULL,'WEEKLY','08:00','17:00','1,2,3,4,5','1'),
  ('nonaktif','CLOSED',NULL,NULL,1,NULL,'DAILY','00:00','00:00',NULL,'0');

-- Volume & Tiering
INSERT INTO sw_routes_commitment_config VALUES (1,'1');
INSERT INTO sw_routes_commitment (rule_name,rule_type,routing_type,node_id,inst_id,metric,period_type,threshold_value,warn_pct,created_dt,status) VALUES
  ('A kuota harian','LIMIT','PRODUCT',1,NULL,'COUNT','DAILY',1,50,'2026-09-01','1');

-- Fallback advice/reversal: tujuan yang dicatat Core
INSERT INTO sw_trans_pg (switch_key,dest_node) VALUES ('PAY0928093000000777TERM01','BILLER_D');
