namespace SyncNet.Library
{
    public class NbStrUtil
    {
        private int _idx;
        private int _lendata;
        private string _strdata;

        public NbStrUtil(string strdata)
        {
            _idx = 0;
            _strdata = strdata;
            _lendata = strdata.Length;
        }

        public string getString(int len)
        {
            string retval = string.Empty;

            try
            {
                if (len + _idx <= _lendata)
                {
                    retval = _strdata.Substring(_idx, len);
                    _idx = _idx + len;
                }
            }
            catch { }

            return retval;
        }
    }
}
