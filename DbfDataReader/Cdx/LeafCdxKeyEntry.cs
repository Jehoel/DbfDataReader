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

        [CLSCompliant(false)]
        public   UInt32 DbfRecordNumber   { get; }
        
        internal Int32  DuplicateBytes { get; }
        internal Int32  TrailingBytes  { get; }

        private String keyAsString;
        public String StringKey => this.keyAsString ?? ( this.keyAsString = Encoding.ASCII.GetString( this.keyBytes ) );
    }
}
