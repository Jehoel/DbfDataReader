using System;
using System.Collections.Generic;
using System.IO;

namespace Dbf.Cdx
{
    /// <summary>Common fields shared by both Internal and External CDX nodes. Note that a "node" is often referred to as a "page" in CDX documentation.</summary>
    public abstract class BaseCdxNode
    {
        #region Read

        public static BaseCdxNode Read(CdxIndexHeader indexHeader, BinaryReader reader)
        {
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            Int64 offset = reader.BaseStream.Position;
            CdxNodeAttributes attributes = (CdxNodeAttributes)reader.ReadUInt16();
            if( attributes.HasFlag( CdxNodeAttributes.LeafNode ) )
            {
                return LeafCdxNode.Read( indexHeader, offset, attributes, reader );
            }
            else
            {
                return InteriorCdxNode.Read( indexHeader, offset, attributes, reader );
            }
        }

        #endregion

        // BaseCdxNode and its subclasses do not have any member-references to any loaded parent, sibling, or child objects.
        // This is intentional and is to prevent leaking memory by having long-lived references to node objects that might not be needed more than once (as-per the consuming application).
        // Besides, the ability to traverse left-or-right is done by the LeftSibling+RightSibling members.

        internal BaseCdxNode
        (
            // Metadata:
            Int64 offset,
            CdxIndexHeader indexHeader,

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

        public Int64             Offset       { get; }
        public CdxIndexHeader    IndexHeader  { get; }
        
        public CdxNodeAttributes Attributes   { get; }
        public UInt16            KeyCount     { get; }
        public Int32             LeftSibling  { get; } // signed, not unsigned, as '-1' has significance.
        public Int32             RightSibling { get; }

        public abstract IList<IKey> GetKeys();

        public const Int32 NoSibling = -1;


    }
}
