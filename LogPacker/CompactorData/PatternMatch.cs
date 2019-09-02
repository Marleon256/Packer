using System;
using System.Text.RegularExpressions;

namespace LogPacker
{
    public class PatternMatch
    {
        public DateTime date;
        public ulong number;
        public string type;
        public string str;

        public PatternMatch(Match match)
        {
            this.str = match.Groups[7].Value;
            this.type = match.Groups[5].Value;
            this.number = ulong.Parse(match.Groups[3].Value);
            this.date = DateTime.ParseExact(
                match.Groups[1].Value,
                "yyyy-MM-dd HH:mm:ss,fff",
                System.Globalization.CultureInfo.InvariantCulture
                );
        }
    }
}
