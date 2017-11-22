using System.IO;
using System.Text;

namespace DbfDataReader
{
    public class FoxProMemoFile : DbfMemoFile
    {
        public FoxProMemoFile(string path) : this(path, Encoding.UTF8)
        {
        }

        public FoxProMemoFile(string path, Encoding encoding) : base(path, encoding)
        {
            this.BlockSize = CalculateBlockSize();
        }

        public FoxProMemoFile(Stream stream, Encoding encoding) : base(stream, encoding)
        {
            this.BlockSize = CalculateBlockSize();
        }

        public override int BlockSize { get; }

        private int CalculateBlockSize()
        {
            this.BinaryReader.BaseStream.Seek(0, SeekOrigin.Begin);

            this.BinaryReader.ReadUInt32(); // next block
            this.BinaryReader.ReadUInt16(); // unused
            return this.BinaryReader.ReadUInt16();
        }

         private static readonly char[] _trailingWhitespaceChars = new char[] { '\0', ' ' };

        public override string BuildMemo(long startBlock)
        {
            var offset = this.GetOffset( startBlock );
            this.BinaryReader.BaseStream.Seek( offset, SeekOrigin.Begin );

            var blockType = this.BinaryReader.ReadUInt32();
            var memoLength = this.BinaryReader.ReadUInt32();

            if( blockType != 1 || memoLength == 0 )
            {
                return string.Empty;
            }

            var memo = new string( this.BinaryReader.ReadChars( DefaultBlockSize ) );
            memo = memo.TrimEnd( _trailingWhitespaceChars );
            return memo;
        }
    }
}
