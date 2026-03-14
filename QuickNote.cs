using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;

namespace QuickNote
{
    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private const int SCREENSHOT_HOTKEY_ID = 9001;

        private uint hotkeyModifiers = 0x0002;     // 默认 Ctrl
        private uint hotkeyVK = 0x7B;              // 默认 F12
        private uint screenshotModifiers = 0x0002; // 默认 Ctrl
        private uint screenshotVK = 0x7A;          // 默认 F11
        private string settingsFile = "settings.txt";

        private TabControl tabControl;
        private TabPage tabNotes;
        private TabPage tabKnowledge;
        private KnowledgePanel knowledgePanel;

        private FlowLayoutPanel container;
        private Button btnAddRow;
        private string dataFile = "notes.txt";
        private ToolTip toolTip;
        private NotifyIcon trayIcon;

        // 常量定义
        private const int ICON_WIDTH = 26; // 图标宽度
        private const int ICON_HEIGHT = 24; // 图标高度
        private const int RIGHT_PADDING = 26; // 右侧预留宽度

        public MainForm()
        {
            this.Text = "快捷便签";
            this.Icon = SystemIcons.Application;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = true;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(400, 600);
            this.BackColor = Color.White;
            this.MinimumSize = new Size(250, 200);

            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(screen.Width - 420, 50);

            toolTip = new ToolTip();

            // 右键菜单
            ContextMenuStrip ctxMenu = new ContextMenuStrip();
            ToolStripMenuItem menuSetHotkey = new ToolStripMenuItem("设置唤起热键...");
            menuSetHotkey.Click += (s, e) => OpenHotkeySettings(false);
            ToolStripMenuItem menuSetScreenshot = new ToolStripMenuItem("设置截图热键...");
            menuSetScreenshot.Click += (s, e) => OpenHotkeySettings(true);
            ctxMenu.Items.Add(menuSetHotkey);
            ctxMenu.Items.Add(menuSetScreenshot);
            this.ContextMenuStrip = ctxMenu;

            // 系统托盘图标
            ContextMenuStrip trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add(menuSetHotkey);
            trayMenu.Items.Add(menuSetScreenshot);
            trayMenu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem menuExit = new ToolStripMenuItem("退出");
            menuExit.Click += (s, e) => { Application.Exit(); };
            trayMenu.Items.Add(menuExit);

            trayIcon = new NotifyIcon();
            trayIcon.Icon = SystemIcons.Application;
            trayIcon.Text = "快捷便签";
            trayIcon.Visible = true;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.DoubleClick += (s, e) => { this.Show(); this.Activate(); };

            // --- TabControl ---
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Font = new Font("Microsoft YaHei", 10F);

            // --- 快捷便签 Tab ---
            tabNotes = new TabPage("快捷便签");
            tabNotes.BackColor = Color.White;

            btnAddRow = new Button();
            btnAddRow.Text = "+ 新增一行";
            btnAddRow.Dock = DockStyle.Bottom;
            btnAddRow.Height = 50;
            btnAddRow.FlatStyle = FlatStyle.Flat;
            btnAddRow.FlatAppearance.BorderSize = 0;
            btnAddRow.BackColor = Color.FromArgb(250, 250, 250);
            btnAddRow.ForeColor = Color.DarkGray;
            btnAddRow.Cursor = Cursors.Hand;
            btnAddRow.Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold);
            btnAddRow.Click += new EventHandler(BtnAddRow_Click);
            btnAddRow.ContextMenuStrip = ctxMenu;
            tabNotes.Controls.Add(btnAddRow);

            container = new FlowLayoutPanel();
            container.Dock = DockStyle.Fill;
            container.FlowDirection = FlowDirection.TopDown;
            container.WrapContents = false;
            container.AutoScroll = true;
            container.Padding = new Padding(10);
            container.Resize += new EventHandler(Container_Resize);
            container.ContextMenuStrip = ctxMenu;
            tabNotes.Controls.Add(container);

            tabControl.TabPages.Add(tabNotes);

            // --- 知识库 Tab ---
            tabKnowledge = new TabPage("知识库");
            tabKnowledge.BackColor = Color.White;
            knowledgePanel = new KnowledgePanel();
            tabKnowledge.Controls.Add(knowledgePanel);
            tabControl.TabPages.Add(tabKnowledge);

            this.Controls.Add(tabControl);

            LoadData();
            if (container.Controls.Count == 0) AddBlock();

            LoadHotkeySettings();
            RegisterHotKey(this.Handle, HOTKEY_ID, hotkeyModifiers, hotkeyVK);
            RegisterHotKey(this.Handle, SCREENSHOT_HOTKEY_ID, screenshotModifiers, screenshotVK);
            UpdateTitle();

            this.FormClosing += new FormClosingEventHandler(MainForm_FormClosing);
        }

        private void UpdateTitle()
        {
            string hotkey = HotkeySettingsDialog.GetHotkeyText(hotkeyModifiers, hotkeyVK);
            string screenshot = HotkeySettingsDialog.GetHotkeyText(screenshotModifiers, screenshotVK);
            this.Text = "快捷便签  [唤起: " + hotkey + "  截图: " + screenshot + "]";
        }

        private void LoadHotkeySettings()
        {
            if (!File.Exists(settingsFile)) return;
            uint mod, vk;
            foreach (string line in File.ReadAllLines(settingsFile))
            {
                if (line.StartsWith("hotkey="))
                {
                    string[] parts = line.Substring(7).Split('|');
                    if (parts.Length == 2 &&
                        uint.TryParse(parts[0], out mod) &&
                        uint.TryParse(parts[1], out vk) &&
                        vk != 0)
                    {
                        hotkeyModifiers = mod;
                        hotkeyVK = vk;
                    }
                }
                else if (line.StartsWith("screenshot="))
                {
                    string[] parts = line.Substring(11).Split('|');
                    if (parts.Length == 2 &&
                        uint.TryParse(parts[0], out mod) &&
                        uint.TryParse(parts[1], out vk) &&
                        vk != 0)
                    {
                        screenshotModifiers = mod;
                        screenshotVK = vk;
                    }
                }
            }
        }

        private void SaveHotkeySettings()
        {
            string[] lines = new string[]
            {
                "hotkey=" + hotkeyModifiers + "|" + hotkeyVK,
                "screenshot=" + screenshotModifiers + "|" + screenshotVK
            };
            try { File.WriteAllLines(settingsFile, lines); } catch { }
        }

        private void OpenHotkeySettings(bool isScreenshot)
        {
            uint currentMod = isScreenshot ? screenshotModifiers : hotkeyModifiers;
            uint currentVK  = isScreenshot ? screenshotVK : hotkeyVK;

            using (HotkeySettingsDialog dlg = new HotkeySettingsDialog(currentMod, currentVK))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    if (isScreenshot)
                    {
                        UnregisterHotKey(this.Handle, SCREENSHOT_HOTKEY_ID);
                        screenshotModifiers = dlg.SelectedModifiers;
                        screenshotVK = dlg.SelectedVK;
                        RegisterHotKey(this.Handle, SCREENSHOT_HOTKEY_ID, screenshotModifiers, screenshotVK);
                    }
                    else
                    {
                        UnregisterHotKey(this.Handle, HOTKEY_ID);
                        hotkeyModifiers = dlg.SelectedModifiers;
                        hotkeyVK = dlg.SelectedVK;
                        RegisterHotKey(this.Handle, HOTKEY_ID, hotkeyModifiers, hotkeyVK);
                    }
                    SaveHotkeySettings();
                    UpdateTitle();
                }
            }
        }

        private void TakeScreenshot()
        {
            // 隐藏主窗口，等待系统重绘
            bool wasVisible = this.Visible;
            this.Hide();
            Application.DoEvents();
            System.Threading.Thread.Sleep(150);

            // 捕获全屏
            Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
            Bitmap fullBitmap = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(fullBitmap))
            {
                g.CopyFromScreen(screenBounds.Location, Point.Empty, screenBounds.Size);
            }

            // 显示选区遮罩
            Bitmap captured = null;
            using (ScreenshotOverlay overlay = new ScreenshotOverlay(fullBitmap))
            {
                overlay.ShowDialog();
                if (overlay.CapturedBitmap != null)
                    captured = overlay.CapturedBitmap;
            }
            fullBitmap.Dispose();

            if (captured != null)
            {
                // 复制到剪贴板
                Clipboard.SetImage(captured);

                // 保存到桌面
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = "screenshot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
                string savePath = Path.Combine(desktopPath, fileName);
                captured.Save(savePath, ImageFormat.Png);
                captured.Dispose();
            }

            if (wasVisible)
            {
                this.Show();
                this.Activate();
            }
        }

        private void BtnAddRow_Click(object sender, EventArgs e)
        {
            AddBlock();
            container.ScrollControlIntoView(container.Controls[container.Controls.Count - 1]);
        }

        private void Container_Resize(object sender, EventArgs e)
        {
            container.SuspendLayout();
            foreach (Control block in container.Controls)
            {
                block.Width = container.ClientSize.Width - 25;

                foreach (Control c in block.Controls)
                {
                    // 现在的结构是 block -> borderPanel -> innerPanel -> TextBox
                    Panel borderPanel = c as Panel;
                    if (borderPanel != null)
                    {
                        // 直接取 Tag 里的 TextBox
                        TextBox tb = borderPanel.Tag as TextBox;
                        if (tb != null)
                        {
                            int targetWidth = block.Width; // 边框填满 block
                            if (borderPanel.Width != targetWidth) borderPanel.Width = targetWidth;

                            // 传入 TextBox 可用宽度
                            AdjustBlockHeight(tb, targetWidth - RIGHT_PADDING - 6);
                        }
                    }
                }
            }
            container.ResumeLayout(true);
        }

        private void AdjustBlockHeight(TextBox tb, int manualWidth = -1)
        {
            // tb -> innerPanel -> borderPanel -> block
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

            // 文本高度 + 顶部Padding(4) + 底部Padding(4)
            int tbHeight = size.Height + 8;
            int minTbHeight = tb.Font.Height + 8;
            if (tbHeight < minTbHeight) tbHeight = minTbHeight;

            // 最小高度要容纳三个图标 (24 * 3 = 72) + 上下边距
            int minIconHeight = ICON_HEIGHT * 3 + 2;
            if (tbHeight < minIconHeight) tbHeight = minIconHeight;

            if (tb.Height != tbHeight) tb.Height = tbHeight;
            if (innerPanel.Height != tbHeight) innerPanel.Height = tbHeight; // 修正：innerPanel 高度应该与 tbHeight 相同

            // borderPanel 高度 = innerPanel + 2px 边框
            if (borderPanel.Height != tbHeight + 2) borderPanel.Height = tbHeight + 2;

            Panel block = borderPanel.Parent as Panel;
            if (block != null)
            {
                int newBlockHeight = borderPanel.Top + borderPanel.Height + 5;
                if (block.Height != newBlockHeight) block.Height = newBlockHeight;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveData();
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            UnregisterHotKey(this.Handle, SCREENSHOT_HOTKEY_ID);
            trayIcon.Visible = false;
            trayIcon.Dispose();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_ID)
                    ToggleVisibility();
                else if (id == SCREENSHOT_HOTKEY_ID)
                    TakeScreenshot();
            }
            base.WndProc(ref m);
        }

        private void ToggleVisibility()
        {
            if (this.Visible)
            {
                this.Hide();
            }
            else
            {
                this.Show();
                this.Activate();
            }
        }

        private void LoadData()
        {
            if (File.Exists(dataFile))
            {
                string[] lines = File.ReadAllLines(dataFile);
                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        string content = line.Replace("{{NEWLINE}}", "\r\n");
                        AddBlock(content);
                    }
                }
            }
        }

        private void SaveData()
        {
            List<string> lines = new List<string>();
            foreach (Control block in container.Controls)
            {
                foreach (Control c in block.Controls)
                {
                    // block -> borderPanel -> Tag(TextBox)
                    Panel borderPanel = c as Panel;
                    if (borderPanel != null && borderPanel.Tag is TextBox)
                    {
                        TextBox tb = borderPanel.Tag as TextBox;
                        if (tb != null && !string.IsNullOrWhiteSpace(tb.Text))
                        {
                            string content = tb.Text.Replace("\r\n", "{{NEWLINE}}").Replace("\n", "{{NEWLINE}}");
                            lines.Add(content);
                        }
                    }
                }
            }
            try { File.WriteAllLines(dataFile, lines.ToArray()); } catch {}
        }

        private void AddBlock(string initialText = "")
        {
            Panel block = new Panel();
            block.Width = container.ClientSize.Width - 25;
            block.Height = 60;
            block.Margin = new Padding(0, 0, 0, 8);
            block.BackColor = Color.White;

            Font font = new Font("Microsoft YaHei", 14F, FontStyle.Regular);

            // --- 灰色边框容器 ---
            Panel borderPanel = new Panel();
            borderPanel.BackColor = Color.LightGray;
            borderPanel.Padding = new Padding(1); // 1px 细边框
            borderPanel.Location = new Point(0, 0); // 顶格
            borderPanel.Width = block.Width;
            borderPanel.Height = 50;

            // --- 内部白色容器 ---
            // 把文本框和按钮都放在这里面，这样它们就共享同一个背景和边框了
            Panel innerPanel = new Panel();
            innerPanel.BackColor = Color.White;
            innerPanel.Dock = DockStyle.Fill;

            // --- 文本框 ---
            TextBox tb = new TextBox();
            tb.Multiline = true;
            tb.WordWrap = true;
            tb.ScrollBars = ScrollBars.None;
            tb.Text = initialText;
            tb.Font = font;
            tb.ForeColor = Color.FromArgb(64, 64, 64);
            tb.BorderStyle = BorderStyle.None;
            // 手动定位，不使用Dock，以便给右侧按钮留出绝对空间
            tb.Location = new Point(3, 4); // 微调位置，文字垂直居中
            tb.Width = innerPanel.Width - RIGHT_PADDING - 6;
            tb.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top; // T, L, R Anchors

            tb.TextChanged += new EventHandler(TextBox_TextChanged);
            tb.KeyDown += new KeyEventHandler(TextBox_KeyDown);

            // --- 删除图标 (右上) ---
            Label lblDel = new Label();
            lblDel.Text = "×";
            lblDel.Font = new Font("Arial", 14F, FontStyle.Bold);
            lblDel.AutoSize = false;
            lblDel.Size = new Size(ICON_WIDTH, ICON_HEIGHT);
            // 绝对定位：紧贴右上角 (0, 0)
            lblDel.Location = new Point(innerPanel.Width - ICON_WIDTH, 0);
            lblDel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblDel.ForeColor = Color.Crimson;
            lblDel.Cursor = Cursors.Hand;
            lblDel.TextAlign = ContentAlignment.MiddleCenter;
            lblDel.Tag = block;
            toolTip.SetToolTip(lblDel, "删除");

            lblDel.Click += new EventHandler(LblDel_Click);
            lblDel.MouseEnter += new EventHandler(Icon_MouseEnter_Del);
            lblDel.MouseLeave += new EventHandler(Icon_MouseLeave_Del);

            // --- 复制图标 (删除下面) ---
            Label lblCopy = new Label();
            lblCopy.Text = "❐";
            lblCopy.Font = new Font("Segoe UI Symbol", 10F, FontStyle.Bold);
            lblCopy.AutoSize = false;
            lblCopy.Size = new Size(ICON_WIDTH, ICON_HEIGHT);
            // 绝对定位：紧贴删除按钮下方
            lblCopy.Location = new Point(innerPanel.Width - ICON_WIDTH, ICON_HEIGHT);
            lblCopy.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblCopy.ForeColor = Color.RoyalBlue;
            lblCopy.Cursor = Cursors.Hand;
            lblCopy.TextAlign = ContentAlignment.MiddleCenter;
            lblCopy.Tag = tb;
            toolTip.SetToolTip(lblCopy, "复制");

            lblCopy.Click += new EventHandler(LblCopy_Click);
            lblCopy.MouseEnter += new EventHandler(Icon_MouseEnter_Copy);
            lblCopy.MouseLeave += new EventHandler(Icon_MouseLeave_Copy);

            // --- 转存到知识库图标 (复制下面) ---
            Label lblTransfer = new Label();
            lblTransfer.Text = "📥";
            lblTransfer.Font = new Font("Segoe UI Symbol", 10F);
            lblTransfer.AutoSize = false;
            lblTransfer.Size = new Size(ICON_WIDTH, ICON_HEIGHT);
            lblTransfer.Location = new Point(innerPanel.Width - ICON_WIDTH, ICON_HEIGHT * 2);
            lblTransfer.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTransfer.ForeColor = Color.FromArgb(0, 150, 80);
            lblTransfer.Cursor = Cursors.Hand;
            lblTransfer.TextAlign = ContentAlignment.MiddleCenter;
            lblTransfer.Tag = tb;
            toolTip.SetToolTip(lblTransfer, "转存到知识库");

            lblTransfer.Click += new EventHandler(LblTransfer_Click);
            lblTransfer.MouseEnter += (s, e) => ((Label)s).ForeColor = Color.FromArgb(0, 200, 100);
            lblTransfer.MouseLeave += (s, e) => ((Label)s).ForeColor = Color.FromArgb(0, 150, 80);

            // 组装层级
            innerPanel.Controls.Add(tb);
            innerPanel.Controls.Add(lblDel);
            innerPanel.Controls.Add(lblCopy);
            innerPanel.Controls.Add(lblTransfer);
            borderPanel.Controls.Add(innerPanel);
            block.Controls.Add(borderPanel);
            container.Controls.Add(block);

            // 互相关联 Tag
            borderPanel.Tag = tb;
            tb.Tag = borderPanel; // 这里的 Tag 还是指向 borderPanel 比较方便

            AdjustBlockHeight(tb);
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = (TextBox)sender;
            AdjustBlockHeight(tb);
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            // tb -> innerPanel -> borderPanel -> block
            Panel innerPanel = tb.Parent as Panel;
            Panel borderPanel = innerPanel.Parent as Panel;
            Panel block = borderPanel.Parent as Panel;

            if (e.KeyCode == Keys.Back && tb.TextLength == 0 && container.Controls.Count > 1)
            {
                int index = container.Controls.IndexOf(block);
                RemoveBlock(block);
                if (index > 0 && index - 1 < container.Controls.Count)
                {
                    var prevBlock = container.Controls[index - 1];
                    // 现在的层级比较深，需要准确找到 TextBox
                    foreach(Control c in prevBlock.Controls)
                    {
                        Panel bp = c as Panel;
                        if (bp != null && bp.Tag is TextBox)
                        {
                            TextBox prevTb = bp.Tag as TextBox;
                            prevTb.Focus();
                            break;
                        }
                    }
                }
            }
        }

        private void LblCopy_Click(object sender, EventArgs e)
        {
            Label btn = (Label)sender;
            TextBox tb = (TextBox)btn.Tag;
            CopyText(tb.Text, btn);
        }

        private void LblDel_Click(object sender, EventArgs e)
        {
            Label btn = (Label)sender;
            Panel block = (Panel)btn.Tag;

            if (container.Controls.Count > 1)
                RemoveBlock(block);
            else
            {
                foreach(Control c in block.Controls)
                {
                    Panel borderPanel = c as Panel;
                    if (borderPanel != null && borderPanel.Tag is TextBox)
                    {
                        TextBox tb = borderPanel.Tag as TextBox;
                        tb.Text = "";
                    }
                }
            }
        }

        private void LblTransfer_Click(object sender, EventArgs e)
        {
            Label btn = (Label)sender;
            TextBox tb = (TextBox)btn.Tag;

            if (string.IsNullOrWhiteSpace(tb.Text))
            {
                MessageBox.Show("便签内容为空，无法转存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            KnowledgeDB db = new KnowledgeDB();
            List<string> tags = db.GetAllTags();

            using (TransferDialog dlg = new TransferDialog(tb.Text, tags))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    knowledgePanel.AddKnowledgeFromNote(
                        dlg.KnowledgeContent,
                        dlg.KnowledgeTags
                    );
                    MessageBox.Show("已成功转存到知识库", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void CopyText(string text, Label btn)
        {
            if (!string.IsNullOrEmpty(text))
            {
                Clipboard.SetText(text);
                string originalText = btn.Text;

                btn.Text = "✔";
                btn.ForeColor = Color.Green;

                Timer t = new Timer();
                t.Interval = 1000; // 1 second
                t.Tag = new object[] { btn, originalText };
                t.Tick += new EventHandler(Timer_Tick);
                t.Start();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Timer t = (Timer)sender;
            object[] states = (object[])t.Tag;
            Label btn = (Label)states[0];
            string originalText = (string)states[1];

            btn.Text = originalText;
            btn.ForeColor = Color.RoyalBlue;

            t.Stop();
            t.Dispose();
        }

        private void RemoveBlock(Control block)
        {
            container.Controls.Remove(block);
            block.Dispose();
            SaveData();
        }

        // --- 悬停变色逻辑 ---

        private void Icon_MouseEnter_Copy(object sender, EventArgs e)
        {
            ((Label)sender).ForeColor = Color.DeepSkyBlue;
        }

        private void Icon_MouseLeave_Copy(object sender, EventArgs e)
        {
            Label lbl = (Label)sender;
            if (lbl.Text != "✔") lbl.ForeColor = Color.RoyalBlue;
        }

        private void Icon_MouseEnter_Del(object sender, EventArgs e)
        {
            ((Label)sender).ForeColor = Color.Red;
        }

        private void Icon_MouseLeave_Del(object sender, EventArgs e)
        {
            ((Label)sender).ForeColor = Color.Crimson;
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class ScreenshotOverlay : Form
    {
        private Bitmap background;
        private Point startPoint;
        private Rectangle selection;
        private bool isDragging = false;

        public Bitmap CapturedBitmap { get; private set; }

        // 绘图资源
        private readonly Pen selectionPen = new Pen(Color.FromArgb(0, 174, 255), 2);
        private readonly SolidBrush darkBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
        private readonly Font dimFont = new Font("Microsoft YaHei", 9F);
        private readonly SolidBrush dimBgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
        private readonly SolidBrush dimTextBrush = new SolidBrush(Color.White);

        public ScreenshotOverlay(Bitmap screenshot)
        {
            background = screenshot;

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = Screen.PrimaryScreen.Bounds;
            this.TopMost = true;
            this.Cursor = Cursors.Cross;
            this.DoubleBuffered = true;
            this.KeyPreview = true;

            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += OnMouseUp;
            this.KeyDown += OnKeyDown;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // 绘制截图作为背景
            g.DrawImage(background, 0, 0);

            // 整屏暗色蒙层
            g.FillRectangle(darkBrush, this.ClientRectangle);

            if (isDragging && selection.Width > 0 && selection.Height > 0)
            {
                // 选中区域：显示原始截图（去掉蒙层）
                g.DrawImage(background, selection, selection, GraphicsUnit.Pixel);

                // 选区边框
                g.DrawRectangle(selectionPen, selection);

                // 尺寸提示文字
                string dimText = selection.Width + " × " + selection.Height;
                SizeF textSize = g.MeasureString(dimText, dimFont);
                int tipX = selection.Right - (int)textSize.Width - 4;
                int tipY = selection.Bottom + 4;
                // 超出底部则显示在选区内上方
                if (tipY + textSize.Height > this.Height)
                    tipY = selection.Bottom - (int)textSize.Height - 4;
                if (tipX < 0) tipX = selection.Left + 2;

                RectangleF tipRect = new RectangleF(tipX - 3, tipY - 2, textSize.Width + 6, textSize.Height + 4);
                g.FillRectangle(dimBgBrush, tipRect);
                g.DrawString(dimText, dimFont, dimTextBrush, tipX, tipY);
            }
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                startPoint = e.Location;
                isDragging = true;
                selection = Rectangle.Empty;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                selection = GetRect(startPoint, e.Location);
                this.Invalidate();
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
            // 选区太小时取消（防止误触）
            if (selection.Width > 4 && selection.Height > 4)
            {
                CapturedBitmap = background.Clone(selection, background.PixelFormat);
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                this.DialogResult = DialogResult.Cancel;
            }
            this.Close();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        private Rectangle GetRect(Point p1, Point p2)
        {
            return new Rectangle(
                Math.Min(p1.X, p2.X),
                Math.Min(p1.Y, p2.Y),
                Math.Abs(p1.X - p2.X),
                Math.Abs(p1.Y - p2.Y));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                selectionPen.Dispose();
                darkBrush.Dispose();
                dimFont.Dispose();
                dimBgBrush.Dispose();
                dimTextBrush.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class HotkeySettingsDialog : Form
    {
        public uint SelectedModifiers { get; private set; }
        public uint SelectedVK { get; private set; }

        private uint pendingModifiers;
        private uint pendingVK;
        private TextBox txtHotkey;

        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        public HotkeySettingsDialog(uint currentModifiers, uint currentVK)
        {
            SelectedModifiers = currentModifiers;
            SelectedVK = currentVK;
            pendingModifiers = currentModifiers;
            pendingVK = currentVK;

            this.Text = "设置热键";
            this.Size = new Size(320, 195);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            Label lblPrompt = new Label();
            lblPrompt.Text = "点击输入框，然后按下新的组合键：";
            lblPrompt.Location = new Point(18, 18);
            lblPrompt.AutoSize = true;
            lblPrompt.Font = new Font("Microsoft YaHei", 9F);

            txtHotkey = new TextBox();
            txtHotkey.Location = new Point(18, 46);
            txtHotkey.Width = 272;
            txtHotkey.ReadOnly = true;
            txtHotkey.Text = GetHotkeyText(currentModifiers, currentVK);
            txtHotkey.Font = new Font("Microsoft YaHei", 12F);
            txtHotkey.TextAlign = HorizontalAlignment.Center;
            txtHotkey.BackColor = Color.FromArgb(245, 245, 245);
            txtHotkey.KeyDown += TxtHotkey_KeyDown;
            txtHotkey.KeyUp += TxtHotkey_KeyUp;
            txtHotkey.PreviewKeyDown += TxtHotkey_PreviewKeyDown;

            Label lblTip = new Label();
            lblTip.Text = "组合键需包含 Ctrl / Alt / Shift 之一";
            lblTip.Location = new Point(18, 82);
            lblTip.AutoSize = true;
            lblTip.Font = new Font("Microsoft YaHei", 8F);
            lblTip.ForeColor = Color.Gray;

            Button btnSave = new Button();
            btnSave.Text = "保存";
            btnSave.Location = new Point(108, 118);
            btnSave.Size = new Size(84, 30);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.BackColor = Color.FromArgb(0, 120, 215);
            btnSave.ForeColor = Color.White;
            btnSave.Cursor = Cursors.Hand;
            btnSave.Font = new Font("Microsoft YaHei", 9F);
            btnSave.Click += (s, e) =>
            {
                SelectedModifiers = pendingModifiers;
                SelectedVK = pendingVK;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            Button btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.Location = new Point(202, 118);
            btnCancel.Size = new Size(84, 30);
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

            this.Controls.AddRange(new Control[] { lblPrompt, txtHotkey, lblTip, btnSave, btnCancel });
        }

        private void TxtHotkey_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            // 确保方向键、Tab 等特殊键也能被 KeyDown 捕获
            e.IsInputKey = true;
        }

        private void TxtHotkey_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            e.Handled = true;

            // 忽略单独的修饰键
            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey ||
                e.KeyCode == Keys.Menu || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin ||
                e.KeyCode == Keys.None)
                return;

            uint modifiers = 0;
            if (e.Control) modifiers |= MOD_CONTROL;
            if (e.Alt)     modifiers |= MOD_ALT;
            if (e.Shift)   modifiers |= MOD_SHIFT;

            if (modifiers == 0) return; // 必须有修饰键

            pendingModifiers = modifiers;
            pendingVK = (uint)e.KeyCode;
            txtHotkey.Text = GetHotkeyText(pendingModifiers, pendingVK);
        }

        private void TxtHotkey_KeyUp(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            e.Handled = true;
        }

        /// <summary>将修饰符和虚拟键码转换为可读字符串，如 "Ctrl+F12"</summary>
        public static string GetHotkeyText(uint modifiers, uint vk)
        {
            List<string> parts = new List<string>();
            if ((modifiers & 0x0002) != 0) parts.Add("Ctrl");
            if ((modifiers & 0x0004) != 0) parts.Add("Shift");
            if ((modifiers & 0x0001) != 0) parts.Add("Alt");
            if ((modifiers & 0x0008) != 0) parts.Add("Win");
            parts.Add(((Keys)vk).ToString());
            return string.Join("+", parts);
        }
    }
}
