using System;
using System.Collections.Generic;
using System.IO;

namespace Dbf.Cdx
{
    public class InteriorCdxNode : BaseCdxNode
    {
        internal static InteriorCdxNode Read(CdxIndexHeader indexHeader, Int64 offset, CdxNodeAttributes attributes, BinaryReader reader)
        {
            UInt16 keyCount     = reader.ReadUInt16();
            Int32  leftSibling  = reader.ReadInt32();
            Int32  rightSibling = reader.ReadInt32();
            Byte[] keyValues    = reader.ReadBytes(500);

#if DEBUG
            if( keyCount     > 250 ) throw new CdxException( CdxErrorCode.InvalidInteriorNodeKeyCount );
            if( leftSibling  <  -1 ) throw new CdxException( CdxErrorCode.InvalidInteriorNodeLeftSibling );
            if( rightSibling <  -1 ) throw new CdxException( CdxErrorCode.InvalidInteriorNodeRightSibling );
#endif

            InteriorIndexKeyEntry[] keyEntries = ParseKeyValues( keyCount, indexHeader.KeyLength, keyValues );

            return new InteriorCdxNode(
                offset,
                indexHeader,

                attributes,
                keyCount,
                leftSibling,
                rightSibling,
                keyValues,
                keyEntries
            );
        }

        private static InteriorIndexKeyEntry[] ParseKeyValues(Int32 keyCount, Int32 keyLength, Byte[] keyValues)
        {
            InteriorIndexKeyEntry[] entries = new InteriorIndexKeyEntry[ keyCount ];

            for( Int32 i = 0; i < keyCount; i++ )
            {
                InteriorIndexKeyEntry entry = InteriorIndexKeyEntry.Read( keyValues, keyLength, i );
                entries[i] = entry;
            }

            return entries;
        }

        private InteriorCdxNode(
            Int64 offset,
            CdxIndexHeader indexHeader,

            CdxNodeAttributes attributes,
            UInt16 keyCount,
            Int32 leftSibling,
            Int32 rightSibling,
            Byte[] keyValues,
            InteriorIndexKeyEntry[] keyEntries
        )
            : base( offset, indexHeader, attributes, keyCount, leftSibling, rightSibling )
        {
            this.KeyValues  = keyValues  ?? throw new ArgumentNullException( nameof(keyValues) );
            this.KeyEntries = keyEntries ?? throw new ArgumentNullException( nameof(keyEntries) );
        }
        
        public Byte[] KeyValues { get; }

        public InteriorIndexKeyEntry[] KeyEntries { get; }

        public override IList<IKey> GetKeys()
        {
            return this.KeyEntries;
        }
    }
}
