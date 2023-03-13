using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public static class Localization
    {
        private static IniFile stringsEng;
        private static IniFile strings;

        static Localization()
        {
            var culture = "fre";

            stringsEng = new IniFile();
            stringsEng.LoadFromResource("FamiStudio.Resources.Localization.FamiStudio.eng");
            strings = new IniFile();
            strings.LoadFromResource($"FamiStudio.Resources.Localization.FamiStudio.{culture}");
        }

        public static void Localize(object o)
        {
            var typeName = o.GetType().Name;
            var fields = o.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(LocalizedString))
                {
                    var fieldName = $"{char.ToUpper(field.Name[0])}{field.Name.Substring(1)}";
                    var str = strings.GetString(typeName, fieldName, null);
                    if (str == null)
                        str = stringsEng.GetString(typeName, fieldName, "### MISSING ###");
                    field.SetValue(o, new LocalizedString(str));
                }
            }
        }
    }

    public class LocalizedString
    {
        public string Value;
        public LocalizedString(string s) { Value = s; }
        public static implicit operator string(LocalizedString s) => s.Value;
        public override string ToString() { return Value; }
    }
}
