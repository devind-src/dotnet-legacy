using Dapper;
using Npgsql;
using SyncNet.Helpers;
using SyncNet.Library;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Security.AccessControl;
using System.Text.RegularExpressions;

namespace SyncNet.DbRepository
{
    public class DbPgSql
    {
        private readonly NbLogger _logger;
        private readonly string _connectionString;
        private static readonly Regex IdentifierRegex = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

        public DbPgSql(string connectionString)
        {
            _logger = new NbLogger(AppProcessor.APPNAME);
            _connectionString = connectionString;
        }

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

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
        private static bool IsValidIdentifier(string identifier)
        {
            return !string.IsNullOrWhiteSpace(identifier) && IdentifierRegex.IsMatch(identifier);
        }
    }
}
