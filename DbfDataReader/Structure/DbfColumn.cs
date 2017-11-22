using System;
using System.IO;
using System.Threading.Tasks;
using Overby.Extensions.AsyncBinaryReaderWriter;

namespace DbfDataReader
{
    /// <summary>Immutable DBF column definition record.</summary>
    public class DbfColumn
    {
        public int           Index        { get; }
        public string        Name         { get; }
        public DbfColumnType ColumnType   { get; }
        public int           Length       { get; }
        public int           DecimalCount { get; }

        public DbfColumn(int index, string name, DbfColumnType columnType, int length, int decimalCount)
        {
            this.Index        = index;
            this.Name         = name;
            this.ColumnType   = columnType;
            this.Length       = length;
            this.DecimalCount = decimalCount;
        }

        private static DbfColumn Create(int index, char[] nameChars, byte columnType, int length, int decimalCount)
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
            char[]        name             = await reader.ReadCharsAsync(11).ConfigureAwait(false);
            byte          columnType       = await reader.ReadByteAsync()   .ConfigureAwait(false);
            uint          fieldDataAddress = await reader.ReadUInt32Async() .ConfigureAwait(false); // ignore field data address
            int           length           = await reader.ReadByteAsync()   .ConfigureAwait(false);
            int           decimalCount     = await reader.ReadByteAsync()   .ConfigureAwait(false);
            byte[]        reserved         = await reader.ReadBytesAsync(14).ConfigureAwait(false); // skip the reserved bytes

            return Create( index, name, columnType, length, decimalCount );
        }

        public static DbfColumn Read(BinaryReader reader, int index)
        {
            char[]        name             = reader.ReadChars(11);
            byte          columnType       = reader.ReadByte();
            uint          fieldDataAddress = reader.ReadUInt32(); // ignore field data address
            int           length           = reader.ReadByte();
            int           decimalCount     = reader.ReadByte();
            byte[]        reserved         = reader.ReadBytes(14); // skip the reserved bytes

            return Create( index, name, columnType, length, decimalCount );
        }
    }
}