using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Overby.Extensions.AsyncBinaryReaderWriter;

namespace Dbf
{
    /// <summary>This class is thread-safe... for now....</summary>
    public class ValueReader
    {
        public static ValueReader Instance { get; } = new ValueReader();

        // TODO: Async versions of these...

        #region Switch

        public Object ReadValue(DbfColumn column, BinaryReader reader, Encoding encoding)
        {
            Object couldBeNull = this.ReadValueInner( column, reader, encoding );
            if( Object.ReferenceEquals( null, couldBeNull ) ) return DBNull.Value;
            return couldBeNull;
        }

        protected virtual Object ReadValueInner(DbfColumn column, BinaryReader reader, Encoding encoding)
        {
            if( column == null ) throw new ArgumentNullException(nameof(column));

            switch( column.ColumnType )
            {
                case DbfColumnType.Boolean       : return this.ReadBoolean( column, reader );
                case DbfColumnType.Character     : return this.ReadCharacter( column, reader, encoding );
                case DbfColumnType.Currency      : return this.ReadCurrency( column, reader );
                case DbfColumnType.Date          : return this.ReadDate( column, reader );
                case DbfColumnType.Timestamp     : return this.ReadTimestamp( column, reader );
                case DbfColumnType.DateTime      : return this.ReadDateTime( column, reader );
                case DbfColumnType.Double        : return this.ReadDouble( column, reader );
                case DbfColumnType.Float         : return this.ReadFloat( column, reader );
                case DbfColumnType.General       : return this.ReadGeneral( column, reader );
                case DbfColumnType.Memo          : return this.ReadMemo( column, reader );
                case DbfColumnType.Number        : return this.ReadNumber( column, reader );
                case DbfColumnType.AutoIncrement : return this.ReadAutoIncrement( column, reader );
                case DbfColumnType.SignedLong    : return this.ReadSignedLong( column, reader );

                case DbfColumnType.DoubleOrBinary: // Make this a case that subclasses can handle in their override of ReadValueInner, or make it a virtual method? The problem is not knowing the true type and having a method return type of Object is just ugly...
                default:
                    throw new NotImplementedException();
            }
        }

        #endregion

        protected static String ReadAsciiString(BinaryReader reader, Int32 length)
        {
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            // NOTE: Previously this used an instance-readonly reusable buffer, but it isn't thread-safe, and the performance gain (if any) probably isn't worthwhile.
            // Unless, perhaps, thread-local storage?
            Byte[] buffer = new Byte[ length ];
            Int32 bytesRead = reader.Read( buffer, 0, length );
            return Encoding.ASCII.GetString( buffer, 0, bytesRead );
        }

        [CLSCompliant(false)]
        protected static async Task<String> ReadAsciiStringAsync(AsyncBinaryReader reader, Int32 length)
        {
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            Byte[] buffer = new Byte[ length ];
            Int32 bytesRead = await reader.ReadAsync( buffer, 0, length ).ConfigureAwait(false);
            return Encoding.ASCII.GetString( buffer, 0, bytesRead );
        }

        #region Text and Bool

        private static readonly Char[] _textPaddingChars = new Char[] { '\0', ' ' };

        public virtual String ReadCharacter(DbfColumn column, BinaryReader reader, Encoding encoding)
        {
            if( column == null ) throw new ArgumentNullException(nameof(column));
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            UInt16 length = (UInt16)(( column.DecimalCount << 8 ) | column.Length); // FoxPro stores the high-byte in DecimalCount
            Byte[] text = reader.ReadBytes( length );
            return this.ReadCharacter( text, encoding );
        }

        [CLSCompliant(false)]
        public virtual async Task<String> ReadCharacterAsync(DbfColumn column, AsyncBinaryReader reader, Encoding encoding)
        {
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            UInt16 length = (UInt16)(( column.DecimalCount << 8 ) | column.Length); // FoxPro stores the high-byte in DecimalCount
            Byte[] text = await reader.ReadBytesAsync( length ).ConfigureAwait(false);
            return this.ReadCharacter( text, encoding );
        }

        protected virtual String ReadCharacter(Byte[] text, Encoding encoding)
        {
            if( encoding == null ) throw new ArgumentNullException(nameof(encoding));

            String textStr = encoding.GetString( text );
            String trimmed = textStr.TrimEnd( _textPaddingChars );

            return trimmed;
        }

        public virtual Boolean? ReadBoolean(DbfColumn column, BinaryReader reader)
        {
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            Char c = (Char)reader.ReadByte();
            return this.ReadBoolean( c );
        }

        [CLSCompliant(false)]
        public virtual async Task<Boolean?> ReadBooleanAsync(DbfColumn column, AsyncBinaryReader reader)
        {
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            Char c = (Char)await reader.ReadByteAsync().ConfigureAwait(false);
            return this.ReadBoolean( c );
        }

        protected virtual Boolean? ReadBoolean(Char characterValue)
        {
            switch( characterValue )
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

        public virtual DateTime? ReadDate(DbfColumn column, BinaryReader reader)
        {
            String dateStr = ReadAsciiString( reader, 8 );
            if( String.IsNullOrWhiteSpace( dateStr ) ) return null;

            DateTime value = DateTime.ParseExact( dateStr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None );
            return value;
        }

        public virtual DateTime? ReadTimestamp(DbfColumn column, BinaryReader reader)
        {
            return this.ReadDateTime( column, reader );
        }

        public virtual DateTime? ReadDateTime(DbfColumn column, BinaryReader reader)
        {
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            // bytes 0-3: date: little-endian 32-bit integer Julian day number.
            // bytes 4-7: time: milliseconds since midnight

            // Julian day number: days since some epoch, but the value 2299161 is 1582-10-15 on the Gregorian calendar (which is 1852-10-04 in Julian).

            Int32 days = reader.ReadInt32();
            Int32 time = reader.ReadInt32();

            Int32 daysSince2299161 = days - 2299161;
            if( daysSince2299161 < 0 ) throw new InvalidOperationException("Invalid DateTime value.");

            DateTime date = _julianDay2299161Local.AddDays( daysSince2299161 );
            DateTime dateTime = date.AddMilliseconds( time );
            return dateTime;
        }

        #endregion

        #region Numbers

        public virtual Decimal? ReadCurrency(DbfColumn column, BinaryReader reader)
        {
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            throw new NotImplementedException();
        }

        public virtual Double? ReadDouble(DbfColumn column, BinaryReader reader)
        {
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            return reader.ReadDouble();
        }
        
        public virtual Single? ReadFloat(DbfColumn column, BinaryReader reader)
        {
            String value = ReadAsciiString( reader, 20 );
            if( String.IsNullOrWhiteSpace( value ) ) return null;

            return Single.Parse( value, NumberStyles.Any, CultureInfo.InvariantCulture );
        }

        /// <summary>Returns null, Int32, or Decimal.</summary>
        public virtual Object ReadNumber(DbfColumn column, BinaryReader reader)
        {
            if( column == null ) throw new ArgumentNullException(nameof(column));

            if( column.Length > 20 ) throw new InvalidOperationException("Number columns cannot exceed 20 characters.");

            // TODO: Handle Column.DecimalCount? Though the original implementation didn't use it...

            String value = ReadAsciiString( reader, column.Length );
            if( String.IsNullOrWhiteSpace( value ) ) return null;
            if( Int32.TryParse( value, NumberStyles.Integer, CultureInfo.InvariantCulture, out Int32 integerNumber ) ) return integerNumber;

            return Decimal.Parse( value, NumberStyles.Any, CultureInfo.InvariantCulture );
        }

        public virtual Int32? ReadAutoIncrement(DbfColumn column, BinaryReader reader)
        {
            return this.ReadSignedLong( column, reader );
        }

        public virtual Int32? ReadSignedLong(DbfColumn column, BinaryReader reader)
        {
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            // Int32
            return reader.ReadInt32();
        }

        #endregion

        #region Blobs

        public virtual Object ReadGeneral(DbfColumn column, BinaryReader reader)
        {
            // Binary
            throw new NotImplementedException();
        }

        public virtual Object ReadMemo(DbfColumn column, BinaryReader reader)
        {
            // Binary
            throw new NotImplementedException();
        }

        #endregion
    }

    public class FoxProValueReader : ValueReader
    {
        public new static FoxProValueReader Instance { get; } = new FoxProValueReader();

        protected override Object ReadValueInner(DbfColumn column, BinaryReader reader, Encoding encoding)
        {
            if( column == null ) throw new ArgumentNullException(nameof(column));
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            if( column.ColumnType == DbfColumnType.DoubleOrBinary )
            {
                return reader.ReadInt16();
            }
            else
            {
                return base.ReadValueInner( column, reader, encoding );
            }
        }
    }

    public class DBase5ValueReader : ValueReader
    {
        public new static DBase5ValueReader Instance { get; } = new DBase5ValueReader();

        protected override Object ReadValueInner(DbfColumn column, BinaryReader reader, Encoding encoding)
        {
            if( column == null ) throw new ArgumentNullException(nameof(column));
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            if( column.ColumnType == DbfColumnType.DoubleOrBinary )
            {
                throw new NotImplementedException();
            }
            else
            {
                return base.ReadValueInner( column, reader, encoding );
            }
        }
    }
}
