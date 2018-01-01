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
            throw new NotImplementedException();
        }
    }

    public enum CdxErrorCode
    {
        None,
        CompoundIndexHeaderDoesNotHaveCompoundIndexOption,
        RootNodeDoesNotHaveRootAttribute,
        LeftmostNodeHasLeftSibling
    }
}
