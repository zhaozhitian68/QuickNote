using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace QuickNote
{
    /// <summary>
    /// 统一数据存储，读写 data.json，单例模式
    /// </summary>
    public class AppData
    {
        private static AppData _instance;
        public static AppData Instance
        {
            get
            {
                if (_instance == null) _instance = new AppData();
                return _instance;
            }
        }

        private string dataFile;

        // 便签
        public List<string> Notes { get; private set; }

        // 知识库
        public List<KnowledgeItem> KnowledgeItems { get; private set; }
        public long KnowledgeNextId { get; set; }

        // CC 配置
        public List<CCProfile> CCProfiles { get; private set; }
        public string CCActiveProfile { get; set; }

        // 热键设置
        public string HotkeyValue { get; set; }      // "mod|vk"
        public string ScreenshotValue { get; set; }  // "mod|vk"

        private AppData()
        {
            dataFile = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "data.json");

            Notes = new List<string>();
            KnowledgeItems = new List<KnowledgeItem>();
            KnowledgeNextId = 1;
            CCProfiles = new List<CCProfile>();
            CCActiveProfile = "";
            HotkeyValue = "2|123";    // 默认 Ctrl+F12
            ScreenshotValue = "2|122"; // 默认 Ctrl+F11

            Load();
            Migrate();
        }

        // ========== 迁移旧文件 ==========

        private void Migrate()
        {
            bool changed = false;
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // 迁移 notes.txt
            string notesFile = Path.Combine(exeDir, "notes.txt");
            if (Notes.Count == 0 && File.Exists(notesFile))
            {
                foreach (string line in File.ReadAllLines(notesFile))
                    if (!string.IsNullOrWhiteSpace(line))
                        Notes.Add(line);
                changed = true;
            }

            // 迁移 knowledge.json
            string knowledgeFile = Path.Combine(exeDir, "knowledge.json");
            if (KnowledgeItems.Count == 0 && File.Exists(knowledgeFile))
            {
                LoadKnowledgeFromFile(knowledgeFile);
                changed = true;
            }

            // 迁移 cc_profiles.json
            string ccFile = Path.Combine(exeDir, "cc_profiles.json");
            if (CCProfiles.Count == 0 && File.Exists(ccFile))
            {
                LoadCCProfilesFromFile(ccFile);
                changed = true;
            }

            // 迁移 settings.txt
            string settingsFile = Path.Combine(exeDir, "settings.txt");
            if (HotkeyValue == "2|123" && File.Exists(settingsFile))
            {
                foreach (string line in File.ReadAllLines(settingsFile))
                {
                    if (line.StartsWith("hotkey=")) HotkeyValue = line.Substring(7);
                    else if (line.StartsWith("screenshot=")) ScreenshotValue = line.Substring(11);
                }
                changed = true;
            }

            if (changed) Save();
        }

        private void LoadKnowledgeFromFile(string file)
        {
            try
            {
                string json = File.ReadAllText(file, Encoding.UTF8);
                int pos = 0;
                while (pos < json.Length)
                {
                    int objStart = json.IndexOf('{', pos);
                    if (objStart < 0) break;
                    int objEnd = json.IndexOf('}', objStart);
                    if (objEnd < 0) break;
                    string objStr = json.Substring(objStart + 1, objEnd - objStart - 1);
                    KnowledgeItem item = ParseKnowledgeItem(objStr);
                    if (item != null)
                    {
                        KnowledgeItems.Add(item);
                        if (item.Id >= KnowledgeNextId) KnowledgeNextId = item.Id + 1;
                    }
                    pos = objEnd + 1;
                }
            }
            catch { }
        }

        private void LoadCCProfilesFromFile(string file)
        {
            try
            {
                string json = File.ReadAllText(file, Encoding.UTF8);
                CCActiveProfile = GetStringValue(json, "active");
                int arrStart = json.IndexOf('[');
                int arrEnd = json.LastIndexOf(']');
                if (arrStart < 0 || arrEnd < 0) return;
                string arrContent = json.Substring(arrStart + 1, arrEnd - arrStart - 1);
                int pos = 0;
                while (pos < arrContent.Length)
                {
                    int objStart = arrContent.IndexOf('{', pos);
                    if (objStart < 0) break;
                    int objEnd = arrContent.IndexOf('}', objStart);
                    if (objEnd < 0) break;
                    string objStr = arrContent.Substring(objStart + 1, objEnd - objStart - 1);
                    CCProfile p = ParseCCProfile(objStr);
                    if (p != null) CCProfiles.Add(p);
                    pos = objEnd + 1;
                }
            }
            catch { }
        }

        // ========== 保存 ==========

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");

                // notes
                sb.AppendLine("  \"notes\": [");
                for (int i = 0; i < Notes.Count; i++)
                {
                    sb.Append("    " + JsonEscape(Notes[i]));
                    if (i < Notes.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("  ],");

                // knowledge
                sb.AppendLine("  \"knowledge\": [");
                for (int i = 0; i < KnowledgeItems.Count; i++)
                {
                    KnowledgeItem item = KnowledgeItems[i];
                    sb.Append("    {");
                    sb.Append("\"id\":" + item.Id);
                    sb.Append(",\"content\":" + JsonEscape(item.Content));
                    sb.Append(",\"tags\":" + JsonEscape(item.Tags));
                    sb.Append(",\"createdAt\":\"" + item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") + "\"");
                    sb.Append(",\"updatedAt\":\"" + item.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss") + "\"");
                    sb.Append("}");
                    if (i < KnowledgeItems.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("  ],");

                // ccProfiles
                sb.AppendLine("  \"ccProfiles\": {");
                sb.AppendLine("    \"active\": " + JsonEscape(CCActiveProfile) + ",");
                sb.AppendLine("    \"profiles\": [");
                for (int i = 0; i < CCProfiles.Count; i++)
                {
                    CCProfile p = CCProfiles[i];
                    sb.Append("      {");
                    sb.Append("\"name\":" + JsonEscape(p.Name));
                    sb.Append(",\"apiKey\":" + JsonEscape(p.ApiKey));
                    sb.Append(",\"authToken\":" + JsonEscape(p.AuthToken));
                    sb.Append(",\"baseUrl\":" + JsonEscape(p.BaseUrl));
                    sb.Append(",\"model\":" + JsonEscape(p.Model));
                    sb.Append(",\"includeCoAuthoredBy\":" + (p.IncludeCoAuthoredBy ? "true" : "false"));
                    sb.Append(",\"skipDangerousMode\":" + (p.SkipDangerousMode ? "true" : "false"));
                    sb.Append(",\"effortLevel\":" + JsonEscape(p.EffortLevel));
                    sb.Append(",\"quotaQueryUrl\":" + JsonEscape(p.QuotaQueryUrl));
                    sb.Append("}");
                    if (i < CCProfiles.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("    ]");
                sb.AppendLine("  },");

                // settings
                sb.AppendLine("  \"settings\": {");
                sb.AppendLine("    \"hotkey\": " + JsonEscape(HotkeyValue) + ",");
                sb.AppendLine("    \"screenshot\": " + JsonEscape(ScreenshotValue));
                sb.AppendLine("  }");

                sb.AppendLine("}");
                File.WriteAllText(dataFile, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        // ========== 加载 ==========

        private void Load()
        {
            if (!File.Exists(dataFile)) return;
            try
            {
                string json = File.ReadAllText(dataFile, Encoding.UTF8);

                // notes 数组
                int notesStart = json.IndexOf("\"notes\"");
                if (notesStart >= 0)
                {
                    int arrOpen = json.IndexOf('[', notesStart);
                    int arrClose = json.IndexOf(']', arrOpen);
                    if (arrOpen >= 0 && arrClose >= 0)
                    {
                        string arrContent = json.Substring(arrOpen + 1, arrClose - arrOpen - 1);
                        int pos = 0;
                        while (pos < arrContent.Length)
                        {
                            int q = arrContent.IndexOf('"', pos);
                            if (q < 0) break;
                            string val = ReadQuotedString(arrContent, q + 1);
                            Notes.Add(val);
                            pos = q + 1;
                            // 跳过这个字符串
                            while (pos < arrContent.Length && arrContent[pos] != '"') pos++;
                            if (pos < arrContent.Length) pos++; // 跳过开头引号
                            // 跳到结束引号
                            while (pos < arrContent.Length && arrContent[pos] != '"')
                            {
                                if (arrContent[pos] == '\\') pos++;
                                pos++;
                            }
                            pos++; // 跳过结束引号
                        }
                    }
                }

                // knowledge 数组
                int knowledgeStart = json.IndexOf("\"knowledge\"");
                if (knowledgeStart >= 0)
                {
                    int arrOpen = json.IndexOf('[', knowledgeStart);
                    int arrClose = FindArrayEnd(json, arrOpen);
                    if (arrOpen >= 0 && arrClose >= 0)
                    {
                        string arrContent = json.Substring(arrOpen + 1, arrClose - arrOpen - 1);
                        int pos = 0;
                        while (pos < arrContent.Length)
                        {
                            int objStart = arrContent.IndexOf('{', pos);
                            if (objStart < 0) break;
                            int objEnd = arrContent.IndexOf('}', objStart);
                            if (objEnd < 0) break;
                            string objStr = arrContent.Substring(objStart + 1, objEnd - objStart - 1);
                            KnowledgeItem item = ParseKnowledgeItem(objStr);
                            if (item != null)
                            {
                                KnowledgeItems.Add(item);
                                if (item.Id >= KnowledgeNextId) KnowledgeNextId = item.Id + 1;
                            }
                            pos = objEnd + 1;
                        }
                    }
                }

                // ccProfiles 对象
                int ccStart = json.IndexOf("\"ccProfiles\"");
                if (ccStart >= 0)
                {
                    int objOpen = json.IndexOf('{', ccStart);
                    int objClose = FindObjectEnd(json, objOpen);
                    if (objOpen >= 0 && objClose >= 0)
                    {
                        string ccJson = json.Substring(objOpen, objClose - objOpen + 1);
                        CCActiveProfile = GetStringValue(ccJson, "active");
                        int arrOpen = ccJson.IndexOf('[');
                        int arrClose = FindArrayEnd(ccJson, arrOpen);
                        if (arrOpen >= 0 && arrClose >= 0)
                        {
                            string arrContent = ccJson.Substring(arrOpen + 1, arrClose - arrOpen - 1);
                            int pos = 0;
                            while (pos < arrContent.Length)
                            {
                                int objStart = arrContent.IndexOf('{', pos);
                                if (objStart < 0) break;
                                int objEnd = arrContent.IndexOf('}', objStart);
                                if (objEnd < 0) break;
                                string objStr = arrContent.Substring(objStart + 1, objEnd - objStart - 1);
                                CCProfile p = ParseCCProfile(objStr);
                                if (p != null) CCProfiles.Add(p);
                                pos = objEnd + 1;
                            }
                        }
                    }
                }

                // settings
                int settingsStart = json.IndexOf("\"settings\"");
                if (settingsStart >= 0)
                {
                    int objOpen = json.IndexOf('{', settingsStart);
                    int objClose = FindObjectEnd(json, objOpen);
                    if (objOpen >= 0 && objClose >= 0)
                    {
                        string settingsJson = json.Substring(objOpen, objClose - objOpen + 1);
                        string hk = GetStringValue(settingsJson, "hotkey");
                        string sc = GetStringValue(settingsJson, "screenshot");
                        if (!string.IsNullOrEmpty(hk)) HotkeyValue = hk;
                        if (!string.IsNullOrEmpty(sc)) ScreenshotValue = sc;
                    }
                }
            }
            catch { }
        }

        // ========== 解析辅助 ==========

        private KnowledgeItem ParseKnowledgeItem(string objStr)
        {
            KnowledgeItem item = new KnowledgeItem();
            item.Id = GetLongValue(objStr, "id");
            item.Content = GetStringValue(objStr, "content");
            item.Tags = GetStringValue(objStr, "tags");
            DateTime dt;
            item.CreatedAt = DateTime.TryParse(GetStringValue(objStr, "createdAt"), out dt) ? dt : DateTime.Now;
            item.UpdatedAt = DateTime.TryParse(GetStringValue(objStr, "updatedAt"), out dt) ? dt : DateTime.Now;
            if (item.Id <= 0) return null;
            return item;
        }

        private CCProfile ParseCCProfile(string objStr)
        {
            CCProfile p = new CCProfile();
            p.Name = GetStringValue(objStr, "name");
            p.ApiKey = GetStringValue(objStr, "apiKey");
            p.AuthToken = GetStringValue(objStr, "authToken");
            p.BaseUrl = GetStringValue(objStr, "baseUrl");
            p.Model = GetStringValue(objStr, "model");
            p.IncludeCoAuthoredBy = GetBoolValue(objStr, "includeCoAuthoredBy");
            p.SkipDangerousMode = GetBoolValue(objStr, "skipDangerousMode");
            p.EffortLevel = GetStringValue(objStr, "effortLevel");
            p.QuotaQueryUrl = GetStringValue(objStr, "quotaQueryUrl");
            if (string.IsNullOrEmpty(p.Name)) return null;
            return p;
        }

        // 找匹配的 ] 位置（处理嵌套）
        private int FindArrayEnd(string s, int openPos)
        {
            if (openPos < 0 || openPos >= s.Length) return -1;
            int depth = 1;
            int i = openPos + 1;
            while (i < s.Length && depth > 0)
            {
                if (s[i] == '[') depth++;
                else if (s[i] == ']') depth--;
                i++;
            }
            return depth == 0 ? i - 1 : -1;
        }

        // 找匹配的 } 位置（处理嵌套）
        private int FindObjectEnd(string s, int openPos)
        {
            if (openPos < 0 || openPos >= s.Length) return -1;
            int depth = 1;
            int i = openPos + 1;
            bool inStr = false;
            while (i < s.Length && depth > 0)
            {
                char c = s[i];
                if (inStr)
                {
                    if (c == '\\') i++;
                    else if (c == '"') inStr = false;
                }
                else
                {
                    if (c == '"') inStr = true;
                    else if (c == '{') depth++;
                    else if (c == '}') depth--;
                }
                i++;
            }
            return depth == 0 ? i - 1 : -1;
        }

        private string ReadQuotedString(string s, int start)
        {
            StringBuilder sb = new StringBuilder();
            int i = start;
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char next = s[i + 1];
                    if (next == '"') { sb.Append('"'); i += 2; continue; }
                    if (next == '\\') { sb.Append('\\'); i += 2; continue; }
                    if (next == 'n') { sb.Append('\n'); i += 2; continue; }
                    if (next == 'r') { sb.Append('\r'); i += 2; continue; }
                    if (next == 't') { sb.Append('\t'); i += 2; continue; }
                    sb.Append(c); i++;
                }
                else if (c == '"') break;
                else { sb.Append(c); i++; }
            }
            return sb.ToString();
        }

        // ========== JSON 工具 ==========

        public string GetStringValue(string objStr, string key)
        {
            string marker = "\"" + key + "\"";
            int idx = objStr.IndexOf(marker);
            if (idx < 0) return "";
            int pos = idx + marker.Length;
            while (pos < objStr.Length && (objStr[pos] == ' ' || objStr[pos] == '\t' || objStr[pos] == '\r' || objStr[pos] == '\n')) pos++;
            if (pos >= objStr.Length || objStr[pos] != ':') return "";
            pos++;
            while (pos < objStr.Length && (objStr[pos] == ' ' || objStr[pos] == '\t' || objStr[pos] == '\r' || objStr[pos] == '\n')) pos++;
            if (pos >= objStr.Length || objStr[pos] != '"') return "";
            return ReadQuotedString(objStr, pos + 1);
        }

        public long GetLongValue(string objStr, string key)
        {
            string marker = "\"" + key + "\":";
            int idx = objStr.IndexOf(marker);
            if (idx < 0) return 0;
            int start = idx + marker.Length;
            while (start < objStr.Length && objStr[start] == ' ') start++;
            int end = start;
            while (end < objStr.Length && objStr[end] >= '0' && objStr[end] <= '9') end++;
            if (end == start) return 0;
            long val;
            long.TryParse(objStr.Substring(start, end - start), out val);
            return val;
        }

        public bool GetBoolValue(string objStr, string key)
        {
            string marker = "\"" + key + "\":";
            int idx = objStr.IndexOf(marker);
            if (idx < 0) return false;
            int start = idx + marker.Length;
            while (start < objStr.Length && objStr[start] == ' ') start++;
            return start + 4 <= objStr.Length && objStr.Substring(start, 4) == "true";
        }

        public string JsonEscape(string s)
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
