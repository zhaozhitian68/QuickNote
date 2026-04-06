using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace QuickNote
{
    public class CCConfigPanel : Panel
    {
        private CCProfileStore store;
        private ComboBox cmbActive;
        private Button btnSwitch;
        private Panel listPanel;
        private Button btnAdd;

        // 详情表单
        private TextBox txtName;
        private TextBox txtApiKey;
        private TextBox txtAuthToken;
        private TextBox txtBaseUrl;
        private ComboBox cmbModel;
        private TextBox txtQuotaUrl;
        private CheckBox chkCoAuthoredBy;
        private CheckBox chkSkipDangerous;
        private ComboBox cmbEffort;
        private Button btnSave;
        private Label lblStatus;

        private string selectedProfileName; // 列表中当前选中的配置名
        private Timer quotaTimer;
        private Dictionary<string, string> quotaCache = new Dictionary<string, string>(); // name -> "50%"

        public CCConfigPanel()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
            this.AutoScroll = true;
            store = new CCProfileStore();
            InitUI();
            RefreshList();

            // 每分钟刷新一次额度
            quotaTimer = new Timer();
            quotaTimer.Interval = 60000;
            quotaTimer.Tick += delegate { FetchAllQuotas(); };
            quotaTimer.Start();
            FetchAllQuotas(); // 启动时立即查一次
        }

        private void InitUI()
        {
            int y = 12;
            int leftMargin = 16;
            int fieldWidth = 320;
            Font labelFont = new Font("Microsoft YaHei", 9F);
            Font inputFont = new Font("Microsoft YaHei", 10F);

            // === 当前配置 + 切换 ===
            Label lblActive = new Label();
            lblActive.Text = "当前配置:";
            lblActive.Font = labelFont;
            lblActive.Location = new Point(leftMargin, y + 4);
            lblActive.AutoSize = true;
            this.Controls.Add(lblActive);

            cmbActive = new ComboBox();
            cmbActive.Font = inputFont;
            cmbActive.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbActive.Location = new Point(leftMargin + 75, y);
            cmbActive.Width = 180;
            this.Controls.Add(cmbActive);

            btnSwitch = new Button();
            btnSwitch.Text = "切换";
            btnSwitch.Font = new Font("Microsoft YaHei", 9F);
            btnSwitch.FlatStyle = FlatStyle.Flat;
            btnSwitch.BackColor = Color.FromArgb(66, 133, 244);
            btnSwitch.ForeColor = Color.White;
            btnSwitch.FlatAppearance.BorderSize = 0;
            btnSwitch.Cursor = Cursors.Hand;
            btnSwitch.Location = new Point(leftMargin + 262, y);
            btnSwitch.Size = new Size(60, 28);
            btnSwitch.Click += BtnSwitch_Click;
            this.Controls.Add(btnSwitch);

            y += 40;

            // === 配置列表标题 ===
            Label lblList = new Label();
            lblList.Text = "── 配置列表 ──";
            lblList.Font = labelFont;
            lblList.ForeColor = Color.Gray;
            lblList.Location = new Point(leftMargin, y);
            lblList.AutoSize = true;
            this.Controls.Add(lblList);
            y += 24;

            // === 配置列表容器 ===
            listPanel = new Panel();
            listPanel.Location = new Point(leftMargin, y);
            listPanel.Width = fieldWidth;
            listPanel.Height = 10; // 动态调整
            listPanel.BackColor = Color.FromArgb(248, 248, 248);
            this.Controls.Add(listPanel);

            // === 新增配置按钮（位置在 RefreshList 中动态设置） ===
            btnAdd = new Button();
            btnAdd.Text = "+ 新增配置";
            btnAdd.Font = new Font("Microsoft YaHei", 9F);
            btnAdd.FlatStyle = FlatStyle.Flat;
            btnAdd.BackColor = Color.White;
            btnAdd.ForeColor = Color.FromArgb(66, 133, 244);
            btnAdd.FlatAppearance.BorderColor = Color.FromArgb(66, 133, 244);
            btnAdd.FlatAppearance.BorderSize = 1;
            btnAdd.Cursor = Cursors.Hand;
            btnAdd.Size = new Size(100, 28);
            btnAdd.Click += BtnAdd_Click;
            this.Controls.Add(btnAdd);

            // === 配置详情标题（位置动态设置） ===
            // 以下控件的 y 坐标在 RefreshList 中根据列表高度动态调整

            // 占位：先创建所有详情控件，RefreshList 时重新定位
            Label lblDetail = new Label();
            lblDetail.Text = "── 配置详情 ──";
            lblDetail.Font = labelFont;
            lblDetail.ForeColor = Color.Gray;
            lblDetail.AutoSize = true;
            lblDetail.Tag = "detailTitle";
            this.Controls.Add(lblDetail);

            // 配置名称
            Label lblName = new Label();
            lblName.Text = "配置名称:";
            lblName.Font = labelFont;
            lblName.AutoSize = true;
            lblName.Tag = "lblName";
            this.Controls.Add(lblName);

            txtName = new TextBox();
            txtName.Font = inputFont;
            txtName.Width = fieldWidth;
            this.Controls.Add(txtName);

            // API Key
            Label lblApiKey = new Label();
            lblApiKey.Text = "API Key (选填):";
            lblApiKey.Font = labelFont;
            lblApiKey.AutoSize = true;
            lblApiKey.Tag = "lblApiKey";
            this.Controls.Add(lblApiKey);

            txtApiKey = new TextBox();
            txtApiKey.Font = inputFont;
            txtApiKey.Width = fieldWidth;
            txtApiKey.UseSystemPasswordChar = true;
            this.Controls.Add(txtApiKey);

            // Auth Token
            Label lblAuthToken = new Label();
            lblAuthToken.Text = "Auth Token (选填):";
            lblAuthToken.Font = labelFont;
            lblAuthToken.AutoSize = true;
            lblAuthToken.Tag = "lblAuthToken";
            this.Controls.Add(lblAuthToken);

            txtAuthToken = new TextBox();
            txtAuthToken.Font = inputFont;
            txtAuthToken.Width = fieldWidth;
            txtAuthToken.UseSystemPasswordChar = true;
            this.Controls.Add(txtAuthToken);

            // Base URL
            Label lblBaseUrl = new Label();
            lblBaseUrl.Text = "Base URL (选填):";
            lblBaseUrl.Font = labelFont;
            lblBaseUrl.AutoSize = true;
            lblBaseUrl.Tag = "lblBaseUrl";
            this.Controls.Add(lblBaseUrl);

            txtBaseUrl = new TextBox();
            txtBaseUrl.Font = inputFont;
            txtBaseUrl.Width = fieldWidth;
            this.Controls.Add(txtBaseUrl);

            // Model
            Label lblModel = new Label();
            lblModel.Text = "Model (选填):";
            lblModel.Font = labelFont;
            lblModel.AutoSize = true;
            lblModel.Tag = "lblModel";
            this.Controls.Add(lblModel);

            cmbModel = new ComboBox();
            cmbModel.Font = inputFont;
            cmbModel.DropDownStyle = ComboBoxStyle.DropDown;
            cmbModel.Width = fieldWidth;
            cmbModel.Items.AddRange(new object[] { "claude-opus-4-6", "claude-sonnet-4-6", "claude-haiku-4-5-20251001" });
            cmbModel.DropDown += CmbModel_DropDown;
            this.Controls.Add(cmbModel);

            // 额度查询 URL
            Label lblQuotaUrl = new Label();
            lblQuotaUrl.Text = "额度查询 URL (选填):";
            lblQuotaUrl.Font = labelFont;
            lblQuotaUrl.AutoSize = true;
            lblQuotaUrl.Tag = "lblQuotaUrl";
            this.Controls.Add(lblQuotaUrl);

            txtQuotaUrl = new TextBox();
            txtQuotaUrl.Font = inputFont;
            txtQuotaUrl.Width = fieldWidth;
            this.Controls.Add(txtQuotaUrl);

            // 高级选项标题
            Label lblAdvanced = new Label();
            lblAdvanced.Text = "── 高级选项 ──";
            lblAdvanced.Font = labelFont;
            lblAdvanced.ForeColor = Color.Gray;
            lblAdvanced.AutoSize = true;
            lblAdvanced.Tag = "lblAdvanced";
            this.Controls.Add(lblAdvanced);

            chkCoAuthoredBy = new CheckBox();
            chkCoAuthoredBy.Text = "includeCoAuthoredBy  — git commit 中自动添加 Co-authored-by: Claude 署名";
            chkCoAuthoredBy.Font = labelFont;
            chkCoAuthoredBy.AutoSize = true;
            this.Controls.Add(chkCoAuthoredBy);

            chkSkipDangerous = new CheckBox();
            chkSkipDangerous.Text = "skipDangerousModePermissionPrompt  — 跳过危险操作的确认弹窗";
            chkSkipDangerous.Font = labelFont;
            chkSkipDangerous.AutoSize = true;
            chkSkipDangerous.Checked = true;
            this.Controls.Add(chkSkipDangerous);

            // Effort Level
            Label lblEffort = new Label();
            lblEffort.Text = "Effort Level  — 思考深度 (low 快速 / medium 均衡 / high 深度):";
            lblEffort.Font = labelFont;
            lblEffort.AutoSize = true;
            lblEffort.Tag = "lblEffort";
            this.Controls.Add(lblEffort);

            cmbEffort = new ComboBox();
            cmbEffort.Font = inputFont;
            cmbEffort.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbEffort.Width = fieldWidth;
            cmbEffort.Items.AddRange(new object[] { "low", "medium", "high" });
            cmbEffort.SelectedIndex = 1;
            this.Controls.Add(cmbEffort);

            // 保存按钮
            btnSave = new Button();
            btnSave.Text = "保存配置";
            btnSave.Font = new Font("Microsoft YaHei", 10F);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.BackColor = Color.FromArgb(52, 168, 83);
            btnSave.ForeColor = Color.White;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Cursor = Cursors.Hand;
            btnSave.Size = new Size(100, 34);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            // 状态提示
            lblStatus = new Label();
            lblStatus.Font = labelFont;
            lblStatus.ForeColor = Color.Green;
            lblStatus.AutoSize = true;
            lblStatus.Text = "";
            this.Controls.Add(lblStatus);
        }

        private void LayoutDetailControls(int startY)
        {
            int y = startY;
            int leftMargin = 16;

            foreach (Control c in this.Controls)
            {
                string tag = c.Tag as string;
                if (tag == "detailTitle") { c.Location = new Point(leftMargin, y); y += 24; }
                else if (tag == "lblName") { c.Location = new Point(leftMargin, y); y += 20; }
                else if (c == txtName) { c.Location = new Point(leftMargin, y); y += 32; }
                else if (tag == "lblApiKey") { c.Location = new Point(leftMargin, y); y += 20; }
                else if (c == txtApiKey) { c.Location = new Point(leftMargin, y); y += 32; }
                else if (tag == "lblAuthToken") { c.Location = new Point(leftMargin, y); y += 20; }
                else if (c == txtAuthToken) { c.Location = new Point(leftMargin, y); y += 32; }
                else if (tag == "lblBaseUrl") { c.Location = new Point(leftMargin, y); y += 20; }
                else if (c == txtBaseUrl) { c.Location = new Point(leftMargin, y); y += 32; }
                else if (tag == "lblModel") { c.Location = new Point(leftMargin, y); y += 20; }
                else if (c == cmbModel) { c.Location = new Point(leftMargin, y); y += 34; }
                else if (tag == "lblQuotaUrl") { c.Location = new Point(leftMargin, y); y += 20; }
                else if (c == txtQuotaUrl) { c.Location = new Point(leftMargin, y); y += 32; }
                else if (tag == "lblAdvanced") { c.Location = new Point(leftMargin, y); y += 24; }
                else if (c == chkCoAuthoredBy) { c.Location = new Point(leftMargin, y); y += 26; }
                else if (c == chkSkipDangerous) { c.Location = new Point(leftMargin, y); y += 26; }
                else if (tag == "lblEffort") { c.Location = new Point(leftMargin, y); y += 20; }
                else if (c == cmbEffort) { c.Location = new Point(leftMargin, y); y += 34; }
                else if (c == btnSave) { c.Location = new Point(leftMargin, y + 4); }
                else if (c == lblStatus) { c.Location = new Point(leftMargin + 110, y + 10); }
            }
        }

        private void RefreshList()
        {
            listPanel.Controls.Clear();
            List<CCProfile> all = store.GetAll();
            string activeName = store.GetActiveProfileName();

            // 刷新顶部下拉框
            cmbActive.Items.Clear();
            for (int i = 0; i < all.Count; i++)
            {
                cmbActive.Items.Add(all[i].Name);
                if (all[i].Name == activeName)
                    cmbActive.SelectedIndex = i;
            }

            int rowY = 4;
            for (int i = 0; i < all.Count; i++)
            {
                CCProfile p = all[i];

                RadioButton rb = new RadioButton();
                // 显示名称 + 额度百分比
                string quotaText = "";
                if (quotaCache.ContainsKey(p.Name))
                    quotaText = " (" + quotaCache[p.Name] + ")";
                rb.Text = p.Name + quotaText;
                rb.Font = new Font("Microsoft YaHei", 9F);
                rb.Location = new Point(8, rowY);
                rb.AutoSize = true;
                rb.Tag = p.Name;
                rb.Checked = (p.Name == selectedProfileName) || (selectedProfileName == null && i == 0);
                rb.CheckedChanged += RadioButton_CheckedChanged;
                listPanel.Controls.Add(rb);

                // 当前激活标记
                if (p.Name == activeName)
                {
                    Label lblMark = new Label();
                    lblMark.Text = "[当前]";
                    lblMark.Font = new Font("Microsoft YaHei", 8F);
                    lblMark.ForeColor = Color.FromArgb(52, 168, 83);
                    lblMark.AutoSize = true;
                    lblMark.Location = new Point(200, rowY + 2);
                    listPanel.Controls.Add(lblMark);
                }

                Button btnDel = new Button();
                btnDel.Text = "删";
                btnDel.Font = new Font("Microsoft YaHei", 8F);
                btnDel.FlatStyle = FlatStyle.Flat;
                btnDel.BackColor = Color.White;
                btnDel.ForeColor = Color.FromArgb(234, 67, 53);
                btnDel.FlatAppearance.BorderColor = Color.FromArgb(234, 67, 53);
                btnDel.FlatAppearance.BorderSize = 1;
                btnDel.Cursor = Cursors.Hand;
                btnDel.Size = new Size(36, 24);
                btnDel.Location = new Point(280, rowY);
                btnDel.Tag = p.Name;
                btnDel.Click += BtnDel_Click;
                listPanel.Controls.Add(btnDel);

                rowY += 30;
            }

            int listHeight = Math.Max(rowY + 4, 10);
            listPanel.Height = listHeight;

            // 新增按钮位置
            btnAdd.Location = new Point(16, listPanel.Bottom + 6);

            // 重新布局详情区域
            LayoutDetailControls(btnAdd.Bottom + 12);

            // 选中第一个或当前选中的配置，回填表单
            if (all.Count > 0)
            {
                if (selectedProfileName == null)
                    selectedProfileName = all[0].Name;
                FillForm(store.FindByName(selectedProfileName));
            }
        }

        private void FillForm(CCProfile p)
        {
            if (p == null) return;
            txtName.Text = p.Name;
            txtApiKey.Text = p.ApiKey;
            txtAuthToken.Text = p.AuthToken;
            txtBaseUrl.Text = p.BaseUrl;
            cmbModel.Text = p.Model;
            txtQuotaUrl.Text = p.QuotaQueryUrl;
            chkCoAuthoredBy.Checked = p.IncludeCoAuthoredBy;
            chkSkipDangerous.Checked = p.SkipDangerousMode;

            int effortIdx = cmbEffort.Items.IndexOf(p.EffortLevel);
            cmbEffort.SelectedIndex = effortIdx >= 0 ? effortIdx : 1;

            // 切换配置时后台刷新模型列表
            FetchModels(p.BaseUrl.TrimEnd('/'), p.ApiKey, p.AuthToken, p.Model);
        }

        private void FetchModels(string baseUrl, string apiKey, string authToken, string currentModel)
        {
            if (string.IsNullOrEmpty(baseUrl)) return;

            System.ComponentModel.BackgroundWorker bw = new System.ComponentModel.BackgroundWorker();
            bw.DoWork += delegate(object s, System.ComponentModel.DoWorkEventArgs args)
            {
                try
                {
                    System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)(48 | 192 | 768 | 3072);
                    System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(baseUrl + "/v1/models");
                    req.Method = "GET";
                    req.Timeout = 8000;
                    if (!string.IsNullOrEmpty(authToken))
                        req.Headers.Add("Authorization", "Bearer " + authToken);
                    else if (!string.IsNullOrEmpty(apiKey))
                        req.Headers.Add("Authorization", "Bearer " + apiKey);

                    List<string> models = new List<string>();
                    using (System.Net.HttpWebResponse resp = (System.Net.HttpWebResponse)req.GetResponse())
                    using (System.IO.StreamReader reader = new System.IO.StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    {
                        string json = reader.ReadToEnd();
                        int pos = 0;
                        while (pos < json.Length)
                        {
                            int idx = json.IndexOf("\"id\"", pos);
                            if (idx < 0) break;
                            int colon = json.IndexOf(':', idx + 4);
                            if (colon < 0) break;
                            int q1 = json.IndexOf('"', colon + 1);
                            if (q1 < 0) break;
                            int q2 = json.IndexOf('"', q1 + 1);
                            if (q2 < 0) break;
                            string id = json.Substring(q1 + 1, q2 - q1 - 1);
                            if (id.StartsWith("claude-"))
                                models.Add(id);
                            pos = q2 + 1;
                        }
                    }
                    args.Result = models;
                }
                catch
                {
                    args.Result = new List<string>(); // 失败时返回空列表
                }
            };

            bw.RunWorkerCompleted += delegate(object s, System.ComponentModel.RunWorkerCompletedEventArgs args)
            {
                List<string> models = args.Result as List<string>;
                cmbModel.Items.Clear();
                if (models != null && models.Count > 0)
                {
                    foreach (string m in models)
                        cmbModel.Items.Add(m);
                    cmbModel.Text = currentModel;
                }
                else
                {
                    cmbModel.SelectedIndex = -1;
                    cmbModel.Text = "";
                }
            };

            bw.RunWorkerAsync();
        }

        private CCProfile ReadForm()
        {
            CCProfile p = new CCProfile();
            p.Name = txtName.Text.Trim();
            p.ApiKey = txtApiKey.Text.Trim();
            p.AuthToken = txtAuthToken.Text.Trim();
            p.BaseUrl = txtBaseUrl.Text.Trim();
            p.Model = cmbModel.Text.Trim();
            p.QuotaQueryUrl = txtQuotaUrl.Text.Trim();
            p.IncludeCoAuthoredBy = chkCoAuthoredBy.Checked;
            p.SkipDangerousMode = chkSkipDangerous.Checked;
            p.EffortLevel = cmbEffort.SelectedItem != null ? cmbEffort.SelectedItem.ToString() : "medium";
            return p;
        }

        private void ShowStatus(string text, Color color)
        {
            lblStatus.Text = text;
            lblStatus.ForeColor = color;
            Timer t = new Timer();
            t.Interval = 3000;
            t.Tick += delegate { lblStatus.Text = ""; t.Stop(); t.Dispose(); };
            t.Start();
        }

        // ========== 额度查询 ==========

        private void FetchAllQuotas()
        {
            List<CCProfile> all = store.GetAll();
            foreach (CCProfile p in all)
            {
                if (string.IsNullOrEmpty(p.QuotaQueryUrl)) continue;
                FetchQuota(p.Name, p.QuotaQueryUrl);
            }
        }

        private void FetchQuota(string profileName, string queryUrl)
        {
            System.ComponentModel.BackgroundWorker bw = new System.ComponentModel.BackgroundWorker();
            bw.DoWork += delegate(object s, System.ComponentModel.DoWorkEventArgs args)
            {
                try
                {
                    System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)(48 | 192 | 768 | 3072);
                    System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(queryUrl);
                    req.Method = "GET";
                    req.Timeout = 8000;
                    req.UserAgent = "Mozilla/5.0";
                    req.Accept = "application/json";
                    using (System.Net.HttpWebResponse resp = (System.Net.HttpWebResponse)req.GetResponse())
                    using (System.IO.StreamReader reader = new System.IO.StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    {
                        string json = reader.ReadToEnd();

                        // 先定位到 token_info 对象，避免被 logs 里的字段干扰
                        string tokenInfoJson = json;
                        int tiIdx = json.IndexOf("\"token_info\"");
                        if (tiIdx >= 0)
                        {
                            int braceOpen = json.IndexOf('{', tiIdx);
                            if (braceOpen >= 0)
                            {
                                int depth = 1, pos = braceOpen + 1;
                                while (pos < json.Length && depth > 0)
                                {
                                    if (json[pos] == '{') depth++;
                                    else if (json[pos] == '}') depth--;
                                    pos++;
                                }
                                tokenInfoJson = json.Substring(braceOpen, pos - braceOpen);
                            }
                        }

                        long remain = GetLongFromJson(tokenInfoJson, "remain_quota_display");
                        long total = GetLongFromJson(tokenInfoJson, "today_added_quota");
                        if (total > 0)
                            args.Result = new object[] { profileName, "当天剩余" + (int)(remain * 100 / total) + "%" };
                        else
                            args.Result = new object[] { profileName, "" };
                    }
                }
                catch
                {
                    args.Result = new object[] { profileName, "" };
                }
            };

            bw.RunWorkerCompleted += delegate(object s, System.ComponentModel.RunWorkerCompletedEventArgs args)
            {
                object[] result = (object[])args.Result;
                string name = (string)result[0];
                string pctText = (string)result[1];
                if (!string.IsNullOrEmpty(pctText))
                    quotaCache[name] = pctText;
                else
                    quotaCache.Remove(name);
                RefreshList();
            };

            bw.RunWorkerAsync();
        }

        private long GetLongFromJson(string json, string key)
        {
            string marker = "\"" + key + "\":";
            int idx = json.IndexOf(marker);
            if (idx < 0) return 0;
            int start = idx + marker.Length;
            while (start < json.Length && json[start] == ' ') start++;
            int end = start;
            while (end < json.Length && json[end] >= '0' && json[end] <= '9') end++;
            if (end == start) return 0;
            long val;
            long.TryParse(json.Substring(start, end - start), out val);
            return val;
        }

        // ========== 事件处理 ==========

        private void CmbModel_DropDown(object sender, EventArgs e)
        {
            string currentText = cmbModel.Text;
            string baseUrl = txtBaseUrl.Text.Trim().TrimEnd('/');
            string apiKey = txtApiKey.Text.Trim();
            string authToken = txtAuthToken.Text.Trim();
            FetchModels(baseUrl, apiKey, authToken, currentText);
        }

        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            if (rb.Checked)
            {
                selectedProfileName = (string)rb.Tag;
                CCProfile p = store.FindByName(selectedProfileName);
                if (p != null) FillForm(p);
            }
        }

        private void BtnSwitch_Click(object sender, EventArgs e)
        {
            if (cmbActive.SelectedItem == null)
            {
                ShowStatus("请先选择配置", Color.Red);
                return;
            }
            string name = cmbActive.SelectedItem.ToString();
            CCProfile p = store.FindByName(name);
            if (p == null) return;

            try
            {
                store.ApplyProfile(p);
                ShowStatus("已切换到「" + name + "」", Color.Green);
                RefreshList();
            }
            catch (Exception ex)
            {
                ShowStatus("切换失败: " + ex.Message, Color.Red);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            // 生成默认名称
            int idx = store.GetAll().Count + 1;
            string defaultName = "配置" + idx;
            while (store.FindByName(defaultName) != null)
            {
                idx++;
                defaultName = "配置" + idx;
            }

            CCProfile p = new CCProfile();
            p.Name = defaultName;
            store.Add(p);
            selectedProfileName = defaultName;
            RefreshList();
        }

        private void BtnDel_Click(object sender, EventArgs e)
        {
            string name = (string)((Button)sender).Tag;
            DialogResult result = MessageBox.Show(
                "确定删除配置「" + name + "」吗？",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            store.Delete(name);
            if (selectedProfileName == name)
                selectedProfileName = null;
            RefreshList();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            CCProfile p = ReadForm();
            if (string.IsNullOrEmpty(p.Name))
            {
                ShowStatus("配置名称不能为空", Color.Red);
                return;
            }
            // 判断是新增还是更新
            if (selectedProfileName != null && store.FindByName(selectedProfileName) != null)
            {
                // 检查改名后是否与其他配置重名
                if (p.Name != selectedProfileName && store.FindByName(p.Name) != null)
                {
                    ShowStatus("配置名称已存在", Color.Red);
                    return;
                }
                store.Update(selectedProfileName, p);
                selectedProfileName = p.Name;
            }
            else
            {
                if (store.FindByName(p.Name) != null)
                {
                    ShowStatus("配置名称已存在", Color.Red);
                    return;
                }
                store.Add(p);
                selectedProfileName = p.Name;
            }

            ShowStatus("已保存", Color.Green);
            RefreshList();
        }
    }
}
