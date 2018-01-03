using System;
using System.Runtime;
using System.Text;
using System.IO;
using System.Globalization;
using System.Diagnostics.Contracts;
using System.Security;
using System.Threading.Tasks;
using System.Threading;

namespace Dbf
{
    internal static class __Error
    {
        public static Exception FileNotOpen()
        {
            throw new NotImplementedException();
        }

        public static Exception EndOfFile()
        {
            throw new NotImplementedException();
        }
    }
    internal static class Environment
    {
        public static String GetResourceString(String name)
        {
            return name;
        }
        public static String GetResourceString(String name, Int32 arg0)
        {
            return name + " " + arg0.ToString( CultureInfo.InvariantCulture );
        }
    }

    public sealed class AsyncBinaryReader : IDisposable
    {
        private const int MaxCharBytesSize = 128;

        private readonly Stream   stream;
        private readonly byte[]   buffer;
        private readonly Decoder  decoder;
        private readonly DecoderNlsHelper decoderNls;
        private          byte[]   charBytes;
        private          char[]   singleChar;
        private          char[]   charBuffer;
        private readonly Byte[]   singleByteBuffer = new Byte[1];
        private readonly int      maxCharsSize;  // From MaxCharBytesSize & Encoding

        // Performance optimization for Read() w/ Unicode.  Speeds us up by ~40% 
        private readonly bool     is2BytesPerChar;
        private readonly bool     leaveOpen;

        public AsyncBinaryReader(Stream input) : this( input, new UTF8Encoding(), false )
        {
        }

        public AsyncBinaryReader(Stream input, Encoding encoding) : this( input, encoding, false )
        {
        }

        public AsyncBinaryReader(Stream input, Encoding encoding, bool leaveOpen)
        {
            if( input == null )
            {
                throw new ArgumentNullException( "input" );
            }
            if( encoding == null )
            {
                throw new ArgumentNullException( "encoding" );
            }
            if( !input.CanRead )
                throw new ArgumentException( Environment.GetResourceString( "Argument_StreamNotReadable" ) );
            Contract.EndContractBlock();
            
            this.stream = input;
            this.decoder = encoding.GetDecoder();
            this.decoderNls = new DecoderNlsHelper( this.decoder );
            this.maxCharsSize = encoding.GetMaxCharCount( MaxCharBytesSize );
            int minBufferSize = encoding.GetMaxByteCount(1);  // max bytes per one char
            if( minBufferSize < 16 )
                minBufferSize = 16;
            this.buffer = new byte[minBufferSize];
            // this.charBuffer and this.charBytes will be left null.

            // For Encodings that always use 2 bytes per char (or more), 
            // special case them here to make Read() & Peek() faster.
            this.is2BytesPerChar = encoding is UnicodeEncoding;
            this.leaveOpen = leaveOpen;

            Contract.Assert( this.decoder != null, "[BinaryReader.ctor]this.decoder!=null" );
        }

        public Stream BaseStream
        {
            get
            {
                return this.stream;
            }
        }

        public void Dispose()
        {
            if( !this.leaveOpen )
            {
                this.stream.Dispose();
            }
        }

        public async Task<Int32> PeekCharAsync()
        {
            Contract.Ensures( Contract.Result<int>() >= -1 );

            if( this.stream == null ) __Error.FileNotOpen();

            if( !this.stream.CanSeek )
                return -1;
            long origPos = this.stream.Position;
            int ch = await ReadAsync().ConfigureAwait(false);
            this.stream.Position = origPos;
            return ch;
        }

        public Task<Int32> ReadAsync()
        {
            Contract.Ensures( Contract.Result<int>() >= -1 );

            if( this.stream == null )
            {
                __Error.FileNotOpen();
            }
            return InternalReadOneCharAsync();
        }

        public async Task<Boolean> ReadBooleanAsync()
        {
            await FillBufferAsync( 1 ).ConfigureAwait(false);
            return ( this.buffer[0] != 0 );
        }

        public async Task<Byte> ReadByteAsync()
        {
            // Inlined to avoid some method call overhead with FillBuffer.
            if( this.stream == null ) __Error.FileNotOpen();

            int b = await this.StreamReadByteAsync(CancellationToken.None).ConfigureAwait(false);
            if( b == -1 ) __Error.EndOfFile();
                
            return (byte)b;
        }

        [CLSCompliant( false )]
        public async Task<SByte> ReadSByteAsync()
        {
            await FillBufferAsync( 1 ).ConfigureAwait(false);
            return (sbyte)( this.buffer[0] );
        }

        public async Task<Char> ReadCharAsync()
        {
            int value = await this.ReadAsync().ConfigureAwait(false);
            if( value == -1 )
            {
                __Error.EndOfFile();
            }
            return (char)value;
        }

        public async Task<Int16> ReadInt16Async()
        {
            await FillBufferAsync( 2 ).ConfigureAwait(false);
            return (short)( this.buffer[0] | this.buffer[1] << 8 );
        }

        [CLSCompliant( false )]
        public async Task<UInt16> ReadUInt16Async()
        {
            await FillBufferAsync( 2 ).ConfigureAwait(false);
            return (ushort)( this.buffer[0] | this.buffer[1] << 8 );
        }

        public async Task<Int32> ReadInt32Async()
        {
            await FillBufferAsync( 4 ).ConfigureAwait(false);
            return (int)( this.buffer[0] | this.buffer[1] << 8 | this.buffer[2] << 16 | this.buffer[3] << 24 );
        }

        [CLSCompliant( false )]
        public async Task<UInt32> ReadUInt32Async()
        {
            await FillBufferAsync( 4 ).ConfigureAwait(false);
            return (uint)( this.buffer[0] | this.buffer[1] << 8 | this.buffer[2] << 16 | this.buffer[3] << 24 );
        }

        public async Task<Int64> ReadInt64Async()
        {
            await FillBufferAsync( 8 ).ConfigureAwait(false);
            uint lo = (uint)(this.buffer[0] | this.buffer[1] << 8 | this.buffer[2] << 16 | this.buffer[3] << 24);
            uint hi = (uint)(this.buffer[4] | this.buffer[5] << 8 | this.buffer[6] << 16 | this.buffer[7] << 24);
            return (long)( (ulong)hi ) << 32 | lo;
        }

        [CLSCompliant( false )]
        public async Task<UInt64> ReadUInt64Async()
        {
            await FillBufferAsync( 8 ).ConfigureAwait(false);
            uint lo = (uint)(this.buffer[0] | this.buffer[1] << 8 | this.buffer[2] << 16 | this.buffer[3] << 24);
            uint hi = (uint)(this.buffer[4] | this.buffer[5] << 8 | this.buffer[6] << 16 | this.buffer[7] << 24);
            return ( (ulong)hi ) << 32 | lo;
        }

#if UNSAFE
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe float ReadSingle() {
            FillBuffer(4);
            uint tmpBuffer = (uint)(this.buffer[0] | this.buffer[1] << 8 | this.buffer[2] << 16 | this.buffer[3] << 24);
            return *((float*)&tmpBuffer);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe double ReadDouble() {
            FillBuffer(8);
            uint lo = (uint)(this.buffer[0] | this.buffer[1] << 8 | this.buffer[2] << 16 | this.buffer[3] << 24);
            uint hi = (uint)(this.buffer[4] | this.buffer[5] << 8 | this.buffer[6] << 16 | this.buffer[7] << 24);

            ulong tmpBuffer = ((ulong)hi) << 32 | lo;
            return *((double*)&tmpBuffer);
        }
#else
        public async Task<Single> ReadSingleAsync()
        {
            await FillBufferAsync(4).ConfigureAwait(false);
            return BitConverter.ToSingle( this.buffer, 0 );
        }

        public async Task<Double> ReadDoubleAsync()
        {
            await FillBufferAsync(8).ConfigureAwait(false);
            return BitConverter.ToDouble( this.buffer, 0 );
        }
#endif

        public async Task<Decimal> ReadDecimalAsync()
        {
            await FillBufferAsync( 16 ).ConfigureAwait(false);
            try
            {
                return DotNetInternals.Decimal_ToDecimal( this.buffer ); // Decimal.ToDecimal(this.buffer);
            }
            catch( ArgumentException e )
            {
                // ReadDecimal cannot leak out ArgumentException
                throw new IOException( Environment.GetResourceString( "Arg_DecBitCtor" ), e );
            }
        }

        public async Task<String> ReadStringAsync()
        {
            Contract.Ensures( Contract.Result<String>() != null );

            if( this.stream == null )
                __Error.FileNotOpen();

            int currPos = 0;
            int n;
            int stringLength;
            int readLength;
            int charsRead;

            // Length of the string in bytes, not chars
            stringLength = await this.Read7BitEncodedIntAsync().ConfigureAwait(false);
            if( stringLength < 0 )
            {
                throw new IOException( Environment.GetResourceString( "IO.IO_InvalidStringLen_Len", stringLength ) );
            }

            if( stringLength == 0 )
            {
                return String.Empty;
            }

            if( this.charBytes == null )
            {
                this.charBytes = new byte[MaxCharBytesSize];
            }

            if( this.charBuffer == null )
            {
                this.charBuffer = new char[this.maxCharsSize];
            }

            StringBuilder sb = null;
            do
            {
                readLength = ( ( stringLength - currPos ) > MaxCharBytesSize ) ? MaxCharBytesSize : ( stringLength - currPos );

                n = await this.stream.ReadAsync( this.charBytes, 0, readLength ).ConfigureAwait(false);
                if( n == 0 )
                {
                    __Error.EndOfFile();
                }

                charsRead = this.decoder.GetChars( this.charBytes, 0, n, this.charBuffer, 0 );

                if( currPos == 0 && n == stringLength )
                    return new String( this.charBuffer, 0, charsRead );

                if( sb == null ) sb = new StringBuilder();
                sb.Append( this.charBuffer, 0, charsRead );
                currPos += n;

            } while( currPos < stringLength );

            return sb.ToString();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "buffer" )]
        public Task<Int32> ReadAsync(char[] buffer, int index, int count)
        {
            if( buffer == null )
            {
                throw new ArgumentNullException( "buffer", Environment.GetResourceString( "ArgumentNull_Buffer" ) );
            }
            if( index < 0 )
            {
                throw new ArgumentOutOfRangeException( "index", Environment.GetResourceString( "ArgumentOutOfRange_NeedNonNegNum" ) );
            }
            if( count < 0 )
            {
                throw new ArgumentOutOfRangeException( "count", Environment.GetResourceString( "ArgumentOutOfRange_NeedNonNegNum" ) );
            }
            if( buffer.Length - index < count )
            {
                throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidOffLen" ) );
            }
            Contract.Ensures( Contract.Result<int>() >= 0 );
            Contract.Ensures( Contract.Result<int>() <= count );
            Contract.EndContractBlock();

            if( this.stream == null )
                __Error.FileNotOpen();

            // SafeCritical: index and count have already been verified to be a valid range for the buffer
            return InternalReadCharsAsync( buffer, index, count );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "buffer" )]
        private async Task<Int32> InternalReadCharsAsync(char[] buffer, int index, int count)
        {
            Contract.Requires( buffer != null );
            Contract.Requires( index >= 0 && count >= 0 );
            Contract.Assert( this.stream != null );

            int numBytes = 0;
            int charsRemaining = count;

            if( this.charBytes == null )
            {
                this.charBytes = new byte[MaxCharBytesSize];
            }

            while( charsRemaining > 0 )
            {
                int charsRead = 0;
                // We really want to know what the minimum number of bytes per char
                // is for our encoding.  Otherwise for UnicodeEncoding we'd have to
                // do ~1+log(n) reads to read n characters.
                numBytes = charsRemaining;

                // special case for DecoderNLS subclasses when there is a hanging byte from the previous loop

                //              DecoderNLS decoder = this.decoder as DecoderNLS;
                if(/*decoder != null && decoder.HasState &&*/ this.decoderNls.HasState && numBytes > 1 )
                {
                    numBytes -= 1;
                }

                if( this.is2BytesPerChar )
                    numBytes <<= 1;
                if( numBytes > MaxCharBytesSize )
                    numBytes = MaxCharBytesSize;

                int position = 0;
                byte[] byteBuffer = null;
//              if (this.isMemoryStream)
//              {
//                  MemoryStream mStream = this.stream as MemoryStream;
//                  Contract.Assert(mStream != null, "this.stream as MemoryStream != null");

//                  position = mStream.InternalGetPosition();
//                  numBytes = mStream.InternalEmulateRead(numBytes);
//                  byteBuffer = mStream.InternalGetBuffer();
//              }
//              else
//              {
                numBytes = await this.stream.ReadAsync( this.charBytes, 0, numBytes ).ConfigureAwait(false);
                byteBuffer = this.charBytes;
//              }

                if( numBytes == 0 )
                {
                    return ( count - charsRemaining );
                }

                Contract.Assert( byteBuffer != null, "expected byteBuffer to be non-null" );

                checked
                {

                    if( position < 0 || numBytes < 0 || position + numBytes > byteBuffer.Length )
                    {
                        throw new ArgumentOutOfRangeException( "byteCount" );
                    }

                    if( index < 0 || charsRemaining < 0 || index + charsRemaining > buffer.Length )
                    {
                        throw new ArgumentOutOfRangeException( "charsRemaining" );
                    }

#if UNSAFE
                    unsafe {
                        fixed (byte* pBytes = byteBuffer) {
                            fixed (char* pChars = buffer) {
                                charsRead = this.decoder.GetChars(
                                    bytes    : pBytes + position,
                                    byteCount: numBytes,
                                    chars    : pChars + index,
                                    charCount: charsRemaining,
                                    flush    : false
                                );
                            }
                        }
                    }
#else
                    charsRead = this.decoder.GetChars(
                        bytes: byteBuffer,
                        byteIndex: position,
                        byteCount: numBytes,
                        chars: buffer,
                        charIndex: index,
                        // TODO: What about 'charsRemaining'?
                        flush: false
                    );
#endif
                }

                charsRemaining -= charsRead;
                index += charsRead;
            }

            // this should never fail
            Contract.Assert( charsRemaining >= 0, "We read too many characters." );

            // we may have read fewer than the number of characters requested if end of stream reached 
            // or if the encoding makes the char count too big for the buffer (e.g. fallback sequence)
            return ( count - charsRemaining );
        }

        private async Task<Int32> InternalReadOneCharAsync()
        {
            // I know having a separate InternalReadOneChar method seems a little 
            // redundant, but this makes a scenario like the security parser code
            // 20% faster, in addition to the optimizations for UnicodeEncoding I
            // put in InternalReadChars.   
            int charsRead = 0;
            int numBytes = 0;
            long posSav = posSav = 0;

            if( this.stream.CanSeek ) posSav = this.stream.Position;
            if( this.charBytes == null ) this.charBytes = new byte[MaxCharBytesSize];
            if( this.singleChar == null ) this.singleChar = new char[1];

            while( charsRead == 0 )
            {
                // We really want to know what the minimum number of bytes per char
                // is for our encoding.  Otherwise for UnicodeEncoding we'd have to
                // do ~1+log(n) reads to read n characters.
                // Assume 1 byte can be 1 char unless this.2BytesPerChar is true.
                numBytes = this.is2BytesPerChar ? 2 : 1;

                int r = await this.StreamReadByteAsync(CancellationToken.None).ConfigureAwait(false);
                this.charBytes[0] = (byte)r;
                if( r == -1 ) numBytes = 0;
                    
                if( numBytes == 2 )
                {
                    r = await this.StreamReadByteAsync(CancellationToken.None).ConfigureAwait(false);
                    this.charBytes[1] = (byte)r;
                    if( r == -1 )
                        numBytes = 1;
                }

                if( numBytes == 0 )
                {
                    return -1;
                }

                Contract.Assert( numBytes == 1 || numBytes == 2, "BinaryReader::InternalReadOneChar assumes it's reading one or 2 bytes only." );

                try
                {
                    charsRead = this.decoder.GetChars( this.charBytes, 0, numBytes, this.singleChar, 0 );
                }
                catch
                {
                    // Handle surrogate char 
                    if( this.stream.CanSeek )
                        this.stream.Seek( ( posSav - this.stream.Position ), SeekOrigin.Current );
                    // else - we can't do much here

                    throw;
                }

                Contract.Assert( charsRead < 2, "InternalReadOneChar - assuming we only got 0 or 1 char, not 2!" );
                //                Console.WriteLine("That became: " + charsRead + " characters.");
            }
            
            if( charsRead == 0 ) return -1;
            
            return this.singleChar[0];
        }

        private static readonly Char[] _emptyCharArray = new Char[0];

        public async Task<Char[]> ReadCharsAsync(int count)
        {
            if( count < 0 )
            {
                throw new ArgumentOutOfRangeException( "count", Environment.GetResourceString( "ArgumentOutOfRange_NeedNonNegNum" ) );
            }
            Contract.Ensures( Contract.Result<char[]>() != null );
            Contract.Ensures( Contract.Result<char[]>().Length <= count );
            Contract.EndContractBlock();
            if( this.stream == null )
            {
                __Error.FileNotOpen();
            }

            if( count == 0 )
            {
                return _emptyCharArray;
            }

            // SafeCritical: we own the chars buffer, and therefore can guarantee that the index and count are valid
            char[] chars = new char[count];
            int n = await InternalReadCharsAsync(chars, 0, count).ConfigureAwait(false);
            if( n != count )
            {
                char[] copy = new char[n];
                DotNetInternals.Buffer_InternalBlockCopy( chars, 0, copy, 0, 2 * n ); // Buffer.InternalBlockCopy(chars, 0, copy, 0, 2*n); // sizeof(char)
                chars = copy;
            }

            return chars;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "buffer" )]
        public Task<Int32> ReadAsync(byte[] buffer, int index, int count)
        {
            if( buffer == null )
                throw new ArgumentNullException( "buffer", Environment.GetResourceString( "ArgumentNull_Buffer" ) );
            if( index < 0 )
                throw new ArgumentOutOfRangeException( "index", Environment.GetResourceString( "ArgumentOutOfRange_NeedNonNegNum" ) );
            if( count < 0 )
                throw new ArgumentOutOfRangeException( "count", Environment.GetResourceString( "ArgumentOutOfRange_NeedNonNegNum" ) );
            if( buffer.Length - index < count )
                throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidOffLen" ) );
            Contract.Ensures( Contract.Result<int>() >= 0 );
            Contract.Ensures( Contract.Result<int>() <= count );
            Contract.EndContractBlock();

            if( this.stream == null ) __Error.FileNotOpen();
            return this.stream.ReadAsync( buffer, index, count );
        }

        private static readonly Byte[] _emptyByteArray = new Byte[0];

        public async Task<Byte[]> ReadBytesAsync(int count)
        {
            if( count < 0 ) throw new ArgumentOutOfRangeException( "count", Environment.GetResourceString( "ArgumentOutOfRange_NeedNonNegNum" ) );
            Contract.Ensures( Contract.Result<byte[]>() != null );
            Contract.Ensures( Contract.Result<byte[]>().Length <= Contract.OldValue( count ) );
            Contract.EndContractBlock();
            if( this.stream == null ) __Error.FileNotOpen();

            if( count == 0 )
            {
                return _emptyByteArray;
            }

            byte[] result = new byte[count];

            int numRead = 0;
            do
            {
                int n = await this.stream.ReadAsync(result, numRead, count).ConfigureAwait(false);
                if( n == 0 )
                    break;
                numRead += n;
                count -= n;
            } while( count > 0 );

            if( numRead != result.Length )
            {
                // Trim array.  This should happen on EOF & possibly net streams.
                byte[] copy = new byte[numRead];
                DotNetInternals.Buffer_InternalBlockCopy( result, 0, copy, 0, numRead ); //Buffer.InternalBlockCopy(result, 0, copy, 0, numRead);
                result = copy;
            }

            return result;
        }

        private async Task FillBufferAsync(int numBytes)
        {
            if( this.buffer != null && ( numBytes < 0 || numBytes > this.buffer.Length ) )
            {
                throw new ArgumentOutOfRangeException( "numBytes", Environment.GetResourceString( "ArgumentOutOfRange_BinaryReaderFillBuffer" ) );
            }
            int bytesRead=0;
            int n = 0;

            if( this.stream == null ) __Error.FileNotOpen();

            // Need to find a good threshold for calling ReadByte() repeatedly
            // vs. calling Read(byte[], int, int) for both buffered & unbuffered
            // streams.
            if( numBytes == 1 )
            {
                n = await this.StreamReadByteAsync(CancellationToken.None).ConfigureAwait(false);
                if( n == -1 )
                    __Error.EndOfFile();
                this.buffer[0] = (byte)n;
                return;
            }

            do
            {
                n = await this.stream.ReadAsync( this.buffer, bytesRead, numBytes - bytesRead ).ConfigureAwait(false);
                if( n == 0 )
                {
                    __Error.EndOfFile();
                }
                bytesRead += n;
            } while( bytesRead < numBytes );
        }

        private async Task<Int32> Read7BitEncodedIntAsync()
        {
            // Read out an Int32 7 bits at a time.  The high bit
            // of the byte when on means to continue reading more bytes.
            int count = 0;
            int shift = 0;
            byte b;
            do
            {
                // Check for a corrupted stream.  Read a max of 5 bytes.
                // In a future version, add a DataFormatException.
                if( shift == 5 * 7 )  // 5 bytes max per Int32, shift += 7
                    throw new FormatException( Environment.GetResourceString( "Format_Bad7BitInt32" ) );

                // ReadByte handles end of stream cases for us.
                b = await this.ReadByteAsync().ConfigureAwait(false);
                count |= ( b & 0x7F ) << shift;
                shift += 7;
            } while( ( b & 0x80 ) != 0 );
            return count;
        }

        private async Task<Int32> StreamReadByteAsync(CancellationToken ct)
        {
            Int32 r = await this.stream.ReadAsync( this.singleByteBuffer, 0, 1, ct ).ConfigureAwait(false);
            switch( r )
            {
                case 0: return -1;
                case 1: return this.singleByteBuffer[0];
                default:
                    throw new InvalidOperationException("Didn't read 0 or 1 bytes from stream. Read " + r + " bytes.");
            }
        }
    }
}