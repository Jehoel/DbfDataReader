using System;
using System.IO;
using System.Text;
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

        private static DbfColumn Create(Int32 index, Char[] nameChars, Byte columnType, Byte length, Byte decimalCount)
        {
            return new DbfColumn(
                index,
                new String( nameChars ).TrimEnd('\0'),
                (DbfColumnType)columnType,
                length,
                decimalCount
            );
        }

        [CLSCompliant(false)]
        public static async Task<DbfColumn> ReadAsync(AsyncBinaryReader reader, Int32 index)
        {
            String name = await ReadNameAsync( reader ).ConfigureAwait(false);
            if( name == null ) return null;

            Byte          columnType       = await reader.ReadByteAsync()   .ConfigureAwait(false);
            UInt32        fieldDataAddress = await reader.ReadUInt32Async() .ConfigureAwait(false); // ignore field data address
            Byte          length           = await reader.ReadByteAsync()   .ConfigureAwait(false);
            Byte          decimalCount     = await reader.ReadByteAsync()   .ConfigureAwait(false);
            Byte[]        reserved         = await reader.ReadBytesAsync(14).ConfigureAwait(false); // skip the reserved bytes

            return new DbfColumn( index, name, (DbfColumnType)columnType, length, decimalCount );
        }

        private static async Task<String> ReadNameAsync(AsyncBinaryReader reader)
        {
            // Check if the first byte is 0x0D which indicates no more column definitions.
            Byte name0 = await reader.ReadByteAsync().ConfigureAwait(false);
            if( name0 == DbfHeader.HeaderTerminator ) return null;

            Byte[] nameBytes = new Byte[11];
            nameBytes[0] = name0;
            Int32 nameRead = await reader.ReadAsync( nameBytes, 1, 10 ).ConfigureAwait(false);
            if( nameRead != 10 ) return null; // Reached EOF somehow.

            String name = Encoding.ASCII.GetString( nameBytes ).TrimEnd( '\0' );
            return name;
        }

        public static DbfColumn Read(BinaryReader reader, Int32 index)
        {
            String name = ReadName( reader );
            if( name == null ) return null;

            Byte          columnType       = reader.ReadByte();
            UInt32        fieldDataAddress = reader.ReadUInt32(); // ignore field data address
            Byte          length           = reader.ReadByte();
            Byte          decimalCount     = reader.ReadByte();
            Byte[]        reserved         = reader.ReadBytes(14); // skip the reserved bytes

            return new DbfColumn( index, name, (DbfColumnType)columnType, length, decimalCount );
        }

        private static String ReadName(BinaryReader reader)
        {
            // Check if the first byte is 0x0D which indicates no more column definitions.
            Byte name0 = reader.ReadByte();
            if( name0 == DbfHeader.HeaderTerminator ) return null;

            Byte[] nameBytes = new Byte[11];
            nameBytes[0] = name0;
            Int32 nameRead = reader.Read( nameBytes, 1, 10 );
            if( nameRead != 10 ) return null; // Reached EOF somehow.

            String name = Encoding.ASCII.GetString( nameBytes ).TrimEnd( '\0' );
            return name;
        }
    }
}