using System;
using System.Reflection;
using System.Text;

namespace Dbf
{
    internal static class DotNetInternals
    {
        #region Buffer_InternalBlockCopy

        private delegate void Buffer_InternalBlockCopy_Delegate(Array src, int srcOffsetBytes, Array dst, int dstOffsetBytes, int byteCount);

        private static readonly Buffer_InternalBlockCopy_Delegate _Buffer_InternalBlockCopy = Create_Buffer_InternalBlockCopyDelegate();

        private static Buffer_InternalBlockCopy_Delegate Create_Buffer_InternalBlockCopyDelegate()
        {
            MethodInfo method = typeof(Buffer).GetMethod( "InternalBlockCopy", BindingFlags.Static | BindingFlags.NonPublic );
            Delegate del = method.CreateDelegate( typeof(Buffer_InternalBlockCopy_Delegate) );
            Buffer_InternalBlockCopy_Delegate del2 = (Buffer_InternalBlockCopy_Delegate)del;
            return del2;
        }

        public static void Buffer_InternalBlockCopy(Array src, int srcOffsetBytes, Array dst, int dstOffsetBytes, int byteCount)
        {
            _Buffer_InternalBlockCopy( src, srcOffsetBytes, dst, dstOffsetBytes, byteCount );
        }

        #endregion

        #region Decimal_ToDecimal

        private delegate Decimal Decimal_ToDecimal_Delegate(Byte[] buffer);

        private static readonly Decimal_ToDecimal_Delegate _Decimal_ToDecimal = Create_Delegate_Decimal_ToDecimal();

        private static Decimal_ToDecimal_Delegate Create_Delegate_Decimal_ToDecimal()
        {
            MethodInfo method = typeof(Decimal).GetMethod( "ToDecimal", BindingFlags.Static | BindingFlags.NonPublic );
            Delegate del = method.CreateDelegate( typeof(Decimal_ToDecimal_Delegate) );
            Decimal_ToDecimal_Delegate del2 = (Decimal_ToDecimal_Delegate)del;
            return del2;
        }

        public static Decimal Decimal_ToDecimal(Byte[] buffer)
        {
            return _Decimal_ToDecimal( buffer );
        }

        #endregion

        #region DecoderNLS

        private const String DecoderNlsFullName = "System.Text.DecoderNLS";

        private static Boolean IsDecoderNls(Decoder decoder)
        {
            Type t = decoder.GetType();
            while( t != null )
            {
                if( t.FullName == DecoderNlsFullName ) return true;

                t = t.BaseType;
            }

            return false;
        }

        public delegate Boolean DecoderNls_HasState();

        private static readonly MethodInfo _DecoderNls_HasState_Getter = Get_DecoderNls_HasState_Getter_MethodInfo();

        private static MethodInfo Get_DecoderNls_HasState_Getter_MethodInfo()
        {
            Type decoderNlsType = typeof(Decoder).Assembly.GetType( DecoderNlsFullName, throwOnError: true );
            PropertyInfo hasStatePropertyInfo = decoderNlsType.GetProperty( "HasState", BindingFlags.Instance | BindingFlags.NonPublic );

            MethodInfo hasStateGetter = hasStatePropertyInfo.GetGetMethod( nonPublic: true );
            return hasStateGetter;
        }

        public static DecoderNls_HasState Create_DecoderNls_HasState(Decoder decoderNls)
        {
            if( IsDecoderNls( decoderNls ) )
            {
                return (DecoderNls_HasState)_DecoderNls_HasState_Getter.CreateDelegate( typeof(DecoderNls_HasState), decoderNls );
            }
            else
            {
                return null;
            }
        }

        #endregion
    }

    internal class DecoderNlsHelper
    {
        private readonly Decoder decoder;
        private readonly DotNetInternals.DecoderNls_HasState hasStateDelegate;

        internal DecoderNlsHelper(Decoder decoder)
        {
            this.decoder          = decoder;
            this.hasStateDelegate = DotNetInternals.Create_DecoderNls_HasState( decoder );
        }

        public Boolean IsDecoderNls => this.hasStateDelegate != null;

        public Boolean HasState => this.hasStateDelegate?.Invoke() ?? false;
    }
}
