using System;
using System.IO;

namespace Dbf.Cdx
{
    public class InteriorCdxNode : BaseCdxNode
    {
        private InteriorCdxNode(
            Int64 offset,
            CompactIndexNodeAttributes attributes,
            UInt16 keyCount,
            Int32 leftSibling,
            Int32 rightSibling,
            Byte[] keyValue
        )
            : base( offset, attributes, keyCount, leftSibling, rightSibling )
        {
            this.KeyValue = keyValue ?? throw new ArgumentNullException( nameof( keyValue ) );
        }
        
        public Byte[] KeyValue { get; }

        internal static InteriorCdxNode Read(Int64 offset, CompactIndexNodeAttributes attributes, BinaryReader reader)
        {
            UInt16 keyCount     = reader.ReadUInt16();
            Int32  leftSibling  = reader.ReadInt32();
            Int32  rightSibling = reader.ReadInt32();
            Byte[] keyValue     = reader.ReadBytes(500);

            return new InteriorCdxNode(
                offset,
                attributes,
                keyCount,
                leftSibling,
                rightSibling,
                keyValue
            );
        }
    }
}
