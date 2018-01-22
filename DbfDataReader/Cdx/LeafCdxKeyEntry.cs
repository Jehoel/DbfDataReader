using System;
using System.Diagnostics;
using System.Text;

namespace Dbf.Cdx
{
    [DebuggerDisplay("KeyValue = {" + nameof(LeafCdxKeyEntry.KeyAsString) + "}, RecordNumber = {" + nameof(LeafCdxKeyEntry.DbfRecordNumber) + "}")]
    public class LeafCdxKeyEntry
    {
        internal LeafCdxKeyEntry(Byte[] keyData, Int32 recordNumber)
        {
            this.KeyBytes        = keyData;
            this.DbfRecordNumber = (UInt32)recordNumber;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays" )]
        public Byte[] KeyBytes        { get; }
        public UInt32 DbfRecordNumber { get; }

        private String keyAsString;
        public String KeyAsString => this.keyAsString ?? ( this.keyAsString = Encoding.ASCII.GetString( this.KeyBytes ) );
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
            Int32 l = recordLength;

            Int64 packedEntryLong_Trail_Dupe_Recno =
                ( l > 0 ? ( (Int64)packedEntry[ l - 1 ] <<  0 ) : 0 ) |
                ( l > 1 ? ( (Int64)packedEntry[ l - 2 ] <<  8 ) : 0 ) |
                ( l > 2 ? ( (Int64)packedEntry[ l - 3 ] << 16 ) : 0 ) |
                ( l > 3 ? ( (Int64)packedEntry[ l - 4 ] << 24 ) : 0 ) |
                ( l > 4 ? ( (Int64)packedEntry[ l - 5 ] << 32 ) : 0 ) |
                ( l > 5 ? ( (Int64)packedEntry[ l - 6 ] << 40 ) : 0 ) |
                ( l > 6 ? ( (Int64)packedEntry[ l - 7 ] << 48 ) : 0 ) |
                ( l > 7 ? ( (Int64)packedEntry[ l - 8 ] << 56 ) : 0 );

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
