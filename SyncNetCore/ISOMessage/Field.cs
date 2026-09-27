namespace SyncNet.ISOMessage
{
    public class Field
    {
        public enum EnumFieldType
        {
            ASCII,
            BCD
        }

        public enum EnumFieldFormat
        {
            Fixed,
            LVAR,
            LLVAR,
            LLLVAR,
            LLLLVAR,
            LLLLLVAR,
            LLLLLLVAR
        }

        public enum EnumFieldAtribute
        {
            n,
            an,
            ans
        }

        public EnumFieldType FieldType;
        public EnumFieldFormat FieldFormat;
        public EnumFieldAtribute FieldAttribute;
        public int FieldLength;
        public string FieldName;
    }
}
