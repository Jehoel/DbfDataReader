using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Dbf.Cdx
{
    public sealed class LeafCdxNode : BaseCdxNode
    {
        private const Int32 IndexKeyBufferLength = 488;

        public static LeafCdxNode Read(CdxFileHeader indexHeader, Int64 offset, CdxNodeAttributes attributes, BinaryReader reader)
        {
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            //CompactIndexNodeAttributes attributes = (CompactIndexNodeAttributes)reader.ReadUInt16();
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

            CdxKeyEntry[] entries;
            UnpackIndexKeys(
                keyCount,
                indexHeader.KeyLength,
                recordNumberDuplicateCountTrailingCountBytes,
                new KeyCmpt( recordNumberMask, recordNumberBitsCount ),
                new KeyCmpt( duplicateByteCountMask, duplicateCountBitsCount ),
                new KeyCmpt( trailingByteCountMask, trailCountBitsCount ),
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
            KeyCmpt recordNumberInfo,
            KeyCmpt duplicateBytesInfo,
            KeyCmpt trailingBytesInfo,
            Byte[] packed,
            out CdxKeyEntry[] entries
        )
        {
            if( packed == null ) throw new ArgumentNullException(nameof(packed));
            if( packed.Length != IndexKeyBufferLength /* 488 */ ) throw new ArgumentException("Value must have a length of 488.", nameof(packed));
            if( packedKeyEntryLength < 2 ) throw new ArgumentException("Packed Key Entries must be at least 2 bytes long.");

            Int32 keyValueSrc = packed.Length;

            entries = new CdxKeyEntry[ keyCount ];

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
                    Int32 recordNumberSigned;

                    // RecordNumbers are in big-endian order.
                    if( recordNumberInfo.BitCount <= 8 )
                    {
                        recordNumberSigned = packed[ a ];
                    }
                    else if( recordNumberInfo.BitCount <= 16 )
                    {
                        recordNumberSigned = ( packed[ a + 1 ] << 8 ) | packed[ a + 0 ];
                    }
                    else if( recordNumberInfo.BitCount <= 24 )
                    {
                        recordNumberSigned = ( packed[ a + 2 ] << 16 ) | ( packed[ a + 1 ] << 8 ) | packed[ a + 0 ];
                    }
                    else if( recordNumberInfo.BitCount <= 32 )
                    {
                        recordNumberSigned = ( packed[ a + 3 ] << 24 ) | ( packed[ a + 2 ] << 16 ) | ( packed[ a + 1 ] << 8 ) | packed[ a + 0 ];
                    }
                    else
                    {
                        // manual for-loop? but this number will never be bigger than 2^32... so just throw for now.
                        throw new NotSupportedException("RecordNumber.BitCount values bigger than 32 are not supported.");
                    }

                    recordNumber = (UInt32)recordNumberSigned & recordNumberInfo.Mask;
                }

                UInt32 duplicateBytes;
                UInt32 trailingBytes;
                {
                    // Get the last two bytes of the array. Note we assume packedKeyEntryLength is >=2
                    Byte bN  = packed[ a + (packedKeyEntryLength-1) ]; // b[N  ]
                    Byte bN1 = packed[ a + (packedKeyEntryLength-2) ]; // b[N-1]
                    Int32 trailingAndDuplicate = ( bN1 << 8 ) | bN;

                    duplicateBytes = (UInt32)( trailingAndDuplicate & duplicateBytesInfo.Mask );

                    /////

                    trailingBytes = (UInt32)( trailingAndDuplicate >> duplicateBytesInfo.BitCount );
                    trailingBytes &= trailingBytesInfo.Mask;
                }

                Int32 newBytesCount = (Int32)(keyLength - duplicateBytes - trailingBytes);

                keyValueSrc -= newBytesCount;

                if( ( i == 0 && duplicateBytes > 0 ) || ( previousKeyData == null && duplicateBytes > 0 ) )
                {
                    throw new InvalidOperationException("KeyEntry specifies duplicate-bytes from previous entry, but there is no previous entry.");
                }

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

                CdxKeyEntry keyEntry = new CdxKeyEntry( keyData, recordNumber, (Int32)duplicateBytes, (Int32)trailingBytes );
                entries[i] = keyEntry;

                previousKeyData = keyData;
            }
        }

        private LeafCdxNode
        (
            // Metadata:
            Int64 offset,
            CdxFileHeader indexHeader,

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
            CdxKeyEntry[] indexKeys
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
//            this.IndexKeysPacked         = indexKeysPacked ?? throw new ArgumentNullException( nameof( indexKeysPacked ) );
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
        public CdxKeyEntry[] IndexKeys        { get; }
    }

    /// <summary>CDX Key Component info.</summary>
    internal struct KeyCmpt
    {
        public KeyCmpt(UInt32 mask, Byte bitCount)
        {
            this.Mask     = mask;
            this.BitCount = bitCount;
        }

        public readonly UInt32 Mask;
        public readonly Byte   BitCount;
    }
}
