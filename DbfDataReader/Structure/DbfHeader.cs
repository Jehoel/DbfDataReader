using System;
using System.IO;
using System.Threading.Tasks;
using Overby.Extensions.AsyncBinaryReaderWriter;

namespace DbfDataReader
{
    /// <summary>Immutable DBF header record.</summary>
    public class DbfHeader
    {
        public Byte     Version      { get; }
        public DateTime UpdatedAt    { get; }
        public Int64    RecordCount  { get; }
        public Int32    HeaderLength { get; }
        public Int32    RecordLength { get; } // Value = (Real record length) + 1 Byte for the DbfRecordStatus.

        public Int32    RecordDataLength => this.RecordLength - 1;

        public const Byte HeaderTerminator = 0x0D;

        public DbfHeader(Byte version, DateTime updatedAt, Int64 recordCount, Int32 headerLength, Int32 recordLength)
        {
            this.Version      = version;
            this.UpdatedAt    = updatedAt;
            this.RecordCount  = recordCount;
            this.HeaderLength = headerLength;
            this.RecordLength = recordLength;
        }

        private static DbfHeader Create(Byte version, Byte updatedYear, Byte updatedMonth, Byte updatedDay, Int64 recordCount, Int32 headerLength, Int32 recordLength)
        {
            return new DbfHeader(
                version,
                new DateTime( 1900 + updatedYear, updatedMonth, updatedDay ),
                recordCount,
                headerLength,
                recordLength
            );
        }

        public static DbfHeader Read(BinaryReader reader)
        {
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            Byte   version      = reader.ReadByte();
            Byte   dateYear     = reader.ReadByte();
            Byte   dateMonth    = reader.ReadByte();
            Byte   dateDay      = reader.ReadByte();
            uint   recordCount  = reader.ReadUInt32();
            ushort headerLength = reader.ReadUInt16();
            ushort recordLength = reader.ReadUInt16();

            reader.ReadBytes(20); // skip the reserved Bytes

            return Create( version, dateYear, dateMonth, dateDay, recordCount, headerLength, recordLength );
        }

        [CLSCompliant(false)]
        public static async Task<DbfHeader> ReadAsync(AsyncBinaryReader reader)
        {
            Byte   version      = await reader.ReadByteAsync()  .ConfigureAwait(false);
            Byte   dateYear     = await reader.ReadByteAsync()  .ConfigureAwait(false);
            Byte   dateMonth    = await reader.ReadByteAsync()  .ConfigureAwait(false);
            Byte   dateDay      = await reader.ReadByteAsync()  .ConfigureAwait(false);
            uint   recordCount  = await reader.ReadUInt32Async().ConfigureAwait(false);;
            ushort headerLength = await reader.ReadUInt16Async().ConfigureAwait(false);;
            ushort recordLength = await reader.ReadUInt16Async().ConfigureAwait(false);;

            await reader.ReadBytesAsync(20).ConfigureAwait(false); // skip the reserved Bytes

            return Create( version, dateYear, dateMonth, dateDay, recordCount, headerLength, recordLength );
        }

        public String VersionDescription
        {
            get
            {
                switch( this.Version )
                {
                    case 0x02: return "FoxPro";
                    case 0x03: return "dBase III without memo file";
                    case 0x04: return "dBase IV without memo file";
                    case 0x05: return "dBase V without memo file";
                    case 0x07: return "Visual Objects 1.x";
                    case 0x30: return "Visual FoxPro";
                    case 0x31: return "Visual FoxPro with AutoIncrement field";
                    case 0x43: return "dBASE IV SQL table files, no memo";
                    case 0x63: return "dBASE IV SQL system files, no memo";
                    case 0x7b: return "dBase IV with memo file";
                    case 0x83: return "dBase III with memo file";
                    case 0x87: return "Visual Objects 1.x with memo file";
                    case 0x8b: return "dBase IV with memo file";
                    case 0x8e: return "dBase IV with SQL table";
                    case 0xcb: return "dBASE IV SQL table files, with memo";
                    case 0xf5: return "FoxPro with memo file";
                    case 0xfb: return "FoxPro without memo file";
                    default  : return "Unknown";
                }
            }
        }

        public Boolean IsFoxPro => this.Version == 0x30 || this.Version == 0x31 || this.Version == 0xF5 || this.Version == 0xFB;
    }
}