using System.Collections.Generic;

namespace SyncNetApi.Services.Tools
{
    /// <summary>Port of legacy Library/NbTlvEmv — BER-TLV parser for ICC/EMV data plus the tag
    /// name lookup table. Only ParseTLV + the tag dictionary are needed for Tools &gt; ICC Data
    /// Decode; legacy's GetInfo/ConstructTLV helpers are used elsewhere in the legacy app for
    /// unrelated features and are not ported here.</summary>
    public static class EmvTlvParser
    {
        // Same mapping as legacy NbTlvEmv.tagDictionary — EMV tag hex code -> descriptive name.
        private static readonly Dictionary<string, string> TagNames = new()
        {
            // Group 1: Card Related Data
            ["4F"] = "Application Identifier(ADF Name)",
            ["57"] = "Track-2 Equivalent Data",
            ["5A"] = "Primary Account Number (PAN)",
            ["5F20"] = "Cardholder Name",
            ["5F24"] = "Application Expiration Date",
            ["5F25"] = "Application Effective Date",
            ["5F28"] = "Issuer Country Code",
            ["5F2A"] = "Transaction Currency Code",
            ["5F2D"] = "Language Preference",
            ["5F34"] = "Application Sequence Number",
            ["5F36"] = "Transaction Currency Exponent",
            ["5F50"] = "Issuer URL",
            ["71"] = "Issuer Script Data",
            ["72"] = "Issuer Script Data",

            // Group 2: Application Related Data
            ["82"] = "Application Interchange Profile",
            ["84"] = "Dedicated File (DF) Name",
            ["8A"] = "Authorization Response Code",
            ["8C"] = "Card Risk Management Data Object List 1 (CDOL1)",
            ["8D"] = "Card Risk Management Data Object List 2 (CDOL2)",
            ["91"] = "Issuer Authentication Data (ARPC + ARC)",
            ["95"] = "Terminal Verification Results (TVR)",
            ["9A"] = "Transaction Date",
            ["9C"] = "Transaction Type",
            ["9F02"] = "Amount, Authorized",
            ["9F03"] = "Amount, Other",
            ["9F06"] = "Application Identified (AID)",
            ["9F07"] = "Application Usage Control",
            ["9F08"] = "Application Version Number",
            ["9F09"] = "Application Version Number (Terminal)",
            ["9F0D"] = "Issuer Action Code - Default",
            ["9F0E"] = "Issuer Action Code - Denial",
            ["9F0F"] = "Issuer Action Code - Online",
            ["9F10"] = "Issuer Application Data",
            ["9F12"] = "Application Preferred Name",
            ["9F16"] = "Merchant Identifier",
            ["9F1A"] = "Terminal Country Code",
            ["9F1E"] = "Interface Device (IFD) Serial Number",
            ["9F26"] = "Application Request Cryptogram ARQC",
            ["9F27"] = "Cryptogram Information Data (CID)",
            ["9F33"] = "EMV Terminal Capabilities",
            ["9F34"] = "Cardholder Verification Method (CVM) Results",
            ["9F35"] = "Terminal Type",
            ["9F36"] = "Application Transaction Counter (ATC)",
            ["9F37"] = "Unpredictable Number",
            ["9F38"] = "Processing Options Data Object List (PDOL)",
            ["9F39"] = "Point of Service (POS) Entry Mode",
            ["9F40"] = "Additional Terminal Capabilities",
            ["9F41"] = "Transaction Sequence Counter",
            ["9F42"] = "Application Currency Code",
            ["9F44"] = "Application Currency Exponent",
            ["9F53"] = "Transaction Category Code",
            ["9F5B"] = "Issuer Script Results",
            ["DF01"] = "Kernel Identifier",

            // Group 3: Risk Management Data
            ["9F1B"] = "Terminal Floor Limit",
            ["9F1D"] = "Terminal Risk Management Data",
            ["9F4E"] = "Merchant Name and Location",
            ["9F66"] = "Terminal Transaction Qualifiers (TTQ)",

            // Group 4: Security Related Data
            ["9F32"] = "Issuer Public Key Exponent",
            ["9F46"] = "ICC Public Key Certificate",
            ["9F47"] = "ICC Public Key Exponent",
            ["9F48"] = "ICC Public Key Remainder",
            ["9F49"] = "Dynamic Data Authentication Data Object List (DDOL)",
            ["9F4A"] = "Static Data Authentication Tag List",
            ["9F4B"] = "Signed Dynamic Application Data",
            ["9F4C"] = "ICC Dynamic Number",

            // Group 5: Terminal Related Data
            ["9F1C"] = "Terminal Identification",
            ["9F7C"] = "Merchant Custom Data",
            ["9F5D"] = "Token Requestor ID",
            ["9F6E"] = "e-Commerce Indicator (ECI)",
        };

        public static string GetTagName(string tag) => TagNames.GetValueOrDefault(tag, "Unknown Tag");

        public readonly record struct TlvEntry(byte[] Tag, int Length, byte[] Value);

        public static IReadOnlyList<TlvEntry> ParseTlv(byte[] data)
        {
            var result = new List<TlvEntry>();
            var index = 0;

            while (index < data.Length)
            {
                var tag = ReadTag(data, ref index);
                var length = ReadLength(data, ref index);
                var value = ReadValue(data, ref index, length);
                result.Add(new TlvEntry(tag, length, value));
            }

            return result;
        }

        private static byte[] ReadTag(byte[] data, ref int index)
        {
            var tag = new List<byte> { data[index++] };

            // If the tag's last 5 bits are all ones, it's a multi-byte tag.
            if ((tag[0] & 0x1F) == 0x1F)
            {
                while ((data[index] & 0x80) == 0x80)
                    tag.Add(data[index++]);
                tag.Add(data[index++]);
            }

            return tag.ToArray();
        }

        private static int ReadLength(byte[] data, ref int index)
        {
            int length = data[index++];

            // If the highest bit is set, the length is multi-byte.
            if (length > 0x80)
            {
                var byteCount = length & 0x7F;
                length = 0;
                for (var i = 0; i < byteCount; i++)
                    length = (length << 8) | data[index++];
            }

            return length;
        }

        private static byte[] ReadValue(byte[] data, ref int index, int length)
        {
            var value = new byte[length];
            Array.Copy(data, index, value, 0, length);
            index += length;
            return value;
        }
    }
}
