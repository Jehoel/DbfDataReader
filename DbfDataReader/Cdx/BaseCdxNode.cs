using System;
using System.IO;

namespace Dbf.Cdx
{
    /// <summary>Common fields shared by both Internal and External CDX nodes. Note that a "node" is often referred to as a "page" in CDX documentation.</summary>
    public abstract class BaseCdxNode
    {
        internal BaseCdxNode
        (
            // Metadata:
            Int64 offset,
            CdxFileHeader indexHeader,
            // Node data:
            CdxNodeAttributes attributes,
            UInt16 keyCount,
            Int32 leftSibling,
            Int32 rightSibling
        )
        {
            this.Offset       = offset;
            this.IndexHeader  = indexHeader;

            this.Attributes   = attributes;
            this.KeyCount     = keyCount;
            this.LeftSibling  = leftSibling;
            this.RightSibling = rightSibling;
        }

        public Int64                      Offset       { get; }
        public CdxFileHeader              IndexHeader  { get; }
        
        public CdxNodeAttributes Attributes   { get; }
        public UInt16                     KeyCount     { get; }
        public Int32                      LeftSibling  { get; }
        public Int32                      RightSibling { get; }

        public static BaseCdxNode Read(CdxFileHeader indexHeader, BinaryReader reader)
        {
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            // TODO: Confirm that "Leaf Node" == External Node, and !LeafNode == "Interior Node"...
            Int64 offset = reader.BaseStream.Position;
            CdxNodeAttributes attributes = (CdxNodeAttributes)reader.ReadUInt16();
            if( attributes.HasFlag( CdxNodeAttributes.LeafNode ) )
            {
                return LeafCdxNode.Read( indexHeader, offset, attributes, reader );
            }
            else
            {
                //return ExteriorCdxNode.Read( indexHeader, offset, attributes, reader );
                return InteriorCdxNode.Read( indexHeader, offset, attributes, reader );
            }
        }
    }
}
