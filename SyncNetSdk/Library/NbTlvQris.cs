using System;

namespace SyncNet.Library
{
    public class NbTlvQris
    {
        private int _len_tag = 3;
        private int _len_length = 3;

        public NbTlvQris()
        {

        }

        public NbTlvQris(int len_tag, int len_length)
        {
            _len_tag = len_tag;
            _len_length = len_length;
        }

        public string SetTagValue(string tag, string value)
        {
            return tag +
                value.Length.ToString().PadLeft(_len_length, '0') +
                value;
        }

        public string GetTagValue(string tlvdata, string tag)
        {
            string[] arr = ExtractTlvMessage(tlvdata);
            int idx = NbConvert.ToInt(tag);

            return arr[idx];
        }

        public string ConstructTlvMessage(string[] de)
        {
            var tlv = new TlvQrisModel();
            string result = "";

            try
            {
                for (int i = 0; i < de.Length; i++)
                {
                    if (de[i] != null && de[i] != "")
                    {
                        tlv.Tag = i.ToString().PadLeft(_len_tag, '0');
                        tlv.Length = de[i].Length.ToString().PadLeft(_len_length, '0');
                        tlv.Value = de[i];

                        result = result +
                            tlv.Tag +
                            tlv.Length +
                            tlv.Value;
                    }
                }
            }
            catch { }

            return result;
        }

        public string[] ExtractTlvMessage(string TlvMessage)
        {
            var tlv = new TlvQrisModel();
            string[] de = new string[1000];
            int offset = 0;

            try
            {
                while (offset <= TlvMessage.Length)
                {
                    if (offset + _len_tag > TlvMessage.Length)
                        break;

                    tlv.Tag = TlvMessage.Substring(offset, _len_tag);
                    tlv.Length = TlvMessage.Substring(offset + _len_tag, _len_length);
                    tlv.Value = TlvMessage.Substring(offset + _len_tag + _len_length, Convert.ToInt32(tlv.Length));

                    de[Convert.ToInt32(tlv.Tag)] = tlv.Value;

                    offset = offset + _len_tag + _len_length + Convert.ToInt32(tlv.Length);
                }
            }
            catch { }

            return de;
        }

        public bool IsTlvMessage(string TlvMessage)
        {
            var tlv = new TlvQrisModel();
            string[] de = new string[1000];
            int offset = 0;
            bool bval = true;

            //check length tlv
            if (TlvMessage.Length < (_len_tag + _len_length)) return false;

            try
            {
                while (offset <= TlvMessage.Length)
                {
                    if (offset + _len_tag > TlvMessage.Length) break;

                    tlv.Tag = TlvMessage.Substring(offset, _len_tag);
                    tlv.Length = TlvMessage.Substring(offset + _len_tag, _len_length);
                    tlv.Value = TlvMessage.Substring(offset + _len_tag + _len_length, Convert.ToInt32(tlv.Length));

                    if (NbSystem.IsNumeric(tlv.Tag) == false)
                    {
                        bval = false;
                        break;
                    }

                    offset = offset + _len_tag + _len_length + Convert.ToInt32(tlv.Length);
                }
            }
            catch
            {
                bval = false;
            }

            return bval;
        }


        internal class TlvQrisModel
        {
            public string Tag;
            public string Length;
            public string Value;
        }
    }
}
