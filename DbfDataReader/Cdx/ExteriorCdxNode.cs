using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Dbf.Cdx
{
    public sealed class ExteriorCdxNode : BaseCdxNode
    {
        private ExteriorCdxNode
        (
            Int64 offset,
            CompactIndexNodeAttributes attributes,
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
            Byte[] indexKeys
        )
            : base( offset, attributes, keyCount, leftSibling, rightSibling )
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
        public Byte[] IndexKeys               { get; }

        private const Int32 IndexKeyBufferLength = 488;

        public static ExteriorCdxNode Read(Int64 offset, CompactIndexNodeAttributes attributes, BinaryReader reader)
        {
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
            Byte[] indexKeys               = reader.ReadBytes( IndexKeyBufferLength );

#if DEBUG
            Int64 posActual = reader.BaseStream.Position;
            Int64 posExpected = offset + 512;
            if( posActual != posExpected ) throw new InvalidOperationException("Didn't read expected number of bytes in CompactIndexExteriorNode.");
#endif

            return new ExteriorCdxNode(
                offset,
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
                indexKeys              
            );
        }

        public IEnumerable<CompactIndexExteriorNodeIndexEntry> GetIndexEntries(CdxFile indexFile)
        {
            BinaryReader reader = indexFile.Reader;

            // Move reader to the start of the IndexKeys block, at 24 bytes offset from the start of the ExteriorNode.
            reader.BaseStream.Seek( this.Offset + 24, SeekOrigin.Begin );

            Int64 end = this.Offset + 512; // TODO: limit this to non-garbage data.

            if( this.IndexKeyEntryLength > 8 ) throw new InvalidOperationException("IndexKey entries must be 8 bytes long or shorter."); // because we use UInt64 for bitwise operations.

            Int32 totalBits = this.RecordNumberBitsCount + this.DuplicateCountBitsCount + this.TrailCountBitsCount;
            if( this.IndexKeyEntryLength * 8 != totalBits ) throw new InvalidOperationException("IndexKeyEntryLength does not match the combined bit count.");
            // for now, we assume all bit-lengths occupy full bytes:
            //if( this.RecordNumberBitsCount   % 8 != 0 ) throw new InvalidOperationException("RecordNumberBitsCount is not an integral number of bytes.");
            //if( this.DuplicateCountBitsCount % 8 != 0 ) throw new InvalidOperationException("DuplicateCountBitsCount is not an integral number of bytes.");
            //if( this.TrailCountBitsCount     % 8 != 0 ) throw new InvalidOperationException("TrailCountBitsCount is not an integral number of bytes.");

            /*Byte[] entryBuffer = new Byte[ this.IndexKeyEntryLength ];

            while( reader.BaseStream.Position < end )
            {
                {
                    Int32 read = reader.Read( entryBuffer, 0, this.IndexKeyEntryLength );
                    if( read != this.IndexKeyEntryLength ) throw new InvalidOperationException("Could not read all bytes of an entry.");

                    // reverse the array because BitConverter will use native byte ordering:
                    //if( BitConverter.IsLittleEndian ) Array.Reverse( entryBuffer );
                }
                
                UInt64 entry = BitConverter.ToUInt64( entryBuffer, 0 );

                // entry format:
                // <record-number><duplicate-bytes-count><trail-count>
                Int32 shiftToGetRecordNumber        = 64 - this.RecordNumberBitsCount;
                Int32 shiftToGetDuplicateBytesCount = ( 64 - this.RecordNumberBitsCount ) - this.DuplicateCountBitsCount;
                Int32 shiftToGetTrailBytesCount     = ( ( 64 - this.RecordNumberBitsCount ) - this.DuplicateCountBitsCount ) - this.TrailCountBitsCount;

                UInt64 recordNumber        = ( entry >> shiftToGetRecordNumber        ) & this.RecordNumberMask;
                UInt64 duplicateBytesCount = ( entry >> shiftToGetDuplicateBytesCount ) & this.DuplicateByteCountMask;
                UInt64 trailBytesCount     = ( entry >> shiftToGetTrailBytesCount     ) & this.TrailingByteCountMask;

                yield return new CompactIndexExteriorNodeIndexEntry( (UInt32)recordNumber, (UInt32)duplicateBytesCount, (UInt32)trailBytesCount );
            }*/

            Boolean supportedBitPacking =
                this.IndexKeyEntryLength == 4 &&
                this.RecordNumberBitsCount == 24 &&
                this.RecordNumberMask == 0x00FFFFFF &&
                this.DuplicateCountBitsCount == 4 &&
                this.DuplicateByteCountMask == 0x0F &&
                this.TrailCountBitsCount == 4 &&
                this.TrailingByteCountMask == 0x0F;

            if( !supportedBitPacking ) throw new NotSupportedException("Unsupported bit-packing options.");

            //////

            Int32 keyValueSrc = IndexKeyBufferLength; // 488

            for( Int32 i = 0; i < this.KeyCount; i++ )
            {
                Byte[] entryBuffer = new Byte[ this.IndexKeyEntryLength ];
                {
                    Int32 read = reader.Read( entryBuffer, 0, this.IndexKeyEntryLength );
                    if( read != this.IndexKeyEntryLength ) throw new InvalidOperationException("Could not read all bytes of an entry.");
                }

                Int32 recordNumberInt =
                    ( entryBuffer[3] << 24 ) |
                    ( entryBuffer[2] << 16 ) |
                    ( entryBuffer[1] <<  8 ) |
                    ( entryBuffer[0] );

                Int64 recordNumber64 = recordNumberInt & this.RecordNumberMask;
                UInt32 recordNumber = (UInt32)recordNumber64;

                Int32 bi = 16 - this.TrailCountBitsCount - this.DuplicateCountBitsCount;

                //////

                Int32 trailAndDupeInt = ( entryBuffer[3] << 8 ) | ( entryBuffer[2] );
                UInt16 trailAndDupe = (UInt16)( trailAndDupeInt >> bi );

                Int32 duplicateBytesCount = ( i == 0 ) ? 0 : ( trailAndDupe & this.DuplicateByteCountMask );
                Int32 trailingBytesCount  = ( trailAndDupe >> this.DuplicateCountBitsCount ) & this.TrailingByteCountMask;

                Int32 newBytesCount = indexFile.Header.KeyLength - duplicateBytesCount - trailingBytesCount;

                keyValueSrc -= newBytesCount;
                Int32 keyValueStart = keyValueSrc;

                CompactIndexExteriorNodeIndexEntry entry = new CompactIndexExteriorNodeIndexEntry()
                {
                    EntryBytes = entryBuffer,
                    RecordNumber = recordNumber,
                    DuplicateBytes = duplicateBytesCount,
                    TrailingBytes = trailingBytesCount,

                    KeyValueIndex0 = keyValueStart,
                    KeyValueIndexN = keyValueStart + newBytesCount
                };

//                if( duplicateBytesCount > 0 )
//                {
//                    entry.KeyValueRanges.Add( new Range( ,  ) );
//                }

                yield return entry;
            }
        }
    }

    [DebuggerDisplay("KeyValue = {KeyValue}, RecordNumber = {RecordNumber}")]
    public class CompactIndexExteriorNodeIndexEntry
    {
        public Byte[] EntryBytes     { get; set; }
        public UInt32 RecordNumber   { get; set; }
        public Int32  DuplicateBytes { get; set; } // what does "duplicate bytes" mean?
        public Int32  TrailingBytes  { get; set; }
        
//        public List<Range> KeyValueRanges { get; } = new List<Range>();

        public Int32  KeyValueIndex0 { get; set; }
        public Int32  KeyValueIndexN { get; set; }
        public Int32  KeyValueLength => this.KeyValueIndexN - this.KeyValueIndex0; //this.KeyValueRanges.Sum( r => r.Length );

        public String KeyValue { get; set; }
    }

    

    public struct Range
    {
        public Range(UInt32 start, UInt32 end)
        {
            this.Start = start;
            this.End   = end;
        }

        public UInt32 Start { get; }
        public UInt32 End   { get; }
        public UInt32 Length => this.End - this.Start;
    }
}
