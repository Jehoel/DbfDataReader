using System;
using System.Runtime.Serialization;

namespace Dbf.Cdx
{
    [Serializable]
    public class CdxException : Exception
    {
        public CdxException() { }
        public CdxException(string message) : base( message ) { }
        public CdxException(string message, Exception inner) : base( message, inner ) { }

        protected CdxException( SerializationInfo info, StreamingContext context)
            : base( info, context )
        {
            this.Code = (CdxErrorCode)info.GetInt32( nameof(this.Code) );
        }

        public CdxException(CdxErrorCode code)
            : base( GetErrorMessage( code ) )
        {
            this.Code    = code;
            this.HResult = (Int32)code;
        }

        public CdxErrorCode Code { get; }

        private static String GetErrorMessage(CdxErrorCode code)
        {
            return code.ToString(); // HACK, TODO: Add localized messages.
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if( info == null ) throw new ArgumentNullException( nameof(info) );

            base.GetObjectData( info, context );

            info.AddValue( nameof(this.Code), this.Code );
        }
    }

    public enum CdxErrorCode
    {
        None,
        CompoundIndexHeaderDoesNotHaveCompoundIndexOption,
        RootNodeDoesNotHaveRootAttribute,
        LeftmostNodeHasLeftSibling,
        InvalidNodeAttributes,
        InvalidInteriorNodeKeyCount,
        InvalidInteriorNodeLeftSibling,
        InvalidInteriorNodeRightSibling,
        InvalidLeafNodeCalculatedKeyStartIndex,
        FirstLeafNodeKeyEntryHasDuplicateBytes,
        DidNotRead1024BytesInCdxIndexHeader,
        InvalidCdxIndexOptionsAttributes,
        InteriorNodeHasNoKeyEntries
    }
}
