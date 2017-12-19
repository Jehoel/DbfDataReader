namespace Dbf
{
    public enum DbfColumnType
    {
        Number = 'N',
        SignedLong = 'I',
        Float = 'F',
        Currency = 'Y',
        Date = 'D',
        DateTime = 'T',
        Boolean = 'L',
        Memo = 'M',
        DoubleOrBinary = 'B',
        General = 'G',
        Character = 'C',
        Double = 'O',
        Timestamp = '@',
        AutoIncrement = '+'
    }
}