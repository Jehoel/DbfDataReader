using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Overby.Extensions.AsyncBinaryReaderWriter;

namespace DbfDataReader
{
    public sealed class AsyncDbfDataReader : DbfDataReader
    {
        private readonly FileStream        fileStream;
        private readonly AsyncBinaryReader binaryReader;
        private          Boolean           isDisposed;
        private          Boolean           isEof;

        private readonly DbfDataReaderOptions options;

        public override Encoding TextEncoding { get; }

        /// <param name="ignoreEof">If true, then a </param>
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
            throw new NotSupportedException("Synchronous reading is not supported.");
        }

        public override Task<Boolean> ReadAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException("TODO!");
        }

        public override Boolean Seek(Int32 recordIndex)
        {
            Int64 desiredOffset = this.GetRecordFileOffset( recordIndex );
            Int64 currentOffset = this.binaryReader.BaseStream.Seek( desiredOffset, SeekOrigin.Begin );
            return desiredOffset == currentOffset;
        }
    }
}
