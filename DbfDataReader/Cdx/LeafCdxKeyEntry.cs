using System;
using System.Diagnostics;
using System.Text;

namespace Dbf.Cdx
{
    [DebuggerDisplay("KeyValue = {" + nameof(LeafCdxKeyEntry.StringKey) + "}, RecordNumber = {" + nameof(LeafCdxKeyEntry.DbfRecordNumber) + "}")]
    public class LeafCdxKeyEntry : IKey
    {
        private readonly Byte[] keyBytes;

        internal LeafCdxKeyEntry(Byte[] keyData, UInt32 recordNumber, Int32 duplicateBytes, Int32 trailingBytes)
        {
            this.keyBytes        = keyData;

            this.DbfRecordNumber = recordNumber;
            this.DuplicateBytes  = duplicateBytes;
            this.TrailingBytes   = trailingBytes;
        }

        public Byte[] KeyBytes => this.keyBytes;

        Boolean IKey.IsInteriorNode => false;

        [CLSCompliant(false)]
        public   UInt32 DbfRecordNumber   { get; }
        
        internal Int32  DuplicateBytes { get; }
        internal Int32  TrailingBytes  { get; }

        private String stringKey;
        public String StringKey => this.stringKey ?? ( this.stringKey = Encoding.ASCII.GetString( this.keyBytes, 0, count: this.keyBytes.Length - this.TrailingBytes ) );
    }

    internal class LeafCdxKeyEntryData
    {
        public LeafCdxKeyEntryData(UInt32 recordNumber, Byte duplicateBytes, Byte trailingBytes)
        {
            this.RecordNumber = recordNumber;
            this.DuplicateBytes = duplicateBytes;
            this.TrailingBytes = trailingBytes;
        }

        public UInt32 RecordNumber   { get; }
        public Byte   DuplicateBytes { get; }
        public Byte   TrailingBytes  { get; }

        public static LeafCdxKeyEntryData Read(Byte[] buffer, Int32 startIndex, Int32 recordLength, KeyComponent recordNumberInfo, KeyComponent duplicateBytesInfo, KeyComponent trailingBytesInfo)
        {
            Int32 a = startIndex;

            UInt32 recordNumber;
            {
                Int32 recordNumberInt;

                // RecordNumbers are in big-endian order.
                if( recordNumberInfo.BitCount <= 8 )
                {
                    recordNumberInt = buffer[ a ];
                }
                else if( recordNumberInfo.BitCount <= 16 )
                {
                    recordNumberInt = ( buffer[ a + 1 ] << 8 ) | buffer[ a + 0 ];
                }
                else if( recordNumberInfo.BitCount <= 24 )
                {
                    recordNumberInt = ( buffer[ a + 2 ] << 16 ) | ( buffer[ a + 1 ] << 8 ) | buffer[ a + 0 ];
                }
                else if( recordNumberInfo.BitCount <= 32 )
                {
                    recordNumberInt = ( buffer[ a + 3 ] << 24 ) | ( buffer[ a + 2 ] << 16 ) | ( buffer[ a + 1 ] << 8 ) | buffer[ a + 0 ];
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
                Byte bN0 = buffer[ a + (recordLength-1) ]; // b[N-0]
                Byte bN1 = buffer[ a + (recordLength-2) ]; // b[N-1]
                Int32 trailingAndDuplicate = ( bN1 << 8 ) | bN0;

                duplicateBytes = (UInt32)( trailingAndDuplicate & duplicateBytesInfo.Mask );

                /////

                trailingBytes = (UInt32)( trailingAndDuplicate >> duplicateBytesInfo.BitCount );
                trailingBytes &= trailingBytesInfo.Mask;

                if( duplicateBytes > Byte.MaxValue ) throw new CdxException( CdxErrorCode.None ); // TODO: Actual error code.
                if( trailingBytes  > Byte.MaxValue ) throw new CdxException( CdxErrorCode.None ); // TODO: Actual error code.
            }

            return new LeafCdxKeyEntryData( recordNumber, (Byte)duplicateBytes, (Byte)trailingBytes );
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
