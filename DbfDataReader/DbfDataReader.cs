using System;
using System.Collections;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Security.AccessControl;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;

namespace DbfDataReader
{
    public abstract class DbfDataReader : DbDataReader
    {
        public DbfTable Table { get; }

        internal DbfDataReader(DbfTable table)
        {
            this.Table = table;
        }

        private DbfRecord current;

        public DbfRecord Current
        {
            get
            {
                if( this.current == null ) throw new InvalidOperationException("No " + nameof(DbfRecord) + " has been read by this " + nameof(DbfDataReader) + " instance.");
                return this.current;
            }
            protected set
            {
                this.current = value;
            }
        }

        public override abstract void Close();

        #region DbDataReader

        #region IDataRecord / Get typed values

        public override Boolean GetBoolean(Int32 ordinal)
        {
            return this.Current.GetBoolean( ordinal );
        }

        public override Byte GetByte(Int32 ordinal)
        {
           return this.Current.GetByte( ordinal );
        }

        public override Int64 GetBytes(Int32 ordinal, Int64 dataOffset, Byte[] buffer, Int32 bufferOffset, Int32 length)
        {
            return this.Current.GetBytes( ordinal, dataOffset, buffer, bufferOffset, length );
        }

        public override Char GetChar(Int32 ordinal)
        {
            return this.Current.GetChar( ordinal );
        }

        public override Int64 GetChars(Int32 ordinal, Int64 dataOffset, Char[] buffer, Int32 bufferOffset, Int32 length)
        {
           return this.Current.GetChars( ordinal, dataOffset, buffer, bufferOffset, length );
        }

        public override DateTime GetDateTime(Int32 ordinal)
        {
            return this.Current.GetDateTime( ordinal );
        }

        public override Decimal GetDecimal(Int32 ordinal)
        {
            return this.Current.GetDecimal( ordinal );
        }

        public override Double GetDouble(Int32 ordinal)
        {
            return this.Current.GetDouble( ordinal );
        }

        public override Single GetFloat(Int32 ordinal)
        {
            return this.Current.GetFloat( ordinal );
        }

        public override Guid GetGuid(Int32 ordinal)
        {
            return this.Current.GetGuid( ordinal );
        }

        public override Int16 GetInt16(Int32 ordinal)
        {
            return this.Current.GetInt16( ordinal );
        }

        public override Int32 GetInt32(Int32 ordinal)
        {
            return this.Current.GetInt32( ordinal );
        }

        public override Int64 GetInt64(Int32 ordinal)
        {
            return this.Current.GetInt64( ordinal );
        }

        public override String GetString(Int32 ordinal)
        {
            return this.Current.GetString( ordinal );
        }

        #endregion

        public override Int32 Depth => 0; // always zero depth.

        public override Int32 FieldCount => this.Current.FieldCount;

        /// <summary>Returns false if the current table file contains no records or if this reader has reached EOF.</summary>
        public override Boolean HasRows
        {
            get
            {
                if( this.IsClosed ) throw new InvalidOperationException("This " + nameof(DbfDataReader) + " is closed.");

                if( this.Table.Header.RecordCount == 0 ) return false;
                
                if( this.EOF ) return false;

                return true;
            }
        }

        public override Int32 RecordsAffected => 0; // we're read-only

        public override Object this[String name] => this.Current[ name ];

        public override Object this[Int32 ordinal] => this.Current[ ordinal ];

        public override String GetDataTypeName(Int32 ordinal) => this.Current.GetDataTypeName( ordinal );

        public override IEnumerator GetEnumerator()
        {
            return new DbEnumerator( this, closeReader: false );
        }

        public override Type GetFieldType(Int32 ordinal) => this.Current.GetFieldType( ordinal );

        public override String GetName(Int32 ordinal) => this.Current.GetName( ordinal );

        public override Int32 GetOrdinal(String name) => this.Current.GetOrdinal( name );

        public override Object GetValue(Int32 ordinal) => this.Current.GetValue( ordinal );

        public override Int32 GetValues(Object[] values) => this.Current.GetValues( values );

        public override Boolean IsDBNull(Int32 ordinal) => this.Current.IsDBNull( ordinal );

        public override Boolean NextResult()
        {
            // xBase does not have a concept of batched results.
            return false;
        }

        #endregion

        protected abstract Boolean EOF { get; }

        public abstract Encoding Encoding { get; }

        // DbDataReader already has a `public virtual Task<Boolean> ReadAsync(CancellatioinToken)` method that calls `this.Read()` synchronously.
        // So `SyncDbfDataReader` doesn't need to do anything.
    }

    [Flags]
    public enum DbfDataReaderOptions
    {
        None          = 0,
        /// <summary>DBF EOF values will be ignored and the entire DBF file will be read.</summary>
        IgnoreEof     = 1 << 0,
        /// <summary>Records with DbfRecordStatus.Deleted values will be read instead of being skipped-over.</summary>
        AllowDeleted = 1 << 1,
        /// <summary>Records with DbfRecordStatus values other than Deleted or Valid will be read instead of being skipped-over.</summary>
        AllowInvalid  = 1 << 2
    }

    public sealed class SyncDbfDataReader : DbfDataReader
    {
        private readonly FileStream   fileStream;
        private readonly BinaryReader binaryReader;
        private          Boolean      isDisposed;
        private          Boolean      isEof;

        private readonly DbfDataReaderOptions options;

        public override Encoding Encoding { get; }

        /// <param name="ignoreEof">If true, then a </param>
        internal SyncDbfDataReader(DbfTable table, String fileName, Boolean randomAccess, Encoding encoding, DbfDataReaderOptions options)
            : base( table )
        {
            FileStream stream = new FileStream( fileName, FileMode.Open, FileSystemRights.ReadData, FileShare.ReadWrite, 4096, randomAccess ? FileOptions.RandomAccess : FileOptions.SequentialScan );
            if( !stream.CanRead || !stream.CanSeek )
            {
                stream.Dispose();
                throw new InvalidOperationException("The created FileStream could not perform both Read and Seek operations.");
            }

            this.binaryReader = new BinaryReader( this.fileStream, Encoding.ASCII, leaveOpen: true );

            this.Encoding = encoding;

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
                        return this.Encoding.GetString( text );
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

        private static Int32 GetDbfColumnTypeLength(DbfColumnType type, Int32 declaredLength)
        {
            // https://www.clicketyclick.dk/databases/xbase/format/data_types.html

            switch( type )
            {
                case DbfColumnType.Boolean       : return 1;
                case DbfColumnType.Character     : return declaredLength;
                case DbfColumnType.Currency      : return declaredLength; // see original version of DbfValueCurrency.cs
                case DbfColumnType.Date          : return 8;
                case DbfColumnType.DateTime      : return 8;
                case DbfColumnType.DoubleOrBinary: throw new NotImplementedException(); // if FoxPro then 8, else declaredLength...
                case DbfColumnType.Float         : return 20;
                case DbfColumnType.General       : throw new NotImplementedException();
                case DbfColumnType.Memo          : return 10; // value is a pointer to a field in a memo file.
                case DbfColumnType.Number        : return 20; // FoxPro and Clipper: 20 chars, 18 in dBase.
                case DbfColumnType.SignedLong    : return 4;
                case DbfColumnType.Autoincrement : return 4; // 'long' == 4
                case DbfColumnType.Timestamp     : return 8;
                case DbfColumnType.Double        : return 8;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }

    internal enum DbfReadResult
    {
        Read,
        Eof,
        Skipped
    }

    public sealed class AsyncDbfDataReader : DbfDataReader
    {

    }
}