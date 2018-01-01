using System;
using System.Diagnostics;
using System.Text;

namespace Dbf.Cdx
{
    [DebuggerDisplay("KeyValue = {StringKey}, RecordNumber = {RecordNumber}")]
    public class CdxKeyEntry : IKey
    {
        private readonly Byte[] keyData;

        internal CdxKeyEntry(Byte[] keyData, UInt32 recordNumber, Int32 duplicateBytes, Int32 trailingBytes)
        {
            this.keyData         = keyData;

            this.DbfRecordNumber = recordNumber;
            this.DuplicateBytes  = duplicateBytes;
            this.TrailingBytes   = trailingBytes;
        }

        public Byte[] KeyBytes => this.keyData;

        [CLSCompliant(false)]
        public   UInt32 DbfRecordNumber   { get; }
        
        internal Int32  DuplicateBytes { get; }
        internal Int32  TrailingBytes  { get; }

        private String keyAsString;
        public String StringKey => this.keyAsString ?? ( this.keyAsString = Encoding.ASCII.GetString( this.keyData ) );
    }
}
