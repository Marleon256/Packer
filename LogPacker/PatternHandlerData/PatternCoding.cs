using System.Collections.Generic;

namespace LogPacker
{
    public class PatternCoding
    {
        public List<string> arguments;
        public byte coding;

        public PatternCoding(List<string> arguments, byte coding)
        {
            this.arguments = arguments;
            this.coding = coding;
        }
    }
}
