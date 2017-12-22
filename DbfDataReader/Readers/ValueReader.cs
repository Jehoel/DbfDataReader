using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Overby.Extensions.AsyncBinaryReaderWriter;

namespace Dbf
{
    public interface IDbfTableType
    {
        DbfActualColumnType GetActualColumnType(DbfColumnType type);

        DbfMemoFile OpenMemoFile(String tableName);
    }

    /// <summary>This class is thread-safe... for now....</summary>
    public static partial class ValueReader
    {
        #region Switch

        public static Object ReadValue(DbfColumn column, BinaryReader reader, Encoding encoding)
        {
            Object couldBeNull = ReadValueInner( column, reader, encoding );
            if( couldBeNull is null ) return DBNull.Value;
            return couldBeNull;
        }

        private static Object ReadValueInner(DbfColumn column, BinaryReader reader, Encoding encoding)
        {
            if( column == null ) throw new ArgumentNullException(nameof(column));

            switch( column.ActualColumnType )
            {
                case DbfActualColumnType.BooleanText         : return ReadBooleanText( column, reader );
                case DbfActualColumnType.ByteArray           : return ReadByteArray( column, reader );
                case DbfActualColumnType.DateText            : return ReadDateText( column, reader );
                case DbfActualColumnType.DateTimeBinaryJulian: return ReadDateTimeBinaryJulian( column, reader );
                case DbfActualColumnType.FloatSingle         : return ReadFloatSingle( column, reader );
                case DbfActualColumnType.FloatDouble         : return ReadFloatDouble( column, reader );
                case DbfActualColumnType.Int16               : return ReadInt16( column, reader );
                case DbfActualColumnType.Int32               : return ReadInt32( column, reader );
                case DbfActualColumnType.Int64               : return ReadInt64( column, reader );
                case DbfActualColumnType.UInt16              : return ReadUInt16( column, reader );
                case DbfActualColumnType.UInt32              : return ReadUInt32( column, reader );
                case DbfActualColumnType.UInt64              : return ReadUInt64( column, reader );
                case DbfActualColumnType.MemoByteArray       : return ReadMemoByteArray( column, reader );
                case DbfActualColumnType.MemoText            : return ReadMemoText( column, reader );
                case DbfActualColumnType.NumberText          : return ReadNumberText( column, reader );
                case DbfActualColumnType.Text                : return ReadText( column, reader, encoding );
                case DbfActualColumnType.TextLong            : return ReadTextLong( column, reader, encoding );
                default:
                    throw new NotImplementedException();
            }
        }

        public static async Task<Object> ReadValueAsync(DbfColumn column, AsyncBinaryReader reader, Encoding encoding)
        {
            Object couldBeNull = await ReadValueInnerAsync( column, reader, encoding ).ConfigureAwait(false);
            if( couldBeNull is null ) return DBNull.Value;
            return couldBeNull;
        }

        private static Task<Object> ReadValueInnerAsync(DbfColumn column, AsyncBinaryReader reader, Encoding encoding)
        {
            if( column == null ) throw new ArgumentNullException(nameof(column));

            switch( column.ActualColumnType )
            {
                case DbfActualColumnType.BooleanText         : return ReadBooleanTextAsync         ( column, reader           ).ContinueWith( ToObject );
                case DbfActualColumnType.ByteArray           : return ReadByteArrayAsync           ( column, reader           ).ContinueWith( ToObject );
                case DbfActualColumnType.DateText            : return ReadDateTextAsync            ( column, reader           ).ContinueWith( ToObject );
                case DbfActualColumnType.DateTimeBinaryJulian: return ReadDateTimeBinaryJulianAsync( column, reader           ).ContinueWith( ToObject );
                case DbfActualColumnType.FloatSingle         : return ReadFloatSingleAsync         ( column, reader           ).ContinueWith( ToObject );
                case DbfActualColumnType.FloatDouble         : return ReadFloatDoubleAsync         ( column, reader           ).ContinueWith( ToObject );
                case DbfActualColumnType.Int16               : return ReadInt16Async               ( column, reader           ).ContinueWith( ToObject );
                case DbfActualColumnType.Int32               : return ReadInt32Async               ( column, reader           ).ContinueWith( ToObject );
                case DbfActualColumnType.Int64               : return ReadInt64Async               ( column, reader           ).ContinueWith( ToObject );
                case DbfActualColumnType.UInt16              : return ReadUInt16Async              ( column, reader           ).ContinueWith( ToObject );
                case DbfActualColumnType.UInt32              : return ReadUInt32Async              ( column, reader           ).ContinueWith( ToObject );
                case DbfActualColumnType.UInt64              : return ReadUInt64Async              ( column, reader           ).ContinueWith( ToObject );
                case DbfActualColumnType.MemoByteArray       : return ReadMemoByteArrayAsync       ( column, reader           ).ContinueWith( ToObject );
                case DbfActualColumnType.MemoText            : return ReadMemoTextAsync            ( column, reader           ).ContinueWith( ToObject );
                case DbfActualColumnType.NumberText          : return ReadNumberTextAsync          ( column, reader           ).ContinueWith( ToObject );
                case DbfActualColumnType.Text                : return ReadTextAsync                ( column, reader, encoding ).ContinueWith( ToObject );
                case DbfActualColumnType.TextLong            : return ReadTextLongAsync            ( column, reader, encoding ).ContinueWith( ToObject );
                default:
                    throw new NotImplementedException();
            }
        }

        private static Object ToObject<T>(Task<T> task)
        {
            return (Object)task.Result;
        }

        #endregion

        private static String ReadAsciiString(BinaryReader reader, Int32 length)
        {
            // NOTE: Previously this used an instance-readonly reusable buffer, but it isn't thread-safe, and the performance gain (if any) probably isn't worthwhile.
            // Unless, perhaps, thread-local storage?

            Byte[] buffer = new Byte[ length ];
            Int32 bytesRead = reader.Read( buffer, 0, length );
            return Encoding.ASCII.GetString( buffer, 0, bytesRead );
        }

        private static async Task<String> ReadAsciiStringAsync(AsyncBinaryReader reader, Int32 length)
        {
            Byte[] buffer = new Byte[ length ];
            Int32 bytesRead = await reader.ReadAsync( buffer, 0, length ).ConfigureAwait(false);
            return Encoding.ASCII.GetString( buffer, 0, bytesRead );
        }

        private static void AssertColumn(DbfColumn column, Int32 expectedLength = -1, Int32 expectedDecimalCount = -1)
        {
            if( expectedLength >= 0 )
            {
                if( column.Length != expectedLength )
                {
                    throw new ArgumentException( "Expected a column length of {0} but encountered {1}.".FormatCurrent( expectedLength, column.Length ) );
                }
            }

            if( expectedDecimalCount >= 0 )
            {
                if( column.DecimalCount != expectedDecimalCount )
                {
                    throw new ArgumentException( "Expected a decimal count of {0} but encountered {1}.".FormatCurrent( expectedDecimalCount, column.DecimalCount ) );
                }
            }
        }

        #region Read Sync

        private static Boolean? ReadBooleanText(DbfColumn column, BinaryReader reader)
        {
            Byte b = reader.ReadByte();
            Char c = (Char)b;
            return ParseBoolean( c );
        }

        private static Boolean? ParseBoolean(Char characterValue)
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

        private static Byte[] ReadByteArray(DbfColumn column, BinaryReader reader)
        {
            return reader.ReadBytes( column.Length );
        }

        private static readonly DateTime _julianDay2299161 = new DateTime( 1582, 10, 15, 0, 0, 0, 0, DateTimeKind.Unspecified );

        private static DateTime? ReadDateText(DbfColumn column, BinaryReader reader)
        {
            // TODO: If it has a Length of 6, does that mean it's "yyMMdd" format?
            AssertColumn( column, 8, 0 );

            String dateStr = ReadAsciiString( reader, column.Length );
            if( String.IsNullOrWhiteSpace( dateStr ) ) return null;

            DateTime value = DateTime.ParseExact( dateStr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None );
            return value;
        }

        private static DateTime? ReadDateTimeBinaryJulian(DbfColumn column, BinaryReader reader)
        {
            AssertColumn( column, 8, 0 );

            // bytes 0-3: date: little-endian 32-bit integer Julian day number.
            // bytes 4-7: time: milliseconds since midnight

            // Julian day number: days since some epoch, but the value 2299161 is 1582-10-15 on the Gregorian calendar (which is 1852-10-04 in Julian).

            Int32 days = reader.ReadInt32();
            Int32 time = reader.ReadInt32();

            Int32 daysSince2299161 = days - 2299161;
            if( daysSince2299161 < 0 ) throw new InvalidOperationException("Invalid DateTime value.");

            DateTime date = _julianDay2299161.AddDays( daysSince2299161 );
            DateTime dateTime = date.AddMilliseconds( time );
            return dateTime;
        }

        private static Single? ReadFloatSingle(DbfColumn column, BinaryReader reader)
        {
            AssertColumn( column, 4, 0 );
            return reader.ReadSingle();
        }

        private static Double? ReadFloatDouble(DbfColumn column, BinaryReader reader)
        {
            AssertColumn( column, 8, 0 );
            return reader.ReadDouble();
        }

        private static Int16? ReadInt16(DbfColumn column, BinaryReader reader)
        {
            AssertColumn( column, 2, 0 );
            return reader.ReadInt16();
        }
        private static UInt16? ReadUInt16(DbfColumn column, BinaryReader reader)
        {
            AssertColumn( column, 2, 0 );
            return reader.ReadUInt16();
        }
        private static Int32? ReadInt32(DbfColumn column, BinaryReader reader)
        {
            AssertColumn( column, 4, 0 );
            return reader.ReadInt32();
        }
        private static UInt32? ReadUInt32(DbfColumn column, BinaryReader reader)
        {
            AssertColumn( column, 4, 0 );
            return reader.ReadUInt32();
        }
        private static Int64? ReadInt64(DbfColumn column, BinaryReader reader)
        {
            AssertColumn( column, 8, 0 );
            return reader.ReadInt64();
        }
        private static UInt64? ReadUInt64(DbfColumn column, BinaryReader reader)
        {
            AssertColumn( column, 8, 0 );
            return reader.ReadUInt64();
        }

        private static MemoBlock ReadMemoByteArray(DbfColumn column, BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        private static MemoBlock ReadMemoText(DbfColumn column, BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        private static Decimal? ReadNumberText(DbfColumn column, BinaryReader reader)
        {
            AssertColumn( column, expectedDecimalCount: 0 ); // TODO: How is DecimalCount handled for NumberText columns?
            if( column.Length > 20 ) throw new InvalidOperationException("Number columns cannot exceed 20 characters.");

            String value = ReadAsciiString( reader, column.Length );
            if( String.IsNullOrWhiteSpace( value ) ) return null;
            return Decimal.Parse( value, NumberStyles.Any, CultureInfo.InvariantCulture );
        }

        private static readonly Char[] _textPaddingChars = new Char[] { '\0', ' ' };

        private static String ReadText(DbfColumn column, BinaryReader reader, Encoding encoding)
        {
            AssertColumn( column, expectedDecimalCount: 0 );

            Byte[] text = reader.ReadBytes( column.Length );
            
            String textStr = encoding.GetString( text );
            String trimmed = textStr.TrimEnd( _textPaddingChars );

            return trimmed;
        }

        private static String ReadTextLong(DbfColumn column, BinaryReader reader, Encoding encoding)
        {
            Int32  length = ( column.DecimalCount << 8 ) | column.Length;
            Byte[] text = reader.ReadBytes( length );
            
            String textStr = encoding.GetString( text );
            String trimmed = textStr.TrimEnd( _textPaddingChars );

            return trimmed;
        }

        #endregion
    }
}
