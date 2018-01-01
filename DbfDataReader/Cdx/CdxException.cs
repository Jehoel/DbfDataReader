using System;

namespace Dbf.Cdx
{
    [Serializable]
    public class CdxException : Exception
    {
        public CdxException() { }
        public CdxException(string message) : base( message ) { }
        public CdxException(string message, Exception inner) : base( message, inner ) { }
        protected CdxException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base( info, context ) { }

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
        InvalidCdxIndexOptionsAttributes
    }
}
