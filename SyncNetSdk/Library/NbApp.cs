using SyncNet.Cryptography;

namespace SyncNet.Library
{
    public class NbApp
    {
        public static string GetLicense(string sn, string expdate, string key)
        {
            string actkey = DesAlgorithm.EncryptHex(sn.PadRight(16, 'F'), key);

            expdate = DesAlgorithm.EncryptHex(expdate.PadRight(16, 'F'), key);
            expdate = DesAlgorithm.EncryptHex(expdate, actkey);

            return actkey + expdate;
        }

        public static string GetExpDate(string license, string sn, string key)
        {
            if (license.Length != 32) return "00000000";

            string actkey = license.Substring(0, 16);
            string expdate = license.Substring(16, 16);

            expdate = DesAlgorithm.DecryptHex(expdate, actkey);
            expdate = DesAlgorithm.DecryptHex(expdate, key);

            return expdate.Substring(0, 8);
        }

        public static bool IsLicenseValid(string license, string sn, string key)
        {
            if (license.Length != 32) return false;

            string actkey = license.Substring(0, 16);
            string expdate = license.Substring(16, 16);

            expdate = DesAlgorithm.DecryptHex(expdate, actkey);
            expdate = DesAlgorithm.DecryptHex(expdate, key);

            actkey = DesAlgorithm.DecryptHex(actkey, key);

            if (sn == actkey && expdate.Substring(8, 8) == "FFFFFFFF")
                return true;
            else
                return false;
        }
    }
}
