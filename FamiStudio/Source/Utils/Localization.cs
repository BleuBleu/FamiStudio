using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public static class Localization
    {
        private static IniFile stringsEng = new IniFile();
        private static IniFile strings    = new IniFile();

        private static bool Initialized = false;

        public static string LanguageCode = "ENG";
        public static bool IsChinese => LanguageCode == "ZHO";
        public static string[] LanguageCodes = new[]
{
            "ENG",
            "SPA",
            "POR",
            "ZHO",
            "DEU",
            "RUS",
            "KOR"
        };

        public static string[] LanguageNames = new[]
        {
            "English",
            "Español",
            "Português",
            "中文 (简体)",
            "Deutsch",
            "Русский",
            "한국어"
        };

        static Localization()
        {
            // Always english in command-line mode.
            var code = Platform.IsCommandLine ? LanguageCodes[0] : Settings.LoadLanguageCodeOnly();
            var idx = GetIndexForLanguageCode(code);

            if (idx < 0)
            {
                idx = GetIndexForLanguageCode(CultureInfo.CurrentUICulture.ThreeLetterISOLanguageName);
                if (idx < 0)
                    idx = 0;
            }

            code = LanguageCodes[idx].ToLower();

            if (Platform.IsDesktop)
            {
                var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                stringsEng.Load(Path.Combine(appPath, "Localization/FamiStudio.eng.ini"));
                strings.Load(Path.Combine(appPath, $"Localization/FamiStudio.{code}.ini"));
            }
            else
            {
                stringsEng.LoadFromResource("FamiStudio.Localization.FamiStudio.eng.ini");
                strings.LoadFromResource($"FamiStudio.Localization.FamiStudio.{code}.ini");
            }

            LanguageCode = code;
            Initialized = true;
        }

        public static int GetIndexForLanguageCode(string code)
        {
            return Array.IndexOf(LanguageCodes, code.ToUpper());
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
            Debug.Assert(Initialized);

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

        private static string LocalizeString(string section, string key, bool fallback = true)
        {
            var str = strings.GetString(section, key, null);
            if (str == null)
            { 
                str = stringsEng.GetString(section, key, null);
                if (str == null && fallback)
                {
                    Debug.Assert(false);
                    str = "### MISSING ###";
                }
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
                // We can override whole sections, or individual strings.
                if (strings.HasSection(section + "_Mobile"))
                {
                    str = LocalizeString(section + "_Mobile", key, false);
                }
                else 
                {
                    str = LocalizeString(section, key + "_Mobile", false);
                }
            }

            if (str == null)
            {
                str = LocalizeString(section, key);
            }

            return str;
        }

        public static string[] ToStringArray(LocalizedString[] locStrings, int count = int.MaxValue)
        {
            count = Math.Min(count, locStrings.Length);
            var strings = new string[count];
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
