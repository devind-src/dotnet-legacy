using SWTCoreLab.DbRepository;
using SyncNet.Library;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SyncNet.DbRepository
{
    public class DbMgr
    {
        public enum EnumStatusApp
        {
            DOWN = 0,
            UP = 1
        }

        private readonly NbLogger _logger;
        private readonly DbService _dbService;

        public DbMgr()
        {
            _dbService = new DbService();
            _logger = new NbLogger();
        }

        #region Service Base
        internal async Task<int> ExecuteAsync(string query)
        {
            var dbResult = await _dbService.ExecuteAsync(query);

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
            var dbResult = await _dbService.ExecuteTransactionAsync(commands!);

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

        #region Custom
        public async Task Initialize()
        {
            //Console.WriteLine("Connecting to db...");

            //test query
            string query = $@"SELECT command_port FROM sw_app 
                WHERE app_name = @app_name";

            //test connect to db
            string commandPort = await GetFieldValue(query, new { app_name = MyApp.APPNAME });

            if (string.IsNullOrEmpty(commandPort) == false)
                await _logger.LogAsync("Connect to db successfull");
            else
                await _logger.LogAsync("Connect to db failed, please check log detail");
        }
        public async Task<int> GetPortApplication()
        {
            string query = $@"SELECT command_port FROM sw_app 
                WHERE app_name = @app_name";

            return NbConvert.ToInt(await GetFieldValue(query, new { app_name = MyApp.APPNAME }));
        }
        public async Task<int> UpdateStatusApp(EnumStatusApp status)
        {
            string sqltext = $@"UPDATE sw_app SET status=@status,last_update=@last_update 
	            WHERE app_name=@app_name";

            object param = new
            {
                status = (int)status,
                last_update = DateTime.Now,
                app_name = MyApp.APPNAME
            };

            return await ExecuteAsync(sqltext, param);
        }
        #endregion
    }
}
