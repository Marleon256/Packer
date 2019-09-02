using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace LogPacker
{
    class MyGZip
    {
        public void Decompress(string toDecompressFilename, string decompressedFilename)
        {
            using (FileStream originalFileStream = File.OpenRead(toDecompressFilename))
            {
                using (FileStream decompressedFileStream = File.OpenWrite(decompressedFilename))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                    }
                }
            }
        }

        public void Compress(string toCompressFilename, string compressedFilename)
        {
            using (FileStream originalFileStream = File.OpenRead(toCompressFilename))
            {
                using (FileStream compressedFileStream = File.OpenWrite(compressedFilename))
                {
                    using (GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionLevel.Optimal))
                    {
                        originalFileStream.CopyTo(compressionStream);
                    }
                }
            }
        }
    }
}
