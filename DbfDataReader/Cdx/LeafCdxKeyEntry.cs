using System;
using System.Diagnostics;
using System.Text;

namespace Dbf.Cdx
{
    [DebuggerDisplay("KeyValue = {" + nameof(LeafCdxKeyEntry.StringKey) + "}, RecordNumber = {" + nameof(LeafCdxKeyEntry.DbfRecordNumber) + "}")]
    public class LeafCdxKeyEntry
    {
        private readonly Byte[] keyBytes;

        internal LeafCdxKeyEntry(Byte[] keyData, Int32 recordNumber, Byte duplicateBytes, Byte trailingBytes)
        {
            this.keyBytes        = keyData;

            this.DbfRecordNumber = (UInt32)recordNumber;
            this.DuplicateBytes  = duplicateBytes;
            this.TrailingBytes   = trailingBytes;
        }

        public Byte[] KeyBytes => this.keyBytes;

        public  UInt32 DbfRecordNumber   { get; }
        
        public  Byte  DuplicateBytes { get; }
        public  Byte  TrailingBytes  { get; }

        private String stringKey;
        public String StringKey => this.stringKey ?? ( this.stringKey = Encoding.ASCII.GetString( this.keyBytes, 0, count: this.keyBytes.Length - this.TrailingBytes ) );
    }

    internal class LeafCdxKeyEntryData
    {
        public LeafCdxKeyEntryData(Int32 recordNumber, Byte duplicateBytes, Byte trailingBytes)
        {
            this.RecordNumber = recordNumber;
            this.DuplicateBytes = duplicateBytes;
            this.TrailingBytes = trailingBytes;
        }

        public Int32 RecordNumber   { get; }
        public Byte  DuplicateBytes { get; }
        public Byte  TrailingBytes  { get; }

        public static LeafCdxKeyEntryData Read(Byte[] buffer, Int32 startIndex, Int32 recordLength, KeyComponent recordNumberInfo, KeyComponent duplicateBytesInfo, KeyComponent trailingBytesInfo)
        {
            if( recordLength > 8 ) throw new CdxException( CdxErrorCode.None ); // TODO: Error code

            Int64 packedEntryLong_Trail_Dupe_Recno = GetPackedEntryAsLong( buffer, startIndex, recordLength );

            // Then extract each component:
            // Always use 32 bits to get the record-number:
                
            Int32 recordNumber = (Int32)( packedEntryLong_Trail_Dupe_Recno & recordNumberInfo.Mask );

            Int64 packedEntryLong_Trail_Dupe = packedEntryLong_Trail_Dupe_Recno >> recordNumberInfo.BitCount;

            Byte duplicateBytes = (Byte)( packedEntryLong_Trail_Dupe & duplicateBytesInfo.Mask );

            Int64 packedEntryLong_Trail = packedEntryLong_Trail_Dupe >> duplicateBytesInfo.BitCount;

            Byte trailingBytes = (Byte)( packedEntryLong_Trail & trailingBytesInfo.Mask );

            LeafCdxKeyEntryData record = new LeafCdxKeyEntryData( recordNumber, duplicateBytes, trailingBytes );
            return record;
        }

        private static Int64 GetPackedEntryAsLong(Byte[] buffer, Int32 startIndex, Int32 recordLength)
        {
            // TODO: Find a way to reverse the bytes without needing the temporary buffer `packedEntry`.

            Byte[] packedEntry = new Byte[ recordLength ]; // this array exists so I can see what the current window of data looks like.
            Array.Copy( buffer, startIndex, packedEntry, 0, packedEntry.Length );

            // Idea: Reverse the array first!
            Array.Reverse( packedEntry );

            // Then convert to a long (assuming entries are never longer than 8 bytes):
            Int32 z = recordLength - 1; // ( startIndex + recordLength ) - 1;
            Int32 l = recordLength;

            Int64 packedEntryLong_Trail_Dupe_Recno =
                ( l > 0 ? ( packedEntry[ z - 0 ] <<  0 ) : 0 ) |
                ( l > 1 ? ( packedEntry[ z - 1 ] <<  8 ) : 0 ) |
                ( l > 2 ? ( packedEntry[ z - 2 ] << 16 ) : 0 ) |
                ( l > 3 ? ( packedEntry[ z - 3 ] << 24 ) : 0 ) |
                ( l > 4 ? ( packedEntry[ z - 4 ] << 32 ) : 0 ) |
                ( l > 5 ? ( packedEntry[ z - 5 ] << 40 ) : 0 ) |
                ( l > 6 ? ( packedEntry[ z - 6 ] << 48 ) : 0 ) |
                ( l > 7 ? ( packedEntry[ z - 7 ] << 56 ) : 0 );

            return packedEntryLong_Trail_Dupe_Recno;
        }
    }

    /// <summary>CDX Key Component info.</summary>
    internal struct KeyComponent
    {
        public KeyComponent(UInt32 mask, Byte bitCount)
        {
            this.Mask     = mask;
            this.BitCount = bitCount;
        }

        public readonly UInt32 Mask;
        public readonly Byte   BitCount;
    }
}
