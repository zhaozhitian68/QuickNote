using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace QuickNote
{
    /// <summary>
    /// 便签转存到知识库的对话框
    /// </summary>
    public class TransferDialog : Form
    {
        public string KnowledgeContent { get; private set; }
        public string KnowledgeTags { get; private set; }

        private TextBox txtTags;
        private Panel tagDropdownPanel;
        private bool isDropdownVisible = false;

        public TransferDialog(string content, List<string> existingTags)
        {
            KnowledgeContent = content;

            this.Text = "转存到知识库";
            this.Size = new Size(380, 180);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            int y = 18;
            int labelX = 18;
            int inputX = 72;
            int inputW = 272;

            // 标签
            Label lblTags = new Label();
            lblTags.Text = "标签:";
            lblTags.Location = new Point(labelX, y + 3);
            lblTags.AutoSize = true;
            lblTags.Font = new Font("Microsoft YaHei", 9F);

            txtTags = new TextBox();
            txtTags.Location = new Point(inputX, y);
            txtTags.Width = inputW;
            txtTags.Font = new Font("Microsoft YaHei", 10F);
            txtTags.Click += (s, e) => ShowTagDropdown(existingTags);
            y += 36;

            // 提示
            Label lblTip = new Label();
            lblTip.Text = "标签用逗号/空格/分号分隔，或点击输入框选择";
            lblTip.Location = new Point(inputX, y);
            lblTip.AutoSize = true;
            lblTip.Font = new Font("Microsoft YaHei", 8F);
            lblTip.ForeColor = Color.Gray;
            y += 30;

            // 按钮
            Button btnOK = new Button();
            btnOK.Text = "确定";
            btnOK.Location = new Point(160, y);
            btnOK.Size = new Size(84, 32);
            btnOK.FlatStyle = FlatStyle.Flat;
            btnOK.BackColor = Color.FromArgb(0, 120, 215);
            btnOK.ForeColor = Color.White;
            btnOK.Cursor = Cursors.Hand;
            btnOK.Font = new Font("Microsoft YaHei", 9F);
            btnOK.Click += (s, e) =>
            {
                KnowledgeTags = txtTags.Text.Trim();
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            Button btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.Location = new Point(254, y);
            btnCancel.Size = new Size(84, 32);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.BackColor = Color.FromArgb(240, 240, 240);
            btnCancel.ForeColor = Color.DarkGray;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.Font = new Font("Microsoft YaHei", 9F);
            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.AddRange(new Control[] {
                lblTags, txtTags,
                lblTip,
                btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void ShowTagDropdown(List<string> existingTags)
        {
            if (existingTags.Count == 0) return;
            if (isDropdownVisible)
            {
                HideTagDropdown();
                return;
            }

            // 创建下拉面板
            tagDropdownPanel = new Panel();
            tagDropdownPanel.BorderStyle = BorderStyle.FixedSingle;
            tagDropdownPanel.BackColor = Color.White;
            tagDropdownPanel.Location = new Point(txtTags.Left, txtTags.Bottom);
            tagDropdownPanel.Width = txtTags.Width;
            tagDropdownPanel.Height = Math.Min(existingTags.Count * 28 + 4, 200);
            tagDropdownPanel.AutoScroll = true;

            // 解析当前已选标签
            string[] selectedTags = SplitTags(txtTags.Text);

            int yPos = 2;
            foreach (string tag in existingTags)
            {
                CheckBox chk = new CheckBox();
                chk.Text = tag;
                chk.Location = new Point(4, yPos);
                chk.Width = tagDropdownPanel.Width - 20;
                chk.Font = new Font("Microsoft YaHei", 9F);
                chk.Checked = Array.IndexOf(selectedTags, tag) >= 0;
                chk.CheckedChanged += (s, e) => UpdateTagsFromCheckboxes();
                tagDropdownPanel.Controls.Add(chk);
                yPos += 28;
            }

            this.Controls.Add(tagDropdownPanel);
            tagDropdownPanel.BringToFront();
            isDropdownVisible = true;

            // 点击其他地方关闭下拉
            txtTags.LostFocus += (s, e) =>
            {
                if (!tagDropdownPanel.Focused && !tagDropdownPanel.ContainsFocus)
                {
                    System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                    timer.Interval = 200;
                    timer.Tick += (ss, ee) =>
                    {
                        timer.Stop();
                        HideTagDropdown();
                    };
                    timer.Start();
                }
            };
        }

        private void HideTagDropdown()
        {
            if (tagDropdownPanel != null && isDropdownVisible)
            {
                this.Controls.Remove(tagDropdownPanel);
                tagDropdownPanel.Dispose();
                tagDropdownPanel = null;
                isDropdownVisible = false;
            }
        }

        private void UpdateTagsFromCheckboxes()
        {
            if (tagDropdownPanel == null) return;

            List<string> selected = new List<string>();
            foreach (Control ctrl in tagDropdownPanel.Controls)
            {
                CheckBox chk = ctrl as CheckBox;
                if (chk != null && chk.Checked)
                {
                    selected.Add(chk.Text);
                }
            }
            txtTags.Text = string.Join(", ", selected.ToArray());
        }

        private string[] SplitTags(string tags)
        {
            if (string.IsNullOrEmpty(tags)) return new string[0];
            string normalized = tags.Replace('，', ',').Replace('；', ',').Replace(';', ',').Replace(' ', ',');
            string[] parts = normalized.Split(',');
            List<string> result = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                string tag = parts[i].Trim();
                if (!string.IsNullOrEmpty(tag)) result.Add(tag);
            }
            return result.ToArray();
        }
    }

    /// <summary>
    /// 新增/编辑知识条目的对话框
    /// </summary>
    public class KnowledgeEditDialog : Form
    {
        public string KnowledgeContent { get; private set; }
        public string KnowledgeTags { get; private set; }

        private TextBox txtContent;
        private TextBox txtTags;
        private Panel tagDropdownPanel;
        private bool isDropdownVisible = false;

        public KnowledgeEditDialog(List<string> existingTags, KnowledgeItem existing = null)
        {
            bool isEdit = existing != null;
            this.Text = isEdit ? "编辑知识" : "新增知识";
            this.Size = new Size(480, 360);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            int y = 18;
            int labelX = 18;
            int inputX = 72;
            int inputW = 376;

            // 标签
            Label lblTags = new Label();
            lblTags.Text = "标签:";
            lblTags.Location = new Point(labelX, y + 3);
            lblTags.AutoSize = true;
            lblTags.Font = new Font("Microsoft YaHei", 9F);

            txtTags = new TextBox();
            txtTags.Location = new Point(inputX, y);
            txtTags.Width = inputW;
            txtTags.Font = new Font("Microsoft YaHei", 10F);
            if (isEdit) txtTags.Text = existing.Tags;
            txtTags.Click += (s, e) => ShowTagDropdown(existingTags);
            y += 30;

            Label lblTagTip = new Label();
            lblTagTip.Text = "标签用逗号/空格/分号分隔，或点击输入框选择";
            lblTagTip.Location = new Point(inputX, y);
            lblTagTip.AutoSize = true;
            lblTagTip.Font = new Font("Microsoft YaHei", 8F);
            lblTagTip.ForeColor = Color.Gray;
            y += 24;

            // 内容
            Label lblContent = new Label();
            lblContent.Text = "内容:";
            lblContent.Location = new Point(labelX, y + 3);
            lblContent.AutoSize = true;
            lblContent.Font = new Font("Microsoft YaHei", 9F);

            txtContent = new TextBox();
            txtContent.Location = new Point(inputX, y);
            txtContent.Width = inputW;
            txtContent.Height = 180;
            txtContent.Multiline = true;
            txtContent.ScrollBars = ScrollBars.Vertical;
            txtContent.Font = new Font("Microsoft YaHei", 10F);
            if (isEdit) txtContent.Text = existing.Content;
            y += 190;

            // 按钮
            Button btnOK = new Button();
            btnOK.Text = "保存";
            btnOK.Location = new Point(260, y);
            btnOK.Size = new Size(84, 32);
            btnOK.FlatStyle = FlatStyle.Flat;
            btnOK.BackColor = Color.FromArgb(0, 120, 215);
            btnOK.ForeColor = Color.White;
            btnOK.Cursor = Cursors.Hand;
            btnOK.Font = new Font("Microsoft YaHei", 9F);
            btnOK.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtContent.Text))
                {
                    MessageBox.Show("内容不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                KnowledgeContent = txtContent.Text;
                KnowledgeTags = txtTags.Text.Trim();
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            Button btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.Location = new Point(354, y);
            btnCancel.Size = new Size(84, 32);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.BackColor = Color.FromArgb(240, 240, 240);
            btnCancel.ForeColor = Color.DarkGray;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.Font = new Font("Microsoft YaHei", 9F);
            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.AddRange(new Control[] {
                lblTags, txtTags, lblTagTip,
                lblContent, txtContent,
                btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void ShowTagDropdown(List<string> existingTags)
        {
            if (existingTags.Count == 0) return;
            if (isDropdownVisible)
            {
                HideTagDropdown();
                return;
            }

            // 创建下拉面板
            tagDropdownPanel = new Panel();
            tagDropdownPanel.BorderStyle = BorderStyle.FixedSingle;
            tagDropdownPanel.BackColor = Color.White;
            tagDropdownPanel.Location = new Point(txtTags.Left, txtTags.Bottom);
            tagDropdownPanel.Width = txtTags.Width;
            tagDropdownPanel.Height = Math.Min(existingTags.Count * 28 + 4, 200);
            tagDropdownPanel.AutoScroll = true;

            // 解析当前已选标签
            string[] selectedTags = SplitTags(txtTags.Text);

            int yPos = 2;
            foreach (string tag in existingTags)
            {
                CheckBox chk = new CheckBox();
                chk.Text = tag;
                chk.Location = new Point(4, yPos);
                chk.Width = tagDropdownPanel.Width - 20;
                chk.Font = new Font("Microsoft YaHei", 9F);
                chk.Checked = Array.IndexOf(selectedTags, tag) >= 0;
                chk.CheckedChanged += (s, e) => UpdateTagsFromCheckboxes();
                tagDropdownPanel.Controls.Add(chk);
                yPos += 28;
            }

            this.Controls.Add(tagDropdownPanel);
            tagDropdownPanel.BringToFront();
            isDropdownVisible = true;

            // 点击其他地方关闭下拉
            txtTags.LostFocus += (s, e) =>
            {
                if (!tagDropdownPanel.Focused && !tagDropdownPanel.ContainsFocus)
                {
                    System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                    timer.Interval = 200;
                    timer.Tick += (ss, ee) =>
                    {
                        timer.Stop();
                        HideTagDropdown();
                    };
                    timer.Start();
                }
            };
        }

        private void HideTagDropdown()
        {
            if (tagDropdownPanel != null && isDropdownVisible)
            {
                this.Controls.Remove(tagDropdownPanel);
                tagDropdownPanel.Dispose();
                tagDropdownPanel = null;
                isDropdownVisible = false;
            }
        }

        private void UpdateTagsFromCheckboxes()
        {
            if (tagDropdownPanel == null) return;

            List<string> selected = new List<string>();
            foreach (Control ctrl in tagDropdownPanel.Controls)
            {
                CheckBox chk = ctrl as CheckBox;
                if (chk != null && chk.Checked)
                {
                    selected.Add(chk.Text);
                }
            }
            txtTags.Text = string.Join(", ", selected.ToArray());
        }

        private string[] SplitTags(string tags)
        {
            if (string.IsNullOrEmpty(tags)) return new string[0];
            string normalized = tags.Replace('，', ',').Replace('；', ',').Replace(';', ',').Replace(' ', ',');
            string[] parts = normalized.Split(',');
            List<string> result = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                string tag = parts[i].Trim();
                if (!string.IsNullOrEmpty(tag)) result.Add(tag);
            }
            return result.ToArray();
        }
    }
}
