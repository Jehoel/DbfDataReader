using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace DbfDataReader
{
    public sealed class SyncDbfDataReader : DbfDataReader
    {
        private readonly FileStream   fileStream;
        private readonly BinaryReader binaryReader;
        private          Boolean      isDisposed;
        private          Boolean      isEof;

        private readonly DbfDataReaderOptions options;

        public override Encoding TextEncoding { get; }

        /// <param name="ignoreEof">If true, then a </param>
        internal SyncDbfDataReader(DbfTable table, Boolean randomAccess, Encoding textEncoding, DbfDataReaderOptions options)
            : base( table )
        {
            FileStream stream = Utility.OpenFileForReading( table.File.FullName, randomAccess, async: false );
            if( !stream.CanRead || !stream.CanSeek )
            {
                stream.Dispose();
                throw new InvalidOperationException("The created FileStream could not perform both Read and Seek operations.");
            }

            this.fileStream = stream;

            this.binaryReader = new BinaryReader( this.fileStream, Encoding.ASCII, leaveOpen: true );

            this.TextEncoding = textEncoding;

            this.options = options;
        }

        public override void Close()
        {
            this.binaryReader.Dispose();
            this.fileStream.Dispose();
            this.isDisposed = true;
        }

        public override Boolean IsClosed => this.isDisposed;

        protected override Boolean EOF => this.isEof;

        private Boolean SetEOF()
        {
            if( this.EOF ) return true;

            if( this.binaryReader.BaseStream.Position == this.binaryReader.BaseStream.Length )
            {
                this.isEof = true;
            }

            return this.EOF;
        }

        public override Boolean Read()
        {
            if( this.EOF ) return false;

            DbfReadResult result;
            do
            {
                result = this.ReadImpl();
            }
            while( result == DbfReadResult.Skipped ); // so EOF and Read won't cause a loop iteration.

            return result == DbfReadResult.Read;
        }

        private DbfReadResult ReadImpl()
        {
            if( this.SetEOF() ) return DbfReadResult.Eof;

            Int64 offset = this.binaryReader.BaseStream.Position;

            DbfRecordStatus recordStatus = (DbfRecordStatus)this.binaryReader.ReadByte();
            if( recordStatus == DbfRecordStatus.Deleted )
            {
                if( this.options.HasFlag(DbfDataReaderOptions.AllowDeleted) )
                {
                    // NOOP.
                }
                else
                {
                    this.binaryReader.BaseStream.Seek( this.Table.Header.RecordDataLength, SeekOrigin.Current ); // skip-over those bytes. TODO: Is Seek() better than Read() for data we don't care about? will Seek() trigger Random-access behaviour - or only Seek() that extends beyond the current buffer (or two?) or goes in a backwards direction?
                    return DbfReadResult.Skipped;
                }
            }
            else if( recordStatus == DbfRecordStatus.EOF )
            {
                if( this.options.HasFlag(DbfDataReaderOptions.IgnoreEof) )
                {
                    // Check the stream length. A "real" EOF should follow.
                    if( this.SetEOF() ) return DbfReadResult.Eof;

                    // Else, NOOP and read as normal, though the data is probably garbage (as in, was-valid-when-written-but-now-probably-meaningless.
                }
                else
                {
                    return DbfReadResult.Eof;
                }
            }
            else if( recordStatus == DbfRecordStatus.Valid )
            {
                // NOOP
            }
            else
            {
                if( this.options.HasFlag(DbfDataReaderOptions.AllowInvalid) )
                {
                    // NOOP
                }
                else
                {
                    this.binaryReader.BaseStream.Seek( this.Table.Header.RecordDataLength, SeekOrigin.Current );
                    return DbfReadResult.Skipped;
                }
            }

            //////////////////////

            if( this.ReadRecord( offset, recordStatus ) )
            {
                return DbfReadResult.Read;
            }
            else
            {
                return DbfReadResult.Eof;
            }
        }

        private Boolean ReadRecord( Int64 offset, DbfRecordStatus recordStatus)
        {
            IList<DbfColumn> cols = this.Table.Columns;

            Object[] values = new Object[ cols.Count ];

            Int32 dataLength = this.Table.Header.RecordDataLength;

            for( Int32 i = 0; i < cols.Count; i++ )
            {
                try
                {
                    Object value = this.ReadValue( cols[i] );
                    values[i] = value;
                }
                catch(EndOfStreamException)
                {
                    this.SetEOF();
                    this.Current = null; // TODO: Set `this.Current` to a partial record?
                    return false;
                }
            }

            this.Current = new DbfRecord( this.Table, offset, recordStatus, values );
            return true;
        }

        private Object ReadValue(DbfColumn column)
        {
            switch( column.ColumnType )
            {
                case DbfColumnType.Autoincrement:
                    {
                        Int32 value = this.binaryReader.ReadInt32();
                        return value;
                    }
                case DbfColumnType.Boolean:
                    {
                        Char c = (Char)this.binaryReader.ReadByte();
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
                                return DBNull.Value;
                            default:
                                throw new InvalidOperationException("Invalid value for Boolean column type."); // TODO: Have a specific exception for this.
                        }
                    }
                case DbfColumnType.Character:
                    {
                        UInt16 length = (UInt16)(( column.DecimalCount << 8 ) | column.Length); // FoxPro stores the high-byte in DecimalCount
                        Byte[] text = this.binaryReader.ReadBytes( length );

                        String textStr = this.TextEncoding.GetString( text );
                        String trimmed = textStr.TrimEnd( _textPaddingChars );

                        return trimmed;
                    }
                case DbfColumnType.Currency:
                    {
                        throw new NotImplementedException();
                    }
                case DbfColumnType.Date:
                    {
                        String dateStr = this.ReadAsciiString( 8 );
                        DateTime value = DateTime.ParseExact( dateStr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None );
                        return value;
                    }
                case DbfColumnType.Timestamp:
                case DbfColumnType.DateTime:
                    {
                        // bytes 0-3: date: little-endian 32-bit integer Julian day number.
                        // bytes 4-7: time: milliseconds since midnight

                        // Julian day number: days since some epoch, but the value 2299161 is 1582-10-15 on the Gregorian calendar (which is 1852-10-04 in Julian).

                        Int32 days = this.binaryReader.ReadInt32();
                        Int32 time = this.binaryReader.ReadInt32();

                        Int32 daysSince2299161 = days - 2299161;
                        if( daysSince2299161 < 0 ) throw new InvalidOperationException("Invalid dateTime value.");

                        DateTime date = _julianDay2299161Local.AddDays( daysSince2299161 );
                        DateTime dateTime = date.AddMilliseconds( time );
                        return dateTime;
                    }
                case DbfColumnType.Double:
                    {
                        return this.binaryReader.ReadDouble();
                    }
                case DbfColumnType.DoubleOrBinary:
                    {
                        if( this.Table.Header.IsFoxPro )
                        {
                            // Int16
                            return this.binaryReader.ReadInt16();
                        }
                        else if( this.Table.Header.Version == 5 ) // dBase V
                        {
                            // Binary
                            throw new NotImplementedException();
                        }
                        else
                        {
                            throw new InvalidOperationException("This column type is not supported in this table file format.");
                        }
                    }
                case DbfColumnType.Float:
                    {
                        String twentyDigits = this.ReadAsciiString( 20 );
                        return Single.Parse( twentyDigits, NumberStyles.Any, CultureInfo.InvariantCulture );
                    }
                case DbfColumnType.General:
                    {
                        // Binary
                        throw new NotImplementedException();
                    }
                case DbfColumnType.Memo:
                     {
                        // Binary
                        throw new NotImplementedException();
                    }
                case DbfColumnType.Number:
                    {
                        if( column.Length > 20 ) throw new InvalidOperationException("Number columns cannot exceed 20 characters.");

                        String value = this.ReadAsciiString( column.Length );
                        if( Decimal.TryParse( value, NumberStyles.Any, CultureInfo.InvariantCulture, out Decimal parsed ) ) return parsed;
                        else return DBNull.Value;
                    }
                case DbfColumnType.SignedLong:
                    {
                        // Int32
                        return this.binaryReader.ReadInt32();
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        
        private readonly Byte[] reusableBuffer = new Byte[ 256 ];

        private String ReadAsciiString(Int32 length)
        {
            Int32 bytesRead = this.binaryReader.Read( this.reusableBuffer, 0, length );
            return Encoding.ASCII.GetString( this.reusableBuffer, 0, bytesRead );
        }

        public override Boolean Seek(Int32 recordIndex)
        {
            Int64 desiredOffset = this.GetRecordFileOffset( recordIndex );
            Int64 currentOffset = this.binaryReader.BaseStream.Seek( desiredOffset, SeekOrigin.Begin );
            return desiredOffset == currentOffset;
        }

        private static readonly DateTime _julianDay2299161Local = new DateTime( 1582, 10, 15, 0, 0, 0, 0, DateTimeKind.Local );
        private static readonly DateTime _julianDay2299161Utc   = new DateTime( 1582, 10, 15, 0, 0, 0, 0, DateTimeKind.Utc   );

        private static readonly Char[] _textPaddingChars = new Char[] { '\0', ' ' };
    }
}
