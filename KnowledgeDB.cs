using System;
using System.Collections.Generic;

namespace QuickNote
{
    public class KnowledgeItem
    {
        public long Id { get; set; }
        public string Content { get; set; }
        public string Tags { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class KnowledgeDB
    {
        private AppData data;

        public KnowledgeDB()
        {
            data = AppData.Instance;
        }

        public long Insert(string content, string tags)
        {
            KnowledgeItem item = new KnowledgeItem();
            item.Id = data.KnowledgeNextId++;
            item.Content = content ?? "";
            item.Tags = tags ?? "";
            item.CreatedAt = DateTime.Now;
            item.UpdatedAt = DateTime.Now;
            data.KnowledgeItems.Add(item);
            data.Save();
            return item.Id;
        }

        public void Update(long id, string content, string tags)
        {
            KnowledgeItem item = FindById(id);
            if (item != null)
            {
                item.Content = content ?? "";
                item.Tags = tags ?? "";
                item.UpdatedAt = DateTime.Now;
                data.Save();
            }
        }

        public void Delete(long id)
        {
            KnowledgeItem item = FindById(id);
            if (item != null)
            {
                data.KnowledgeItems.Remove(item);
                data.Save();
            }
        }

        public KnowledgeItem GetById(long id)
        {
            return FindById(id);
        }

        public List<KnowledgeItem> GetAll()
        {
            List<KnowledgeItem> result = new List<KnowledgeItem>(data.KnowledgeItems);
            result.Sort(delegate(KnowledgeItem a, KnowledgeItem b) {
                return b.UpdatedAt.CompareTo(a.UpdatedAt);
            });
            return result;
        }

        public List<KnowledgeItem> Search(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return GetAll();
            string kw = keyword.ToLower();
            List<KnowledgeItem> result = new List<KnowledgeItem>();
            for (int i = 0; i < data.KnowledgeItems.Count; i++)
            {
                KnowledgeItem item = data.KnowledgeItems[i];
                bool match = (item.Content != null && item.Content.ToLower().Contains(kw))
                          || (item.Tags != null && item.Tags.ToLower().Contains(kw));
                if (match) result.Add(item);
            }
            result.Sort(delegate(KnowledgeItem a, KnowledgeItem b) {
                return b.UpdatedAt.CompareTo(a.UpdatedAt);
            });
            return result;
        }

        public List<string> GetAllTags()
        {
            List<string> tags = new List<string>();
            for (int i = 0; i < data.KnowledgeItems.Count; i++)
            {
                string itemTags = data.KnowledgeItems[i].Tags;
                if (string.IsNullOrEmpty(itemTags)) continue;
                string normalized = itemTags.Replace('，', ',').Replace('；', ',').Replace(';', ',').Replace(' ', ',');
                foreach (string t in normalized.Split(','))
                {
                    string tag = t.Trim();
                    if (!string.IsNullOrEmpty(tag) && !tags.Contains(tag))
                        tags.Add(tag);
                }
            }
            tags.Sort();
            return tags;
        }

        private KnowledgeItem FindById(long id)
        {
            for (int i = 0; i < data.KnowledgeItems.Count; i++)
                if (data.KnowledgeItems[i].Id == id) return data.KnowledgeItems[i];
            return null;
        }
    }
}
