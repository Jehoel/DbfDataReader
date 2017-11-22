using System;
using System.IO;
using System.Threading.Tasks;
using Overby.Extensions.AsyncBinaryReaderWriter;

namespace DbfDataReader
{
    /// <summary>Immutable DBF header record.</summary>
    public class DbfHeader
    {
        public byte     Version      { get; }
        public DateTime UpdatedAt    { get; }
        public long     RecordCount  { get; }
        public int      HeaderLength { get; }
        public int      RecordLength { get; }

        public DbfHeader(byte version, DateTime updatedAt, long recordCount, int headerLength, int recordLength)
        {
            this.Version      = version;
            this.UpdatedAt    = updatedAt;
            this.RecordCount  = recordCount;
            this.HeaderLength = headerLength;
            this.RecordLength = recordLength;
        }

        private static DbfHeader Create(byte version, byte updatedYear, byte updatedMonth, byte updatedDay, long recordCount, int headerLength, int recordLength)
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
            byte   version      = reader.ReadByte();
            byte   dateYear     = reader.ReadByte();
            byte   dateMonth    = reader.ReadByte();
            byte   dateDay      = reader.ReadByte();
            uint   recordCount  = reader.ReadUInt32();
            ushort headerLength = reader.ReadUInt16();
            ushort recordLength = reader.ReadUInt16();

            reader.ReadBytes(20); // skip the reserved bytes

            return Create( version, dateYear, dateMonth, dateDay, recordCount, headerLength, recordLength );
        }

        [CLSCompliant(false)]
        public static async Task<DbfHeader> ReadAsync(AsyncBinaryReader reader)
        {
            byte   version      = await reader.ReadByteAsync()  .ConfigureAwait(false);
            byte   dateYear     = await reader.ReadByteAsync()  .ConfigureAwait(false);
            byte   dateMonth    = await reader.ReadByteAsync()  .ConfigureAwait(false);
            byte   dateDay      = await reader.ReadByteAsync()  .ConfigureAwait(false);
            uint   recordCount  = await reader.ReadUInt32Async().ConfigureAwait(false);;
            ushort headerLength = await reader.ReadUInt16Async().ConfigureAwait(false);;
            ushort recordLength = await reader.ReadUInt16Async().ConfigureAwait(false);;

            await reader.ReadBytesAsync(20).ConfigureAwait(false); // skip the reserved bytes

            return Create( version, dateYear, dateMonth, dateDay, recordCount, headerLength, recordLength );
        }

        public string VersionDescription
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

        public bool IsFoxPro => this.Version == 0x30 || this.Version == 0x31 || this.Version == 0xF5 || this.Version == 0xFB;
    }
}