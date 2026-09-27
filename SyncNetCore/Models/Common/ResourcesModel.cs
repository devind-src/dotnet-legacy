namespace SyncNet.Models.Common
{
    public class ResourcesModel
    {
        public AesKey AES { get; set; }
        public DesKey DES { get; set; }

        public class AesKey
        {            
            public AesConfig Config { get; set; } = new();
        }

        public class AesConfig
        {
            public string Key { get; set; }
            public string IV { get; set; }
        }

        public class DesKey
        {
            public string Database { get; set; }
            public string Config { get; set; }
            public HsmKey HSM { get; set; } = new();
        }

        public class HsmKey
        {
            public string LMK_1 { get; set; }
            public string LMK_2 { get; set; }
        }
    }
}
