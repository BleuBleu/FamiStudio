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
            stringsEng.LoadFromResource("FamiStudio.Resources.Localization.FamiStudio.eng.ini");
            strings = new IniFile();
            strings.LoadFromResource($"FamiStudio.Resources.Localization.FamiStudio.{culture}.ini");

            Font     = LocalizeString("Localization", "Font",     false);
            FontBold = LocalizeString("Localization", "FontBold", false);

            Debug.Assert(Font != null && FontBold != null);
        }

        public static void Localize(object o)
        {
            LocalizeInternal(o.GetType(), o);
        }

        public static void LocalizeType(object o, Type type)
        {
            Debug.Assert(o.GetType().IsSubclassOf(type));
            LocalizeInternal(type, o);
        }

        public static void LocalizeStatic(Type type)
        {
            LocalizeInternal(type);
        }

        private static void LocalizeInternal(Type type, Object o = null)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(LocalizedString))
                {
                    var fieldName = $"{char.ToUpper(field.Name[0])}{field.Name.Substring(1)}";
                    var typeName = field.DeclaringType.Name;
                    field.SetValue(o, new LocalizedString(LocalizeStringWithPlatformOverride(typeName, fieldName)));
                }
                else if (field.FieldType == typeof(LocalizedString[]))
                {
                    var baseFieldName = $"{char.ToUpper(field.Name[0])}{field.Name.Substring(1)}";
                    var typeName = field.DeclaringType.Name;
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
            { 
                str = stringsEng.GetString(section, key, null);
                Debug.Assert(str != null);
                if (str == null)
                    str = "### MISSING ###";
            }
            if (str != null && str.Contains('\\'))
            { 
                str = str.Replace("\\n", "\n");
            }
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

        public static string[] ToStringArray(LocalizedString[] locStrings)
        {
            var strings = new string[locStrings.Length];
            for (int i = 0; i < strings.Length; i++)
                strings[i] = locStrings[i];
            return strings;
        }
    }

    public class LocalizedString
    {
        public string Value;
        public LocalizedString(string s) { Value = s; }
        public static implicit operator string(LocalizedString s) => s?.Value;
        public string this[string pad] => Value+pad;
        public string Colon => Platform.IsMobile ? Value : Value + ":";
        public string Period => Value + ".";
        public override string ToString() { return Value; }
        public string Format(params object[] args) => string.Format(ToString(), args);
    }
}
