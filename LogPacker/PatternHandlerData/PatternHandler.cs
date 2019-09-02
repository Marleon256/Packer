using LogPacker.PatternHandlerData;
using System.Collections.Generic;
using System.Text;

namespace LogPacker
{
    class PatternHandler
    {
        private static readonly string ArgumentMark = "\0";

        Dictionary<string, byte> typeCharCoding = new Dictionary<string, byte>();
        byte currentTypeCodingByte = 0;

        Dictionary<string, Dictionary<int, List<Pattern>>> typePatternDictionaryMapping = 
            new Dictionary<string, Dictionary<int, List<Pattern>>>();
        byte currentPatternCodingByte = 0;

        Dictionary<string, Dictionary<int, List<PatternCandidate>>> typeCandidateDictionaryMapping =
            new Dictionary<string, Dictionary<int, List<PatternCandidate>>>();

        public CodingInfo GetNewTypeInfo(PatternMatch patternMatch)
        {
            if (currentTypeCodingByte == 255)
            {
                return null;
            }
            if (!typeCharCoding.ContainsKey(patternMatch.type))
            {
                typeCharCoding[patternMatch.type] = currentTypeCodingByte;
                var codingInfoToReturn = new CodingInfo(patternMatch.type, currentTypeCodingByte);
                currentTypeCodingByte++;
                return codingInfoToReturn;
            }
            return null;
        }

        public TypeCoding GetTypeCoding(PatternMatch patternMatch)
        {
            if (!typeCharCoding.ContainsKey(patternMatch.type))
                return null;
            return new TypeCoding(typeCharCoding[patternMatch.type]);
        }

        public CodingInfo GetNewPatternInfo(PatternMatch patternMatch)
        {
            if (currentPatternCodingByte == 255)
            {
                return null;
            }
            var strSplit = patternMatch.str.Split(' ');
            if (strSplit.Length == 1)
            {
                return null;
            }
            if (!typeCandidateDictionaryMapping.ContainsKey(patternMatch.type))
            {
                typeCandidateDictionaryMapping[patternMatch.type] = new Dictionary<int, List<PatternCandidate>>();
            }
            if (!typeCandidateDictionaryMapping[patternMatch.type].ContainsKey(strSplit.Length))
            {
                typeCandidateDictionaryMapping[patternMatch.type][strSplit.Length] = new List<PatternCandidate>();
            }
            foreach (var candidate in typeCandidateDictionaryMapping[patternMatch.type][strSplit.Length])
            {
                var anyShared = false;
                var differentPartsCounter = 0;
                var formatStringBuilder = new StringBuilder(patternMatch.str.Length);
                var patternSplit = new string[strSplit.Length];
                for (int i = 0; i < strSplit.Length; i++)
                {
                    if (strSplit[i] == candidate.strSplit[i])
                    {
                        anyShared = true;
                        patternSplit[i] = candidate.strSplit[i];
                        formatStringBuilder.Append(strSplit[i]);
                    }
                    else
                    {
                        patternSplit[i] = ArgumentMark;
                        formatStringBuilder.Append($"{{{differentPartsCounter}}}");
                        differentPartsCounter++;
                    }
                    formatStringBuilder.Append(' ');
                }
                formatStringBuilder.Remove(formatStringBuilder.Length - 1, 1);
                if (anyShared)
                {
                    var formatPattern = formatStringBuilder.ToString();
                    if (!typePatternDictionaryMapping.ContainsKey(patternMatch.type))
                        typePatternDictionaryMapping[patternMatch.type] = new Dictionary<int, List<Pattern>>();
                    if (!typePatternDictionaryMapping[patternMatch.type].ContainsKey(strSplit.Length))
                        typePatternDictionaryMapping[patternMatch.type][strSplit.Length] = new List<Pattern>();
                    typePatternDictionaryMapping[patternMatch.type][strSplit.Length].Add(new Pattern(patternSplit, currentPatternCodingByte));
                    var toReturn = new CodingInfo(formatPattern, currentPatternCodingByte);
                    currentPatternCodingByte++;
                    return toReturn;
                }
            }
            typeCandidateDictionaryMapping[patternMatch.type][strSplit.Length].Add(new PatternCandidate(strSplit));
            return null;
        }

        public PatternCoding GetPatternCoding(PatternMatch patternMatch)
        {
            if (!typePatternDictionaryMapping.ContainsKey(patternMatch.type))
            {
                return null;
            }
            var strSplit = patternMatch.str.Split(' ');
            if (!typePatternDictionaryMapping[patternMatch.type].ContainsKey(strSplit.Length))
            {
                return null;
            }
            foreach (var pattern in typePatternDictionaryMapping[patternMatch.type][strSplit.Length])
            {
                var arguments = new List<string>();
                var needPattern = true;
                for (int i = 0; i < strSplit.Length; i++)
                {
                    if (pattern.strSplit[i] == ArgumentMark)
                    {
                        arguments.Add(strSplit[i]);
                    }
                    else
                    {
                        needPattern &= pattern.strSplit[i] == strSplit[i];
                        if (!needPattern)
                            break;
                    }
                }
                if (needPattern)
                {
                    return new PatternCoding(arguments, pattern.coding);
                }
            }
            return null;
        }


        private class Pattern : PatternCandidate
        {
            public byte coding;

            public Pattern(string[] split, byte coding) : base(split)
            {
                this.coding = coding;
            }
        }

        private class PatternCandidate
        {
            public string[] strSplit;

            public PatternCandidate(string[] strSplit)
            {
                this.strSplit = strSplit;
            }
        }
    }
}
