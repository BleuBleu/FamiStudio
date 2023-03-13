using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public static class Localization
    {
        private static IniFile stringsEng;
        private static IniFile strings;

        public static string Font     { get; private set; }
        public static string FontBold { get; private set; }

        static Localization()
        {
            var culture = "fre"; // This is a test. Should come from the OS.

            stringsEng = new IniFile();
            stringsEng.LoadFromResource("FamiStudio.Resources.Localization.FamiStudio.eng");
            strings = new IniFile();
            strings.LoadFromResource($"FamiStudio.Resources.Localization.FamiStudio.{culture}");

            Font     = LocalizeString("Localization", "Font",     false);
            FontBold = LocalizeString("Localization", "FontBold", false);

            Debug.Assert(Font != null && FontBold != null);
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
                    field.SetValue(o, new LocalizedString(LocalizeStringWithPlatformOverride(typeName, fieldName)));
                }
                else if (field.FieldType == typeof(LocalizedString[]))
                {
                    var baseFieldName = $"{char.ToUpper(field.Name[0])}{field.Name.Substring(1)}";
                    var array = field.GetValue(o) as LocalizedString[];
                    for (int i = 0; i < array.Length; i++)
                    {
                        var fieldName = $"{baseFieldName}_{i}";
                        array[i] = new LocalizedString(LocalizeStringWithPlatformOverride(typeName, fieldName));
                    }
                }
            }
        }

        private static string LocalizeString(string section, string key, bool missing = true)
        {
            var str = strings.GetString(section, key, null);
            if (str == null)
                str = stringsEng.GetString(section, key, missing ? "### MISSING ###" : null);
            return str;
        }

        private static string LocalizeStringWithPlatformOverride(string section, string key)
        {
            string str = null;

            if (Platform.IsMobile)
            {
                str = LocalizeString(section, key + "_Mobile", false);
            }

            if (str == null)
            {
                str = LocalizeString(section, key);
            }

            return str;
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
