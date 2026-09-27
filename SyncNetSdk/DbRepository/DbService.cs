using Dapper;
using Npgsql;
using SyncNet.Common;
using SyncNet.Helpers;
using SyncNet.Library;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SyncNet.DbRepository
{
    public class DbService
    {
        private readonly NbLogger _logger;
        private static readonly Regex IdentifierRegex = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

        public DbService()
        {
            _logger = new NbLogger(AppProcessor.APPNAME);
        }

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(SdkConfig.ConnectionString);
        }


        #region Async Methods
        public async Task<DataTable> QueryFirstDataTableAsync(string sql, object param = null, int? commandTimeout = null)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();

            using var reader = await conn.ExecuteReaderAsync(sql, param, commandTimeout: commandTimeout);

            var dt = new DataTable();
            dt.Load(reader);

            return dt;
        }
        public async Task<DataRow> QueryFirstDataRowAsync(string sql, object param = null, int? commandTimeout = null)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();

            using var reader = await conn.ExecuteReaderAsync(sql, param, commandTimeout: commandTimeout);

            var dt = new DataTable();
            dt.Load(reader);

            // kalau ada row, return row pertama
            if (dt.Rows.Count > 0)
                return dt.Rows[0];

            return null;
        }
        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, int? commandTimeout = null)
        {
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();

                var result = await conn.QueryAsync<T>(sql, param, commandTimeout: commandTimeout);
                return result.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, sql, param);
                throw;
            }
        }
        public async Task<T> QueryFirstAsync<T>(string sql, object param = null, int? commandTimeout = null)
        {
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();

                return await conn.QueryFirstOrDefaultAsync<T>(sql, param, commandTimeout: commandTimeout);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, sql, param);
                throw;
            }
        }

        public async Task<DbResult> ExecuteAsync(string sql, object param = null, int? commandTimeout = null)
        {
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();

                var rows = await conn.ExecuteAsync(sql, param, commandTimeout: commandTimeout);

                return new DbResult
                {
                    IsSuccess = rows > 0,
                    AffectedRows = rows
                };
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, sql, param);

                return new DbResult
                {
                    AffectedRows = 0,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
        public async Task<DbResult> ExecuteTransactionAsync(IEnumerable<(string Sql, object Param)> commands, int? commandTimeout = null)
        {
            string sql = string.Empty;
            object param = null;

            using var conn = CreateConnection();
            await conn.OpenAsync();

            using var trans = await conn.BeginTransactionAsync();
            try
            {
                int totalAffected = 0;

                foreach (var cmd in commands)
                {
                    sql = cmd.Sql;
                    param = cmd.Param;

                    var rows = await conn.ExecuteAsync(cmd.Sql, cmd.Param, trans, commandTimeout: commandTimeout);
                    totalAffected += rows;
                }

                await trans.CommitAsync();

                return new DbResult
                {
                    IsSuccess = true,
                    AffectedRows = totalAffected
                };
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();

                await _logger.LogAsync(ex, sql, param);

                return new DbResult
                {
                    AffectedRows = 0,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<bool> ExistsAsync(string sql, object param = null, int? commandTimeout = null)
        {
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();

                var result = await conn.ExecuteScalarAsync<int?>(sql, param, commandTimeout: commandTimeout);
                return result.HasValue;
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, sql, param);
                throw;
            }
        }
        public async Task<bool> ExistsAsync(string tableName, string columnName, object value, int? commandTimeout = null)
        {
            // Validasi identifier untuk mencegah SQL Injection,
            // karena nama tabel/kolom tidak bisa diparameterisasi lewat Dapper
            if (!IsValidIdentifier(tableName))
                throw new ArgumentException($"Nama tabel tidak valid: {tableName}", nameof(tableName));

            if (!IsValidIdentifier(columnName))
                throw new ArgumentException($"Nama kolom tidak valid: {columnName}", nameof(columnName));

            string sql = $"SELECT 1 FROM {tableName} WHERE {columnName} = @Value LIMIT 1";

            return await ExistsAsync(sql, new { Value = value }, commandTimeout);
        }
        #endregion


        #region Sync Methods
        public DataTable QueryFirstDataTable(string sql, object param = null, int? commandTimeout = null)
        {
            using var conn = CreateConnection();
            conn.Open();

            using var reader = conn.ExecuteReader(sql, param, commandTimeout: commandTimeout);

            var dt = new DataTable();
            dt.Load(reader);

            return dt;
        }
        public DataRow QueryFirstDataRow(string sql, object param = null, int? commandTimeout = null)
        {
            using var conn = CreateConnection();
            conn.Open();

            using var reader = conn.ExecuteReader(sql, param, commandTimeout: commandTimeout);

            var dt = new DataTable();
            dt.Load(reader);

            // kalau ada row, return row pertama
            if (dt.Rows.Count > 0)
                return dt.Rows[0];

            return null;
        }
        public IEnumerable<T> Query<T>(string sql, object param = null, int? commandTimeout = null)
        {
            try
            {
                using var conn = CreateConnection();
                conn.Open();

                var result = conn.Query<T>(sql, param, commandTimeout: commandTimeout);
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.Log(ex, sql, param);
                throw;
            }
        }
        public T QueryFirst<T>(string sql, object param = null, int? commandTimeout = null)
        {
            try
            {
                using var conn = CreateConnection();
                conn.Open();

                return conn.QueryFirstOrDefault<T>(sql, param, commandTimeout: commandTimeout);
            }
            catch (Exception ex)
            {
                _logger.Log(ex, sql, param);
                throw;
            }
        }

        public DbResult Execute(string sql, object param = null, int? commandTimeout = null)
        {
            try
            {
                using var conn = CreateConnection();
                conn.Open();

                var rows = conn.Execute(sql, param, commandTimeout: commandTimeout);

                return new DbResult
                {
                    IsSuccess = rows > 0,
                    AffectedRows = rows
                };
            }
            catch (Exception ex)
            {
                _logger.Log(ex, sql, param);

                return new DbResult
                {
                    AffectedRows = 0,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
        public DbResult ExecuteTransaction(List<QueryModel> queries, int? commandTimeout = null)
        {
            var commands = queries.Select(q => (q.sqltext, q.param));
            return ExecuteTransaction(commands, commandTimeout);
        }
        public DbResult ExecuteTransaction(IEnumerable<(string Sql, object Param)> commands, int? commandTimeout = null)
        {
            string sql = string.Empty;
            object param = null;

            using var conn = CreateConnection();
            conn.Open();

            using var trans = conn.BeginTransaction();
            try
            {
                int totalAffected = 0;

                foreach (var cmd in commands)
                {
                    sql = cmd.Sql;
                    param = cmd.Param;

                    var rows = conn.Execute(cmd.Sql, cmd.Param, trans, commandTimeout: commandTimeout);
                    totalAffected += rows;
                }

                trans.Commit();

                return new DbResult
                {
                    IsSuccess = true,
                    AffectedRows = totalAffected
                };
            }
            catch (Exception ex)
            {
                trans.Rollback();

                _logger.Log(ex, sql, param);

                return new DbResult
                {
                    AffectedRows = 0,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public bool Exists(string sql, object param = null, int? commandTimeout = null)
        {
            try
            {
                using var conn = CreateConnection();
                conn.Open();

                var result = conn.ExecuteScalar<int?>(sql, param, commandTimeout: commandTimeout);
                return result.HasValue;
            }
            catch (Exception ex)
            {
                _logger.Log(ex, sql, param);
                throw;
            }
        }
        public bool Exists(string tableName, string columnName, object value, int? commandTimeout = null)
        {
            // Validasi identifier untuk mencegah SQL Injection,
            // karena nama tabel/kolom tidak bisa diparameterisasi lewat Dapper
            if (!IsValidIdentifier(tableName))
                throw new ArgumentException($"Nama tabel tidak valid: {tableName}", nameof(tableName));

            if (!IsValidIdentifier(columnName))
                throw new ArgumentException($"Nama kolom tidak valid: {columnName}", nameof(columnName));

            string sql = $"SELECT 1 FROM {tableName} WHERE {columnName} = @Value LIMIT 1";

            return Exists(sql, new { Value = value }, commandTimeout);
        }
        private static bool IsValidIdentifier(string identifier)
        {
            return !string.IsNullOrWhiteSpace(identifier) && IdentifierRegex.IsMatch(identifier);
        }
        #endregion
    }
}
