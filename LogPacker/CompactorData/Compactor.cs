using LogPacker.CompacterData;
using LogPacker.PatternHandlerData;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace LogPacker
{
    class Compactor
    {
        private static readonly int DefaultBufferSize = 1024 * 1024 * 10;
        private static readonly Regex FormatRegex = new Regex(
                "^(\\d{4}-\\d\\d-\\d\\d \\d\\d:\\d\\d:\\d\\d,\\d{3})([ ]*)(\\d+)([ ]*)([^ ]+)([ ]*)(.*)",
                RegexOptions.Compiled
                );
        private static readonly Encoding NeedEncoding = Encoding.GetEncoding("ISO-8859-1");
        private static readonly string NewLine = "\n";
        private static readonly int MaxStringLength = 200;
        private static readonly int MinStringLength = 20;

        private long lastDateBinary = default(DateTime).ToBinary();
        private ulong lastNumber = 0;
        private FileStream compactFileStream;
        private PatternHandler patternHandler;

        public void Compact(string fileToCompact, string compactFile)
        {
            patternHandler = new PatternHandler();
            using (var compactFileStream = File.Create(compactFile, DefaultBufferSize))
            {
                using (var compressedFileStream =  File.OpenRead(fileToCompact))
                {
                    this.compactFileStream = compactFileStream;
                    var needSize = Math.Min(DefaultBufferSize, compressedFileStream.Length);
                    byte[] buffer = new byte[needSize];
                    var readed = compressedFileStream.Read(buffer, 0, (int)needSize);
                    var tailString = "";
                    while (readed > 0)
                    {
                        var readedString = NeedEncoding.GetString(buffer);
                        var resultStrings = readedString.Split(new char[] { '\n' });
                        resultStrings[0] = tailString + resultStrings[0];
                        for (int i = 0; i < resultStrings.Length - 1; i++)
                        {
                            var currentString = resultStrings[i];
                            HandleString(currentString);
                            CodeStringRaw(NewLine);
                        }
                        var currentTail = resultStrings[resultStrings.Length - 1];
                        if (compressedFileStream.Length - compressedFileStream.Position == 0)
                        {
                            HandleString(currentTail);
                        }
                        else
                        {
                            tailString = currentTail;
                        }
                        if (compressedFileStream.Length - compressedFileStream.Position < DefaultBufferSize)
                            buffer = new byte[compressedFileStream.Length - compressedFileStream.Position];
                        needSize = Math.Min(compressedFileStream.Length - compressedFileStream.Position, DefaultBufferSize);
                        readed = compressedFileStream.Read(buffer, 0, (int)needSize);
                    }
                }
            }

        }

        private void HandleString(string currentString)
        {
            if (currentString.Length > MaxStringLength || currentString.Length < MinStringLength)
            {
                CodeStringRaw(currentString);
                return;
            }
            var match = FormatRegex.Match(currentString);
            if (match.Success)
                if (TestMatch(match))
                {
                    var patternMatch = new PatternMatch(match);

                    var typeCoding = patternHandler.GetTypeCoding(patternMatch);
                    if (typeCoding == null)
                    {
                        var typeInfo = patternHandler.GetNewTypeInfo(patternMatch);
                        if (typeInfo == null)
                        {
                            CodeStringRaw(currentString);
                            return;
                        }
                        CodeTypeInfo(typeInfo);
                        typeCoding = patternHandler.GetTypeCoding(patternMatch);
                    }

                    var patternCoding = patternHandler.GetPatternCoding(patternMatch);
                    if (patternCoding == null)
                    {
                        var patternInfo = patternHandler.GetNewPatternInfo(patternMatch);
                        if (patternInfo == null)
                        {
                            CodeStringRaw(currentString);
                            return;
                        }
                        CodePatternInfo(patternInfo);
                        patternCoding = patternHandler.GetPatternCoding(patternMatch);
                    }

                    CodePatternString(patternCoding, typeCoding, patternMatch);
                    return;
                }
            CodeStringRaw(currentString);      
        }

        private void CodeTypeInfo(CodingInfo typeCoding)
        {
            uint length = 1 + (uint)NeedEncoding.GetByteCount(typeCoding.codedString);
            length <<= 2;
            length |= (uint)TypeBits.TypeInfo; 

            byte[] lenBytes = BitConverter.GetBytes(length);
            compactFileStream.Write(lenBytes);

            compactFileStream.WriteByte(typeCoding.coding);

            compactFileStream.Write(NeedEncoding.GetBytes(typeCoding.codedString));
        }

        private void CodePatternInfo(CodingInfo pattern)
        {
            uint length = 1 + (uint)NeedEncoding.GetByteCount(pattern.codedString);
            length <<= 2;
            length |= (uint)TypeBits.PatternInfo;

            byte[] lenBytes = BitConverter.GetBytes(length);
            compactFileStream.Write(lenBytes);

            compactFileStream.WriteByte(pattern.coding);

            compactFileStream.Write(NeedEncoding.GetBytes(pattern.codedString));
        }

        private void CodePatternString(PatternCoding patternCoding, TypeCoding typeCoding, PatternMatch patternMatch)
        {

            uint length = 8 + 8 + 1 + 1 + 2 * (uint)patternCoding.arguments.Count;
            for (int i = 0; i < patternCoding.arguments.Count; i++)
                length += (uint)NeedEncoding.GetByteCount(patternCoding.arguments[i]);
            length <<= 2;
            length |= (uint)TypeBits.PatternString;

            byte[] lenBytes = BitConverter.GetBytes(length);
            compactFileStream.Write(lenBytes);

            var difference = patternMatch.date.ToBinary() - lastDateBinary;
            byte[] dateBytes = BitConverter.GetBytes(difference);
            compactFileStream.Write(dateBytes);
            lastDateBinary = patternMatch.date.ToBinary();

            byte[] numBytes = BitConverter.GetBytes(patternMatch.number - lastNumber);
            compactFileStream.Write(numBytes);
            lastNumber = patternMatch.number;

            compactFileStream.WriteByte(typeCoding.coding);

            compactFileStream.WriteByte(patternCoding.coding);

            for (int i = 0; i < patternCoding.arguments.Count; i++)
            {
                ushort bytesCount = (ushort)NeedEncoding.GetByteCount(patternCoding.arguments[i]);
                byte[] argLenBytes = BitConverter.GetBytes(bytesCount);
                compactFileStream.Write(argLenBytes);
                compactFileStream.Write(NeedEncoding.GetBytes(patternCoding.arguments[i]));
            }
        }

        private void CodeStringRaw(string str)
        {
            uint length = (uint)NeedEncoding.GetByteCount(str);

            length <<= 2;

            byte[] lenBytes = BitConverter.GetBytes(length);
            compactFileStream.Write(lenBytes);
            compactFileStream.Write(NeedEncoding.GetBytes(str.ToCharArray()));
        }

        private bool TestMatch(Match match)
        {
            if (!(match.Groups[2].Length == 1))
            {
                return false;
            }
            if (!((match.Groups[3].Length + match.Groups[4].Length) == 7 || (match.Groups[3].Length > 6 && match.Groups[4].Length == 1)))
            {
                return false;
            }
            if (!((match.Groups[5].Length + match.Groups[6].Length) == 6 || (match.Groups[5].Length > 5 && match.Groups[6].Length == 1)))
            {
                return false;
            }
            return true;
        }
    }
}
