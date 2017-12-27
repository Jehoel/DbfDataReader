﻿using System;
using System.IO;

namespace Dbf.Cdx
{
    /// <summary>Common fields shared by both Internal and External CDX nodes. Note that a "node" is often referred to as a "page" in CDX documentation.</summary>
    public abstract class BaseCdxNode
    {
        protected BaseCdxNode
        (
            Int64 offset,
            CompactIndexNodeAttributes attributes,
            UInt16 keyCount,
            Int32 leftSibling,
            Int32 rightSibling
        )
        {
            this.Offset       = offset;
            this.Attributes   = attributes;
            this.KeyCount     = keyCount;
            this.LeftSibling  = leftSibling;
            this.RightSibling = rightSibling;
        }

        public Int64                      Offset       { get; }
        public CompactIndexNodeAttributes Attributes   { get; }
        public UInt16                     KeyCount     { get; }
        public Int32                      LeftSibling  { get; }
        public Int32                      RightSibling { get; }

        public static BaseCdxNode Read(BinaryReader reader)
        {
            // TODO: Confirm that "Leaf Node" == External Node, and !LeafNode == "Interior Node"...
            Int64 offset = reader.BaseStream.Position;
            CompactIndexNodeAttributes attributes = (CompactIndexNodeAttributes)reader.ReadUInt16();
            if( attributes.HasFlag( CompactIndexNodeAttributes.LeafNode ) )
            {
                return ExteriorCdxNode.Read( offset, attributes, reader );
            }
            else
            {
                return InteriorCdxNode.Read( offset, attributes, reader );
            }
        }
    }
}