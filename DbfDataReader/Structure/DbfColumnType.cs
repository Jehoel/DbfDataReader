using System;

namespace Dbf
{
    public enum DbfColumnType
    {
        // http://www.dbase.com/Knowledgebase/INT/db7_file_fmt.htm
        // http://dbfread.readthedocs.io/en/latest/field_types.html

        Number         = 'N',
        SignedLong     = 'I', // 4-bytes, aka "Integer"
        Float          = 'F', // Number stored as a string
        Currency       = 'Y',
        Date           = 'D',
        DateTime       = 'T',
        Logical        = 'L', // aka "Boolean"
        Memo           = 'M', // 10-digit memo format
        B              = 'B', // Binary in dBase 7: 10-digit memo format, UInt16 in FoxPro
        General        = 'G', // aka OLE, 10-digit memo format
        Character      = 'C',
        Double         = 'O', // 8 bytes
        Timestamp      = '@', // 8 bytes
        AutoIncrement  = '+', // dBase 7: 4-bytes, same representation as SignedLong

        // From Visual FoxPro: https://msdn.microsoft.com/en-us/library/st4a0s68(v=vs.80).aspx
        Blob           = 'W',
        Picture        = 'P',
        VarBinary      = 'Q',
        VarChar        = 'V',
        NullFlags      = '0' // Zero. This column stores which other column cells in a row contain null or non-null values.
    }

    // Two competing ideas:
    // 1. Have a single DbfColumnType enum whose values corresponds to the declared-type, and let subclassed ValueReader types decide what the actual type is - which means the virtual methods must return Object
    // 2. Have two enums: DbfDeclaredColumnType and DbfActualColumnType. A simpler method can perform the mapping from Declared-to-Actual column-type, while specialisation exists in a single non-virtual ValueReader utility class.

    public enum DbfActualColumnType
    {
        /// <summary>A boolean value stored as a single ASCII character, e.g. 'Y', 'N', 'T', 'F', (in both upper-case and lower-case) etc.</summary>
        BooleanText,
        /// <summary>A number stored as a Base-10 ASCII string, with leading padding. Will be returned as a Decimal.</summary>
        NumberText,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64, // TODO: Is this ever used?
        UInt64,
        CurrencyInt64,
        /// <summary>Binary data stored inline in the file.</summary>
        ByteArray,
        /// <summary>Binary data stored in a memo file. The inline value will be a UInt32 block pointer.</summary>
        Memo4ByteArray,
        /// <summary>Binary data stored in a memo file. The inline value will be a 10-digit NumberText block pointer. dBase 7 uses the 'B' column type for this.</summary>
        Memo10ByteArray,
        /// <summary>ASCII (or other encoding, as-per parameter) text value inline in the table. Restricted to 0-255 characters. Only the DbfColumn.Length property is used.</summary>
        Text,
        /// <summary>ASCII (or other encoding, as-per parameter) text value inline in the table. Can be up to 65535 characters in length. The DbfColumn.DecimalCount property holds the UInt16 high-order byte.</summary>
        TextLong,
        /// <summary>ASCII (or other encoding, as-per parameter) text stored in a memo file. The inline value will be a UInt32 block pointer.</summary>
        Memo4Text,
        /// <summary>ASCII (or other encoding, as-per parameter) text stored in a memo file. The inline value will be a 10-digit NumberText block pointer.</summary>
        Memo10Text,
        /// <summary>Binary IEEI-754 single-precision (4-byte) value.</summary>
        FloatSingle,
        /// <summary>Binary IEEI-754 double-precision (8-byte) value.</summary>
        FloatDouble,
        /// <summary>A date value stored as ASCII text in the form yyMMdd or yyyyMMdd - except I've only ever seen the 8-character version.</summary>
        DateText,
        /// <summary>A date+time value stored as a (UInt32 julianDay, UInt32 timeOfDayMilliseconds) </summary>
        DateTimeBinaryJulian,
        /// <summary>Unsure how it works, but seems to be 1 byte long.</summary>
        NullFlags
    }

    public class MemoBlock
    {
        internal MemoBlock(UInt64 blockNumber)
        {
            this.DbtBlockNumber = blockNumber;
        }

        /// <summary>"10 digits representing a .DBT block number. The number is stored as a string, right justified and padded with blanks."</summary>
        public UInt64 DbtBlockNumber { get; }
    }
}