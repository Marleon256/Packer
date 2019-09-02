using LogPacker.CompacterData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LogPacker
{
    class Decompactor
    {
        private static readonly int DefaultBufferSize = 1024 * 1024 * 10;
        private static readonly Encoding NeedEncoding = Encoding.GetEncoding("ISO-8859-1");

        private long lastDateBinary = default(DateTime).ToBinary();
        private ulong lastNumber = 0;
        private FileStream patternCodedFile;
        private FileStream patternDecodedFile;

        public void Decompact(string compactFilename, string decompactFilename)
        {
            using (var patternCodedFile = File.OpenRead(compactFilename))
            {
                using (var patternDecodedFile = File.Create(decompactFilename, DefaultBufferSize))
                {
                    this.patternCodedFile = patternCodedFile;
                    this.patternDecodedFile = patternDecodedFile;
                    var patternCoding = new Dictionary<byte, string>();
                    var staticCoding = new Dictionary<byte, string>();
                    var tempBytes = new byte[4];
                    var count = 0;
                    while (patternCodedFile.Read(tempBytes, 0, 4) > 0)
                    {
                        count++;
                        var length = BitConverter.ToUInt32(tempBytes, 0);
                        var test = length & 3;
                        length >>= 2;
                        if (test == (int)TypeBits.StringRaw)
                        {
                            DecodeStringRaw(length);
                        }
                        if (test == (int)TypeBits.PatternInfo)
                        {
                            DecodePatternInfo(length, patternCoding);
                        }
                        if (test == (int)TypeBits.TypeInfo)
                        {
                            DecodeTypeInfo(length, staticCoding);
                        }
                        if (test == (int)TypeBits.PatternString)
                        {
                            DecodePatternString(length, patternCoding, staticCoding);
                        }
                    }
                }
            }
        }

        public void DecodeStringRaw(uint length)
        {
            var buffer = new byte[length];
            patternCodedFile.Read(buffer, 0, (int)length);
            patternDecodedFile.Write(buffer);
        }

        public void DecodePatternInfo(uint length, Dictionary<byte, string> patternCoding)
        {
            var codingByte = (byte)patternCodedFile.ReadByte();
            length -= 1;
            var patternBuffer = new byte[length];
            patternCodedFile.Read(patternBuffer, 0, (int)length);
            var resultString = NeedEncoding.GetString(patternBuffer);
            patternCoding[codingByte] = resultString;
        }

        public void DecodeTypeInfo(uint length, Dictionary<byte, string> staticCoding)
        {
            var codingByte = (byte)patternCodedFile.ReadByte();
            length -= 1;
            var patternBuffer = new byte[length];
            patternCodedFile.Read(patternBuffer, 0, (int)length);
            var resultString = NeedEncoding.GetString(patternBuffer);
            staticCoding[codingByte] = resultString;
        }

        public void DecodePatternString(uint length, Dictionary<byte, string> patternCoding,  Dictionary<byte, string> staticCoding)
        {
            var longBytes = new byte[8];
            patternCodedFile.Read(longBytes, 0, 8);
            lastDateBinary += BitConverter.ToInt64(longBytes);
            var date = DateTime.FromBinary(lastDateBinary);
            patternCodedFile.Read(longBytes, 0, 8);
            lastNumber += BitConverter.ToUInt64(longBytes);
            var type = staticCoding[(byte)patternCodedFile.ReadByte()];
            var finalPattern = patternCoding[(byte)patternCodedFile.ReadByte()];
            length -= 8 + 8 + 1 + 1;
            var allArguments = new List<string>();
            while (length > 0)
            {
                var shortBytes = new byte[2];
                patternCodedFile.Read(shortBytes, 0, 2);
                var argumentLength = BitConverter.ToUInt16(shortBytes);
                var argumentBytes = new byte[argumentLength];
                patternCodedFile.Read(argumentBytes, 0, argumentLength);
                var argument = NeedEncoding.GetString(argumentBytes);
                allArguments.Add(argument);
                length -= 2 + (uint)argumentLength;
            }
            var resuStr = string.Format(finalPattern, allArguments.ToArray());
            var resu = $"{date.ToString("yyyy-MM-dd HH:mm:ss,fff")} {lastNumber,-6} {type,-5} {resuStr}";
            patternDecodedFile.Write(NeedEncoding.GetBytes(resu));
        }


    }


}
