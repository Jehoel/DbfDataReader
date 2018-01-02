using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Overby.Extensions.AsyncBinaryReaderWriter;

namespace Dbf
{
	public static partial class ValueReader
	{
        private static async Task<Boolean?> ReadBooleanTextAsync(DbfColumn column, AsyncBinaryReader reader)
        {
            Byte b = await reader.ReadByteAsync().ConfigureAwait(false);
            Char c = (Char)b;
            return ParseBoolean( c );
        }


        private static async Task<Byte[]> ReadByteArrayAsync(DbfColumn column, AsyncBinaryReader reader)
        {
            return await reader.ReadBytesAsync( column.Length ).ConfigureAwait(false);
        }


        private static async Task<DateTime?> ReadDateTextAsync(DbfColumn column, AsyncBinaryReader reader)
        {
            // TODO: If it has a Length of 6, does that mean it's "yyMMdd" format?
            AssertColumn( column, 8, 0 );

            String dateStr = await ReadAsciiStringAsync( reader, column.Length ).ConfigureAwait(false);
            if( String.IsNullOrWhiteSpace( dateStr ) ) return null;

            DateTime value = DateTime.ParseExact( dateStr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None );
            return value;
        }


        private static async Task<DateTime?> ReadDateTimeBinaryJulianAsync(DbfColumn column, AsyncBinaryReader reader)
        {
            AssertColumn( column, 8, 0 );

            // bytes 0-3: date: little-endian 32-bit integer Julian day number.
            // bytes 4-7: time: milliseconds since midnight

            // Julian day number: days since some epoch, but the value 2299161 is 1582-10-15 on the Gregorian calendar (which is 1852-10-04 in Julian).

            Int32 days = await reader.ReadInt32Async().ConfigureAwait(false);
            Int32 time = await reader.ReadInt32Async().ConfigureAwait(false);

            Int32 daysSince2299161 = days - 2299161;
            if( daysSince2299161 < 0 ) throw new InvalidOperationException("Invalid DateTime value.");

            DateTime date = _julianDay2299161.AddDays( daysSince2299161 );
            DateTime dateTime = date.AddMilliseconds( time );
            return dateTime;
        }


        private static async Task<Single?> ReadFloatSingleAsync(DbfColumn column, AsyncBinaryReader reader)
        {
            AssertColumn( column, 4, 0 );
            return await reader.ReadSingleAsync().ConfigureAwait(false);
        }


        private static async Task<Double?> ReadFloatDoubleAsync(DbfColumn column, AsyncBinaryReader reader)
        {
            AssertColumn( column, 8, 0 );
            return await reader.ReadDoubleAsync().ConfigureAwait(false);
        }


        private static async Task<Int16?> ReadInt16Async(DbfColumn column, AsyncBinaryReader reader)
        {
            AssertColumn( column, 2, 0 );
            return await reader.ReadInt16Async().ConfigureAwait(false);
        }


        private static async Task<UInt16?> ReadUInt16Async(DbfColumn column, AsyncBinaryReader reader)
        {
            AssertColumn( column, 2, 0 );
            return await reader.ReadUInt16Async().ConfigureAwait(false);
        }


        private static async Task<Int32?> ReadInt32Async(DbfColumn column, AsyncBinaryReader reader)
        {
            AssertColumn( column, 4, 0 );
            return await reader.ReadInt32Async().ConfigureAwait(false);
        }


        private static async Task<UInt32?> ReadUInt32Async(DbfColumn column, AsyncBinaryReader reader)
        {
            AssertColumn( column, 4, 0 );
            return await reader.ReadUInt32Async().ConfigureAwait(false);
        }


        private static async Task<Int64?> ReadInt64Async(DbfColumn column, AsyncBinaryReader reader)
        {
            AssertColumn( column, 8, 0 );
            return await reader.ReadInt64Async().ConfigureAwait(false);
        }


        private static async Task<UInt64?> ReadUInt64Async(DbfColumn column, AsyncBinaryReader reader)
        {
            AssertColumn( column, 8, 0 );
            return await reader.ReadUInt64Async().ConfigureAwait(false);
        }

        private static async Task<Decimal?> ReadNumberTextAsync(DbfColumn column, AsyncBinaryReader reader)
        {
            AssertColumn( column, expectedDecimalCount: 0 ); // TODO: How is DecimalCount handled for NumberText columns?
            if( column.Length > 20 ) throw new InvalidOperationException("Number columns cannot exceed 20 characters.");

            String value = await ReadAsciiStringAsync( reader, column.Length ).ConfigureAwait(false);
            if( String.IsNullOrWhiteSpace( value ) ) return null;
            return Decimal.Parse( value, NumberStyles.Any, CultureInfo.InvariantCulture );
        }


        private static async Task<String> ReadTextAsync(DbfColumn column, AsyncBinaryReader reader, Encoding encoding)
        {
            AssertColumn( column, expectedDecimalCount: 0 );

            Byte[] text = await reader.ReadBytesAsync( column.Length ).ConfigureAwait(false);
            
            String textStr = encoding.GetString( text );
            String trimmed = textStr.TrimEnd( _textPaddingChars );

            return trimmed;
        }


        private static async Task<String> ReadTextLongAsync(DbfColumn column, AsyncBinaryReader reader, Encoding encoding)
        {
            Int32  length = ( column.DecimalCount << 8 ) | column.Length;
            Byte[] text = await reader.ReadBytesAsync( length ).ConfigureAwait(false);
            
            String textStr = encoding.GetString( text );
            String trimmed = textStr.TrimEnd( _textPaddingChars );

            return trimmed;
        }


	}
}


