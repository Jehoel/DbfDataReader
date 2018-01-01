using System;
using System.Collections.Generic;
using System.IO;

namespace Dbf.Cdx
{
    /// <summary>Also known as an Exterior CDX Node.</summary>
    public sealed class LeafCdxNode : BaseCdxNode
    {
        #region Read

        private const Int32 IndexKeyBufferLength = 488;

        /// <summary>Reads a Leaf CDX node from +1 bytes from its start in the file, the first byte is the Node Attributes byte which should be passed-in as a parameter.</summary>
        public static LeafCdxNode Read(CdxIndexHeader indexHeader, Int64 offset, CdxNodeAttributes attributes, BinaryReader reader)
        {
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            UInt16 keyCount                = reader.ReadUInt16();
            Int32  leftSibling             = reader.ReadInt32();
            Int32  rightSibling            = reader.ReadInt32();
            UInt16 freeSpace               = reader.ReadUInt16();
            UInt32 recordNumberMask        = reader.ReadUInt32();
            Byte   duplicateByteCountMask  = reader.ReadByte();
            Byte   trailingByteCountMask   = reader.ReadByte();
            Byte   recordNumberBitsCount   = reader.ReadByte();
            Byte   duplicateCountBitsCount = reader.ReadByte();
            Byte   trailCountBitsCount     = reader.ReadByte();
            Byte   recordNumberDuplicateCountTrailingCountBytes = reader.ReadByte();
            Byte[] indexKeysPacked         = reader.ReadBytes( IndexKeyBufferLength );

#if DEBUG
            Int64 posActual = reader.BaseStream.Position;
            Int64 posExpected = offset + 512;
            if( posActual != posExpected ) throw new InvalidOperationException("Didn't read expected number of bytes in CompactIndexExteriorNode.");
#endif

            LeafCdxKeyEntry[] entries;
            UnpackIndexKeys(
                keyCount,
                indexHeader.KeyLength,
                recordNumberDuplicateCountTrailingCountBytes,
                new KeyComponent( recordNumberMask, recordNumberBitsCount ),
                new KeyComponent( duplicateByteCountMask, duplicateCountBitsCount ),
                new KeyComponent( trailingByteCountMask, trailCountBitsCount ),
                indexKeysPacked,
                out entries
            );

            return new LeafCdxNode(
                offset,
                indexHeader,

                attributes,
                keyCount,
                leftSibling,            
                rightSibling,           
                freeSpace,              
                recordNumberMask,       
                duplicateByteCountMask, 
                trailingByteCountMask,  
                recordNumberBitsCount,  
                duplicateCountBitsCount,
                trailCountBitsCount,    
                recordNumberDuplicateCountTrailingCountBytes,
                entries
            );
        }

        private static void UnpackIndexKeys(
            UInt16 keyCount,
            UInt16 keyLength,
            Byte packedKeyEntryLength,
            KeyComponent recordNumberInfo,
            KeyComponent duplicateBytesInfo,
            KeyComponent trailingBytesInfo,
            Byte[] packed,
            out LeafCdxKeyEntry[] entries
        )
        {
            if( packed == null ) throw new ArgumentNullException(nameof(packed));
            if( packed.Length != IndexKeyBufferLength /* 488 */ ) throw new ArgumentException("Value must have a length of 488.", nameof(packed));
            if( packedKeyEntryLength < 2 ) throw new ArgumentException("Packed Key Entries must be at least 2 bytes long.");

            Int32 keyValueSrc = packed.Length;

            entries = new LeafCdxKeyEntry[ keyCount ];

            Byte[] previousKeyData = null;
            for( Int32 i = 0; i < keyCount; i++ )
            {
#if DEBUG
                Byte[] packedEntry = new Byte[ packedKeyEntryLength ]; // this array exists so I can see what the current window of data looks like.
                Array.Copy( packed, i * packedKeyEntryLength, packedEntry, 0, packedEntry.Length );
#endif

                Int32 a = i * packedKeyEntryLength; // start of a packed keyEntry

                UInt32 recordNumber;
                {
                    Int32 recordNumberInt;

                    // RecordNumbers are in big-endian order.
                    if( recordNumberInfo.BitCount <= 8 )
                    {
                        recordNumberInt = packed[ a ];
                    }
                    else if( recordNumberInfo.BitCount <= 16 )
                    {
                        recordNumberInt = ( packed[ a + 1 ] << 8 ) | packed[ a + 0 ];
                    }
                    else if( recordNumberInfo.BitCount <= 24 )
                    {
                        recordNumberInt = ( packed[ a + 2 ] << 16 ) | ( packed[ a + 1 ] << 8 ) | packed[ a + 0 ];
                    }
                    else if( recordNumberInfo.BitCount <= 32 )
                    {
                        recordNumberInt = ( packed[ a + 3 ] << 24 ) | ( packed[ a + 2 ] << 16 ) | ( packed[ a + 1 ] << 8 ) | packed[ a + 0 ];
                    }
                    else
                    {
                        // manual for-loop? but this number will never be bigger than 2^32... so just throw for now.
                        throw new NotSupportedException("RecordNumber.BitCount values bigger than 32 are not supported.");
                    }

                    recordNumber = (UInt32)recordNumberInt & recordNumberInfo.Mask;
                }

                UInt32 duplicateBytes;
                UInt32 trailingBytes;
                {
                    // Get the last two bytes of the array. Note we assume packedKeyEntryLength is >=2
                    Byte bN0 = packed[ a + (packedKeyEntryLength-1) ]; // b[N-0]
                    Byte bN1 = packed[ a + (packedKeyEntryLength-2) ]; // b[N-1]
                    Int32 trailingAndDuplicate = ( bN1 << 8 ) | bN0;

                    duplicateBytes = (UInt32)( trailingAndDuplicate & duplicateBytesInfo.Mask );

                    /////

                    trailingBytes = (UInt32)( trailingAndDuplicate >> duplicateBytesInfo.BitCount );
                    trailingBytes &= trailingBytesInfo.Mask;
                }

                Int32 newBytesCount = (Int32)(keyLength - duplicateBytes - trailingBytes);

                keyValueSrc -= newBytesCount;

#if DEBUG
                if( keyValueSrc < 0 ) throw new CdxException( CdxErrorCode.InvalidLeafNodeCalculatedKeyStartIndex );
                if( ( i == 0 && duplicateBytes > 0 ) || ( previousKeyData == null && duplicateBytes > 0 ) ) throw new CdxException( CdxErrorCode.FirstLeafNodeKeyEntryHasDuplicateBytes );
#endif

                //////////////////

                Byte[] keyData = new Byte[ keyLength ];// - trailingBytes ];
                for( UInt32 d = 0; d < duplicateBytes; d++ )
                {
                    keyData[d] = previousKeyData[d];
                }

                for( UInt32 b = duplicateBytes, src = 0; src < newBytesCount; b++, src++ )
                {
                    keyData[b] = packed[ keyValueSrc + src ];
                }

                LeafCdxKeyEntry keyEntry = new LeafCdxKeyEntry( keyData, recordNumber, (Int32)duplicateBytes, (Int32)trailingBytes );
                entries[i] = keyEntry;

                previousKeyData = keyData;
            }
        }

        /// <summary>CDX Key Component info.</summary>
        private struct KeyComponent
        {
            public KeyComponent(UInt32 mask, Byte bitCount)
            {
                this.Mask     = mask;
                this.BitCount = bitCount;
            }

            public readonly UInt32 Mask;
            public readonly Byte   BitCount;
        }

        #endregion

        private LeafCdxNode
        (
            // Metadata:
            Int64 offset,
            CdxIndexHeader indexHeader,

            // Node data:
            CdxNodeAttributes attributes,
            UInt16 keyCount,
            Int32 leftSibling,
            Int32 rightSibling,
            UInt16 freeSpace,
            UInt32 recordNumberMask,
            Byte duplicateByteCountMask,
            Byte trailingByteCountMask,
            Byte recordNumberBitsCount,
            Byte duplicateCountBitsCount,
            Byte trailCountBitsCount,
            Byte recordNumberDuplicateCountTrailingCountBytes,
            LeafCdxKeyEntry[] indexKeys
        )
            : base( offset, indexHeader, attributes, keyCount, leftSibling, rightSibling )
        {
            this.FreeSpace               = freeSpace;
            this.RecordNumberMask        = recordNumberMask;
            this.DuplicateByteCountMask  = duplicateByteCountMask;
            this.TrailingByteCountMask   = trailingByteCountMask;
            this.RecordNumberBitsCount   = recordNumberBitsCount;
            this.DuplicateCountBitsCount = duplicateCountBitsCount;
            this.TrailCountBitsCount     = trailCountBitsCount;
            this.IndexKeyEntryLength     = recordNumberDuplicateCountTrailingCountBytes;
            this.IndexKeys               = indexKeys ?? throw new ArgumentNullException( nameof( indexKeys ) );
        }

        public UInt16 FreeSpace               { get; }
        public UInt32 RecordNumberMask        { get; }
        public Byte   DuplicateByteCountMask  { get; }
        public Byte   TrailingByteCountMask   { get; }
        public Byte   RecordNumberBitsCount   { get; }
        public Byte   DuplicateCountBitsCount { get; }
        public Byte   TrailCountBitsCount     { get; }
        /// <summary>Number of bytes holding record number, duplicate count and trailing count</summary>
        public Byte   IndexKeyEntryLength     { get; }
        public LeafCdxKeyEntry[] IndexKeys        { get; }

        public override IList<IKey> GetKeys()
        {
            return this.IndexKeys;
        }
    }
}
