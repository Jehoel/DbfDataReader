using System;
using System.IO;
using System.Threading.Tasks;
using Overby.Extensions.AsyncBinaryReaderWriter;

namespace DbfDataReader
{
    /// <summary>Immutable DBF column definition record.</summary>
    public class DbfColumn
    {
        public Int32         Index        { get; }
        public String        Name         { get; }
        public DbfColumnType ColumnType   { get; }
        public Byte          Length       { get; }
        public Byte          DecimalCount { get; }

        public DbfColumn(Int32 index, String name, DbfColumnType columnType, Byte length, Byte decimalCount)
        {
            this.Index        = index;
            this.Name         = name;
            this.ColumnType   = columnType;
            this.Length       = length;
            this.DecimalCount = decimalCount;
        }

        private static DbfColumn Create(Int32 index, char[] nameChars, Byte columnType, Byte length, Byte decimalCount)
        {
            return new DbfColumn(
                index,
                new string( nameChars ).TrimEnd('\0'),
                (DbfColumnType)columnType,
                length,
                decimalCount
            );
        }

        [CLSCompliant(false)]
        public static async Task<DbfColumn> ReadAsync(AsyncBinaryReader reader, int index)
        {
            Char[]        name             = await reader.ReadCharsAsync(11).ConfigureAwait(false);
            Byte          columnType       = await reader.ReadByteAsync()   .ConfigureAwait(false);
            UInt32        fieldDataAddress = await reader.ReadUInt32Async() .ConfigureAwait(false); // ignore field data address
            Byte          length           = await reader.ReadByteAsync()   .ConfigureAwait(false);
            Byte          decimalCount     = await reader.ReadByteAsync()   .ConfigureAwait(false);
            Byte[]        reserved         = await reader.ReadBytesAsync(14).ConfigureAwait(false); // skip the reserved bytes

            return Create( index, name, columnType, length, decimalCount );
        }

        public static DbfColumn Read(BinaryReader reader, int index)
        {
            Char[]        name             = reader.ReadChars(11);
            Byte          columnType       = reader.ReadByte();
            UInt32        fieldDataAddress = reader.ReadUInt32(); // ignore field data address
            Byte          length           = reader.ReadByte();
            Byte          decimalCount     = reader.ReadByte();
            Byte[]        reserved         = reader.ReadBytes(14); // skip the reserved bytes

            return Create( index, name, columnType, length, decimalCount );
        }
    }
}