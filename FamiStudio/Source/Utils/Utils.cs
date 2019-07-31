using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    static class Utils
    {
        public static int Clamp(int val, int min, int max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        public static string MakeNiceAsmName(string name)
        {
            string niceName = "";
            foreach (var c in name)
            {
                if (char.IsLetterOrDigit(c))
                    niceName += char.ToLower(c);
                else if (char.IsWhiteSpace(c) && niceName.Last() != '_')
                    niceName += '_';
                else if (c == '_' || c == '-')
                    niceName += c;
            }
            return niceName;
        }
    }
}
