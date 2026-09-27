using SyncNet.Common;
using SyncNet.DbRepository;
using SyncNet.Models.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace SyncNet.Services
{
    class BusinessDateService : IDisposable
    {
        private readonly Dictionary<string, BusinessDateModel> _bsnDate = [];
        private readonly Dictionary<string, string> _pubHoliday = [];

        private Timer _tmr;

        public async Task Start()
        {
            await InitData();

            //start timer
            _tmr = new Timer(new TimerCallback(CheckCutover), null, 0, 500);
        }

        public async Task Resync()
        {
            await InitData();
        }

        public string getDateSettlement(string CalendarName)
        {
            //default next day
            DateTime dtSettle = DateTime.Now.AddDays(1);

            string ret = dtSettle.ToString("yyyyMMdd");

            if (_bsnDate.TryGetValue(CalendarName, out BusinessDateModel m) == true)
            {
                //default calculate for 2 weeks
                for (int i = 0; i < AppConfig.MaxDaySettlement; i++)
                {
                    //get settlement date
                    if (IsNextDaySettlement(m, ref dtSettle) == true)
                    {
                        ret = dtSettle.ToString("yyyyMMdd");
                        break;
                    }

                    //add days
                    dtSettle = dtSettle.AddDays(1);
                }
            }

            return ret;
        }

        private async Task InitData()
        {
            //clear item
            _bsnDate.Clear();
            _pubHoliday.Clear();

            //business date
            DataTable tbl = await DbMgr.GetBusinessDate();
            foreach (DataRow row in tbl.Rows)
            {
                BusinessDateModel m = new BusinessDateModel
                {
                    CalendarName = row["business_calendar"].ToString(),
                    TimeCutover = row["time_cutover"].ToString(),
                    CurrentBusinessDate = row["current_bsn_date"].ToString(),
                    PreviousBusinessDate = row["previous_bsn_date"].ToString(),

                    Sun = row["sun"].ToString(),
                    Mon = row["mon"].ToString(),
                    Tue = row["tue"].ToString(),
                    Wed = row["wed"].ToString(),
                    Thu = row["thu"].ToString(),
                    Fri = row["fri"].ToString(),
                    Sat = row["sat"].ToString()
                };

                if (string.IsNullOrEmpty(m.CurrentBusinessDate) == true)
                    m.CurrentBusinessDate = DateTime.Now.AddDays(-1).ToString("dd-MM-yyyy");

                if (string.IsNullOrEmpty(m.PreviousBusinessDate) == true)
                    m.PreviousBusinessDate = DateTime.Now.AddDays(-2).ToString("dd-MM-yyyy");

                //add to list
                if (_bsnDate.ContainsKey(m.CalendarName) == false)
                    _bsnDate.Add(m.CalendarName, m);
            }

            //public holiday
            DataTable tbl2 = await DbMgr.GetPublicHoliday();
            foreach (DataRow row in tbl2.Rows)
            {
                string dt = row["holiday_date"].ToString();

                if (_pubHoliday.ContainsKey(dt) == false)
                    _pubHoliday.Add(dt, dt);
            }
        }

        private bool IsNextDaySettlement(BusinessDateModel m, ref DateTime dtSettle)
        {
            //no cutover on public holiday
            if (_pubHoliday.ContainsKey(dtSettle.ToString("yyyy-MM-dd")) == true) return false;

            DayOfWeek dw = dtSettle.DayOfWeek;
            bool ret = false;

            switch (dw)
            {
                case DayOfWeek.Sunday:
                    if (m.Sun == "1") ret = true;
                    break;
                case DayOfWeek.Monday:
                    if (m.Mon == "1") ret = true;
                    break;
                case DayOfWeek.Tuesday:
                    if (m.Tue == "1") ret = true;
                    break;
                case DayOfWeek.Wednesday:
                    if (m.Wed == "1") ret = true;
                    break;
                case DayOfWeek.Thursday:
                    if (m.Thu == "1") ret = true;
                    break;
                case DayOfWeek.Friday:
                    if (m.Fri == "1") ret = true;
                    break;
                case DayOfWeek.Saturday:
                    if (m.Sat == "1") ret = true;
                    break;
            }

            return ret;
        }

        private bool IsCutover(BusinessDateModel m)
        {
            DayOfWeek dw = DateTime.Today.DayOfWeek;
            bool ret = false;

            switch (dw)
            {
                case DayOfWeek.Sunday:
                    if (m.Sun == "1") ret = true;
                    break;
                case DayOfWeek.Monday:
                    if (m.Mon == "1") ret = true;
                    break;
                case DayOfWeek.Tuesday:
                    if (m.Tue == "1") ret = true;
                    break;
                case DayOfWeek.Wednesday:
                    if (m.Wed == "1") ret = true;
                    break;
                case DayOfWeek.Thursday:
                    if (m.Thu == "1") ret = true;
                    break;
                case DayOfWeek.Friday:
                    if (m.Fri == "1") ret = true;
                    break;
                case DayOfWeek.Saturday:
                    if (m.Sat == "1") ret = true;
                    break;
            }

            return ret;
        }

        private async void CheckCutover(object state)
        {
            try
            {
                //set date
                DateTime dtnow = DateTime.Now;

                //no cutover on public holiday
                if (_pubHoliday.ContainsKey(dtnow.ToString("yyyy-MM-dd")) == true) return;

                //check cutover
                foreach (BusinessDateModel m in _bsnDate.Values)
                {
                    //no cutover
                    if (IsCutover(m) == false) return;

                    //check time cutover
                    if (dtnow.ToString("HH:mm:ss") == m.TimeCutover &&
                        dtnow.ToString("dd-MM-yyyy") != m.CurrentBusinessDate)
                    {
                        //update data
                        m.PreviousBusinessDate = m.CurrentBusinessDate;
                        m.CurrentBusinessDate = dtnow.ToString("dd-MM-yyyy");

                        //update database
                        await DbMgr.UpdateBusinessDate(m);

                        //pooling to transaction manager
                        await MyApp.OnCutover(m.CalendarName);
                    }
                }
            }
            catch { }
        }

        public void Dispose()
        {
            _bsnDate.Clear();

            if (_tmr != null) _tmr.Dispose();
        }
    }
}
