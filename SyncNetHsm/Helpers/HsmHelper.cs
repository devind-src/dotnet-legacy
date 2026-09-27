namespace SyncNet.Helpers
{
    public static class HsmHelper
    {
        public static int GetKeyLength(this string key)
        {
            int len = key.Length;

            switch (key.Length)
            {
                case 49:
                    len = 48;
                    break;
                case 33:
                    len = 32;
                    break;
                default:
                    len = key.Length;
                    break;
            }

            return len;
        }

        public static string AddKeyScheme(this string key)
        {
            switch (key.Length)
            {
                case 48:
                    return "T" + key;
                case 32:
                    return "U" + key;
                default:
                    return key;
            }
        }
        public static string GetKeyScheme(int len)
        {
            switch (len)
            {
                case 48:
                    return "T";
                case 32:
                    return "U";
                case 16:
                    return "Z";
                default:
                    return "";
            }
        }
        public static string RemoveKeyScheme(this string key)
        {
            switch (key.Length)
            {
                case 49:
                    return key.Substring(1);
                case 33:
                    return key.Substring(1);
                default:
                    return key;
            }
        }
    }
}
