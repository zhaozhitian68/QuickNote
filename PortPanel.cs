using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace QuickNote
{
    public class PortPanel : Panel
    {
        private TextBox txtPort;
        private Button btnQuery;
        private Button btnKill;
        private Label lblResult;
        private Panel resultPanel;

        private int foundPid = -1;

        public PortPanel()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
            this.Padding = new Padding(16);
            InitUI();
        }

        private void InitUI()
        {
            // --- 输入区域 ---
            var inputPanel = new Panel();
            inputPanel.Dock = DockStyle.Top;
            inputPanel.Height = 44;

            txtPort = new TextBox();
            txtPort.Font = new Font("Microsoft YaHei", 11F);
            txtPort.Text = "输入端口号，如 8080";
            txtPort.ForeColor = Color.Gray;
            txtPort.GotFocus += (s, e) => { if (txtPort.ForeColor == Color.Gray) { txtPort.Text = ""; txtPort.ForeColor = Color.Black; } };
            txtPort.LostFocus += (s, e) => { if (string.IsNullOrEmpty(txtPort.Text)) { txtPort.Text = "输入端口号，如 8080"; txtPort.ForeColor = Color.Gray; } };
            txtPort.Location = new Point(0, 6);
            txtPort.Width = 220;
            txtPort.Height = 32;
            txtPort.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; QueryPort(); } };
            inputPanel.Controls.Add(txtPort);

            btnQuery = new Button();
            btnQuery.Text = "查询";
            btnQuery.Font = new Font("Microsoft YaHei", 10F);
            btnQuery.FlatStyle = FlatStyle.Flat;
            btnQuery.BackColor = Color.FromArgb(66, 133, 244);
            btnQuery.ForeColor = Color.White;
            btnQuery.FlatAppearance.BorderSize = 0;
            btnQuery.Cursor = Cursors.Hand;
            btnQuery.Location = new Point(228, 6);
            btnQuery.Size = new Size(70, 32);
            btnQuery.Click += (s, e) => QueryPort();
            inputPanel.Controls.Add(btnQuery);

            this.Controls.Add(inputPanel);

            // --- 结果区域 ---
            resultPanel = new Panel();
            resultPanel.Dock = DockStyle.Fill;
            resultPanel.Padding = new Padding(0, 12, 0, 0);

            lblResult = new Label();
            lblResult.Font = new Font("Microsoft YaHei", 10F);
            lblResult.ForeColor = Color.FromArgb(51, 51, 51);
            lblResult.Dock = DockStyle.Top;
            lblResult.AutoSize = false;
            lblResult.Height = 120;
            lblResult.Text = "";
            resultPanel.Controls.Add(lblResult);

            var killPanel = new Panel();
            killPanel.Dock = DockStyle.Top;
            killPanel.Height = 44;

            btnKill = new Button();
            btnKill.Text = "强制终止该进程";
            btnKill.Font = new Font("Microsoft YaHei", 10F);
            btnKill.FlatStyle = FlatStyle.Flat;
            btnKill.BackColor = Color.FromArgb(234, 67, 53);
            btnKill.ForeColor = Color.White;
            btnKill.FlatAppearance.BorderSize = 0;
            btnKill.Cursor = Cursors.Hand;
            btnKill.Size = new Size(160, 36);
            btnKill.Location = new Point(0, 4);
            btnKill.Visible = false;
            btnKill.Click += (s, e) => KillProcess();
            killPanel.Controls.Add(btnKill);

            // WinForms Dock 顺序：后添加的 Top 排在上面，所以先加 killPanel 再加 lblResult
            resultPanel.Controls.Add(killPanel);
            resultPanel.Controls.Add(lblResult);

            this.Controls.Add(resultPanel);
        }

        private void QueryPort()
        {
            foundPid = -1;
            btnKill.Visible = false;

            var portText = txtPort.Text.Trim();
            int port = 0;
            if (string.IsNullOrEmpty(portText))
            {
                lblResult.Text = "请输入端口号。";
                return;
            }
            if (!int.TryParse(portText, out port) || port < 1 || port > 65535)
            {
                lblResult.Text = "端口号无效，请输入 1-65535 之间的数字。";
                return;
            }

            try
            {
                var psi = new ProcessStartInfo("netstat", "-ano")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var proc = Process.Start(psi);
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                string matchLine = null;
                foreach (var rawLine in output.Split('\n'))
                {
                    var line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    // 匹配 LISTENING 状态的端口
                    if (line.Contains("LISTENING") && ContainsPort(line, port))
                    {
                        matchLine = line;
                        break;
                    }
                    // 也匹配 ESTABLISHED 等状态
                    if (matchLine == null && ContainsPort(line, port) && (line.StartsWith("TCP") || line.StartsWith("UDP")))
                    {
                        matchLine = line;
                    }
                }

                if (matchLine != null)
                {
                    // 提取 PID（最后一列）
                    var parts = matchLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var pidStr = parts[parts.Length - 1];
                    int pid = 0;
                    if (int.TryParse(pidStr, out pid))
                    {
                        foundPid = pid;
                        string processName = "未知";
                        try
                        {
                            var p = Process.GetProcessById(pid);
                            processName = p.ProcessName;
                        }
                        catch { }

                        lblResult.Text = "端口 " + port + " 被占用\n\n"
                            + "PID:  " + pid + "\n"
                            + "进程: " + processName + "\n"
                            + "详情: " + matchLine;
                        btnKill.Visible = true;
                    }
                    else
                    {
                        lblResult.Text = "端口 " + port + " 似乎被占用，但无法解析 PID。\n" + matchLine;
                    }
                }
                else
                {
                    lblResult.Text = "端口 " + port + " 当前没有被任何进程占用。";
                }
            }
            catch (Exception ex)
            {
                lblResult.Text = "查询失败: " + ex.Message;
            }
        }

        private bool ContainsPort(string line, int port)
        {
            // 匹配 :port 后面跟空格或行尾，避免 80 匹配到 8080
            string suffix = ":" + port;
            int idx = line.IndexOf(suffix);
            while (idx >= 0)
            {
                int afterIdx = idx + suffix.Length;
                if (afterIdx >= line.Length || line[afterIdx] == ' ')
                    return true;
                idx = line.IndexOf(suffix, afterIdx);
            }
            return false;
        }

        private void KillProcess()
        {
            if (foundPid <= 0) return;

            var result = MessageBox.Show(
                "确定要强制终止 PID " + foundPid + " 吗？\n\n未保存的数据可能会丢失。",
                "确认终止",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                var p = Process.GetProcessById(foundPid);
                p.Kill();
                p.WaitForExit(3000);
                lblResult.Text = "进程 " + foundPid + " 已被终止。";
                btnKill.Visible = false;
                foundPid = -1;
            }
            catch (ArgumentException)
            {
                lblResult.Text = "进程 " + foundPid + " 已不存在。";
                btnKill.Visible = false;
                foundPid = -1;
            }
            catch (Exception ex)
            {
                lblResult.Text = "终止失败: " + ex.Message + "\n\n可能需要以管理员权限运行。";
            }
        }
    }
}
