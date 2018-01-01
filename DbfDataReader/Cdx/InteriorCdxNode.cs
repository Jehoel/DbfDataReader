using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public class InteriorIndexKeyEntry : IKey
    {
        private readonly Byte[] keyBytes;
        //public ReadOnlyCollection<Byte> KeyBytes { get; }
        public Byte[] KeyBytes => this.keyBytes;

        /// <summary>DBF record number for this key (so if it's an exact match there's no need to traverse-down to a Leaf Node).</summary>
        public UInt32 DbfRecordNumber { get; }

        /// <summary>Location in the file of the node for this key range.</summary>
        public UInt32 NodePointer   { get; }

        public InteriorIndexKeyEntry(Byte[] keyBytes, UInt32 recordNumber, UInt32 nodePointer)
        {
            this.keyBytes        = keyBytes;
            //this.KeyBytes      = new ReadOnlyCollection<byte>( this.keyBytes );

            this.DbfRecordNumber = recordNumber;
            this.NodePointer     = nodePointer;
        }

        public static InteriorIndexKeyEntry Read(Byte[] keyBuffer, Int32 keyLength, Int32 indexEntryIdx)
        {
            // Microsoft's documentation is incorrect.
            // Their documentation for Compound CDX refers to normal *.idx documentation, which states that each key is followed by "4 hex characters".
            // In CDX inner nodes, however, each key is actually followed by two UInt32 values (for a total of 8 bytes): recordNumber, and nodePointer

            Int32 startIdx = (keyLength + 8) * indexEntryIdx;

            Byte[] key = new Byte[ keyLength ];
            Array.Copy( keyBuffer, startIdx, key, 0, keyLength );

            Int32 i = startIdx + keyLength;
            Int32 recordNumber = keyBuffer[ i + 3 ] | ( keyBuffer[ i + 2 ] << 8 ) | ( keyBuffer[ i + 1 ] << 16 ) | ( keyBuffer[ i + 0 ] << 24 );

            i += 4;

            Int32 nPage = keyBuffer[ i + 3 ] | ( keyBuffer[ i + 2 ] << 8 ) | ( keyBuffer[ i + 1 ] << 16 ) | ( keyBuffer[ i + 0 ] << 24 );

            return new InteriorIndexKeyEntry( key, (UInt32)recordNumber, (UInt32)nPage ); 
        }
    }
}
