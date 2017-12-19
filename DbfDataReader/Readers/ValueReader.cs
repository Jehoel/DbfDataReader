using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Overby.Extensions.AsyncBinaryReaderWriter;

namespace DbfDataReader
{
    /// <summary>This class is thread-safe... for now....</summary>
    public class ValueReader
    {
        public static ValueReader Instance { get; } = new ValueReader();

        // TODO: Async versions of these...

        #region Switch

        public Object ReadValue(DbfColumn column, BinaryReader rdr, Encoding encoding)
        {
            Object couldBeNull = this.ReadValueInner( column, rdr, encoding );
            if( Object.ReferenceEquals( null, couldBeNull ) ) return DBNull.Value;
            return couldBeNull;
        }

        protected virtual Object ReadValueInner(DbfColumn column, BinaryReader rdr, Encoding encoding)
        {
            switch( column.ColumnType )
            {
                case DbfColumnType.Boolean       : return this.ReadBoolean( column, rdr );
                case DbfColumnType.Character     : return this.ReadCharacter( column, rdr, encoding );
                case DbfColumnType.Currency      : return this.ReadCurrency( column, rdr );
                case DbfColumnType.Date          : return this.ReadDate( column, rdr );
                case DbfColumnType.Timestamp     : return this.ReadTimestamp( column, rdr );
                case DbfColumnType.DateTime      : return this.ReadDateTime( column, rdr );
                case DbfColumnType.Double        : return this.ReadDouble( column, rdr );
                case DbfColumnType.Float         : return this.ReadFloat( column, rdr );
                case DbfColumnType.General       : return this.ReadGeneral( column, rdr );
                case DbfColumnType.Memo          : return this.ReadMemo( column, rdr );
                case DbfColumnType.Number        : return this.ReadNumber( column, rdr );
                case DbfColumnType.Autoincrement : return this.ReadAutoincrement( column, rdr );
                case DbfColumnType.SignedLong    : return this.ReadSignedLong( column, rdr );

                case DbfColumnType.DoubleOrBinary: // Make this a case that subclasses can handle in their override of ReadValueInner, or make it a virtual method? The problem is not knowing the true type and having a method return type of Object is just ugly...
                default:
                    throw new NotImplementedException();
            }
        }

        #endregion

        private String ReadAsciiString(BinaryReader rdr, Int32 length)
        {
            // NOTE: Previously this used an instance-readonly reusable buffer, but it isn't thread-safe, and the performance gain (if any) probably isn't worthwhile.
            // Unless, perhaps, thread-local storage?
            Byte[] buffer = new Byte[ length ];
            Int32 bytesRead = rdr.Read( buffer, 0, length );
            return Encoding.ASCII.GetString( buffer, 0, bytesRead );
        }

        private async Task<String> ReadAsciiStringAsync(AsyncBinaryReader rdr, Int32 length)
        {
            Byte[] buffer = new Byte[ length ];
            Int32 bytesRead = await rdr.ReadAsync( buffer, 0, length ).ConfigureAwait(false);
            return Encoding.ASCII.GetString( buffer, 0, bytesRead );
        }

        #region Text and Bool

        private static readonly Char[] _textPaddingChars = new Char[] { '\0', ' ' };

        public virtual String ReadCharacter(DbfColumn column, BinaryReader rdr, Encoding encoding)
        {
            UInt16 length = (UInt16)(( column.DecimalCount << 8 ) | column.Length); // FoxPro stores the high-byte in DecimalCount
            Byte[] text = rdr.ReadBytes( length );
            return this.ReadCharacter( text, encoding );
        }

        public virtual async Task<String> ReadCharacterAsync(DbfColumn column, AsyncBinaryReader rdr, Encoding encoding)
        {
            UInt16 length = (UInt16)(( column.DecimalCount << 8 ) | column.Length); // FoxPro stores the high-byte in DecimalCount
            Byte[] text = await rdr.ReadBytesAsync( length ).ConfigureAwait(false);
            return this.ReadCharacter( text, encoding );
        }

        protected virtual String ReadCharacter(Byte[] text, Encoding encoding)
        {
            String textStr = encoding.GetString( text );
            String trimmed = textStr.TrimEnd( _textPaddingChars );

            return trimmed;
        }

        public virtual Boolean? ReadBoolean(DbfColumn column, BinaryReader rdr)
        {
            Char c = (Char)rdr.ReadByte();
            return this.ReadBoolean( c );
        }

        public virtual async Task<Boolean?> ReadBooleanAsync(DbfColumn column, AsyncBinaryReader rdr)
        {
            Char c = (Char)await rdr.ReadByteAsync().ConfigureAwait(false);
            return this.ReadBoolean( c );
        }

        protected virtual Boolean? ReadBoolean(Char c)
        {
            switch( c )
            {
                case 'Y':
                case 'y':
                case 'T':
                case 't':
                    return true;
                case 'N':
                case 'n':
                case 'F':
                case 'f':
                    return false;
                case '?':
                case ' ':
                    return null;
                default:
                    throw new InvalidOperationException("Invalid value for Boolean column type."); // TODO: Have a specific exception for this.
            }
        }

        #endregion

        #region Dates/DateTimes

        private static readonly DateTime _julianDay2299161Local = new DateTime( 1582, 10, 15, 0, 0, 0, 0, DateTimeKind.Local );
        private static readonly DateTime _julianDay2299161Utc   = new DateTime( 1582, 10, 15, 0, 0, 0, 0, DateTimeKind.Utc   );

        public virtual DateTime? ReadDate(DbfColumn column, BinaryReader rdr)
        {
            String dateStr = this.ReadAsciiString( rdr, 8 );
            if( String.IsNullOrWhiteSpace( dateStr ) ) return null;

            DateTime value = DateTime.ParseExact( dateStr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None );
            return value;
        }

        public virtual DateTime? ReadTimestamp(DbfColumn column, BinaryReader rdr)
        {
            return this.ReadDateTime( column, rdr );
        }

        public virtual DateTime? ReadDateTime(DbfColumn column, BinaryReader rdr)
        {
            // bytes 0-3: date: little-endian 32-bit integer Julian day number.
            // bytes 4-7: time: milliseconds since midnight

            // Julian day number: days since some epoch, but the value 2299161 is 1582-10-15 on the Gregorian calendar (which is 1852-10-04 in Julian).

            Int32 days = rdr.ReadInt32();
            Int32 time = rdr.ReadInt32();

            Int32 daysSince2299161 = days - 2299161;
            if( daysSince2299161 < 0 ) throw new InvalidOperationException("Invalid DateTime value.");

            DateTime date = _julianDay2299161Local.AddDays( daysSince2299161 );
            DateTime dateTime = date.AddMilliseconds( time );
            return dateTime;
        }

        #endregion

        #region Numbers

        public virtual Decimal? ReadCurrency(DbfColumn column, BinaryReader rdr)
        {
            throw new NotImplementedException();
        }

        public virtual Double? ReadDouble(DbfColumn column, BinaryReader rdr)
        {
            return rdr.ReadDouble();
        }
        
        public virtual Single? ReadFloat(DbfColumn column, BinaryReader rdr)
        {
            String value = this.ReadAsciiString( rdr, 20 );
            if( String.IsNullOrWhiteSpace( value ) ) return null;

            return Single.Parse( value, NumberStyles.Any, CultureInfo.InvariantCulture );
        }

        public virtual Decimal? ReadNumber(DbfColumn column, BinaryReader rdr)
        {
            if( column.Length > 20 ) throw new InvalidOperationException("Number columns cannot exceed 20 characters.");

            String value = this.ReadAsciiString( rdr, column.Length );
            if( String.IsNullOrWhiteSpace( value ) ) return null;

            return Decimal.Parse( value, NumberStyles.Any, CultureInfo.InvariantCulture );
        }

        public virtual Int32? ReadAutoincrement(DbfColumn column, BinaryReader rdr)
        {
            return this.ReadSignedLong( column, rdr );
        }

        public virtual Int32? ReadSignedLong(DbfColumn column, BinaryReader rdr)
        {
            // Int32
            return rdr.ReadInt32();
        }

        #endregion

        #region Blobs

        public virtual Object ReadGeneral(DbfColumn column, BinaryReader rdr)
        {
            // Binary
            throw new NotImplementedException();
        }

        public virtual Object ReadMemo(DbfColumn column, BinaryReader rdr)
        {
            // Binary
            throw new NotImplementedException();
        }

        #endregion
    }

    public class FoxProValueReader : ValueReader
    {
        public new static FoxProValueReader Instance { get; } = new FoxProValueReader();

        protected override Object ReadValueInner(DbfColumn column, BinaryReader rdr, Encoding encoding)
        {
            if( column.ColumnType == DbfColumnType.DoubleOrBinary )
            {
                return rdr.ReadInt16();
            }
            else
            {
                return base.ReadValueInner( column, rdr, encoding );
            }
        }
    }

    public class DBase5ValueReader : ValueReader
    {
        public new static DBase5ValueReader Instance { get; } = new DBase5ValueReader();

        protected override Object ReadValueInner(DbfColumn column, BinaryReader rdr, Encoding encoding)
        {
            if( column.ColumnType == DbfColumnType.DoubleOrBinary )
            {
                throw new NotImplementedException();
            }
            else
            {
                return base.ReadValueInner( column, rdr, encoding );
            }
        }
    }
}
