using SWTCoreLab.DbRepository;
using SyncNet.Common;
using SyncNet.Logging;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SyncNet.DbRepository
{
    public class DbMgr
    {
        private readonly CustomLogger _logger;
        private readonly DbService _dbService;

        #region Service Base
        public DbMgr(DbService dbService, CustomLogger logger)
        {
            _dbService = dbService;
            _logger = logger;
        }

        internal async void Initialize()
        {
            //Console.WriteLine("Connecting to db...");

            //test query
            string query = $@"SELECT command_port FROM sw_app 
                WHERE app_name = @app_name";

            //test connect to db
            string commandPort = await GetFieldValue(query, new { app_name = AppConfig.APPNAME });

            if (string.IsNullOrEmpty(commandPort) == false)
                _logger.Log("Connect to db successfull");
            else
                _logger.Log("Connect to db failed, please check log detail");
        }

        internal async Task<int> ExecuteAsync(string query)
        {
            var dbResult = await _dbService.ExecuteAsync(query, null);

            return dbResult.IsSuccess ? 0 : -1;
        }

        internal async Task<int> ExecuteAsync(string sqltext, object param)
        {
            var dbResult = await _dbService.ExecuteAsync(sqltext, param);

            return dbResult.IsSuccess ? 0 : -1;
        }

        internal async Task<int> ExecuteAsync(List<QueryModel> queries)
        {
            var commands = queries.Select(q => (q.sqltext, q.param));
            var dbResult = await _dbService.ExecuteTransactionAsync(commands);

            return dbResult.IsSuccess ? 0 : -1;
        }

        internal async Task<string> GetFieldValue(string query)
        {
            return await _dbService.QueryFirstAsync<string>(query);
        }

        internal async Task<string> GetFieldValue(string query, object param)
        {
            return await _dbService.QueryFirstAsync<string>(query, param);
        }


        internal async Task<DataTable> GetRecords(string query)
        {
            return await _dbService.QueryFirstDataTableAsync(query);
        }

        internal async Task<DataTable> GetRecords(string query, object param)
        {
            return await _dbService.QueryFirstDataTableAsync(query, param);
        }

        internal async Task<DataRow> GetRow(string query)
        {
            return await _dbService.QueryFirstDataRowAsync(query);
        }

        internal async Task<DataRow> GetRow(string query, object param)
        {
            return await _dbService.QueryFirstDataRowAsync(query, param);
        }

        internal async Task<bool> IsRecordExist(string query)
        {
            return await _dbService.ExistsAsync(query);
        }

        internal async Task<bool> IsRecordExist(string query, object param)
        {
            return await _dbService.ExistsAsync(query, param);
        }

        #endregion
    }
}
