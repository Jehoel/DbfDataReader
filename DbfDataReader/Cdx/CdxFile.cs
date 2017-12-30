using System;
using System.IO;

namespace Dbf.Cdx
{
    // https://msdn.microsoft.com/en-us/library/s8tb8f47(v=vs.80).aspx
    // Compact Index *.idx and Compound Index *.cdx both share the same file structure.

    public sealed class CdxFile : IDisposable
    {
        private readonly BinaryReader reader;

        private CdxFile(CdxFileHeader header, BaseCdxNode rootNode, BinaryReader reader)
        {
            this.Header   = header;
            this.RootNode = rootNode;
            this.reader   = reader;
        }

        public void Dispose()
        {
            this.reader.Dispose();
        }

        internal BinaryReader Reader => this.reader;

        public CdxFileHeader Header { get; }

        public BaseCdxNode RootNode { get; }

        public static CdxFile Open(String fileName)
        {
            FileStream fs = new FileStream( fileName, FileMode.Open, FileAccess.Read, FileShare.Read );
            try
            {
                BinaryReader rdr = new BinaryReader( fs );

                CdxFileHeader header = CdxFileHeader.Read( rdr );

                rdr.BaseStream.Seek( header.RootNodePointer, SeekOrigin.Begin );

                BaseCdxNode rootNode = BaseCdxNode.Read( header.KeyLength, rdr, readCdxFileHeader: false );

                // The root node (and its siblings? or is it limited to only one node?) is special
                // ...its keys are actually "tag names" which are the names of the sub-indexes it contains.

                return new CdxFile( header, rootNode, rdr );
            }
            catch
            {
                fs.Dispose();
                throw;
            }
        }

        public BaseCdxNode ReadNode(UInt32 recordNumber)
        {
            this.reader.BaseStream.Seek( recordNumber, SeekOrigin.Begin );

            // TODO: Cache nodes in-memory?
            BaseCdxNode node = BaseCdxNode.Read( this.Header.KeyLength, this.reader, readCdxFileHeader: true );
            return node;
        }
    }
}
