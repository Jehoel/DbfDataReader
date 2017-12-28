using System;
using System.Diagnostics;
using System.Text;

namespace Dbf.Cdx
{
    [DebuggerDisplay("KeyValue = {StringKey}, RecordNumber = {RecordNumber}")]
    public class CdxKeyEntry
    {
        private readonly Byte[] keyData;

        internal CdxKeyEntry(Byte[] keyData, UInt32 recordNumber, Int32 duplicateBytes, Int32 trailingBytes)
        {
            this.keyData        = keyData;

            this.RecordNumber   = recordNumber;
            this.DuplicateBytes = duplicateBytes;
            this.TrailingBytes  = trailingBytes;
        }

        [CLSCompliant(false)]
        public   UInt32 RecordNumber   { get; }
        
        internal Int32  DuplicateBytes { get; }
        internal Int32  TrailingBytes  { get; }

        private String keyAsString;
        public String StringKey => this.keyAsString ?? ( this.keyAsString = Encoding.ASCII.GetString( this.keyData ) );
    }
}
