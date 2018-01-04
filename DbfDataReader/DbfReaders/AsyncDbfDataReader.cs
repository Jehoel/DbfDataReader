using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dbf
{
    public class AsyncDbfDataReader : DbfDataReader
    {
        private readonly FileStream        fileStream;
        private readonly AsyncBinaryReader binaryReader;
        private          Boolean           isDisposed;
        private          Boolean           isEof;

        private readonly DbfDataReaderOptions options;

        public override Encoding TextEncoding { get; }

        internal AsyncDbfDataReader(DbfTable table, Boolean randomAccess, Encoding encoding, DbfDataReaderOptions options)
            : base( table )
        {
            FileStream stream = Utility.OpenFileForReading( table.File.FullName, randomAccess, async: true );
            if( !stream.CanRead || !stream.CanSeek )
            {
                stream.Dispose();
                throw new InvalidOperationException("The created FileStream could not perform both Read and Seek operations.");
            }
            if( !stream.IsAsync )
            {
                stream.Dispose();
                throw new InvalidOperationException("The created FileStream is not asynchronous.");
            }

            this.fileStream = stream;

            this.binaryReader = new AsyncBinaryReader( this.fileStream, Encoding.ASCII, leaveOpen: true );

            this.TextEncoding = encoding;

            this.options = options;
        }

        public override void Close()
        {
            this.binaryReader.Dispose();
            this.fileStream.Dispose();
            this.isDisposed = true;
        }

        public override Int64 CurrentOffset => this.binaryReader.BaseStream.Position;

        public override Boolean IsClosed => this.isDisposed;

        protected override Boolean Eof => this.isEof;

        protected override Boolean SetEof()
        {
            if( this.Eof ) return true;

            if( this.binaryReader.BaseStream.Position == this.binaryReader.BaseStream.Length )
            {
                this.isEof = true;
            }

            return this.Eof;
        }

        public override Boolean Read()
        {
            throw new NotSupportedException("Synchronous reading is not supported.");
        }

        public override async Task<Boolean> ReadAsync(CancellationToken cancellationToken)
        {
            if( this.SetEof() ) return false;

            DbfReadResult result;
            do
            {
                result = await this.ReadImplAsync( cancellationToken ).ConfigureAwait(false);
            }
            while( result == DbfReadResult.Skipped );

            return result == DbfReadResult.Read;
        }

        private async Task<DbfReadResult> ReadImplAsync(CancellationToken cancellationToken)
        {
            if( this.SetEof() ) return DbfReadResult.Eof;

            Int32 recordIndex = this.GetRecordIndexFromCurrentOffset();
            Int64 offset = this.binaryReader.BaseStream.Position;

            DbfRecordStatus recordStatus = (DbfRecordStatus)await this.binaryReader.ReadByteAsync().ConfigureAwait(false);

            DbfReadResult initReadResult = ShouldRead( recordStatus, this.options );
            if( initReadResult == DbfReadResult.Skipped )
            {
                this.binaryReader.BaseStream.Seek( this.Table.Header.RecordDataLength, SeekOrigin.Current ); // skip-over those bytes. TODO: Is Seek() better than Read() for data we don't care about? will Seek() trigger Random-access behaviour - or only Seek() that extends beyond the current buffer (or two?) or goes in a backwards direction?
                return DbfReadResult.Skipped;
            }
            else if( initReadResult == DbfReadResult.Eof )
            {
                return DbfReadResult.Eof;
            }

            //////////////////////

            if( await this.ReadRecordAsync( this.binaryReader, recordIndex, offset, recordStatus ).ConfigureAwait(false) )
            {
                Int64 expectedPosition = offset + this.Table.Header.RecordLength; // offset + (record-status == 1) + (record-data)
                Int64 actualPosition   = this.binaryReader.BaseStream.Position;
                if( actualPosition != expectedPosition )
                {
                    if( actualPosition > expectedPosition ) throw new InvalidOperationException("Read beyond record.");
                    else throw new InvalidOperationException("Read less than record length.");
                }

                return DbfReadResult.Read;
            }
            else
            {
                return DbfReadResult.Eof;
            }
        }

        [CLSCompliant(false)]
        protected virtual async Task<Boolean> ReadRecordAsync(AsyncBinaryReader reader, Int32 recordIndex, Int64 offset, DbfRecordStatus recordStatus)
        {
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            IList<DbfColumn> cols = this.Table.Columns;

            Object[] values = new Object[ cols.Count ];

            for( Int32 i = 0; i < cols.Count; i++ )
            {
                try
                {
                    Object value = await ValueReader.ReadValueAsync( cols[i], reader, this.TextEncoding ).ConfigureAwait(false);
                    values[i] = value;
                }
                catch(EndOfStreamException)
                {
                    this.SetEof();
                    this.Current = null; // TODO: Set `this.Current` to a partial record?
                    return false;
                }
            }

            this.Current = new DbfRecord( this.Table, recordIndex, offset, recordStatus, values );
            return true;
        }

        /// <summary>Seeks to the zero-based record index. Call Read() to read the record after seeking.</summary>
        public override Boolean Seek(Int32 zeroBasedRecordIndex)
        {
            // There is no SeekAsync: https://stackoverflow.com/questions/47986340/are-seeks-cheaper-than-reads-and-does-forward-seeking-fall-foul-of-the-sequenti

            Int64 desiredOffset = this.GetRecordFileOffset( zeroBasedRecordIndex );
            Int64 currentOffset = this.binaryReader.BaseStream.Seek( desiredOffset, SeekOrigin.Begin );
            return desiredOffset == currentOffset;
        }
    }
}
