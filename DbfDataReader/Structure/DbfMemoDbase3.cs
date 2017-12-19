using System.IO;
using System.Text;

namespace Dbf
{
    public class Dbase3MemoFile : DbfMemoFile
    {
        public Dbase3MemoFile(string fileName) : base(fileName)
        {
        }

        public Dbase3MemoFile(string fileName, Encoding encoding) : base( fileName, encoding )
        {
        }

        public Dbase3MemoFile(Stream stream, Encoding encoding) : base(stream, encoding)
        {
        }

        private static readonly char[] _trailingWhitespaceChars = new char[] { '\0', ' ' };

        public override string BuildMemo(long startBlock)
        {
            long offset = this.GetOffset( startBlock );
            
            this.BinaryReader.BaseStream.Seek( offset, SeekOrigin.Begin );

            StringBuilder stringBuilder = new StringBuilder();

            string block;
            do
            {
                block = new string( this.BinaryReader.ReadChars( DefaultBlockSize ) ); // TODO: This could be optimized with a `ReadCharsExcludingTrailingNulls` method that still reads the specified number of chars but doesn't need a trim and extra copy.
                block = block.TrimEnd( _trailingWhitespaceChars );

                stringBuilder.Append( block );

            } while( block.Length < DefaultBlockSize ); // TODO: Is this right? continue reading if it read less bytes (after trimming!) than the block size?

            return stringBuilder.ToString();
        }
    }
}