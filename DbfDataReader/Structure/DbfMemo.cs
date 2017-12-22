using System;
using System.IO;
using System.Text;

namespace Dbf
{
    public abstract class DbfMemoFile : IDisposable
    {
        protected const int BlockHeaderSize  =   8;
        protected const int DefaultBlockSize = 512;

        private readonly BinaryReader binaryReader;
        protected BinaryReader BinaryReader => this.binaryReader;

        public    String       FileName     { get; }

        protected DbfMemoFile(string fileName)
            : this( fileName, Encoding.UTF8 )
        {
        }

        protected DbfMemoFile(string fileName, Encoding encoding)
        {
            if( !File.Exists( fileName ) )
            {
                throw new FileNotFoundException( "The xBase memo file does not exist.", fileName );
            }

            this.FileName = fileName;

            FileStream fileStream = new FileStream( fileName, FileMode.Open );
            try
            {
                this.binaryReader = new BinaryReader( fileStream, encoding );
            }
            catch
            {
                fileStream.Dispose();
                throw;
            }
        }

        protected DbfMemoFile(Stream stream, Encoding encoding)
        {
            this.FileName = null;
            this.binaryReader = new BinaryReader( stream, encoding );
        }

        public void Close()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            this.Dispose( disposing: true );
            GC.SuppressFinalize( this );
        }

        protected virtual void Dispose(bool disposing)
        {
            if( disposing )
            {
                this.binaryReader.Dispose();
            }
        }
        
        public abstract string BuildMemo(long startBlock);

        public virtual int BlockSize        => DefaultBlockSize;
        public         int BlockContentSize => this.BlockSize + BlockHeaderSize;

        public string Get(long startBlock)
        {
            return startBlock <= 0 ? string.Empty : BuildMemo( startBlock );
        }

        public long GetOffset(long startBlock)
        {
            return startBlock * this.BlockSize;
        }

        public int GetContentSize(int memoSize)
        {
            return ( memoSize - this.BlockSize ) + BlockHeaderSize;
        }
    }
}