using System.IO;

namespace LogPacker
{

    internal static class EntryPoint
    {
        public static void Main(string[] args)
        {
            if (args.Length == 2)
            {
                var (inputFile, outputFile) = (args[0], args[1]);
                var compactFileName = "temp.txt";
                var myGZipCompressor = new MyGZip();
                var compactor = new Compactor();
                compactor.Compact(inputFile, compactFileName);
                myGZipCompressor.Compress(compactFileName, outputFile);
                File.Delete(compactFileName);
                return;
            }

            if (args.Length == 3 && args[0] == "-d")
            {
                var (inputFile, outputFile) = (args[1], args[2]);
                var compactFileName = "temp.txt";
                var myGZip = new MyGZip();
                var decompactor = new Decompactor();
                myGZip.Decompress(inputFile, compactFileName);
                decompactor.Decompact(compactFileName, outputFile);
                File.Delete(compactFileName);
                return;
            }
        }
    }
}