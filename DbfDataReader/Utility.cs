using System;
using System.IO;
using System.Security.AccessControl;

namespace Dbf
{
    internal static class Utility
    {
        public static FileStream OpenFileForReading(String fileName, Boolean randomAccess, Boolean async)
        {
            FileOptions options = ( randomAccess ? FileOptions.RandomAccess : FileOptions.SequentialScan ) | ( async ? FileOptions.Asynchronous : FileOptions.None );

            return new FileStream( fileName, FileMode.Open, FileSystemRights.ReadData, FileShare.ReadWrite, 4096, options );
        }
    }

    internal static class BuildOptions
    {
        public const Boolean StrictChecks = true;
    }
}
