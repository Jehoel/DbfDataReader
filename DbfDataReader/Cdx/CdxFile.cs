using System;
using System.IO;

namespace Dbf.Cdx
{
    // https://msdn.microsoft.com/en-us/library/s8tb8f47(v=vs.80).aspx
    // Compact Index *.idx and Compound Index *.cdx both share the same file structure.

    public sealed class CdxFile : IDisposable
    {
        private readonly BinaryReader reader;

        private CdxFile(FileInfo fileInfo, CdxIndexHeader header, BaseCdxNode rootNode, BinaryReader reader)
        {
            this.FileInfo = fileInfo;
            this.Header   = header;
            this.RootNode = rootNode;
            this.reader   = reader;
        }

        public void Dispose()
        {
            this.reader.Dispose();
        }

        internal BinaryReader Reader => this.reader;

        public FileInfo FileInfo { get; }

        public CdxIndexHeader Header { get; }

        /// <summary>Root node that contains the Compound Index root that indexes the tag-names and point to contained indexes. This is not an actual index of DBF values.</summary>
        public BaseCdxNode RootNode { get; }

        public static CdxFile Open(String fileName)
        {
            FileStream fs = new FileStream( fileName, FileMode.Open, FileAccess.Read, FileShare.Read );
            try
            {
                BinaryReader rdr = new BinaryReader( fs );

                CdxIndexHeader header = CdxIndexHeader.Read( rdr );

                rdr.BaseStream.Seek( header.RootNodePointer, SeekOrigin.Begin );

                BaseCdxNode rootNode = BaseCdxNode.Read( header, rdr );

                // The root node (and its siblings? or is it limited to only one node?) is special
                // ...its keys are actually "tag names" which are the names of the sub-indexes it contains.

                // Question: Should the tags be preloaded?

                return new CdxFile( new FileInfo( fileName ), header, rootNode, rdr );
            }
            catch
            {
                fs.Dispose();
                throw;
            }
        }

        public CdxIndex ReadIndex(String tagName)
        {
            throw new NotImplementedException();

            // search this.RootNode for the tagName
            // then return the CdxIndex for it.
        }

        // TODO: The return-type of this function should be a derived type, e.g. CompactIndexRoot or something to avoid confusion.
        public CdxIndex ReadIndex(UInt32 compactIndexOffsetInCompoundIndexFile)
        {
            this.reader.BaseStream.Seek( compactIndexOffsetInCompoundIndexFile, SeekOrigin.Begin );

            CdxIndexHeader indexHeader = CdxIndexHeader.Read( this.reader );

            if( !indexHeader.Options.HasFlag(CdxIndexOptions.IsCompoundIndexHeader) ) throw new CdxException( CdxErrorCode.CompoundIndexHeaderDoesNotHaveCompoundIndexOption );

            this.reader.BaseStream.Seek( indexHeader.RootNodePointer, SeekOrigin.Begin );

            BaseCdxNode rootNode = BaseCdxNode.Read( indexHeader, this.reader );
            
            if( !rootNode.Attributes.HasFlag(CdxNodeAttributes.RootNode) ) throw new CdxException( CdxErrorCode.RootNodeDoesNotHaveRootAttribute );

            return new CdxIndex( this, indexHeader, rootNode );
        }
    }
}
