using SyncNet.IsoMessage;

namespace ApiBiller.Message
{
    internal class IsoTemplate : FieldFormatter
    {
        public IsoTemplate()
        {
            SetField(0, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 16, "Primary Bitmap");
            SetField(1, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 16, "Secondary Bitmap");
            SetField(2, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.n, 19, "Primary Account Number");
            SetField(3, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 6, "Processing Code");
            SetField(4, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 12, "Amount Transaction");
            SetField(5, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 12, "Amount Transaction");
            SetField(7, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 10, "Date Time Transaction");
            SetField(9, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 8, "Convertion Rate");
            SetField(11, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 6, "Systems Trace Audit Number");
            SetField(12, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 6, "Time Local");
            SetField(13, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 4, "Date Local");
            SetField(14, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 4, "Expiry Date");
            SetField(15, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 4, "Settlement Date");
            SetField(16, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 4, "Convertion Date");
            SetField(17, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 4, "Capture Date");
            SetField(18, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 4, "Merchant Type");
            SetField(22, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 3, "POS Entry Mode");
            SetField(24, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 3, "NII");
            SetField(25, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.n, 2, "POS Condition Code");
            SetField(32, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.n, 11, "Acquirer ID");
            SetField(33, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.n, 11, "Forwarding ID");
            SetField(35, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.an, 37, "Track 2 Data");
            SetField(37, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.ans, 12, "Reference Number");
            SetField(38, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.ans, 6, "Auth Response");
            SetField(39, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.ans, 2, "Response Code");
            SetField(41, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.ans, 8, "Card Acceptor Terminal ID");
            SetField(42, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.ans, 15, "Card Acceptor ID Code");
            SetField(43, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.ans, 40, "Card Acceptor Name Location");
            SetField(48, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLVAR, Field.EnumFieldAtribute.ans, 999, "Additional data");
            SetField(49, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.ans, 3, "Currency");
            SetField(50, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.ans, 3, "Currency");
            SetField(52, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 16, "PIN Data");
            SetField(54, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLVAR, Field.EnumFieldAtribute.ans, 999, "Customer Balance");
            SetField(55, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLVAR, Field.EnumFieldAtribute.ans, 999, "ICC Data");
            SetField(60, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.ans, 99, "Private Data");
            SetField(62, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLVAR, Field.EnumFieldAtribute.ans, 999, "Private Data");
            SetField(63, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.ans, 99, "Private Data");
            SetField(90, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 42, "Original Data Elemen");
            SetField(95, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.Fixed, Field.EnumFieldAtribute.an, 42, "Replacement Amount");
            SetField(100, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.n, 11, "Receiving inst ID");
            SetField(102, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLVAR, Field.EnumFieldAtribute.ans, 28, "Account Number");
            SetField(127, Field.EnumFieldType.ASCII, Field.EnumFieldFormat.LLLVAR, Field.EnumFieldAtribute.ans, 999, "Private data");
        }
    }
}
