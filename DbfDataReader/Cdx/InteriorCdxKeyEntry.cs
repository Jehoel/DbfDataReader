using System;
using System.Diagnostics;
using System.Text;

namespace Dbf.Cdx
{
    [DebuggerDisplay("KeyValue = {" + nameof(InteriorIndexKeyEntry.StringKey) + "}, RecordNumber = {" + nameof(InteriorIndexKeyEntry.DbfRecordNumber) + "}")]
    public sealed class InteriorIndexKeyEntry
    {
        private readonly Byte[] keyBytes;
        //public ReadOnlyCollection<Byte> KeyBytes { get; }
        public Byte[] KeyBytes => this.keyBytes;

        /// <summary>DBF record number for this key (so if it's an exact match there's no need to traverse-down to a Leaf Node).</summary>
        public UInt32 DbfRecordNumber { get; }

        /// <summary>Location in the file of the next node for this key range.</summary>
        public Int32 NodePointer   { get; }

        private String keyAsString;
        public String StringKey => this.keyAsString ?? ( this.keyAsString = Encoding.ASCII.GetString( this.keyBytes ) );

        internal InteriorIndexKeyEntry(Byte[] keyBytes, UInt32 recordNumber, Int32 nodePointer)
        {
            this.keyBytes        = keyBytes;
            //this.KeyBytes      = new ReadOnlyCollection<byte>( this.keyBytes );

            this.DbfRecordNumber = recordNumber;
            this.NodePointer     = nodePointer;
        }

        internal static InteriorIndexKeyEntry Read(Byte[] keyBuffer, Int32 keyLength, Int32 indexEntryIdx)
        {
            // Microsoft's documentation is incorrect.
            // Their documentation for Compound CDX refers to normal *.idx documentation, which states that each key is followed by "4 hex characters".
            // In CDX inner nodes, however, each key is actually followed by two UInt32 values (for a total of 8 bytes): recordNumber, and nodePointer

            checked
            {
                Int32 startIdx = checked (keyLength + 8) * indexEntryIdx;

                Byte[] key = new Byte[ keyLength ];
                Array.Copy( keyBuffer, startIdx, key, 0, keyLength );

                Int32 i = startIdx + keyLength;
                Int32 recordNumber = keyBuffer[ i + 3 ] | ( keyBuffer[ i + 2 ] << 8 ) | ( keyBuffer[ i + 1 ] << 16 ) | ( keyBuffer[ i + 0 ] << 24 );

                i += 4;

                Int32 nodePointer = keyBuffer[ i + 3 ] | ( keyBuffer[ i + 2 ] << 8 ) | ( keyBuffer[ i + 1 ] << 16 ) | ( keyBuffer[ i + 0 ] << 24 );

                return new InteriorIndexKeyEntry( key, (UInt32)recordNumber, nodePointer ); 
            }
        }
    }
}
