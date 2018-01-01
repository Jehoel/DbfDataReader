using System;

namespace Dbf.Cdx
{
    public class CdxIndex
    {
        internal CdxIndex(CdxFile file, CdxIndexHeader indexHeader, BaseCdxNode rootNode)
        {
            this.File     = file;
            this.Header   = indexHeader;
            this.RootNode = rootNode;
        }

        // TagName is not included as a member because it's possible to parse a CDX Index (2nd degree node from the *file* root) without knowing the tag name.
        public CdxFile        File     { get; }
        public CdxIndexHeader Header   { get; }
        public BaseCdxNode    RootNode { get; }

        public BaseCdxNode ReadNode(Int32 nodeOffset)
        {
            this.File.Reader.BaseStream.Seek( nodeOffset, SeekOrigin.Begin );

            BaseCdxNode node = BaseCdxNode.Read( this.Header, this.File.Reader );
            return node;
        }
    }
}
