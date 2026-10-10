using System;
using System.Drawing;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HotynHelper
{
    public class Form1 : Form
    {
        private static readonly Color BG       = Color.FromArgb(10, 10, 10);
        private static readonly Color BG_DARK  = Color.FromArgb(5, 5, 5);
        private static readonly Color BG_CARD  = Color.FromArgb(17, 20, 24);
        private static readonly Color BORDER   = Color.FromArgb(28, 42, 53);
        private static readonly Color ACCENT   = Color.FromArgb(79, 195, 247);
        private static readonly Color ACCENT_D = Color.FromArgb(2, 136, 209);
        private static readonly Color TEXT     = Color.FromArgb(232, 244, 248);
        private static readonly Color TEXT_DIM = Color.FromArgb(107, 124, 133);

        private static readonly HttpClient http = new HttpClient();

        private Panel _card = null!;
        private Label _lblTitle = null!;
        private Label _lblSubtitle = null!;
        private TextBox _txtInput = null!;
        private Panel _resultsPanel = null!;
        private RichTextBox _resultsBox = null!;
        private Label _lblStatus = null!;

        public Form1()
        {
            InitializeUI();
            http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "ru-RU,ru;q=0.9");
        }

        private void InitializeUI()
        {
            Text = "Hotyn Helper";
            Size = new Size(920, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BG;
            ForeColor = TEXT;
            Font = new Font("Segoe UI", 10F);
            MinimumSize = new Size(700, 500);
            DoubleBuffered = true;

            var header = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = BG_DARK };
            Controls.Add(header);

            var logo = new Label
            {
                Text = "⬢",
                ForeColor = ACCENT,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                Location = new Point(18, 10),
                Size = new Size(44, 44),
                TextAlign = ContentAlignment.MiddleCenter
            };
            header.Controls.Add(logo);

            string[] chips = { "📜 Правила", "⚖ Законы", "🏙 New York", "🗺 Карта", "🎉 Ивенты" };
            int x = 70;
            foreach (var chipText in chips)
            {
                bool active = chipText.Contains("New York");
                var chip = new Label
                {
                    Text = chipText,
                    BackColor = active ? ACCENT_D : BG_CARD,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5F),
                    Padding = new Padding(12, 6, 12, 6),
                    AutoSize = true,
                    Location = new Point(x, 17),
                    Cursor = Cursors.Hand
                };
                var parts = chipText.Split(' ');
                if (parts.Length > 1)
                {
                    string query = parts.Length > 2 ? parts[1] + " " + parts[2] : parts[1];
                    chip.Click += (s, e) => { _txtInput.Text = query; _txtInput.Focus(); };
                }
                header.Controls.Add(chip);
                x += chip.PreferredWidth + 12;
            }

            _card = new Panel
            {
                BackColor = BG_CARD,
                Size = new Size(640, 280),
                Location = new Point((ClientSize.Width - 640) / 2, 150),
                Anchor = AnchorStyles.None
            };
            _card.Paint += (s, e) =>
                e.Graphics.DrawRectangle(new Pen(BORDER, 1), 0, 0, _card.Width - 1, _card.Height - 1);
            Controls.Add(_card);

            var icon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI", 26F),
                ForeColor = ACCENT,
                Size = new Size(70, 70),
                Location = new Point((_card.Width - 70) / 2, 24),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _card.Controls.Add(icon);

            _lblTitle = new Label
            {
                Text = "Похожих пунктов нет",
                ForeColor = TEXT,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 100),
                Size = new Size(_card.Width - 40, 30)
            };
            _card.Controls.Add(_lblTitle);

            _lblSubtitle = new Label
            {
                Text = "Опишите ситуацию своими словами: кто, что сделал,\nпри каких обстоятельствах.",
                ForeColor = TEXT_DIM,
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 135),
                Size = new Size(_card.Width - 40, 50)
            };
            _card.Controls.Add(_lblSubtitle);

            var inputWrap = new Panel
            {
                BackColor = BG_DARK,
                Location = new Point(24, 200),
                Size = new Size(_card.Width - 48, 44)
            };
            inputWrap.Paint += (s, e) =>
                e.Graphics.DrawRectangle(new Pen(ACCENT_D, 1), 0, 0, inputWrap.Width - 1, inputWrap.Height - 1);
            _card.Controls.Add(inputWrap);

            _txtInput = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = BG_DARK,
                ForeColor = TEXT,
                Font = new Font("Segoe UI", 11F),
                Location = new Point(8, 12),
                Width = inputWrap.Width - 220
            };
            _txtInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _ = SearchAsync(); }
            };
            inputWrap.Controls.Add(_txtInput);

            var btnSend = new Label
            {
                Text = "Enter  ▸  разобрать ситуацию",
                BackColor = ACCENT_D,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Size = new Size(200, 36),
                Location = new Point(inputWrap.Width - 208, 4),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnSend.Click += (s, e) => _ = SearchAsync();
            inputWrap.Controls.Add(btnSend);

            _resultsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                Visible = false,
                Padding = new Padding(20, 10, 20, 10)
            };
            Controls.Add(_resultsPanel);
            _resultsPanel.BringToFront();

            _resultsBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = BG_CARD,
                ForeColor = TEXT,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 10F),
                ReadOnly = true,
                DetectUrls = false
            };
            _resultsPanel.Controls.Add(_resultsBox);

            _lblStatus = new Label
            {
                Text = "готов",
                Dock = DockStyle.Bottom,
                Height = 24,
                ForeColor = TEXT_DIM,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };
            Controls.Add(_lblStatus);

            Resize += (s, e) =>
                _card.Location = new Point((ClientSize.Width - _card.Width) / 2,
                                           (ClientSize.Height - _card.Height) / 2);
        }

        private async Task SearchAsync()
        {
            string query = _txtInput.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            _lblTitle.Text = "Ищу...";
            _lblSubtitle.Text = "Обрабатываю запрос к forum.crystalrp.ru";
            _lblStatus.Text = $"поиск: {query}";

            try
            {
                string url = $"https://forum.crystalrp.ru/search/search?q={Uri.EscapeDataString(query)}&o=relevance";
                string html = await http.GetStringAsync(url);

                var matches = Regex.Matches(html,
                    @"<h3[^>]*class=""contentRow-title""[^>]*>\s*<a[^>]*href=""([^""]+)""[^>]*>(.*?)</a>",
                    RegexOptions.Singleline);

                if (matches.Count == 0)
                {
                    _lblTitle.Text = "Похожих пунктов нет";
                    _lblSubtitle.Text = "Ничего не найдено. Возможно, нужен вход на форум\nили Cloudflare блокирует запрос.";
                    _lblStatus.Text = "0 результатов";
                    return;
                }

                _lblTitle.Text = $"Найдено: {matches.Count}";
                _lblSubtitle.Text = "Результаты ниже ↓";
                _lblStatus.Text = $"готово · {matches.Count} результатов";

                _card.Visible = false;
                _resultsPanel.Visible = true;
                _resultsPanel.BringToFront();

                _resultsBox.Clear();
                int i = 1;
                foreach (Match m in matches)
                {
                    if (i > 20) break;
                    string href = m.Groups[1].Value;
                    if (href.StartsWith("/")) href = "https://forum.crystalrp.ru" + href;
                    string title = Regex.Replace(m.Groups[2].Value, "<.*?>", "").Trim();

                    _resultsBox.SelectionColor = ACCENT;
                    _resultsBox.SelectionFont = new Font("Segoe UI", 11F, FontStyle.Bold);
                    _resultsBox.AppendText($"{i}. {title}\n");

                    _resultsBox.SelectionColor = Color.FromArgb(38, 198, 218);
                    _resultsBox.SelectionFont = new Font("Consolas", 9F);
                    _resultsBox.AppendText($"   {href}\n\n");
                    i++;
                }
            }
            catch (Exception ex)
            {
                _lblTitle.Text = "Ошибка";
                _lblSubtitle.Text = $"Не удалось выполнить запрос:\n{ex.Message}";
                _lblStatus.Text = "ошибка";
            }
        }
    }
}
