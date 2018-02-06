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
        internal static LeafCdxNode Read(CdxIndexHeader indexHeader, Int64 offset, CdxNodeAttributes attributes, BinaryReader reader)
        {
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

            if( BuildOptions.StrictChecks )
            {
                Int64 posActual = reader.BaseStream.Position;
                Int64 posExpected = offset + 512;
                if( posActual != posExpected ) throw new InvalidOperationException("Didn't read expected number of bytes in " + nameof(LeafCdxNode) + ".");
            }

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
            if( packedKeyEntryLength > 8 ) throw new ArgumentException("Packed Key Entry lengths must be 8 bytes or shorter.");

            List<LeafCdxKeyEntryData> temp = new List<LeafCdxKeyEntryData>();

            Byte[] previousKeyData = null;
            for( Int32 i = 0; i < keyCount; i++ )
            {
                LeafCdxKeyEntryData record = LeafCdxKeyUtility.Read( packed, i * packedKeyEntryLength, packedKeyEntryLength, recordNumberInfo, duplicateBytesInfo, trailingBytesInfo );
                temp.Add( record );
            }

            // Get the key values:

            entries = new LeafCdxKeyEntry[ keyCount ];

            Int32 keyValueSrc = packed.Length;

            for( Int32 i = 0; i < keyCount; i++ )
            {
                LeafCdxKeyEntryData record = temp[i];

                Int32 newBytesCount = keyLength - record.DuplicateBytes - record.TrailingBytes;

                keyValueSrc -= newBytesCount;

                if( BuildOptions.StrictChecks )
                {
                    if( keyValueSrc < 0 ) throw new CdxException( CdxErrorCode.InvalidLeafNodeCalculatedKeyStartIndex );
                    if( ( i == 0 && record.DuplicateBytes > 0 ) || ( previousKeyData == null && record.DuplicateBytes > 0 ) ) throw new CdxException( CdxErrorCode.FirstLeafNodeKeyEntryHasDuplicateBytes );
                }

                //////////////////

                Int32 actualKeyLength = keyLength - record.TrailingBytes;

                Byte[] keyData = new Byte[ actualKeyLength ];
                for( UInt32 d = 0; d < Math.Min( record.DuplicateBytes, actualKeyLength ); d++ )
                {
                    keyData[d] = previousKeyData[d];
                }

                for
                (
                    UInt32 b = record.DuplicateBytes, src = 0;
                    src < newBytesCount && b < actualKeyLength;
                    b++, src++
                )
                {
                    keyData[b] = packed[ keyValueSrc + src ];
                }

                LeafCdxKeyEntry keyEntry = new LeafCdxKeyEntry( keyData, record.RecordNumber );
                entries[i] = keyEntry;

                previousKeyData = keyData;
            }
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
        public IReadOnlyList<LeafCdxKeyEntry> IndexKeys { get; }
    }
}
