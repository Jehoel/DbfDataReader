#define BranchlessUnrolledLoop
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
    }

    public static class LeafCdxKeyUtility
    {
        internal static LeafCdxKeyEntryData Read(Byte[] buffer, Int32 startIndex, Int32 recordLength, KeyComponent recordNumberInfo, KeyComponent duplicateBytesInfo, KeyComponent trailingBytesInfo)
        {
            if( recordLength > 8 ) throw new CdxException( CdxErrorCode.None ); // TODO: Error code

            Int64 packedEntryLong_Trail_Dupe_Recno = GetPackedEntryAsInt64( buffer, startIndex, recordLength );

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

        public static Int64 GetPackedEntryAsInt64(Byte[] buffer, Int32 startIndex, Int32 recordLength)
        {

#if BranchlessUnrolledLoop

            Int64 packedEntry = 0;
            switch( recordLength )
            {
                case 8:
                    packedEntry |= ( (Int64)buffer[ startIndex + 0 ] <<  0 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 1 ] <<  8 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 2 ] << 16 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 3 ] << 24 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 4 ] << 32 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 5 ] << 40 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 6 ] << 48 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 7 ] << 56 );
                    break;
                case 7:
                    packedEntry |= ( (Int64)buffer[ startIndex + 0 ] <<  0 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 1 ] <<  8 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 2 ] << 16 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 3 ] << 24 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 4 ] << 32 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 5 ] << 40 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 6 ] << 48 );
                    break;
                case 6:
                    packedEntry |= ( (Int64)buffer[ startIndex + 0 ] <<  0 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 1 ] <<  8 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 2 ] << 16 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 3 ] << 24 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 4 ] << 32 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 5 ] << 40 );
                    break;
                case 5:
                    packedEntry |= ( (Int64)buffer[ startIndex + 0 ] <<  0 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 1 ] <<  8 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 2 ] << 16 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 3 ] << 24 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 4 ] << 32 );
                    break;
                case 4:
                    packedEntry |= ( (Int64)buffer[ startIndex + 0 ] <<  0 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 1 ] <<  8 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 2 ] << 16 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 3 ] << 24 );
                    break;
                case 3:
                    packedEntry |= ( (Int64)buffer[ startIndex + 0 ] <<  0 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 1 ] <<  8 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 2 ] << 16 );
                    break;
                case 2:
                    packedEntry |= ( (Int64)buffer[ startIndex + 0 ] <<  0 );
                    packedEntry |= ( (Int64)buffer[ startIndex + 1 ] <<  8 );
                    break;
                case 1:
                    packedEntry |= ( (Int64)buffer[ startIndex + 0 ] <<  0 );
                    break;
            }

            return packedEntry;

#elif BranchedUnrolledLoop

            // Not exactly branchless, but hopefully easy to see.
            Int64 packedEntry = 0;
            if( recordLength >= 1 ) packedEntry |= ( (Int64)buffer[ startIndex + 0 ] <<  0 );
            if( recordLength >= 2 ) packedEntry |= ( (Int64)buffer[ startIndex + 1 ] <<  8 );
            if( recordLength >= 3 ) packedEntry |= ( (Int64)buffer[ startIndex + 2 ] << 16 );
            if( recordLength >= 4 ) packedEntry |= ( (Int64)buffer[ startIndex + 3 ] << 24 );
            if( recordLength >= 5 ) packedEntry |= ( (Int64)buffer[ startIndex + 4 ] << 32 );
            if( recordLength >= 6 ) packedEntry |= ( (Int64)buffer[ startIndex + 5 ] << 40 );
            if( recordLength >= 7 ) packedEntry |= ( (Int64)buffer[ startIndex + 6 ] << 48 );
            if( recordLength >= 8 ) packedEntry |= ( (Int64)buffer[ startIndex + 7 ] << 56 );

            return packedEntry;

#elif ShiftLoop

            Int64 packedEntry = 0;
            for( Int32 i = recordLength; i >= 0; i-- )
            {
                packedEntry = ( packedEntry << 8 ) | buffer[ startIndex + i ];
            }
            return packedEntry;

#else

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

#endif
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
