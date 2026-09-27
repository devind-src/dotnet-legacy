using System.Collections.Generic;

namespace SyncNet.ISOMessage
{
    public class FieldFormatter : IFieldFormatter
    {
        public enum EnumMsgFormat { ASC, BCD };

        private Dictionary<int, Field> _DataElement;
        private EnumMsgFormat _MsgTypeFormat;
        private EnumMsgFormat _BitmapFormat;
        private EnumMsgFormat _LenVarFormat;

        public EnumMsgFormat MsgTypeFormat
        {
            get { return this._MsgTypeFormat; }
            set { this._MsgTypeFormat = value; }
        }

        public EnumMsgFormat BitmapFormat
        {
            get { return this._BitmapFormat; }
            set { this._BitmapFormat = value; }
        }

        public EnumMsgFormat LenVarFormat
        {
            get { return this._LenVarFormat; }
            set { this._LenVarFormat = value; }
        }

        public FieldFormatter()
        {
            this._MsgTypeFormat = EnumMsgFormat.ASC;
            this._BitmapFormat = EnumMsgFormat.ASC;
            this._LenVarFormat = EnumMsgFormat.ASC;
            this._DataElement = new Dictionary<int, Field>();

            SetField(0, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 16, "Primary Bitmap");
            SetField(1, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 16, "Secondary Bitmap");
            SetField(2, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.n, 19, "Primary Account Number");
            SetField(3, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 6, "Processing Code");
            SetField(4, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 12, "Amount Transaction");
            SetField(5, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 12, "Amount Settlement");
            SetField(7, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 14, "Transmission Date and Time");
            SetField(9, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 8, "Conversion Rate, Settlement");
            SetField(11, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 12, "Systems Trace Audit Number");
            SetField(12, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 6, "Time, Local Transaction");
            SetField(13, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 4, "Date, Local Transaction");
            SetField(14, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 4, "Date, Expiration");
            SetField(15, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 8, "Date, Settlement");
            SetField(16, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 4, "Date, Conversion");
            SetField(17, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 4, "Date, Capture");
            SetField(18, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 4, "Merchant Type");
            SetField(22, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 3, "POS Entry Mode");
            SetField(23, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 3, "Card Sequence Number");
            SetField(24, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 4, "Network Identification Number");
            SetField(25, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 2, "POS Condition Code");
            SetField(26, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 2, "POS PIN Capture Code");
            SetField(27, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 1, "Authorization ID Response Length");
            SetField(28, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 9, "Amount, Transaction Fee");
            SetField(29, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 9, "Amount, Settlement Fee");
            SetField(30, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 9, "Amount, Transaction Processing Fee");
            SetField(31, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 9, "Amount, Settle Processing Fee");
            SetField(32, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.n, 11, "Acquiring Institution ID Code");
            SetField(33, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.n, 11, "Forwarding Institution ID Code");
            SetField(35, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.an, 37, "Track 2 Data");
            SetField(37, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 12, "Retrieval Reference Number");
            SetField(38, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 6, "Authorization ID Response");
            SetField(39, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 4, "Response Code");
            SetField(40, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 3, "Service Restriction Code");
            SetField(41, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 16, "Card Acceptor Terminal ID");
            SetField(42, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 15, "Card Acceptor ID Code");
            SetField(43, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.ans, 40, "Card Acceptor Name Location");
            SetField(44, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.n, 25, "Additional Response Data");
            SetField(45, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.an, 76, "Track 1 Data");
            SetField(48, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLVAR, Field.EnumFieldAtribute.ans, 999, "Additional Data");
            SetField(49, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 3, "Currency Code, Transaction");
            SetField(50, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 3, "Currency Code, Settlement");
            SetField(52, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 16, "PIN Data");
            SetField(53, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLVAR, Field.EnumFieldAtribute.an, 100, "Security Related Control Information");
            SetField(54, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLVAR, Field.EnumFieldAtribute.ans, 120, "Additional Amounts");
            SetField(56, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.ans, 99, "Message Reason Code");
            SetField(57, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLVAR, Field.EnumFieldAtribute.n, 3, "Authorization Life-cycle Code");
            SetField(58, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLVAR, Field.EnumFieldAtribute.n, 11, "Authorizing Agent Institution");
            SetField(59, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLVAR, Field.EnumFieldAtribute.ans, 255, "Echo Data");
            SetField(60, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLVAR, Field.EnumFieldAtribute.ans, 999, "Private Field");
            SetField(61, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLVAR, Field.EnumFieldAtribute.ans, 999, "Private Field");
            SetField(62, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLVAR, Field.EnumFieldAtribute.ans, 999, "Private Field");
            SetField(63, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLVAR, Field.EnumFieldAtribute.ans, 999, "Private Field");
            SetField(64, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 2, "Split Transaction");
            SetField(65, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 2, "Amount Flag");
            SetField(66, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 1, "Settlement Code");
            SetField(67, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 2, "Extended Payment Code");
            SetField(70, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 3, "Network Management Information Code");
            SetField(73, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 6, "Date, Action");
            SetField(74, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 10, "Credits, Number");
            SetField(75, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 10, "Credits, Reversal Number");
            SetField(76, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 10, "Debits, Number");
            SetField(77, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 10, "Debits, Reversal Number");
            SetField(78, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 10, "Transfer, Number");
            SetField(79, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 10, "Transfer, Reversal Number");
            SetField(80, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 10, "Inquiries, Number");
            SetField(81, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 10, "Authorizations, Number");
            SetField(82, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 12, "Credits, Processing Fee Amount");
            SetField(83, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 12, "Credits, Transaction Fee Amount");
            SetField(84, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 12, "Debits, Processing Fee Amount");
            SetField(85, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 12, "Debits, Transaction Fee Amount");
            SetField(86, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 16, "Credits, Amount");
            SetField(87, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 16, "Credits, Reversal Amount");
            SetField(88, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 16, "Debits, Amount");
            SetField(89, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 16, "Debits, Reversal Amount");
            SetField(90, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 42, "Original Data Elements");
            SetField(91, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 1, "File Update Code");
            SetField(95, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 42, "Replacement Amounts");
            SetField(97, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 17, "Amount, Net Settlement");
            SetField(98, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 25, "Payee");
            SetField(100, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.n, 11, "Receiving Institution ID Code");
            SetField(101, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.n, 17, "File Name");
            SetField(102, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.ans, 28, "Account Identification 1");
            SetField(103, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.ans, 28, "Account Identification 2");
            SetField(104, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.n, 34, "Transaction Description");
            SetField(118, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLVAR, Field.EnumFieldAtribute.n, 100, "Payments, Number");
            SetField(119, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLVAR, Field.EnumFieldAtribute.n, 10, "Payments, Reversal Number");
            SetField(120, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 2, "Authorization Profile");
            SetField(123, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLVAR, Field.EnumFieldAtribute.an, 15, "POS Data Code");
            SetField(124, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 48, "Fees Structure");
            SetField(125, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.n, 32, "Transaction Number");
            SetField(126, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.ans, 25, "Time Transaction Received");
            SetField(127, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLLLLVAR, Field.EnumFieldAtribute.ans, 999999, "Private Field");
        }

        public void SetField(int FieldNumber, Field.EnumFieldType FieldType, Field.EnumFieldFormat FieldFormat, Field.EnumFieldAtribute FieldAttribute, int FieldLength, string FieldName)
        {
            //remove data element if exist
            if (_DataElement.ContainsKey(FieldNumber) == true)
                _DataElement.Remove(FieldNumber);

            Field f = new Field();

            f.FieldType = FieldType;
            f.FieldFormat = FieldFormat;
            f.FieldAttribute = FieldAttribute;
            f.FieldLength = FieldLength;
            f.FieldName = FieldName;

            _DataElement.Add(FieldNumber, f);
        }

        public Field.EnumFieldType GetFieldType(int FieldNumber)
        {
            Field.EnumFieldType ret = Field.EnumFieldType.ASCII;

            Field f = new Field();

            if (_DataElement.TryGetValue(FieldNumber, out f) == true)
                ret = f.FieldType;

            return ret;
        }

        public Field.EnumFieldFormat GetFieldFormat(int FieldNumber)
        {
            Field.EnumFieldFormat ret = new Field.EnumFieldFormat();
            Field f = new Field();

            if (_DataElement.TryGetValue(FieldNumber, out f) == true)
                ret = f.FieldFormat;

            return ret;
        }

        public Field.EnumFieldAtribute GetFieldAttribute(int FieldNumber)
        {
            Field.EnumFieldAtribute ret = new Field.EnumFieldAtribute();
            Field f = new Field();

            if (_DataElement.TryGetValue(FieldNumber, out f) == true)
                ret = f.FieldAttribute;

            return ret;
        }

        public int GetFieldLength(int FieldNumber)
        {
            Field f = new Field();
            int ret = 0;

            if (_DataElement.TryGetValue(FieldNumber, out f) == true)
                ret = f.FieldLength;

            return ret;
        }

        public string GetFieldName(int FieldNumber)
        {
            Field f = new Field();
            string ret = "";

            if (_DataElement.TryGetValue(FieldNumber, out f) == true)
                ret = f.FieldName;

            return ret;
        }

        public int GetLengthVar(int FieldNumber)
        {
            Field f = new Field();
            int ret = 0;

            if (_DataElement.TryGetValue(FieldNumber, out f) == true)
            {
                switch (f.FieldFormat)
                {
                    case Field.EnumFieldFormat.LVAR:
                        ret = 1;
                        break;
                    case Field.EnumFieldFormat.LLVAR:
                        ret = 2;
                        break;
                    case Field.EnumFieldFormat.LLLVAR:
                        ret = 3;
                        break;
                    case Field.EnumFieldFormat.LLLLVAR:
                        ret = 4;
                        break;
                    case Field.EnumFieldFormat.LLLLLVAR:
                        ret = 5;
                        break;
                    case Field.EnumFieldFormat.LLLLLLVAR:
                        ret = 6;
                        break;
                }
            }
            return ret;
        }
    }
}
