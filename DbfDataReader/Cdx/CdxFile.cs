using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dbf.Cdx
{
    // https://msdn.microsoft.com/en-us/library/s8tb8f47(v=vs.80).aspx
    // Compact Index *.idx and Compound Index *.cdx both share the same file structure.

    public sealed class CdxFile : IDisposable
    {
        private readonly BinaryReader reader;

        private CdxFile(FileInfo fileInfo, CdxIndexHeader header, BaseCdxNode rootNode, BinaryReader reader)
        {
            this.FileInfo      = fileInfo;
            this.Header        = header;
            this.RootNode      = rootNode;

            this.reader        = reader;
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Reliability", "CA2000:Dispose objects before losing scope" )]
        public static CdxFile Open(String fileName)
        {
            FileStream fs = Utility.OpenFileForReading( fileName, randomAccess: true, async: false );
            try
            {
                BinaryReader rdr = new BinaryReader( fs );

                CdxIndexHeader header = CdxIndexHeader.Read( rdr );

                rdr.BaseStream.Seek( header.RootNodePointer, SeekOrigin.Begin );

                BaseCdxNode rootNode = BaseCdxNode.Read( header, rdr );

                // The root node (and its siblings? or is it limited to only one node?) is special
                // ...its keys are actually "tag names" which are the names of the sub-indexes it contains.

                return new CdxFile( new FileInfo( fileName ), header, rootNode, rdr );
            }
            catch
            {
                fs.Dispose();
                throw;
            }
        }

        public IDictionary<String,CdxIndex> ReadTaggedIndexes()
        {
            CdxIndex tagIndex = new CdxIndex( this, this.Header, this.RootNode );

            List<LeafCdxKeyEntry> keys = IndexSearcher.GetAllKeys( tagIndex ).ToList(); // ToList() so we don't move the BinaryReader all over the place.
            
            return keys.ToDictionary(
                key => key.StringKey,
                key => this.ReadIndex( key.DbfRecordNumber ) // In the case of tagged-indexes, the 'recno' value (`DbfRecordNumber`) is actually the offset in the CDX file.
            );
        }

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
