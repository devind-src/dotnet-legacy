using Dapper;
using Npgsql;
using SyncNet.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace SyncNet.DbRepository
{
    public class DbService
    {
        private string _connectionString;

        public DbService(string connectionString)
        {
            _connectionString = connectionString;
        }

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }


        #region Base Methods
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
        public async Task<IEnumerable<T>> QueryAsync<T>(
            string sql,
            object param = null,
            int? commandTimeout = null)
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
                await MyApp.Logger($"QueryAsync Error: {ex.Message.NormalizeNewLine()}");
                throw;
            }
        }

        public async Task<T> QueryFirstAsync<T>(
            string sql,
            object param = null,
            int? commandTimeout = null)
        {
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();

                return await conn.QueryFirstOrDefaultAsync<T>(sql, param, commandTimeout: commandTimeout);
            }
            catch (Exception ex)
            {
                await MyApp.Logger($"QueryFirstAsync Error: {ex.Message.NormalizeNewLine()}");
                throw;
            }
        }

        public async Task<DbResult> ExecuteAsync(
            string sql,
            object param = null,
            int? commandTimeout = null)
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
                await MyApp.Logger($"ExecuteAsync Error: {ex.Message.NormalizeNewLine()}");

                return new DbResult
                {
                    AffectedRows = 0,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<DbResult> ExecuteTransactionAsync(
            IEnumerable<(string Sql, object Param)> commands,
            int? commandTimeout = null)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();

            using var trans = await conn.BeginTransactionAsync();
            try
            {
                int totalAffected = 0;

                foreach (var cmd in commands)
                {
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

                await MyApp.Logger($"ExecuteTransactionAsync Error: {ex.Message.NormalizeNewLine()}");

                return new DbResult
                {
                    AffectedRows = 0,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<bool> ExistsAsync(
            string sql,
            object param = null,
            int? commandTimeout = null)
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
                await MyApp.Logger($"ExistsAsync Error: {ex.Message.NormalizeNewLine()}");
                throw;
            }
        }

        public async Task<bool> ExistsAsync(
            string tableName,
            string columnName,
            object value,
            int? commandTimeout = null)
        {
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();

                // Gunakan SELECT 1 ... LIMIT 1 agar efisien
                string sql = $"SELECT 1 FROM {tableName} WHERE {columnName} = @Value LIMIT 1";

                var result = await conn.ExecuteScalarAsync<int?>(sql, new { Value = value }, commandTimeout: commandTimeout);
                return result.HasValue;
            }
            catch (Exception ex)
            {
                await MyApp.Logger($"ExistsAsync Error: {ex.Message.NormalizeNewLine()}");
                throw;
            }
        }
        #endregion
    }
}
