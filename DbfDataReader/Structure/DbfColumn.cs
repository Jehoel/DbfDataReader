using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Dbf
{
    /// <summary>Immutable DBF column definition record.</summary>
    [DebuggerDisplay("Name={Name}, Index={Index}, Type={ColumnType}({Length})")]
    public class DbfColumn
    {
        public Int32               Index            { get; }
        public UInt32              FieldDataAddress { get; }
        public String              Name             { get; }
        public DbfColumnType       ColumnType       { get; }
        public Byte                Length           { get; }
        public Byte                DecimalCount     { get; }
        public IReadOnlyList<Byte> Reserved         { get; }

        public DbfActualColumnType ActualColumnType { get; }

        public DbfColumn(Int32 index, UInt32 fieldDataAddress, String name, DbfColumnType columnType, Byte length, Byte decimalCount, DbfActualColumnType actualColumnType, Byte[] reserved)
        {
            this.Index            = index;
            this.FieldDataAddress = fieldDataAddress;
            this.Name             = name;
            this.ColumnType       = columnType;
            this.Length           = length;
            this.DecimalCount     = decimalCount;
            this.Reserved         = reserved;

            this.ActualColumnType = actualColumnType;
        }

        public DbfColumn(Int32 index, String name, DbfColumnType columnType, Byte length, Byte decimalCount, DbfActualColumnType actualColumnType)
        {
            this.Index            = index;
            this.FieldDataAddress = 0;
            this.Name             = name;
            this.ColumnType       = columnType;
            this.Length           = length;
            this.DecimalCount     = decimalCount;
            this.Reserved         = null;

            this.ActualColumnType = actualColumnType;
        }

        [CLSCompliant(false)]
        public static async Task<DbfColumn> ReadAsync(IDbfTableType tableType, AsyncBinaryReader reader, Int32 index)
        {
            String name = await ReadNameAsync( reader ).ConfigureAwait(false);
            if( name == null ) return null;

            Byte       columnType       = await reader.ReadByteAsync()   .ConfigureAwait(false);
            UInt32     fieldDataAddress = await reader.ReadUInt32Async() .ConfigureAwait(false);
            Byte       length           = await reader.ReadByteAsync()   .ConfigureAwait(false);
            Byte       decimalCount     = await reader.ReadByteAsync()   .ConfigureAwait(false);
            Byte[]     reserved         = await reader.ReadBytesAsync(14).ConfigureAwait(false);

            DbfColumnType columnType2 = (DbfColumnType)columnType;
            DbfActualColumnType actualColumnType = tableType.GetActualColumnType( columnType2 );

            return new DbfColumn( index, fieldDataAddress, name, columnType2, length, decimalCount, actualColumnType, reserved );
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

        public static DbfColumn Read(IDbfTableType tableType, BinaryReader reader, Int32 index)
        {
            if( tableType == null ) throw new ArgumentNullException(nameof(tableType));
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            String name = ReadName( reader );
            if( name == null ) return null;

            Byte        columnType       = reader.ReadByte();
            UInt32      fieldDataAddress = reader.ReadUInt32(); // ignore field data address
            Byte        length           = reader.ReadByte();
            Byte        decimalCount     = reader.ReadByte();
            Byte[]      reserved         = reader.ReadBytes(14); // skip the reserved bytes

            DbfColumnType columnType2 = (DbfColumnType)columnType;
            DbfActualColumnType actualColumnType = tableType.GetActualColumnType( columnType2 );

            return new DbfColumn( index, fieldDataAddress, name, columnType2, length, decimalCount, actualColumnType, reserved );
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