using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QuickNote
{
    /// <summary>
    /// 知识库数据项
    /// </summary>
    public class KnowledgeItem
    {
        public long Id { get; set; }
        public string Content { get; set; }
        public string Tags { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 知识库数据操作类（JSON 文件存储，零外部依赖）
    /// </summary>
    public class KnowledgeDB
    {
        private string dataFile;
        private List<KnowledgeItem> items;
        private long nextId;

        public KnowledgeDB(string file = "knowledge.json")
        {
            dataFile = file;
            items = new List<KnowledgeItem>();
            nextId = 1;
            Load();
        }

        /// <summary>
        /// 插入新知识条目
        /// </summary>
        public long Insert(string content, string tags)
        {
            KnowledgeItem item = new KnowledgeItem();
            item.Id = nextId++;
            item.Content = content ?? "";
            item.Tags = tags ?? "";
            item.CreatedAt = DateTime.Now;
            item.UpdatedAt = DateTime.Now;
            items.Add(item);
            Save();
            return item.Id;
        }

        /// <summary>
        /// 更新知识条目
        /// </summary>
        public void Update(long id, string content, string tags)
        {
            KnowledgeItem item = FindById(id);
            if (item != null)
            {
                item.Content = content ?? "";
                item.Tags = tags ?? "";
                item.UpdatedAt = DateTime.Now;
                Save();
            }
        }

        /// <summary>
        /// 删除知识条目
        /// </summary>
        public void Delete(long id)
        {
            KnowledgeItem item = FindById(id);
            if (item != null)
            {
                items.Remove(item);
                Save();
            }
        }

        /// <summary>
        /// 根据 ID 获取单条知识
        /// </summary>
        public KnowledgeItem GetById(long id)
        {
            return FindById(id);
        }

        /// <summary>
        /// 获取所有知识条目（按更新时间倒序）
        /// </summary>
        public List<KnowledgeItem> GetAll()
        {
            List<KnowledgeItem> result = new List<KnowledgeItem>(items);
            // 按更新时间倒序排列
            result.Sort(delegate(KnowledgeItem a, KnowledgeItem b)
            {
                return b.UpdatedAt.CompareTo(a.UpdatedAt);
            });
            return result;
        }

        /// <summary>
        /// 关键词搜索（内容、标签中包含关键词，不区分大小写）
        /// </summary>
        public List<KnowledgeItem> Search(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return GetAll();
            }

            string kw = keyword.ToLower();
            List<KnowledgeItem> result = new List<KnowledgeItem>();

            for (int i = 0; i < items.Count; i++)
            {
                KnowledgeItem item = items[i];
                bool match = false;
                if (item.Content != null && item.Content.ToLower().Contains(kw)) match = true;
                if (!match && item.Tags != null && item.Tags.ToLower().Contains(kw)) match = true;

                if (match) result.Add(item);
            }

            result.Sort(delegate(KnowledgeItem a, KnowledgeItem b)
            {
                return b.UpdatedAt.CompareTo(a.UpdatedAt);
            });
            return result;
        }

        /// <summary>
        /// 获取所有标签（去重）
        /// </summary>
        public List<string> GetAllTags()
        {
            List<string> tags = new List<string>();
            for (int i = 0; i < items.Count; i++)
            {
                string itemTags = items[i].Tags;
                if (string.IsNullOrEmpty(itemTags)) continue;

                // 支持多种分隔符：英文逗号、中文逗号、空格、分号
                string normalized = itemTags.Replace('，', ',').Replace('；', ',').Replace(';', ',').Replace(' ', ',');
                string[] tagArray = normalized.Split(',');
                for (int j = 0; j < tagArray.Length; j++)
                {
                    string tag = tagArray[j].Trim();
                    if (!string.IsNullOrEmpty(tag) && !tags.Contains(tag))
                    {
                        tags.Add(tag);
                    }
                }
            }
            tags.Sort();
            return tags;
        }

        private KnowledgeItem FindById(long id)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Id == id) return items[i];
            }
            return null;
        }

        // ========== JSON 序列化/反序列化（手写，零依赖） ==========

        private void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[");
                for (int i = 0; i < items.Count; i++)
                {
                    KnowledgeItem item = items[i];
                    sb.Append("  {");
                    sb.Append("\"id\":" + item.Id);
                    sb.Append(",\"content\":" + JsonEscape(item.Content));
                    sb.Append(",\"tags\":" + JsonEscape(item.Tags));
                    sb.Append(",\"createdAt\":\"" + item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") + "\"");
                    sb.Append(",\"updatedAt\":\"" + item.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss") + "\"");
                    sb.Append("}");
                    if (i < items.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("]");
                File.WriteAllText(dataFile, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private void Load()
        {
            if (!File.Exists(dataFile)) return;

            try
            {
                string json = File.ReadAllText(dataFile, Encoding.UTF8);
                items = new List<KnowledgeItem>();
                nextId = 1;

                // 简易 JSON 数组解析：逐个提取 {...} 对象
                int pos = 0;
                while (pos < json.Length)
                {
                    int objStart = json.IndexOf('{', pos);
                    if (objStart < 0) break;
                    int objEnd = json.IndexOf('}', objStart);
                    if (objEnd < 0) break;

                    string objStr = json.Substring(objStart + 1, objEnd - objStart - 1);
                    KnowledgeItem item = ParseItem(objStr);
                    if (item != null)
                    {
                        items.Add(item);
                        if (item.Id >= nextId) nextId = item.Id + 1;
                    }
                    pos = objEnd + 1;
                }
            }
            catch { }
        }

        private KnowledgeItem ParseItem(string objStr)
        {
            KnowledgeItem item = new KnowledgeItem();
            item.Id = GetLongValue(objStr, "id");
            item.Content = GetStringValue(objStr, "content");
            item.Tags = GetStringValue(objStr, "tags");

            string createdStr = GetStringValue(objStr, "createdAt");
            string updatedStr = GetStringValue(objStr, "updatedAt");

            DateTime dt;
            item.CreatedAt = DateTime.TryParse(createdStr, out dt) ? dt : DateTime.Now;
            item.UpdatedAt = DateTime.TryParse(updatedStr, out dt) ? dt : DateTime.Now;

            if (item.Id <= 0) return null;
            return item;
        }

        private long GetLongValue(string objStr, string key)
        {
            string marker = "\"" + key + "\":";
            int idx = objStr.IndexOf(marker);
            if (idx < 0) return 0;
            int start = idx + marker.Length;
            // 跳过空格
            while (start < objStr.Length && objStr[start] == ' ') start++;
            int end = start;
            while (end < objStr.Length && objStr[end] >= '0' && objStr[end] <= '9') end++;
            if (end == start) return 0;
            long val;
            long.TryParse(objStr.Substring(start, end - start), out val);
            return val;
        }

        private string GetStringValue(string objStr, string key)
        {
            string marker = "\"" + key + "\":\"";
            int idx = objStr.IndexOf(marker);
            if (idx < 0) return "";
            int start = idx + marker.Length;
            StringBuilder sb = new StringBuilder();
            int i = start;
            while (i < objStr.Length)
            {
                char c = objStr[i];
                if (c == '\\' && i + 1 < objStr.Length)
                {
                    char next = objStr[i + 1];
                    if (next == '"') { sb.Append('"'); i += 2; continue; }
                    if (next == '\\') { sb.Append('\\'); i += 2; continue; }
                    if (next == 'n') { sb.Append('\n'); i += 2; continue; }
                    if (next == 'r') { sb.Append('\r'); i += 2; continue; }
                    if (next == 't') { sb.Append('\t'); i += 2; continue; }
                    sb.Append(c);
                    i++;
                }
                else if (c == '"')
                {
                    break;
                }
                else
                {
                    sb.Append(c);
                    i++;
                }
            }
            return sb.ToString();
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
