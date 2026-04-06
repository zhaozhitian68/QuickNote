using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace QuickNote
{
    public class ChatPanel : Panel
    {
        private RichTextBox rtbHistory;
        private TextBox txtInput;
        private Button btnSend;
        private Button btnClear;

        private List<string[]> messages = new List<string[]>(); // ["role", "content"]
        private const int MAX_MESSAGES = 20;
        private bool isSending = false;

        public ChatPanel()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
            InitUI();
        }

        private void InitUI()
        {
            Font uiFont = new Font("Microsoft YaHei", 10F);

            // === 底部工具栏（清空按钮）===
            Panel bottomBar = new Panel();
            bottomBar.Dock = DockStyle.Bottom;
            bottomBar.Height = 36;
            bottomBar.BackColor = Color.FromArgb(248, 248, 248);

            btnClear = new Button();
            btnClear.Text = "清空上下文";
            btnClear.Font = new Font("Microsoft YaHei", 9F);
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.BackColor = Color.White;
            btnClear.ForeColor = Color.FromArgb(150, 150, 150);
            btnClear.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnClear.FlatAppearance.BorderSize = 1;
            btnClear.Cursor = Cursors.Hand;
            btnClear.Size = new Size(90, 26);
            btnClear.Location = new Point(8, 5);
            btnClear.Click += BtnClear_Click;
            bottomBar.Controls.Add(btnClear);

            // === 输入区域 ===
            Panel inputPanel = new Panel();
            inputPanel.Dock = DockStyle.Bottom;
            inputPanel.Height = 88;
            inputPanel.BackColor = Color.White;
            inputPanel.Padding = new Padding(8, 6, 8, 6);

            btnSend = new Button();
            btnSend.Text = "发送";
            btnSend.Font = uiFont;
            btnSend.FlatStyle = FlatStyle.Flat;
            btnSend.BackColor = Color.FromArgb(66, 133, 244);
            btnSend.ForeColor = Color.White;
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Cursor = Cursors.Hand;
            btnSend.Size = new Size(60, 76);
            btnSend.Dock = DockStyle.Right;
            btnSend.Click += BtnSend_Click;
            inputPanel.Controls.Add(btnSend);

            txtInput = new TextBox();
            txtInput.Font = uiFont;
            txtInput.Multiline = true;
            txtInput.ScrollBars = ScrollBars.Vertical;
            txtInput.BorderStyle = BorderStyle.FixedSingle;
            txtInput.Dock = DockStyle.Fill;
            txtInput.KeyDown += TxtInput_KeyDown;
            inputPanel.Controls.Add(txtInput);

            // === 对话历史 ===
            rtbHistory = new RichTextBox();
            rtbHistory.Dock = DockStyle.Fill;
            rtbHistory.ReadOnly = true;
            rtbHistory.BackColor = Color.White;
            rtbHistory.BorderStyle = BorderStyle.None;
            rtbHistory.Font = uiFont;
            rtbHistory.ScrollBars = RichTextBoxScrollBars.Vertical;
            rtbHistory.Padding = new Padding(8);

            // 添加顺序：Fill 最先，Bottom 后加（WinForms Dock 顺序）
            this.Controls.Add(rtbHistory);
            this.Controls.Add(inputPanel);
            this.Controls.Add(bottomBar);
        }

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Alt)
            {
                e.SuppressKeyPress = true;
                SendMessage();
            }
            else if (e.KeyCode == Keys.Enter && e.Alt)
            {
                e.SuppressKeyPress = true;
                int pos = txtInput.SelectionStart;
                txtInput.Text = txtInput.Text.Insert(pos, "\r\n");
                txtInput.SelectionStart = pos + 2;
            }
        }

        private void BtnSend_Click(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            messages.Clear();
            rtbHistory.Clear();
        }

        private void SendMessage()
        {
            if (isSending) return;
            string text = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // 读取当前激活的 CC 配置
            CCProfileStore store = new CCProfileStore();
            string activeName = store.GetActiveProfileName();
            CCProfile profile = string.IsNullOrEmpty(activeName) ? null : store.FindByName(activeName);

            string apiKey = profile != null ? profile.ApiKey : "";
            string authToken = profile != null ? profile.AuthToken : "";
            string baseUrl = profile != null && !string.IsNullOrEmpty(profile.BaseUrl)
                ? profile.BaseUrl.TrimEnd('/')
                : "https://api.anthropic.com";
            string model = profile != null && !string.IsNullOrEmpty(profile.Model)
                ? profile.Model
                : "claude-opus-4-6";

            if (string.IsNullOrEmpty(apiKey) && string.IsNullOrEmpty(authToken) && profile == null)
            {
                AppendText("【提示】请先在「CC配置」Tab 中配置并激活一个配置。\n\n", Color.OrangeRed);
                return;
            }

            txtInput.Text = "";
            txtInput.Enabled = false;
            btnSend.Enabled = false;
            isSending = true;

            // 添加用户消息到上下文
            messages.Add(new string[] { "user", text });
            if (messages.Count > MAX_MESSAGES)
                messages.RemoveAt(0);

            // 显示用户消息
            AppendText("你: ", Color.FromArgb(30, 100, 200), true);
            AppendText(text + "\n\n", Color.FromArgb(30, 100, 200));

            // 显示 Claude 前缀
            AppendText("Claude: ", Color.FromArgb(80, 80, 80), true);

            // 后台线程发请求
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;

            string requestBody = BuildRequestBody(model);
            string endpoint = baseUrl + "/v1/messages";
            string key = apiKey;
            string token = authToken;

            worker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                BackgroundWorker bw = (BackgroundWorker)s;
                StringBuilder fullReply = new StringBuilder();
                string errorMsg = null;

                try
                {
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)(48 | 192 | 768 | 3072);
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(endpoint);
                    req.Method = "POST";
                    req.ContentType = "application/json";
                    req.Headers.Add("anthropic-version", "2023-06-01");
                    req.Accept = "text/event-stream";
                    req.Timeout = 60000;

                    if (!string.IsNullOrEmpty(token))
                        req.Headers.Add("Authorization", "Bearer " + token);
                    else
                        req.Headers.Add("x-api-key", key);

                    byte[] bodyBytes = Encoding.UTF8.GetBytes(requestBody);
                    req.ContentLength = bodyBytes.Length;
                    using (Stream reqStream = req.GetRequestStream())
                        reqStream.Write(bodyBytes, 0, bodyBytes.Length);

                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    using (StreamReader reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (!line.StartsWith("data:")) continue;
                            string data = line.Substring(5).Trim();
                            if (data == "[DONE]") break;

                            string chunk = ExtractDeltaText(data);
                            if (!string.IsNullOrEmpty(chunk))
                            {
                                fullReply.Append(chunk);
                                bw.ReportProgress(0, chunk);
                            }

                            // 检查是否结束
                            if (data.Contains("\"message_stop\"")) break;
                        }
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Response != null)
                    {
                        using (StreamReader r = new StreamReader(ex.Response.GetResponseStream()))
                            errorMsg = "请求失败: " + r.ReadToEnd();
                    }
                    else
                    {
                        errorMsg = "请求失败: " + ex.Message;
                    }
                }
                catch (Exception ex)
                {
                    errorMsg = "错误: " + ex.Message;
                }

                args.Result = new object[] { fullReply.ToString(), errorMsg };
            };

            worker.ProgressChanged += delegate(object s, ProgressChangedEventArgs args)
            {
                string chunk = (string)args.UserState;
                AppendText(chunk, Color.FromArgb(50, 50, 50));
                rtbHistory.ScrollToCaret();
            };

            worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            {
                object[] result = (object[])args.Result;
                string fullReply = (string)result[0];
                string errorMsg = (string)result[1];

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    AppendText("\n" + errorMsg + "\n\n", Color.Red);
                    // 回滚：移除刚加的用户消息
                    if (messages.Count > 0 && messages[messages.Count - 1][0] == "user")
                        messages.RemoveAt(messages.Count - 1);
                }
                else
                {
                    AppendText("\n\n", Color.FromArgb(50, 50, 50));
                    // 添加 assistant 回复到上下文
                    if (!string.IsNullOrEmpty(fullReply))
                    {
                        messages.Add(new string[] { "assistant", fullReply });
                        if (messages.Count > MAX_MESSAGES)
                            messages.RemoveAt(0);
                    }
                }

                txtInput.Enabled = true;
                btnSend.Enabled = true;
                isSending = false;
                txtInput.Focus();
                rtbHistory.ScrollToCaret();
            };

            worker.RunWorkerAsync();
        }

        private string BuildRequestBody(string model)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"model\":" + JsonEscape(model) + ",");
            sb.Append("\"max_tokens\":4096,");
            sb.Append("\"stream\":true,");
            sb.Append("\"messages\":[");
            for (int i = 0; i < messages.Count; i++)
            {
                sb.Append("{\"role\":" + JsonEscape(messages[i][0]) + ",\"content\":" + JsonEscape(messages[i][1]) + "}");
                if (i < messages.Count - 1) sb.Append(",");
            }
            sb.Append("]}");
            return sb.ToString();
        }

        /// <summary>从 SSE data 行中提取 delta.text</summary>
        private string ExtractDeltaText(string data)
        {
            // 找 "type":"content_block_delta"
            if (!data.Contains("content_block_delta")) return null;

            // 找 "text":"..."
            string marker = "\"text\":\"";
            int idx = data.IndexOf(marker);
            if (idx < 0) return null;

            int start = idx + marker.Length;
            StringBuilder sb = new StringBuilder();
            int i = start;
            while (i < data.Length)
            {
                char c = data[i];
                if (c == '\\' && i + 1 < data.Length)
                {
                    char next = data[i + 1];
                    if (next == '"') { sb.Append('"'); i += 2; continue; }
                    if (next == '\\') { sb.Append('\\'); i += 2; continue; }
                    if (next == 'n') { sb.Append('\n'); i += 2; continue; }
                    if (next == 'r') { sb.Append('\r'); i += 2; continue; }
                    if (next == 't') { sb.Append('\t'); i += 2; continue; }
                    sb.Append(c); i++;
                }
                else if (c == '"') { break; }
                else { sb.Append(c); i++; }
            }
            return sb.ToString();
        }

        private void AppendText(string text, Color color, bool bold = false)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string, Color, bool>(AppendText), text, color, bold);
                return;
            }
            rtbHistory.SelectionStart = rtbHistory.TextLength;
            rtbHistory.SelectionLength = 0;
            rtbHistory.SelectionColor = color;
            if (bold)
                rtbHistory.SelectionFont = new Font(rtbHistory.Font, FontStyle.Bold);
            else
                rtbHistory.SelectionFont = rtbHistory.Font;
            rtbHistory.AppendText(text);
            rtbHistory.SelectionColor = rtbHistory.ForeColor;
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
