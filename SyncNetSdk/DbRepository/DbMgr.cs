using SyncNet.Helpers;
using SyncNet.Library;
using SyncNet.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SyncNet.DbRepository
{
    public class DbMgr : SyncNet.Routing.Failover.ISupplierStatusStore, SyncNet.Routing.Schedule.IRoutingScheduleStore
    {
        public enum EnumStatusApp { DOWN = 0, UP = 1 }

        private readonly NbLogger _logger;
        private readonly DbService _dbService;

        public DbMgr()
        {
            _logger = new NbLogger(AppProcessor.APPNAME);
            _dbService = new DbService();
        }

        #region Service Base Sync Methods
        public DbResult Execute(string query)
        {
            return _dbService.Execute(query, null);
        }
        public DbResult Execute(string sqltext, object param)
        {
            return _dbService.Execute(sqltext, param);
        }
        public DbResult Execute(List<QueryModel> queries)
        {
            var commands = queries.Select(q => (q.sqltext, q.param));
            return _dbService.ExecuteTransaction(commands);
        }
        public DbResult Execute(IEnumerable<(string Sql, object Param)> commands)
        {
            return _dbService.ExecuteTransaction(commands);
        }

        public string GetFieldValue(string query)
        {
            return _dbService.QueryFirst<string>(query);
        }
        public string GetFieldValue(string query, object param)
        {
            return _dbService.QueryFirst<string>(query, param);
        }

        public DataTable GetRecords(string query)
        {
            return _dbService.QueryFirstDataTable(query);
        }
        public DataTable GetRecords(string query, object param)
        {
            return _dbService.QueryFirstDataTable(query, param);
        }

        public DataRow GetRow(string query)
        {
            return _dbService.QueryFirstDataRow(query);
        }
        public DataRow GetRow(string query, object param)
        {
            return _dbService.QueryFirstDataRow(query, param);
        }

        public bool IsRecordExist(string query)
        {
            return _dbService.Exists(query);
        }
        public bool IsRecordExist(string query, object param)
        {
            return _dbService.Exists(query, param);
        }
        #endregion

        #region Service Base Async Methods
        public async Task<DbResult> ExecuteAsync(string query)
        {
            return await _dbService.ExecuteAsync(query, null);
        }
        public async Task<DbResult> ExecuteAsync(string sqltext, object param)
        {
            return await _dbService.ExecuteAsync(sqltext, param);
        }
        public async Task<DbResult> ExecuteAsync(List<QueryModel> queries)
        {
            var commands = queries.Select(q => (q.sqltext, q.param));
            return await _dbService.ExecuteTransactionAsync(commands);
        }
        public async Task<DbResult> ExecuteAsync(IEnumerable<(string Sql, object Param)> commands)
        {
            return await _dbService.ExecuteTransactionAsync(commands);
        }

        public async Task<string> GetFieldValueAsync(string query)
        {
            return await _dbService.QueryFirstAsync<string>(query);
        }
        public async Task<string> GetFieldValueAsync(string query, object param)
        {
            return await _dbService.QueryFirstAsync<string>(query, param);
        }


        public async Task<DataTable> GetRecordsAsync(string query)
        {
            return await _dbService.QueryFirstDataTableAsync(query);
        }
        public async Task<DataTable> GetRecordsAsync(string query, object param)
        {
            return await _dbService.QueryFirstDataTableAsync(query, param);
        }

        public async Task<DataRow> GetRowAsync(string query)
        {
            return await _dbService.QueryFirstDataRowAsync(query);
        }
        public async Task<DataRow> GetRowAsync(string query, object param)
        {
            return await _dbService.QueryFirstDataRowAsync(query, param);
        }

        public async Task<bool> IsRecordExistAsync(string query)
        {
            return await _dbService.ExistsAsync(query);
        }
        public async Task<bool> IsRecordExistAsync(string query, object param)
        {
            return await _dbService.ExistsAsync(query, param);
        }
        #endregion

        #region Routing
        public async Task<List<RoutesModel.RoutesBySource>> GetRoutesBySource()
        {
            var res = new List<RoutesModel.RoutesBySource>();

            string query = @"SELECT 
                sr.node_id_in,sn_in.node_name AS node_name_source,
                sr.node_id_out,sn_out.node_name AS node_name_dest,
                sr.inst_id,sr.notes
                FROM sw_routes_by_source sr
                JOIN sw_nodes sn_in ON sn_in.node_id=sr.node_id_in
                JOIN sw_nodes sn_out ON sn_out.node_id=sr.node_id_out";

            var tbl = await GetRecordsAsync(query);
            foreach (DataRow row in tbl.Rows)
            {
                res.Add(new RoutesModel.RoutesBySource
                {
                    NodeIdSource = row["node_id_in"].ToString().ToNumber(),
                    NodeNameSource = row["node_name_source"].ToString(),
                    NodeIdDest = row["node_id_out"].ToString().ToNumber(),
                    NodeNameDest = row["node_name_dest"].ToString(),
                    ProductId = row["inst_id"].ToString(),
                    Notes = row["notes"].ToString()
                });
            }

            return res;
        }
        public async Task<List<RoutesModel.RoutesByBin>> GetRoutesByBin()
        {
            var res = new List<RoutesModel.RoutesByBin>();

            string query = @"SELECT sb.group_id,sg.group_name,
                sb.node_id, sn.node_name AS node_name
                FROM sw_routes_by_bin sb
                JOIN sw_nodes sn ON sn.node_id=sb.node_id
                JOIN sw_group sg ON sg.group_id=sb.group_id";

            var tbl = await GetRecordsAsync(query);
            foreach (DataRow row in tbl.Rows)
            {
                res.Add(new RoutesModel.RoutesByBin
                {
                    GroupId = row["group_id"].ToString().ToNumber(),
                    GroupName = row["group_name"].ToString(),
                    NodeId = row["node_id"].ToString().ToNumber(),
                    NodeName = row["node_name"].ToString()
                });
            }

            return res;
        }
        public async Task<List<RoutesModel.RoutesByProduct>> GetRoutesByProduct()
        {
            var res = new List<RoutesModel.RoutesByProduct>();

            string query = @"SELECT si.inst_id,sp.product_name,
                si.node_id,sn.node_name AS node_name,si.notes,si.fee_sharing,si.lb_weight
                FROM sw_routes_by_inst si
                LEFT JOIN sw_product sp on sp.product_code=si.inst_id 
                JOIN sw_nodes sn ON sn.node_id=si.node_id";

            var tbl = await GetRecordsAsync(query);
            foreach (DataRow row in tbl.Rows)
            {
                res.Add(new RoutesModel.RoutesByProduct
                {
                    ProductId = row["inst_id"].ToString(),
                    ProductName = row["product_name"].ToString(),
                    NodeId = row["node_id"].ToString().ToNumber(),
                    NodeName = row["node_name"].ToString(),
                    Notes = row["notes"].ToString(),
                    FeeSharing = ToNullableInt(row["fee_sharing"]),
                    LbWeight = row["lb_weight"].ToString().ToNumber()
                });
            }

            return res;
        }
        public async Task<List<RoutesModel.RoutesByProductAlt>> GetRoutesByProductAlt()
        {
            var res = new List<RoutesModel.RoutesByProductAlt>();

            string query = @"SELECT sa.inst_id,sa.node_id,sn.node_name,sa.priority,sa.fee_sharing,sa.lb_weight
                FROM sw_routes_by_inst_alt sa
                JOIN sw_nodes sn ON sn.node_id=sa.node_id
                WHERE sa.status='1'
                ORDER BY sa.inst_id,sa.priority";

            var tbl = await GetRecordsAsync(query);
            foreach (DataRow row in tbl.Rows)
            {
                res.Add(new RoutesModel.RoutesByProductAlt
                {
                    ProductId = row["inst_id"].ToString(),
                    NodeId = row["node_id"].ToString().ToNumber(),
                    NodeName = row["node_name"].ToString(),
                    Priority = row["priority"].ToString().ToNumber(),
                    FeeSharing = ToNullableInt(row["fee_sharing"]),
                    LbWeight = row["lb_weight"].ToString().ToNumber()
                });
            }

            return res;
        }
        public async Task<List<RoutesModel.RoutesMargin>> GetRoutesMargin()
        {
            var res = new List<RoutesModel.RoutesMargin>();

            string query = @"SELECT sd.inst_id,sp.product_name,sd.notes,sd.routing_mode,
                sn.node_name AS static_node_name
                FROM sw_routes_margin sd
                LEFT JOIN sw_product sp on sp.product_code=sd.inst_id
                LEFT JOIN sw_nodes sn ON sn.node_id=sd.static_node_id";

            var tbl = await GetRecordsAsync(query);
            foreach (DataRow row in tbl.Rows)
            {
                res.Add(new RoutesModel.RoutesMargin
                {
                    ProductId = row["inst_id"].ToString(),
                    ProductName = row["product_name"].ToString(),
                    Notes = row["notes"].ToString(),
                    RoutingMode = row["routing_mode"].ToString(),
                    StaticNodeName = row["static_node_name"].ToString()
                });
            }

            return res;
        }
        #endregion

        #region Routing Failover
        public async Task<List<FailoverModel.SupplierStatus>> GetSupplierStatus()
        {
            var res = new List<FailoverModel.SupplierStatus>();

            string query = @"SELECT supplier_id,status,last_rc_code,consecutive_suspect_count,
                consecutive_failed_count,consecutive_pending_count,consecutive_latency_count,
                block_reason,last_latency_ms,
                retry_count,last_tran_dt,blocked_since,blocked_until,updated_by,updated_dt
                FROM sw_routes_supplier_status";

            var tbl = await GetRecordsAsync(query);
            foreach (DataRow row in tbl.Rows)
            {
                res.Add(new FailoverModel.SupplierStatus
                {
                    SupplierId = row["supplier_id"].ToString(),
                    Status = Enum.TryParse(row["status"].ToString(), out FailoverModel.SupplierStatusEnum s) ? s : FailoverModel.SupplierStatusEnum.ACTIVE,
                    LastRcCode = row["last_rc_code"] == DBNull.Value ? null : row["last_rc_code"].ToString(),
                    ConsecutiveSuspectCount = row["consecutive_suspect_count"].ToString().ToNumber(),
                    ConsecutiveFailedCount = row["consecutive_failed_count"].ToString().ToNumber(),
                    ConsecutivePendingCount = row["consecutive_pending_count"].ToString().ToNumber(),
                    ConsecutiveLatencyCount = row["consecutive_latency_count"].ToString().ToNumber(),
                    BlockReason = row["block_reason"] == DBNull.Value ? null : row["block_reason"].ToString(),
                    LastLatencyMs = row["last_latency_ms"] == DBNull.Value ? null : (int?)row["last_latency_ms"].ToString().ToNumber(),
                    RetryCount = row["retry_count"].ToString().ToNumber(),
                    LastTranDt = row["last_tran_dt"] == DBNull.Value ? null : (DateTime?)row["last_tran_dt"],
                    BlockedSince = row["blocked_since"] == DBNull.Value ? null : (DateTime?)row["blocked_since"],
                    BlockedUntil = row["blocked_until"] == DBNull.Value ? null : (DateTime?)row["blocked_until"],
                    UpdatedBy = row["updated_by"] == DBNull.Value ? null : row["updated_by"].ToString(),
                    UpdatedDt = row["updated_dt"] == DBNull.Value ? null : (DateTime?)row["updated_dt"]
                });
            }

            return res;
        }

        public async Task<List<FailoverModel.Config>> GetFailoverConfig()
        {
            var res = new List<FailoverModel.Config>();

            string query = @"SELECT routing_type,inst_id,rc_link_down,rc_suspect,max_consecutive_suspect,
                link_down_cooldown_minutes,suspect_cooldown_minutes,
                rc_failed,max_consecutive_failed,failed_cooldown_minutes,
                rc_pending,max_consecutive_pending,pending_cooldown_minutes,
                latency_threshold_ms,max_consecutive_latency,latency_cooldown_minutes,is_active
                FROM sw_routes_failover_config";

            var tbl = await GetRecordsAsync(query);
            foreach (DataRow row in tbl.Rows)
            {
                res.Add(new FailoverModel.Config
                {
                    RoutingType = row["routing_type"].ToString(),
                    InstId = row["inst_id"] == DBNull.Value ? null : row["inst_id"].ToString(),
                    RcLinkDown = row["rc_link_down"].ToString(),
                    RcSuspect = row["rc_suspect"].ToString(),
                    MaxConsecutiveSuspect = row["max_consecutive_suspect"].ToString().ToNumber(),
                    LinkDownCooldownMinutes = row["link_down_cooldown_minutes"] == DBNull.Value ? null : (int?)row["link_down_cooldown_minutes"].ToString().ToNumber(),
                    SuspectCooldownMinutes = row["suspect_cooldown_minutes"] == DBNull.Value ? null : (int?)row["suspect_cooldown_minutes"].ToString().ToNumber(),
                    RcFailed = row["rc_failed"] == DBNull.Value ? null : row["rc_failed"].ToString(),
                    MaxConsecutiveFailed = row["max_consecutive_failed"].ToString().ToNumber(),
                    FailedCooldownMinutes = row["failed_cooldown_minutes"] == DBNull.Value ? null : (int?)row["failed_cooldown_minutes"].ToString().ToNumber(),
                    RcPending = row["rc_pending"] == DBNull.Value ? null : row["rc_pending"].ToString(),
                    MaxConsecutivePending = row["max_consecutive_pending"].ToString().ToNumber(),
                    PendingCooldownMinutes = row["pending_cooldown_minutes"] == DBNull.Value ? null : (int?)row["pending_cooldown_minutes"].ToString().ToNumber(),
                    LatencyThresholdMs = row["latency_threshold_ms"] == DBNull.Value ? null : (int?)row["latency_threshold_ms"].ToString().ToNumber(),
                    MaxConsecutiveLatency = row["max_consecutive_latency"].ToString().ToNumber(),
                    LatencyCooldownMinutes = row["latency_cooldown_minutes"] == DBNull.Value ? null : (int?)row["latency_cooldown_minutes"].ToString().ToNumber(),
                    IsActive = row["is_active"].ToString() == "1"
                });
            }

            return res;
        }

        public async Task UpsertSupplierStatus(FailoverModel.SupplierStatus status)
        {
            string sqltext = @"INSERT INTO sw_routes_supplier_status
                (supplier_id,status,last_rc_code,consecutive_suspect_count,
                 consecutive_failed_count,consecutive_pending_count,consecutive_latency_count,
                 block_reason,last_latency_ms,retry_count,
                 last_tran_dt,blocked_since,blocked_until,updated_by,updated_dt)
                VALUES(@supplier_id,@status,@last_rc_code,@consecutive_suspect_count,
                 @consecutive_failed_count,@consecutive_pending_count,@consecutive_latency_count,
                 @block_reason,@last_latency_ms,@retry_count,
                 @last_tran_dt,@blocked_since,@blocked_until,@updated_by,@updated_dt)
                ON CONFLICT (supplier_id) DO UPDATE SET
                 status=@status,last_rc_code=@last_rc_code,
                 consecutive_suspect_count=@consecutive_suspect_count,
                 consecutive_failed_count=@consecutive_failed_count,
                 consecutive_pending_count=@consecutive_pending_count,
                 consecutive_latency_count=@consecutive_latency_count,
                 block_reason=@block_reason,last_latency_ms=@last_latency_ms,retry_count=@retry_count,
                 last_tran_dt=@last_tran_dt,blocked_since=@blocked_since,blocked_until=@blocked_until,
                 updated_by=@updated_by,updated_dt=@updated_dt";

            object param = new
            {
                supplier_id = status.SupplierId,
                status = status.Status.ToString(),
                last_rc_code = status.LastRcCode,
                consecutive_suspect_count = status.ConsecutiveSuspectCount,
                consecutive_failed_count = status.ConsecutiveFailedCount,
                consecutive_pending_count = status.ConsecutivePendingCount,
                consecutive_latency_count = status.ConsecutiveLatencyCount,
                block_reason = status.BlockReason,
                last_latency_ms = status.LastLatencyMs,
                retry_count = status.RetryCount,
                last_tran_dt = status.LastTranDt,
                blocked_since = status.BlockedSince,
                blocked_until = status.BlockedUntil,
                updated_by = status.UpdatedBy,
                updated_dt = status.UpdatedDt ?? DateTime.Now
            };

            await ExecuteAsync(sqltext, param);
        }

        public async Task<long> InsertTranMap(TranMapModel m)
        {
            string sqltext = @"INSERT INTO sw_routes_tran_map
                (merchant_id,terminal_id,refnum,routing_type,inst_id,denom,node_name,
                 inquiry_switch_key,switch_key,created_dt)
                VALUES(@merchant_id,@terminal_id,@refnum,@routing_type,@inst_id,@denom,@node_name,
                 @inquiry_switch_key,@switch_key,@created_dt)
                RETURNING id";

            object param = new
            {
                merchant_id = m.MerchantId,
                terminal_id = m.TerminalId,
                refnum = m.Refnum,
                routing_type = m.RoutingType,
                inst_id = m.InstId,
                denom = m.Denom,
                node_name = m.NodeName,
                inquiry_switch_key = m.InquirySwitchKey,
                switch_key = m.SwitchKey,
                created_dt = DateTime.Now
            };

            return await _dbService.QueryFirstAsync<long>(sqltext, param);
        }

        // baris terbaru (id terbesar) bila kunci komposit duplikat
        public async Task<TranMapModel> GetTranMapByRef(string merchantId, string terminalId, string refnum)
        {
            string query = @"SELECT id,merchant_id,terminal_id,refnum,routing_type,inst_id,denom,
                node_name,inquiry_switch_key,switch_key
                FROM sw_routes_tran_map
                WHERE merchant_id=@merchant_id AND terminal_id=@terminal_id AND refnum=@refnum
                ORDER BY id DESC LIMIT 1";

            var row = await GetRowAsync(query,
                new { merchant_id = merchantId, terminal_id = terminalId, refnum = refnum });

            return MapTranMap(row);
        }

        public async Task<TranMapModel> GetTranMapBySwitchKey(string switchKey)
        {
            string query = @"SELECT id,merchant_id,terminal_id,refnum,routing_type,inst_id,denom,
                node_name,inquiry_switch_key,switch_key
                FROM sw_routes_tran_map
                WHERE switch_key=@switch_key
                ORDER BY id DESC LIMIT 1";

            var row = await GetRowAsync(query, new { switch_key = switchKey });

            return MapTranMap(row);
        }

        public async Task UpdateTranMapSwitchKey(long id, string switchKey)
        {
            string sqltext = @"UPDATE sw_routes_tran_map
                SET switch_key=@switch_key,updated_dt=@updated_dt WHERE id=@id";

            await ExecuteAsync(sqltext, new { id = id, switch_key = switchKey, updated_dt = DateTime.Now });
        }

        // fallback terakhir untuk advice/reversal: node tujuan yang dicatat core pada transaksi asal
        public async Task<string> GetDestNodeBySwitchKey(string switchKey)
        {
            string query = "SELECT dest_node FROM sw_trans_pg WHERE switch_key=@switch_key";

            return await GetFieldValueAsync(query, new { switch_key = switchKey });
        }

        private static int? ToNullableInt(object value)
        {
            return value == null || value == DBNull.Value ? null : Convert.ToInt32(value);
        }

        private static TranMapModel MapTranMap(DataRow row)
        {
            if (row == null) return null;

            return new TranMapModel
            {
                Id = Convert.ToInt64(row["id"]),
                MerchantId = row["merchant_id"].ToString(),
                TerminalId = row["terminal_id"].ToString(),
                Refnum = row["refnum"].ToString(),
                RoutingType = row["routing_type"].ToString(),
                InstId = row["inst_id"].ToString(),
                Denom = row["denom"] == DBNull.Value ? null : Convert.ToInt32(row["denom"]),
                NodeName = row["node_name"].ToString(),
                InquirySwitchKey = row["inquiry_switch_key"] == DBNull.Value ? null : row["inquiry_switch_key"].ToString(),
                SwitchKey = row["switch_key"] == DBNull.Value ? null : row["switch_key"].ToString()
            };
        }

        public async Task InsertFailoverLog(string routingType, string instId, long? denom, string traceNumber,
            string fromSupplierId, string toSupplierId, string rcCode, string reason, int? latencyMs = null, int? scheduleId = null)
        {
            string sqltext = @"INSERT INTO sw_routes_failover_log
                (routing_type,inst_id,denom,trace_number,from_supplier_id,to_supplier_id,rc_code,reason,latency_ms,schedule_id,created_dt)
                VALUES(@routing_type,@inst_id,@denom,@trace_number,@from_supplier_id,@to_supplier_id,@rc_code,@reason,@latency_ms,@schedule_id,@created_dt)";

            object param = new
            {
                routing_type = routingType,
                inst_id = instId,
                denom = denom,
                trace_number = traceNumber,
                from_supplier_id = fromSupplierId,
                to_supplier_id = toSupplierId,
                rc_code = rcCode,
                reason = reason,
                latency_ms = latencyMs,
                schedule_id = scheduleId,
                created_dt = DateTime.Now
            };

            await ExecuteAsync(sqltext, param);
        }
        #endregion

        #region Routing Schedule
        // aturan Jadwal Routing yang masih bisa berlaku (Fase 3): aktif, aturan Sekali yang belum
        // selesai, dan pola berulang yang masa berlakunya belum lewat. node wajib (R8).
        public async Task<List<RoutingScheduleModel>> GetRoutingSchedules()
        {
            var res = new List<RoutingScheduleModel>();

            string query = @"SELECT s.id,s.rule_name,s.rule_type,s.routing_type,s.inst_id,sn.node_name,s.priority,
                s.recurrence,s.start_dt,s.end_dt,s.time_start,s.time_end,s.days_of_week,s.days_of_month,
                s.valid_from,s.valid_until
                FROM sw_routes_schedule s
                JOIN sw_nodes sn ON sn.node_id=s.node_id
                WHERE s.status='1'
                  AND (s.recurrence<>'ONCE' OR s.end_dt > @now)
                  AND (s.valid_until IS NULL OR s.valid_until >= @today)";

            var tbl = await GetRecordsAsync(query, new { now = DateTime.Now, today = DateTime.Today });
            foreach (DataRow row in tbl.Rows)
            {
                string recurrence = row["recurrence"].ToString();
                res.Add(new RoutingScheduleModel
                {
                    Id = Convert.ToInt32(row["id"]),
                    Name = row["rule_name"].ToString(),
                    RuleType = row["rule_type"].ToString(),
                    RoutingType = row["routing_type"] == DBNull.Value ? null : row["routing_type"].ToString(),
                    InstId = row["inst_id"] == DBNull.Value ? null : row["inst_id"].ToString(),
                    NodeName = row["node_name"].ToString(),
                    Priority = row["priority"] == DBNull.Value ? null : Convert.ToInt16(row["priority"]),
                    Recurrence = recurrence,
                    StartDt = row["start_dt"] == DBNull.Value ? null : Convert.ToDateTime(row["start_dt"]),
                    EndDt = row["end_dt"] == DBNull.Value ? null : Convert.ToDateTime(row["end_dt"]),
                    TimeStart = ToTime(row["time_start"]),
                    TimeEnd = ToTime(row["time_end"]),
                    Days = ParseDays(recurrence == RoutingScheduleModel.WEEKLY ? row["days_of_week"] : row["days_of_month"]),
                    ValidFrom = ToDate(row["valid_from"]),
                    ValidUntil = ToDate(row["valid_until"])
                });
            }

            return res;
        }

        private static TimeSpan? ToTime(object v) => v switch
        {
            TimeSpan ts => ts,
            TimeOnly to => to.ToTimeSpan(),
            DBNull or null => null,
            _ => TimeSpan.Parse(v.ToString())
        };

        private static DateTime? ToDate(object v) => v switch
        {
            DateTime d => d.Date,
            DateOnly d => d.ToDateTime(TimeOnly.MinValue),
            DBNull or null => null,
            _ => Convert.ToDateTime(v).Date
        };

        private static List<int> ParseDays(object v) =>
            v == DBNull.Value || v == null
                ? []
                : v.ToString().Split(',').Select(s => int.TryParse(s.Trim(), out var d) ? d : 0).Where(d => d > 0).ToList();
        #endregion

        #region Fees
        public async Task<List<FeesModel.FeesPayment>> GetFeesPayment()
        {
            var res = new List<FeesModel.FeesPayment>();

            string query = @"SELECT product_id,merchant_id,submerchant_id,fee_type,routing_mode,static_node_id,
                fixed_fee,fixed_fee_acq,fixed_fee_mer,fixed_fee_iss,fixed_fee_bil,fixed_fee_swt,
                percent_fee,percent_fee_acq,percent_fee_mer,percent_fee_iss,percent_fee_bil,percent_fee_swt
                FROM sw_fees";

            var tbl = await GetRecordsAsync(query);
            foreach (DataRow row in tbl.Rows)
            {
                res.Add(new FeesModel.FeesPayment
                {
                    ProductId = row["product_id"].ToString(),
                    MerchantId = row["merchant_id"].ToString(),
                    SubMerchantId = row["submerchant_id"].ToString(),
                    IsFixedFee = row["fee_type"].ToString() == "0",
                    RoutingMode = row["routing_mode"] == DBNull.Value ? null : row["routing_mode"].ToString(),
                    StaticNodeId = ToNullableInt(row["static_node_id"]),

                    FixedFeeTotal = row["fixed_fee"].ToString().ToNumber(),
                    FixedFeeAcq = row["fixed_fee_acq"].ToString().ToNumber(),
                    FixedFeeMer = row["fixed_fee_mer"].ToString().ToNumber(),
                    FixedFeeIss = row["fixed_fee_iss"].ToString().ToNumber(),
                    FixedFeeBil = row["fixed_fee_bil"].ToString().ToNumber(),
                    FixedFeeSwt = row["fixed_fee_swt"].ToString().ToNumber(),

                    PercentFeeTotal = row["percent_fee"].ToString().ToDecimal(),
                    PercentFeeAcq = row["percent_fee_acq"].ToString().ToDecimal(),
                    PercentFeeMer = row["percent_fee_mer"].ToString().ToDecimal(),
                    PercentFeeIss = row["percent_fee_iss"].ToString().ToDecimal(),
                    PercentFeeBil = row["percent_fee_bil"].ToString().ToDecimal(),
                    PercentFeeSwt = row["percent_fee_swt"].ToString().ToDecimal()
                });
            }

            return res;
        }
        public async Task<List<FeesModel.PriceSupplier>> GetPriceSupplier()
        {
            var res = new List<FeesModel.PriceSupplier>();

            string query = @"SELECT sm.supplier_id,
                sm.biller_code,sp.product_name,
                sm.denom,sm.harga_beli,sm.harga_jual,sm.margin,sm.status,sm.priority,sm.lb_weight
                FROM sw_margin_supplier sm
                LEFT JOIN sw_product sp ON sp.product_code=sm.biller_code";

            var tbl = await GetRecordsAsync(query);
            foreach (DataRow row in tbl.Rows)
            {
                res.Add(new FeesModel.PriceSupplier
                {
                    SupplierId = row["supplier_id"].ToString(),
                    ProductId = row["biller_code"].ToString(),
                    ProductName = row["product_name"].ToString(),
                    Denom = row["denom"].ToString().ToNumber(),
                    PurchasePrice = row["harga_beli"].ToString().ToNumber(),
                    SellingPrice = row["harga_jual"].ToString().ToNumber(),
                    Margin = row["margin"].ToString().ToNumber(),
                    IsActive = row["status"].ToString() == "1",
                    Priority = ToNullableInt(row["priority"]),
                    LbWeight = row["lb_weight"].ToString().ToNumber()
                });
            }

            return res;
        }
        public async Task<List<FeesModel.PriceMerchant>> GetPriceMerchant()
        {
            var res = new List<FeesModel.PriceMerchant>();

            string query = @"SELECT sm.merchant_id,
                sm.biller_code,sp.product_name,
                sm.denom,sm.harga_jual 
                FROM sw_margin_merchant sm
                LEFT JOIN sw_product sp ON sp.product_code=sm.biller_code";

            var tbl = await GetRecordsAsync(query);
            foreach (DataRow row in tbl.Rows)
            {
                res.Add(new FeesModel.PriceMerchant
                {
                    MerchantId = row["merchant_id"].ToString(),
                    ProductId = row["biller_code"].ToString(),
                    ProductName = row["product_name"].ToString(),
                    Denom = row["denom"].ToString().ToNumber(),
                    Price = row["harga_jual"].ToString().ToNumber()
                });
            }

            return res;
        }
        #endregion

        #region Product
        public async Task<List<ProductModel.Product>> GetProduct()
        {
            var res = new List<ProductModel.Product>();

            string query = @"SELECT x.product_code,x.product_name,x.category,y.is_topup 
                FROM sw_product x JOIN sw_product_category y ON y.category = x.category";

            var tbl = await GetRecordsAsync(query);
            foreach (DataRow row in tbl.Rows)
            {
                res.Add(new ProductModel.Product
                {
                    ProductId = row["product_code"].ToString(),
                    ProductName = row["product_name"].ToString(),
                    ProductCategory = row["category"].ToString(),
                    IsProductTopup = row["is_topup"].ToString() == "1"
                });
            }

            return res;
        }
        public async Task<List<ProductModel.Product>> GetProductTopup()
        {
            var res = new List<ProductModel.Product>();

            string query = @"SELECT x.product_code,x.product_name,x.category,y.is_topup 
                FROM sw_product x JOIN sw_product_category y ON y.category = x.category
                WHERE y.is_topup='1'";

            var tbl = await GetRecordsAsync(query);
            foreach (DataRow row in tbl.Rows)
            {
                res.Add(new ProductModel.Product
                {
                    ProductId = row["product_code"].ToString(),
                    ProductName = row["product_name"].ToString(),
                    ProductCategory = row["category"].ToString(),
                    IsProductTopup = row["is_topup"].ToString() == "1"
                });
            }

            return res;
        }
        public async Task<List<ProductModel.Product>> GetProductPayment()
        {
            var res = new List<ProductModel.Product>();

            string query = @"SELECT x.product_code,x.product_name,x.category,y.is_topup 
                FROM sw_product x JOIN sw_product_category y ON y.category = x.category
                WHERE y.is_topup='0'";

            var tbl = await GetRecordsAsync(query);
            foreach (DataRow row in tbl.Rows)
            {
                res.Add(new ProductModel.Product
                {
                    ProductId = row["product_code"].ToString(),
                    ProductName = row["product_name"].ToString(),
                    ProductCategory = row["category"].ToString(),
                    IsProductTopup = row["is_topup"].ToString() == "1"
                });
            }

            return res;
        }
        public async Task<List<ProductModel.Mapping>> GetProductMapping(string appName)
        {
            var res = new List<ProductModel.Mapping>();

            string query = @"SELECT source_biller_code,denom,dest_biller_code 
                FROM sw_product_mapping WHERE app_name = @app_name";

            var tbl = await GetRecordsAsync(query, new { app_name = appName });
            foreach (DataRow row in tbl.Rows)
            {
                res.Add(new ProductModel.Mapping
                {
                    ProductId = row["source_biller_code"].ToString(),
                    Denom = row["denom"].ToString().ToNumber(),
                    BillerCode = row["dest_biller_code"].ToString()
                });
            }

            return res;
        }
        public async Task<string> GetProductMapping(string appName, string productId)
        {
            string query = @"SELECT dest_biller_code 
                FROM sw_product_mapping WHERE app_name = @app_name 
                AND source_biller_code = @source_biller_code AND denom = 0
                LIMIT 1";

            return await GetFieldValueAsync(query,
                new
                {
                    app_name = appName,
                    source_code_biller = productId
                });
        }
        public async Task<string> GetProductMapping(string appName, string productId, int denom)
        {
            string query = @"SELECT dest_biller_code 
                FROM sw_product_mapping WHERE app_name = @app_name 
                AND source_biller_code = @source_biller_code AND denom = @denom
                LIMIT 1";

            return await GetFieldValueAsync(query,
                new
                {
                    app_name = appName,
                    source_code_biller = productId,
                    denom = denom
                });
        }
        #endregion

        #region Internal Methods
        internal async Task<DataTable> GetConnection(string AppName)
        {
            string query = $@"SELECT 
	            SC.node_id,SN.node_name,SC.conn_name,SN.inst_id,
	            SN.parameter,SC.protocol,SC.tcp_header_format,
	            SC.tcp_hi_lo,SC.conn_type,SC.ip_address,SC.port,
	            SC.queue_inbox,SC.queue_outbox,SN.port_in,SN.port_out,
	            SC.max_conn,SC.always_connected,SN.auto_signon,SN.echo_timer,
	            SN.keychange_timer,SN.pin_translate,SN.request_timeout,SN.advice_timeout,		
	            SN.sensitive_data,SC.ws_url,SC.ws_header,SC.ws_method,
	            SC.ws_content,SC.ws_ipsource,SC.ws_proxy_url,SC.ws_proxy_port,
	            SC.ws_key,SC.ws_user,SC.ws_pswd,SC.one_socket_only,SC.retry_delay
            FROM 
	            sw_connections SC
	            INNER JOIN sw_nodes SN ON SN.node_id = SC.node_id
            WHERE
	            SN.app_name = @app_name";

            return await GetRecordsAsync(query, new { app_name = AppName });
        }
        internal async Task<DataTable> GetNodes(string AppName)
        {
            string query = $@"SELECT 
	            node_name,
	            app_name,
	            port_in,
	            port_out,
	            inst_id,
	            auto_signon,
	            auto_reversal,
	            keychange_timer,
	            echo_timer,
	            request_timeout,
	            advice_timeout,
                category,
                pin_translate,
                saf_limit 
            FROM sw_nodes
            WHERE app_name=@app_name";

            return await GetRecordsAsync(query, new { app_name = AppName });
        }
        internal async Task<int> GetPortCommand(string AppName)
        {
            string query = $"SELECT command_port FROM sw_app WHERE app_name=@app_name";
            string val = await GetFieldValueAsync(query, new { app_name = AppName });

            return NbConvert.ToInt(val);
        }
        internal async Task<EndPointModel> GetEndPointLogServices()
        {
            var ret = new EndPointModel();

            string query = $"SELECT host,command_port FROM sw_app WHERE app_name = @app_name";
            DataRow rec = await GetRowAsync(query, new { app_name = "Log Services" });

            if (rec != null)
            {
                ret.Host = rec["host"].ToString();
                ret.Port = Convert.ToInt32(rec["command_port"]);
            }

            return ret;
        }
        internal async Task UpdateApp(string AppName, EnumStatusApp status)
        {
            string sqltext = $@"UPDATE sw_app SET status = @status,last_update = @last_update 
	            WHERE app_name = @app_name";

            object param = new
            {
                status = (int)status,
                last_update = DateTime.Now,
                app_name = AppName
            };

            await ExecuteAsync(sqltext, param);
        }
        internal async Task UpdateNodes(string AppName, EnumStatusApp status)
        {
            string sqltext = $@"UPDATE sw_nodes SET status = @status WHERE app_name = @app_name";

            object param = new
            {
                status = (int)status,
                app_name = AppName
            };

            await ExecuteAsync(sqltext, param);
        }
        internal async Task UpdateNodeRemote(string NodeName, EnumStatusApp status)
        {
            string query;

            if (status == EnumStatusApp.UP)
                query = $@"UPDATE sw_nodes SET 
                    last_connected = @dtnow,
                    remote = @status 
                    WHERE node_name = @node_name";
            else
                query = $@"UPDATE sw_nodes SET 
                    last_disconnected = @dtnow,
                    remote = @status 
                    WHERE node_name = @node_name";

            object param = new
            {
                status = (int)status,
                dtnow = DateTime.Now,
                node_name = NodeName
            };

            await ExecuteAsync(query, param);
        }
        internal async Task UpdateNodeSource(string NodeName, EnumStatusApp status)
        {
            string query = $@"UPDATE sw_nodes SET 
                conn_in = @status 
                WHERE node_name = @node_name";

            object param = new
            {
                status = (int)status,
                node_name = NodeName
            };

            await ExecuteAsync(query, param);
        }
        internal async Task UpdateNodeSink(string NodeName, EnumStatusApp status)
        {
            string query = $@"UPDATE sw_nodes SET 
                conn_out = @status 
                WHERE node_name = @node_name";

            object param = new
            {
                status = (int)status,
                node_name = NodeName
            };

            await ExecuteAsync(query, param);
        }
        internal async Task UpdateConnection(string ConnName, EnumStatusApp status)
        {
            string query;

            if (status == EnumStatusApp.UP)
                query = $@"UPDATE sw_connections SET 
                    last_connected = @dtnow,
                    remote = @status 
                    WHERE conn_name = @conn_name";
            else
                query = $@"UPDATE sw_connections SET 
                    last_disconnected = @dtnow,
                    remote = @status 
                    WHERE conn_name = @conn_name";

            object param = new
            {
                status = (int)status,
                dtnow = DateTime.Now,
                conn_name = ConnName
            };

            await ExecuteAsync(query, param);
        }
        #endregion
    }
}
