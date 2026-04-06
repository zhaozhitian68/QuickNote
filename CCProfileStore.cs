using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QuickNote
{
    public class CCProfile
    {
        public string Name { get; set; }
        public string ApiKey { get; set; }
        public string AuthToken { get; set; }
        public string BaseUrl { get; set; }
        public string Model { get; set; }
        public bool IncludeCoAuthoredBy { get; set; }
        public bool SkipDangerousMode { get; set; }
        public string EffortLevel { get; set; }
        public string QuotaQueryUrl { get; set; }

        public CCProfile()
        {
            Name = "";
            ApiKey = "";
            AuthToken = "";
            BaseUrl = "";
            Model = "opus[1m]";
            IncludeCoAuthoredBy = false;
            SkipDangerousMode = true;
            EffortLevel = "medium";
            QuotaQueryUrl = "";
        }
    }

    public class CCProfileStore
    {
        private AppData data;

        public CCProfileStore()
        {
            data = AppData.Instance;
        }

        public List<CCProfile> GetAll()
        {
            return new List<CCProfile>(data.CCProfiles);
        }

        public string GetActiveProfileName()
        {
            return data.CCActiveProfile;
        }

        public void SetActiveProfileName(string name)
        {
            data.CCActiveProfile = name ?? "";
            data.Save();
        }

        public CCProfile FindByName(string name)
        {
            for (int i = 0; i < data.CCProfiles.Count; i++)
                if (data.CCProfiles[i].Name == name) return data.CCProfiles[i];
            return null;
        }

        public void Add(CCProfile profile)
        {
            data.CCProfiles.Add(profile);
            data.Save();
        }

        public void Update(string originalName, CCProfile updated)
        {
            for (int i = 0; i < data.CCProfiles.Count; i++)
            {
                if (data.CCProfiles[i].Name == originalName)
                {
                    data.CCProfiles[i] = updated;
                    if (data.CCActiveProfile == originalName)
                        data.CCActiveProfile = updated.Name;
                    data.Save();
                    return;
                }
            }
        }

        public void Delete(string name)
        {
            for (int i = 0; i < data.CCProfiles.Count; i++)
            {
                if (data.CCProfiles[i].Name == name)
                {
                    data.CCProfiles.RemoveAt(i);
                    if (data.CCActiveProfile == name)
                        data.CCActiveProfile = "";
                    data.Save();
                    return;
                }
            }
        }

        public string ApplyProfile(CCProfile profile)
        {
            string claudeDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".claude");
            if (!Directory.Exists(claudeDir))
                Directory.CreateDirectory(claudeDir);

            string settingsPath = Path.Combine(claudeDir, "settings.json");

            Dictionary<string, string> existingTopLevel = new Dictionary<string, string>();
            Dictionary<string, string> existingEnv = new Dictionary<string, string>();
            if (File.Exists(settingsPath))
            {
                string content = File.ReadAllText(settingsPath, Encoding.UTF8);
                ParseSettingsJson(content, existingTopLevel, existingEnv);
            }

            if (!string.IsNullOrEmpty(profile.ApiKey))
                existingEnv["ANTHROPIC_API_KEY"] = profile.ApiKey;
            else
                existingEnv.Remove("ANTHROPIC_API_KEY");
            if (!string.IsNullOrEmpty(profile.AuthToken))
                existingEnv["ANTHROPIC_AUTH_TOKEN"] = profile.AuthToken;
            else
                existingEnv.Remove("ANTHROPIC_AUTH_TOKEN");
            if (!string.IsNullOrEmpty(profile.BaseUrl))
                existingEnv["ANTHROPIC_BASE_URL"] = profile.BaseUrl;
            else
                existingEnv.Remove("ANTHROPIC_BASE_URL");

            existingTopLevel["model"] = profile.Model;
            existingTopLevel["includeCoAuthoredBy"] = profile.IncludeCoAuthoredBy ? "true" : "false";
            existingTopLevel["skipDangerousModePermissionPrompt"] = profile.SkipDangerousMode ? "true" : "false";
            existingTopLevel["effortLevel"] = profile.EffortLevel;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"env\": {");
            int envIdx = 0;
            foreach (var kv in existingEnv)
            {
                sb.Append("    " + JsonEscape(kv.Key) + ": " + JsonEscape(kv.Value));
                envIdx++;
                if (envIdx < existingEnv.Count) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  },");
            int topIdx = 0;
            foreach (var kv in existingTopLevel)
            {
                string val = kv.Value;
                if (val == "true" || val == "false")
                    sb.Append("  " + JsonEscape(kv.Key) + ": " + val);
                else
                    sb.Append("  " + JsonEscape(kv.Key) + ": " + JsonEscape(val));
                topIdx++;
                if (topIdx < existingTopLevel.Count) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("}");

            File.WriteAllText(settingsPath, sb.ToString(), Encoding.UTF8);
            data.CCActiveProfile = profile.Name;
            data.Save();
            return settingsPath;
        }

        private void ParseSettingsJson(string json, Dictionary<string, string> topLevel, Dictionary<string, string> env)
        {
            int envIdx = json.IndexOf("\"env\"");
            int envBrace = -1, envEnd = -1;
            if (envIdx >= 0)
            {
                envBrace = json.IndexOf('{', envIdx);
                if (envBrace >= 0)
                {
                    envEnd = json.IndexOf('}', envBrace);
                    if (envEnd >= 0)
                        ParseKeyValues(json.Substring(envBrace + 1, envEnd - envBrace - 1), env);
                }
            }

            int outerStart = json.IndexOf('{');
            int outerEnd = json.LastIndexOf('}');
            if (outerStart < 0 || outerEnd < 0) return;
            string outer = json.Substring(outerStart + 1, outerEnd - outerStart - 1);

            if (envIdx >= 0 && envBrace >= 0 && envEnd >= 0)
            {
                int relStart = envIdx - outerStart - 1;
                int relEnd = envEnd - outerStart;
                if (relStart >= 0 && relEnd <= outer.Length)
                {
                    int afterEnv = relEnd;
                    while (afterEnv < outer.Length && (outer[afterEnv] == ',' || outer[afterEnv] == ' ' || outer[afterEnv] == '\r' || outer[afterEnv] == '\n'))
                        afterEnv++;
                    outer = outer.Substring(0, relStart) + outer.Substring(afterEnv);
                }
            }
            ParseKeyValues(outer, topLevel);
        }

        private void ParseKeyValues(string fragment, Dictionary<string, string> dict)
        {
            int pos = 0;
            while (pos < fragment.Length)
            {
                int keyStart = fragment.IndexOf('"', pos);
                if (keyStart < 0) break;
                int keyEnd = fragment.IndexOf('"', keyStart + 1);
                if (keyEnd < 0) break;
                string key = fragment.Substring(keyStart + 1, keyEnd - keyStart - 1);

                int colon = fragment.IndexOf(':', keyEnd);
                if (colon < 0) break;

                int valStart = colon + 1;
                while (valStart < fragment.Length && (fragment[valStart] == ' ' || fragment[valStart] == '\t' || fragment[valStart] == '\r' || fragment[valStart] == '\n'))
                    valStart++;
                if (valStart >= fragment.Length) break;

                string value;
                if (fragment[valStart] == '"')
                {
                    int valEnd = valStart + 1;
                    while (valEnd < fragment.Length)
                    {
                        if (fragment[valEnd] == '\\') { valEnd += 2; continue; }
                        if (fragment[valEnd] == '"') break;
                        valEnd++;
                    }
                    value = fragment.Substring(valStart + 1, valEnd - valStart - 1).Replace("\\\"", "\"").Replace("\\\\", "\\");
                    pos = valEnd + 1;
                }
                else if (fragment[valStart] == '{')
                {
                    int depth = 1, objEnd = valStart + 1;
                    while (objEnd < fragment.Length && depth > 0)
                    {
                        if (fragment[objEnd] == '{') depth++;
                        else if (fragment[objEnd] == '}') depth--;
                        objEnd++;
                    }
                    pos = objEnd;
                    continue;
                }
                else
                {
                    int valEnd = valStart;
                    while (valEnd < fragment.Length && fragment[valEnd] != ',' && fragment[valEnd] != '}' && fragment[valEnd] != '\r' && fragment[valEnd] != '\n')
                        valEnd++;
                    value = fragment.Substring(valStart, valEnd - valStart).Trim();
                    pos = valEnd;
                }
                dict[key] = value;
            }
        }

        private string JsonEscape(string s)
        {
            if (s == null) return "\"\"";
            StringBuilder sb = new StringBuilder();
            sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '"') sb.Append("\\\"");
                else if (c == '\\') sb.Append("\\\\");
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\t') sb.Append("\\t");
                else sb.Append(c);
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
