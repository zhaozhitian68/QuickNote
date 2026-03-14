using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace QuickNote
{
    /// <summary>
    /// 知识库界面面板
    /// </summary>
    public class KnowledgePanel : Panel
    {
        private KnowledgeDB db;
        private TextBox txtSearch;
        private FlowLayoutPanel tagPanel;
        private FlowLayoutPanel itemsPanel;
        private Button btnAddKnowledge;
        private ToolTip toolTip;

        private string currentTag = null; // null 表示"全部"
        private string currentSearchKeyword = "";
        private Timer searchTimer; // 实时搜索防抖
        private int currentPage = 1; // 当前页码
        private const int PAGE_SIZE = 10; // 每页显示数量

        private const int ICON_WIDTH = 26;
        private const int ICON_HEIGHT = 24;
        private const int KB_RIGHT_PADDING = 26;

        public KnowledgePanel()
        {
            db = new KnowledgeDB();
            toolTip = new ToolTip();

            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
            this.Padding = new Padding(10);

            InitializeControls();
            RefreshTagButtons();
            RefreshKnowledgeList();
        }

        private void InitializeControls()
        {
            // 知识条目列表（先添加 Fill，让它在最底层）
            itemsPanel = new FlowLayoutPanel();
            itemsPanel.Dock = DockStyle.Fill;
            itemsPanel.FlowDirection = FlowDirection.TopDown;
            itemsPanel.WrapContents = false;
            itemsPanel.AutoScroll = true;
            itemsPanel.Padding = new Padding(0, 10, 0, 10);
            itemsPanel.Resize += ItemsPanel_Resize;
            this.Controls.Add(itemsPanel);

            // 新增按钮
            btnAddKnowledge = new Button();
            btnAddKnowledge.Text = "+ 新增知识条目";
            btnAddKnowledge.Dock = DockStyle.Bottom;
            btnAddKnowledge.Height = 50;
            btnAddKnowledge.FlatStyle = FlatStyle.Flat;
            btnAddKnowledge.FlatAppearance.BorderSize = 0;
            btnAddKnowledge.BackColor = Color.FromArgb(250, 250, 250);
            btnAddKnowledge.ForeColor = Color.DarkGray;
            btnAddKnowledge.Cursor = Cursors.Hand;
            btnAddKnowledge.Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold);
            btnAddKnowledge.Click += BtnAddKnowledge_Click;
            this.Controls.Add(btnAddKnowledge);

            // 标签筛选面板（后添加，显示在上面）
            tagPanel = new FlowLayoutPanel();
            tagPanel.Dock = DockStyle.Top;
            tagPanel.AutoSize = true;
            tagPanel.MaximumSize = new Size(0, 200); // 最大高度200px
            tagPanel.FlowDirection = FlowDirection.LeftToRight;
            tagPanel.WrapContents = true; // 允许换行
            tagPanel.Padding = new Padding(0, 8, 0, 8);
            this.Controls.Add(tagPanel);

            // 搜索框（最后添加，显示在最上面）
            txtSearch = new TextBox();
            txtSearch.Dock = DockStyle.Top;
            txtSearch.Height = 36;
            txtSearch.Font = new Font("Microsoft YaHei", 12F);
            txtSearch.ForeColor = Color.Gray;
            txtSearch.Text = "🔍 搜索知识库...";
            txtSearch.Enter += (s, e) =>
            {
                if (txtSearch.Text == "🔍 搜索知识库...")
                {
                    txtSearch.Text = "";
                    txtSearch.ForeColor = Color.Black;
                }
            };
            txtSearch.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    txtSearch.Text = "🔍 搜索知识库...";
                    txtSearch.ForeColor = Color.Gray;
                }
            };
            txtSearch.TextChanged += (s, e) =>
            {
                // 实时搜索防抖
                if (searchTimer != null) searchTimer.Stop();
                searchTimer = new Timer();
                searchTimer.Interval = 300; // 300ms 防抖
                searchTimer.Tick += (ss, ee) =>
                {
                    searchTimer.Stop();
                    currentSearchKeyword = txtSearch.Text == "🔍 搜索知识库..." ? "" : txtSearch.Text;
                    currentPage = 1; // 搜索时重置到第一页
                    RefreshKnowledgeList();
                };
                searchTimer.Start();
            };
            this.Controls.Add(txtSearch);
        }

        /// <summary>
        /// 刷新标签按钮
        /// </summary>
        private void RefreshTagButtons()
        {
            tagPanel.Controls.Clear();

            // "全部" 按钮
            Button btnAll = CreateTagButton("全部", null);
            tagPanel.Controls.Add(btnAll);

            // 现有标签
            List<string> tags = db.GetAllTags();
            foreach (string tag in tags)
            {
                Button btn = CreateTagButton(tag, tag);
                tagPanel.Controls.Add(btn);
            }
        }

        private Button CreateTagButton(string text, string tag)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.AutoSize = true;
            btn.MinimumSize = new Size(40, 26);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor = Cursors.Hand;
            btn.Font = new Font("Microsoft YaHei", 9F);
            btn.Margin = new Padding(0, 0, 12, 4);
            btn.Padding = new Padding(0);
            btn.Tag = tag;

            // 高亮当前选中标签（超链接样式）
            if ((currentTag == null && tag == null) ||
                (currentTag != null && currentTag == tag))
            {
                btn.BackColor = Color.Transparent;
                btn.ForeColor = Color.FromArgb(0, 120, 215);
                btn.Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold | FontStyle.Underline);
            }
            else
            {
                btn.BackColor = Color.Transparent;
                btn.ForeColor = Color.FromArgb(100, 100, 100);
                btn.Font = new Font("Microsoft YaHei", 9F);
            }

            btn.Click += (s, e) =>
            {
                currentTag = tag;
                currentPage = 1; // 切换标签时重置到第一页
                RefreshTagButtons();
                RefreshKnowledgeList();
            };

            // 鼠标悬停效果
            btn.MouseEnter += (s, e) =>
            {
                if ((currentTag == null && tag == null) || (currentTag != null && currentTag == tag))
                    return;
                btn.ForeColor = Color.FromArgb(0, 120, 215);
                btn.Font = new Font("Microsoft YaHei", 9F, FontStyle.Underline);
            };
            btn.MouseLeave += (s, e) =>
            {
                if ((currentTag == null && tag == null) || (currentTag != null && currentTag == tag))
                    return;
                btn.ForeColor = Color.FromArgb(100, 100, 100);
                btn.Font = new Font("Microsoft YaHei", 9F);
            };

            return btn;
        }

        /// <summary>
        /// 刷新知识列表
        /// </summary>
        public void RefreshKnowledgeList()
        {
            itemsPanel.SuspendLayout();
            itemsPanel.Controls.Clear();

            List<KnowledgeItem> items;
            if (string.IsNullOrWhiteSpace(currentSearchKeyword))
            {
                items = db.GetAll();
            }
            else
            {
                items = db.Search(currentSearchKeyword);
            }

            // 按标签筛选
            if (currentTag != null)
            {
                List<KnowledgeItem> filtered = new List<KnowledgeItem>();
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].Tags != null)
                    {
                        string[] tagArray = SplitTags(items[i].Tags);
                        bool hasTag = false;
                        for (int j = 0; j < tagArray.Length; j++)
                        {
                            if (tagArray[j] == currentTag)
                            {
                                hasTag = true;
                                break;
                            }
                        }
                        if (hasTag)
                        {
                            filtered.Add(items[i]);
                        }
                    }
                }
                items = filtered;
            }

            string debugMsg = "keyword='" + currentSearchKeyword + "' tag='" + (currentTag ?? "null") + "' result=" + items.Count;
            System.Diagnostics.Debug.WriteLine(debugMsg);

            // 分页处理
            int totalCount = items.Count;
            int totalPages = (totalCount + PAGE_SIZE - 1) / PAGE_SIZE;
            if (currentPage > totalPages && totalPages > 0) currentPage = totalPages;
            if (currentPage < 1) currentPage = 1;

            int startIndex = (currentPage - 1) * PAGE_SIZE;
            int endIndex = Math.Min(startIndex + PAGE_SIZE, totalCount);

            // 显示当前页的知识
            for (int i = startIndex; i < endIndex; i++)
            {
                Panel card = CreateKnowledgeCard(items[i]);
                itemsPanel.Controls.Add(card);
            }

            // 添加分页控件
            if (totalPages > 1)
            {
                Panel paginationPanel = CreatePaginationPanel(currentPage, totalPages);
                itemsPanel.Controls.Add(paginationPanel);
            }

            itemsPanel.ResumeLayout(true);
        }

        /// <summary>
        /// 分割标签（支持中文逗号、英文逗号、空格、分号）
        /// </summary>
        private string[] SplitTags(string tags)
        {
            if (string.IsNullOrEmpty(tags)) return new string[0];

            // 替换所有分隔符为统一的分隔符
            string normalized = tags.Replace('，', ',').Replace('；', ',').Replace(';', ',').Replace(' ', ',');
            string[] parts = normalized.Split(',');

            List<string> result = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                string tag = parts[i].Trim();
                if (!string.IsNullOrEmpty(tag))
                {
                    result.Add(tag);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// 创建分页控件
        /// </summary>
        private Panel CreatePaginationPanel(int current, int total)
        {
            Panel panel = new Panel();
            panel.Width = itemsPanel.ClientSize.Width - 25;
            panel.Height = 40;
            panel.Margin = new Padding(0, 10, 0, 0);

            Label lblInfo = new Label();
            lblInfo.Text = "第 " + current + " / " + total + " 页";
            lblInfo.Font = new Font("Microsoft YaHei", 9F);
            lblInfo.ForeColor = Color.Gray;
            lblInfo.AutoSize = true;
            lblInfo.Location = new Point(10, 10);
            panel.Controls.Add(lblInfo);

            // 上一页按钮
            if (current > 1)
            {
                Button btnPrev = new Button();
                btnPrev.Text = "上一页";
                btnPrev.Size = new Size(70, 28);
                btnPrev.Location = new Point(panel.Width / 2 - 80, 6);
                btnPrev.Anchor = AnchorStyles.Top;
                btnPrev.FlatStyle = FlatStyle.Flat;
                btnPrev.BackColor = Color.FromArgb(240, 240, 240);
                btnPrev.Cursor = Cursors.Hand;
                btnPrev.Font = new Font("Microsoft YaHei", 9F);
                btnPrev.Click += (s, e) =>
                {
                    currentPage--;
                    RefreshKnowledgeList();
                };
                panel.Controls.Add(btnPrev);
            }

            // 下一页按钮
            if (current < total)
            {
                Button btnNext = new Button();
                btnNext.Text = "下一页";
                btnNext.Size = new Size(70, 28);
                btnNext.Location = new Point(panel.Width / 2 + 10, 6);
                btnNext.Anchor = AnchorStyles.Top;
                btnNext.FlatStyle = FlatStyle.Flat;
                btnNext.BackColor = Color.FromArgb(240, 240, 240);
                btnNext.Cursor = Cursors.Hand;
                btnNext.Font = new Font("Microsoft YaHei", 9F);
                btnNext.Click += (s, e) =>
                {
                    currentPage++;
                    RefreshKnowledgeList();
                };
                panel.Controls.Add(btnNext);
            }

            return panel;
        }

        /// <summary>
        /// 创建知识卡片（结构与快捷便签一致：block -> borderPanel -> innerPanel -> TextBox + 按钮）
        /// </summary>
        private Panel CreateKnowledgeCard(KnowledgeItem item)
        {
            Panel block = new Panel();
            block.Width = itemsPanel.ClientSize.Width - 25;
            block.Height = 60;
            block.Margin = new Padding(0, 0, 0, 8);
            block.BackColor = Color.White;

            Font font = new Font("Microsoft YaHei", 11F, FontStyle.Regular);

            // 灰色边框容器
            Panel borderPanel = new Panel();
            borderPanel.BackColor = Color.LightGray;
            borderPanel.Padding = new Padding(1);
            borderPanel.Location = new Point(0, 0);
            borderPanel.Width = block.Width;
            borderPanel.Height = 50;

            // 内部白色容器
            Panel innerPanel = new Panel();
            innerPanel.BackColor = Color.White;
            innerPanel.Dock = DockStyle.Fill;

            // 文本框（只读，点击可编辑）
            TextBox tb = new TextBox();
            tb.Multiline = true;
            tb.WordWrap = true;
            tb.ScrollBars = ScrollBars.None;
            tb.ReadOnly = true;
            tb.Text = item.Content;
            tb.Font = font;
            tb.ForeColor = Color.FromArgb(64, 64, 64);
            tb.BackColor = Color.White;
            tb.BorderStyle = BorderStyle.None;
            tb.Cursor = Cursors.IBeam;
            tb.Location = new Point(3, 4);
            tb.Width = innerPanel.Width - KB_RIGHT_PADDING - 6;
            tb.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            tb.Tag = item; // Tag 存 KnowledgeItem，用于编辑
            tb.DoubleClick += (s, e) => EditKnowledge(item);

            // 标签和日期（显示在文本框下方，通过 borderPanel.Tag 关联）
            Label lblMeta = new Label();
            string metaText = "";
            if (!string.IsNullOrEmpty(item.Tags))
            {
                string[] tagArr = SplitTags(item.Tags);
                for (int i = 0; i < tagArr.Length; i++)
                {
                    if (i > 0) metaText += " ";
                    metaText += "#" + tagArr[i];
                }
            }
            metaText += "     " + item.UpdatedAt.ToString("yyyy-MM-dd");
            lblMeta.Text = metaText;
            lblMeta.Font = new Font("Microsoft YaHei", 8F);
            lblMeta.ForeColor = Color.FromArgb(150, 150, 150);
            lblMeta.AutoSize = false;
            lblMeta.Height = 18;
            lblMeta.Width = block.Width - KB_RIGHT_PADDING - 6;
            lblMeta.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            // Location 会在 AdjustKBBlockHeight 中设置

            // 复制按钮
            Label lblCopy = new Label();
            lblCopy.Text = "❐";
            lblCopy.Font = new Font("Segoe UI Symbol", 10F, FontStyle.Bold);
            lblCopy.AutoSize = false;
            lblCopy.Size = new Size(ICON_WIDTH, ICON_HEIGHT);
            lblCopy.Location = new Point(innerPanel.Width - ICON_WIDTH, 0);
            lblCopy.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblCopy.ForeColor = Color.RoyalBlue;
            lblCopy.Cursor = Cursors.Hand;
            lblCopy.TextAlign = ContentAlignment.MiddleCenter;
            lblCopy.Tag = item;
            toolTip.SetToolTip(lblCopy, "复制内容");
            lblCopy.Click += LblCopy_Click;
            lblCopy.MouseEnter += (s, e) => lblCopy.ForeColor = Color.DeepSkyBlue;
            lblCopy.MouseLeave += (s, e) => lblCopy.ForeColor = Color.RoyalBlue;

            // 删除按钮
            Label lblDel = new Label();
            lblDel.Text = "×";
            lblDel.Font = new Font("Arial", 14F, FontStyle.Bold);
            lblDel.AutoSize = false;
            lblDel.Size = new Size(ICON_WIDTH, ICON_HEIGHT);
            lblDel.Location = new Point(innerPanel.Width - ICON_WIDTH, ICON_HEIGHT);
            lblDel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblDel.ForeColor = Color.Crimson;
            lblDel.Cursor = Cursors.Hand;
            lblDel.TextAlign = ContentAlignment.MiddleCenter;
            lblDel.Tag = item;
            toolTip.SetToolTip(lblDel, "删除");
            lblDel.Click += LblDel_Click;
            lblDel.MouseEnter += (s, e) => lblDel.ForeColor = Color.Red;
            lblDel.MouseLeave += (s, e) => lblDel.ForeColor = Color.Crimson;

            // 组装层级
            innerPanel.Controls.Add(tb);
            innerPanel.Controls.Add(lblMeta);
            innerPanel.Controls.Add(lblCopy);
            innerPanel.Controls.Add(lblDel);
            borderPanel.Controls.Add(innerPanel);
            block.Controls.Add(borderPanel);

            // 关联 Tag，用于 Resize 时找到 TextBox 和 lblMeta
            borderPanel.Tag = new object[] { tb, lblMeta };

            AdjustKBBlockHeight(tb, lblMeta);

            return block;
        }

        /// <summary>
        /// 计算知识卡片高度（与快捷便签的 AdjustBlockHeight 逻辑一致）
        /// </summary>
        private void AdjustKBBlockHeight(TextBox tb, Label lblMeta, int manualWidth = -1)
        {
            Panel innerPanel = tb.Parent as Panel;
            if (innerPanel == null) return;
            Panel borderPanel = innerPanel.Parent as Panel;
            if (borderPanel == null) return;

            int width = manualWidth > 0 ? manualWidth : tb.Width;
            if (width <= 0) return;

            TextFormatFlags flags = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl;
            string textToMeasure = tb.Text;
            if (textToMeasure.EndsWith("\r\n") || textToMeasure.EndsWith("\n")) textToMeasure += "|";
            if (string.IsNullOrEmpty(textToMeasure)) textToMeasure = "|";

            Size size = TextRenderer.MeasureText(textToMeasure, tb.Font, new Size(width, 0), flags);

            int tbHeight = size.Height + 8;
            int minTbHeight = tb.Font.Height + 8;
            if (tbHeight < minTbHeight) tbHeight = minTbHeight;

            // 最小高度要容纳两个图标
            int minIconHeight = ICON_HEIGHT * 2 + 2;

            if (tb.Height != tbHeight) tb.Height = tbHeight;

            // lblMeta 紧跟在 TextBox 下方
            lblMeta.Location = new Point(3, 4 + tbHeight + 2);

            int totalContentHeight = tbHeight + 18 + 8; // TextBox + lblMeta + padding
            if (totalContentHeight < minIconHeight) totalContentHeight = minIconHeight;

            if (innerPanel.Height != totalContentHeight) innerPanel.Height = totalContentHeight;
            if (borderPanel.Height != totalContentHeight + 2) borderPanel.Height = totalContentHeight + 2;

            Panel block = borderPanel.Parent as Panel;
            if (block != null)
            {
                int newBlockHeight = borderPanel.Top + borderPanel.Height + 5;
                if (block.Height != newBlockHeight) block.Height = newBlockHeight;
            }
        }

        private void BtnAddKnowledge_Click(object sender, EventArgs e)
        {
            List<string> tags = db.GetAllTags();
            using (KnowledgeEditDialog dlg = new KnowledgeEditDialog(tags))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    db.Insert(dlg.KnowledgeContent, dlg.KnowledgeTags);
                    RefreshTagButtons();
                    RefreshKnowledgeList();
                }
            }
        }

        private void EditKnowledge(KnowledgeItem item)
        {
            List<string> tags = db.GetAllTags();
            using (KnowledgeEditDialog dlg = new KnowledgeEditDialog(tags, item))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    db.Update(item.Id, dlg.KnowledgeContent, dlg.KnowledgeTags);
                    RefreshTagButtons();
                    RefreshKnowledgeList();
                }
            }
        }

        private void LblCopy_Click(object sender, EventArgs e)
        {
            Label lbl = (Label)sender;
            KnowledgeItem item = (KnowledgeItem)lbl.Tag;

            string fullText = item.Content;
            if (!string.IsNullOrEmpty(fullText))
            {
                Clipboard.SetText(fullText);
                string originalText = lbl.Text;
                lbl.Text = "✔";
                lbl.ForeColor = Color.Green;

                Timer t = new Timer();
                t.Interval = 1000;
                t.Tag = new object[] { lbl, originalText };
                t.Tick += (s, ee) =>
                {
                    Timer timer = (Timer)s;
                    object[] states = (object[])timer.Tag;
                    Label label = (Label)states[0];
                    string orig = (string)states[1];
                    label.Text = orig;
                    label.ForeColor = Color.RoyalBlue;
                    timer.Stop();
                    timer.Dispose();
                };
                t.Start();
            }
        }

        private void LblDel_Click(object sender, EventArgs e)
        {
            Label lbl = (Label)sender;
            KnowledgeItem item = (KnowledgeItem)lbl.Tag;

            DialogResult result = MessageBox.Show(
                "确定要删除这条知识吗？",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                db.Delete(item.Id);
                RefreshTagButtons();
                RefreshKnowledgeList();
            }
        }

        /// <summary>
        /// 从外部添加知识（便签转存）
        /// </summary>
        public void AddKnowledgeFromNote(string content, string tags)
        {
            db.Insert(content, tags);
            RefreshTagButtons();
            RefreshKnowledgeList();
        }

        /// <summary>
        /// 窗口大小变化时，重新计算所有知识卡片的宽度和高度
        /// </summary>
        private void ItemsPanel_Resize(object sender, EventArgs e)
        {
            itemsPanel.SuspendLayout();
            foreach (Control block in itemsPanel.Controls)
            {
                block.Width = itemsPanel.ClientSize.Width - 25;

                foreach (Control c in block.Controls)
                {
                    Panel borderPanel = c as Panel;
                    if (borderPanel != null && borderPanel.Tag is object[])
                    {
                        object[] tags = (object[])borderPanel.Tag;
                        TextBox tb = tags[0] as TextBox;
                        Label lblMeta = tags[1] as Label;
                        if (tb != null && lblMeta != null)
                        {
                            int targetWidth = block.Width;
                            if (borderPanel.Width != targetWidth) borderPanel.Width = targetWidth;
                            AdjustKBBlockHeight(tb, lblMeta, targetWidth - KB_RIGHT_PADDING - 6);
                        }
                    }
                }
            }
            itemsPanel.ResumeLayout(true);
        }
    }
}
