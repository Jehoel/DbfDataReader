using System;
using System.IO;
using System.Text;

namespace DbfDataReader
{
    public class Dbase4MemoFile : DbfMemoFile
    {
        public Dbase4MemoFile(string path) : base(path)
        {
        }

        public Dbase4MemoFile(string path, Encoding encoding) : base(path, encoding)
        {
        }

        public Dbase4MemoFile(Stream stream, Encoding encoding) : base(stream, encoding)
        {
        }

        public override string BuildMemo(long startBlock)
        {
            throw new NotImplementedException();
        }
    }
}