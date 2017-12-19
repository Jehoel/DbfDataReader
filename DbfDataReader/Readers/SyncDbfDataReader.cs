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

            if( this.ReadRecord() )
            {
                return DbfReadResult.Read;
            }
            else
            {
                return DbfReadResult.Eof;
            }
        }

        private Boolean ReadRecord()
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
                    // TODO: Set `this.Current` to a partial record?
                    return false;
                }
            }

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
                        return this.TextEncoding.GetString( text );
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
                case DbfColumnType.DateTime:
                case DbfColumnType.Double:
                case DbfColumnType.DoubleOrBinary:
                case DbfColumnType.Float:
                case DbfColumnType.General:
                case DbfColumnType.Memo:
                case DbfColumnType.Number:
                case DbfColumnType.SignedLong:
                case DbfColumnType.Timestamp:
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
    }
}
