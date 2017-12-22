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

    // Two competing ideas:
    // 1. Have a single DbfColumnType enum whose values corresponds to the declared-type, and let subclassed ValueReader types decide what the actual type is - which means the virtual methods must return Object
    // 2. Have two enums: DbfDeclaredColumnType and DbfActualColumnType. A simpler method can perform the mapping from Declared-to-Actual column-type, while specialisation exists in a single non-virtual ValueReader utility class.

    public enum DbfActualColumnType
    {
        /// <summary>A number stored as a Base-10 ASCII string, with leading padding. May be an integer or decimal.</summary>
        TextNumber,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64, // TODO: Is this ever used?
        UInt64,
        /// <summary>Binary data stored inline in the file.</summary>
        ByteArray,
        /// <summary>Binary data stored in a memo file. The inline value will be a block pointer.</summary>
        MemoByteArray,
        Text, // ASCII or other encoding text value.
        MemoText,
        /// <summary>Binary IEEI-754 single-precision (4-byte) value.</summary>
        SingleFloat,
        /// <summary>Binary IEEI-754 double-precision (8-byte) value.</summary>
        DoubleFloat,
        /// <summary>A date value stored as ASCII text in the form yyMMdd or yyyyMMdd.</summary>
        TextDate,
        /// <summary>A date+time value stored as a (UInt32 julianDay, UInt32 timeOfDayMilliseconds) </summary>
        BinaryJulianDateTime
    }
}