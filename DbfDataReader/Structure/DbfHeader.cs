using System;
using System.IO;
using System.Threading.Tasks;
using Overby.Extensions.AsyncBinaryReaderWriter;

namespace Dbf
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

        public Boolean IsFoxPro
        {
            get
            {
                switch( this.Version )
                {
                    case DbfVersions.FoxPro: // TODO: This was originally excluded from the IsFoxPro definition. Is there a reason for that? Did the format change between FoxPro and Visual FoxPro?
                    case DbfVersions.FoxProNoMemo:
                    case DbfVersions.FoxProWithMemo:
                    case DbfVersions.VisualFoxPro:
                    case DbfVersions.VisualFoxProWithAutoIncrement:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public Boolean HasMemoFile
        {
            get
            {
                switch( this.Version )
                {
                    case DbfVersions.DBase3WithMemo:
                    case DbfVersions.DBase4SqlWithMemo:
                    case DbfVersions.DBase4WithMemo:
                    case DbfVersions.DBase4WithMemoAlt:
                    case DbfVersions.FoxProWithMemo:
                    case DbfVersions.VisualObjects1XWithMemo:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public DbfVersionFamily VersionFamily
        {
            get
            {
                switch( this.Version )
                {
                    case DbfVersions.FoxPro:
                    case DbfVersions.FoxProNoMemo:
                    case DbfVersions.FoxProWithMemo:
                        return DbfVersionFamily.FoxPro;
                    case DbfVersions.VisualFoxPro:
                    case DbfVersions.VisualFoxProWithAutoIncrement:
                        return DbfVersionFamily.VisualFoxPro;
                    case DbfVersions.DBase3NoMemo:
                    case DbfVersions.DBase3WithMemo:
                        return DbfVersionFamily.DBase3;
                    case DbfVersions.DBase4NoMemo:
                    case DbfVersions.DBase4Sql:
                    case DbfVersions.DBase4SqlNoMemo:
                    case DbfVersions.DBase4SqlSystemNoMemo:
                    case DbfVersions.DBase4SqlWithMemo:
                    case DbfVersions.DBase4WithMemoAlt:
                        return DbfVersionFamily.DBase4;
                    case DbfVersions.DBase5NoMemo:
                        return DbfVersionFamily.DBase5;
                    default:
                        return DbfVersionFamily.Unknown;
                }
            }
        }
    }

    public static class DbfVersions
    {
        public const Byte FoxPro                        = 0x02;
        public const Byte DBase3NoMemo                  = 0x03;
        public const Byte DBase4NoMemo                  = 0x04;
        public const Byte DBase5NoMemo                  = 0x05;
        public const Byte VisualObjects1X               = 0x07;

        public const Byte VisualFoxPro                  = 0x30;
        public const Byte VisualFoxProWithAutoIncrement = 0x31;
        public const Byte DBase4SqlNoMemo               = 0x43;
        public const Byte DBase4SqlSystemNoMemo         = 0x63;
        public const Byte DBase4WithMemo                = 0x7B;

        public const Byte DBase3WithMemo                = 0x83;
        public const Byte VisualObjects1XWithMemo       = 0x87;
        public const Byte DBase4WithMemoAlt             = 0x8B;
        public const Byte DBase4Sql                     = 0x8E; // No information if this type has a memo or not.
        public const Byte DBase4SqlWithMemo             = 0xCB;

        public const Byte FoxProWithMemo                = 0xF5;
        public const Byte FoxProNoMemo                  = 0xFB;
    }

    public enum DbfVersionFamily
    {
        Unknown,
        FoxPro,
        VisualFoxPro, // I'm assuming they're different
        DBase3,
        DBase4,
        DBase5,
        VisualObjects1X
    }

    // https://en.wikipedia.org/wiki/.dbf - has a table showing the bitfield definitions of the 8-bit version byte, interesting!
    // It seems to be from here: http://www.oocities.org/geoff_wass/dBASE/GaryWhite/dBASE/FAQ/qformt.htm
    // https://msdn.microsoft.com/en-us/library/st4a0s68(v=vs.80).aspx
    [Flags]
    public enum DbfVersion
    {
        None       = 0,
        // Bits 0-2 denote version:
        Version1   = 0b0000_0001,
        Version2   = 0b0000_0010,
        Version3   = 0b0000_0011,
        Version4   = 0b0000_0100,
        Version5   = 0b0000_0101,
        Version6   = 0b0000_0110,
        Version7   = 0b0000_0111,

        // Bit 3: dBase memo file:
        HasMemo1   = 0b0000_1000,

        // Bits 4-6: "Presence of a SQL table" - no idea what this means.
        HasSqlTable = 0b

        HasDosMemo = 0b1000_0000,
        Has
    }
}