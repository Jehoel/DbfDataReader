﻿using System;

namespace Dbf.Cdx
{
    public interface IKey
    {
        /// <summary>This is a Byte[] instead of ReadOnlyCollection&lt;Byte&gt; for performance reasons (as Encoding.ASCII.GetString only accepts Byte[] or Byte*).</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays" )]
        Byte[] KeyBytes { get; }

        UInt32 DbfRecordNumber { get; }
    }
}
