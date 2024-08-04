using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace FamiStudio
{
    public class IniFile
    {
        Dictionary<string, Dictionary<string, string>> iniContent = new Dictionary<string, Dictionary<string, string>>();

        public bool Load(string filename, bool trim = true)
        {
            try
            {
                LoadInternal(File.ReadAllLines(filename), trim);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool LoadFromResource(string filename, bool trim = true)
        {
            try
            {
                var str = "";
                using (Stream stream = typeof(IniFile).Assembly.GetManifestResourceStream(filename))
                using (StreamReader reader = new StreamReader(stream))
                {
                    str = reader.ReadToEnd();
                }

                LoadInternal(str.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries), trim);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void LoadInternal(string[] lines, bool trim)
        {
            var sectionName = "";
            var sectionValues = new Dictionary<string, string>();

            foreach (string line in lines)
            {
                var trimmedLine = trim ? line.Trim() : line;

                if (trimmedLine.Length == 0)
                    continue;

                // Comments.
                if (trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";"))
                    continue;

                var idx = trimmedLine.IndexOfAny(new[] { '#', ';' });
                if (idx >= 0)
                    trimmedLine = trimmedLine.Substring(0, idx).Trim();

                if (trimmedLine.StartsWith("["))
                {
                    if (sectionName != "")
                    {
                        iniContent.Add(sectionName, sectionValues);
                        sectionName = "";
                        sectionValues = new Dictionary<string, string>();
                    }

                    sectionName = trimmedLine.TrimStart('[').TrimEnd(']');
                }
                else
                {
                    int eq = trimmedLine.IndexOf('=');
                    if (eq >= 0)
                    {
                        var key   = trimmedLine.Substring(0, eq);
                        var value = trimmedLine.Substring(eq + 1);

                        if (!sectionValues.ContainsKey(key))
                        {
                            sectionValues.Add(key, value);
                        }
                    }
                }
            }

            if (sectionName != "")
            {
                iniContent.Add(sectionName, sectionValues);
            }
        }

        public int GetInt(string section, string key, int defaultValue)
        {
            try
            {
                if (TryGetString(section, key, out var value))
                {
                    return int.Parse(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public float GetFloat(string section, string key, float defaultValue)
        {
            try
            {   
                if (TryGetString(section, key, out var value))
                {
                    return float.Parse(value, CultureInfo.InvariantCulture);
                }
            }
            catch { }

            return defaultValue;
        }

        public bool GetBool(string section, string key, bool defaultValue)
        {
            try
            {
                if (TryGetString(section, key, out var value))
                {
                    return bool.Parse(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public string GetString(string section, string key, string defaultValue)
        {
            if (TryGetString(section, key, out var value))
            {
                return value;
            }
            else
            {
                return defaultValue;
            }
        }

        private bool TryGetString(string section, string key, out string value)
        {
            if (iniContent.TryGetValue(section, out var values))
            {
                return values.TryGetValue(key, out value);
            }

            value = null;
            return false;
        }

        public bool HasSection(string section)
        {
            return iniContent.ContainsKey(section);
        }

        public bool HasKey(string section, string key)
        {
            if (iniContent.TryGetValue(section, out var sectionValues))
            {
                return sectionValues.ContainsKey(key);
            }

            return false;
        }

        public void SetInt(string section, string key, int value)
        {
            if (!iniContent.ContainsKey(section))
                iniContent[section] = new Dictionary<string, string>();
            iniContent[section][key] = value.ToString();
        }

        public void SetFloat(string section, string key, float value)
        {
            if (!iniContent.ContainsKey(section))
                iniContent[section] = new Dictionary<string, string>();
            iniContent[section][key] = value.ToString(CultureInfo.InvariantCulture);
        }

        public void SetBool(string section, string key, bool value)
        {
            if (!iniContent.ContainsKey(section))
                iniContent[section] = new Dictionary<string, string>();
            iniContent[section][key] = value.ToString();
        }

        public void SetString(string section, string key, string value)
        {
            if (!iniContent.ContainsKey(section))
                iniContent[section] = new Dictionary<string, string>();
            iniContent[section][key] = value == null ? "" : value;
        }

        public void Save(string filename)
        {
            var lines = new List<string>();

            foreach (var itSection in iniContent)
            {
                var sectionName = itSection.Key;
                var sectionValues = itSection.Value;

                lines.Add("[" + sectionName + "]");

                foreach (var itValues in sectionValues)
                {
                    lines.Add(itValues.Key + "=" + itValues.Value);
                }

                lines.Add("");
            }

            try
            {
                System.IO.File.WriteAllLines(filename, lines);
            }
            catch
            {
                Console.WriteLine($"Error saving INI file {filename}.");
            }
        }
    }
}
