namespace SyncNet.ISOMessage
{
    interface IFieldFormatter
    {
        Field.EnumFieldType GetFieldType(int field_nr);
        Field.EnumFieldFormat GetFieldFormat(int field_nr);
        Field.EnumFieldAtribute GetFieldAttribute(int field_nr);
        int GetFieldLength(int field_nr);
        string GetFieldName(int field_nr);
        int GetLengthVar(int field_nr);
    }
}
