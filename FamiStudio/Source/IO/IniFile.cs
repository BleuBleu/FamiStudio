using System.Collections.Generic;

namespace FamiStudio
{
    public class IniFile
    {
        public static Dictionary<string, Dictionary<string, string>> ParseINI(string filename)
        {
            var lines = System.IO.File.ReadAllLines(filename);
            var sectionName = "";
            var sectionValues = new Dictionary<string, string>();
            var iniContent = new Dictionary<string, Dictionary<string, string>>();

            foreach (string line in lines)
            {
                if (line.StartsWith("["))
                {
                    if (sectionName != "")
                    {
                        iniContent.Add(sectionName, sectionValues);
                        sectionName = "";
                        sectionValues = new Dictionary<string, string>();
                    }

                    sectionName = line.TrimStart('[').TrimEnd(']');
                }
                else
                {
                    int eq = line.IndexOf('=');
                    if (eq >= 0)
                    {
                        sectionValues.Add(line.Substring(0, eq), line.Substring(eq + 1));
                    }
                }
            }

            if (sectionName != "")
            {
                iniContent.Add(sectionName, sectionValues);
            }

            return iniContent;
        }

        public static void WriteINI(string filename, Dictionary<string, Dictionary<string, string>> content)
        {
            var lines = new List<string>();

            foreach (var itSection in content)
            {
                var sectionName = itSection.Key;
                var SectionValues = itSection.Value;

                lines.Add("[" + sectionName + "]");

                foreach (var itValues in SectionValues)
                {
                    lines.Add(itValues.Key + "=" + itValues.Value);
                }

                lines.Add("");
            }

            System.IO.File.WriteAllLines(filename, lines);
        }
    }
}
