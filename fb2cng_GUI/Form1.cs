using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace fb2cng_GUI
{
    // Головна форма налаштувань програми
    public partial class Form1 : Form
    {
        // Посилання на об'єкт конфігурації (налаштування)
        private AppSettings _settings;

        // Елементи інтерфейсу: текстові підписи
        private Label lblLang, lblFormat, lblMenu;

        // Елементи інтерфейсу: випадаючі списки
        private ComboBox cbLang, cbFormat;

        // Елементи інтерфейсу: прапорці (чекбокси)
        private CheckBox chkFolder, chkConfig, chkDeleteMain, chkDeleteSub;
        private CheckBox chkMinimize, chkHideProgress;
        // НОВИЙ ЕЛЕМЕНТ: Чекбокс для перезапису файлів
        private CheckBox chkOverwrite;

        // Елементи інтерфейсу: текстові поля
        private TextBox txtFolder, txtConfig, txtMenu;

        // Елементи інтерфейсу: кнопки дій та вибору файлів/папок
        private Button btnFolderBrowse, btnConfigBrowse, btnIntegrate, btnOk, btnCancel, btnThemeToggle;

        // Кнопка "Довідка" (прямокутний значок питання у стилі теми)
        private Button btnHelp;

        // Форма для відображення спливаючого вікна з описом програми
        private Form infoTooltipForm;

        // Конструктор форми: завантажує дані та налаштовує зовнішній вигляд
        public Form1()
        {
            // Завантажуємо збережені налаштування з файлу
            _settings = AppSettings.Load();

            // Створюємо та розміщуємо всі компоненти на формі вручну
            InitializeComponentsManual();

            // Заповнюємо елементи UI значеннями з налаштувань
            ApplySettingsToUI();

            // Застосовуємо поточну мову локалізації
            ApplyLocalization();

            // Встановлюємо тему оформлення (світлу або темну)
            ApplyTheme();
        }

        // Ручне проектування та розміщення елементів інтерфейсу
        private void InitializeComponentsManual()
        {
            // Загальні параметри головного вікна програми
            Text = "GUI for fb2cng";
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); // Іконка в кут програми

            // ЗБІЛЬШЕНО ВИСОТУ ВІКНА: з 660 на 690, щоб уникнути накладання елементів внизу
            Size = new Size(500, 690);

            FormBorderStyle = FormBorderStyle.FixedSingle;  // Заборона зміни розміру вікна
            MaximizeBox = false;                            // Вимкнення кнопки розгортання на весь екран
            StartPosition = FormStartPosition.CenterScreen; // Поява по центру екрана
            Font = new Font("Segoe UI", 10F, FontStyle.Regular); // Стандартний шрифт

            // Підібрані координати для уникнення накладання кнопок внизу додатка
            int currentY = 6;
            int padding = 12;


            // --- КНОПКА ДОВІДКИ (У СТИЛІ КНОПКИ ТЕМИ ТА ЗАКРУГЛЕННЯМ 6) ---
            btnHelp = new Button
            {
                Location = new Point(430, currentY),
                Size = new Size(32, 32),
                Text = "?",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            MakeButtonRounded(btnHelp, 6); // Прямокутна форма з легким закругленням кутів
            btnHelp.Click += BtnHelp_Click; // Прив'язка події натискання
            Controls.Add(btnHelp);

            // 1. Блок: Мова інтерфейсу
            currentY += 18;
            lblLang = new Label { Location = new Point(20, currentY), Size = new Size(400, 20) };
            cbLang = new ComboBox { Location = new Point(20, currentY + 22), Size = new Size(440, 30), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            cbLang.Items.AddRange(new object[] { "English", "Українська", "Русский" });
            cbLang.SelectedIndexChanged += (s, e) => { ApplyLocalization(); };
            Controls.AddRange(new Control[] { lblLang, cbLang });

            // 2. Блок: Назва пункту контекстного меню
            currentY += 60 + padding;
            lblMenu = new Label { Location = new Point(20, currentY), Size = new Size(440, 20) };
            txtMenu = new TextBox { Location = new Point(20, currentY + 22), Size = new Size(440, 30), BorderStyle = BorderStyle.FixedSingle };
            Controls.AddRange(new Control[] { lblMenu, txtMenu });

            // 3. Блок: Формат вихідного документа
            currentY += 60 + padding;
            lblFormat = new Label { Location = new Point(20, currentY), Size = new Size(440, 20) };
            cbFormat = new ComboBox { Location = new Point(20, currentY + 22), Size = new Size(440, 30), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            cbFormat.Items.AddRange(new object[] { "EPUB2", "KEPUB", "EPUB3", "AZW8", "KFX", "PDF", "TXT", "MD" });
            Controls.AddRange(new Control[] { lblFormat, cbFormat });
            
            // 4. Блок: Папка для збереження результату
            currentY += 60 + padding;
            chkFolder = new CheckBox { Location = new Point(20, currentY), Size = new Size(440, 24) };
            txtFolder = new TextBox { Location = new Point(20, currentY + 26), Size = new Size(390, 30), BorderStyle = BorderStyle.FixedSingle };
            btnFolderBrowse = new Button { Location = new Point(420, currentY + 25), Size = new Size(40, 26), Text = "📁", FlatStyle = FlatStyle.Flat };
            MakeButtonRounded(btnFolderBrowse, 4);
            chkFolder.CheckedChanged += (s, e) => { txtFolder.Enabled = btnFolderBrowse.Enabled = chkFolder.Checked; };
            btnFolderBrowse.Click += (s, e) =>
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        txtFolder.Text = fbd.SelectedPath;
                    }
                }
            };
            Controls.AddRange(new Control[] { chkFolder, txtFolder, btnFolderBrowse });

            // 5. Блок: Конфігураційний файл (.yaml)
            currentY += 60 + padding;
            chkConfig = new CheckBox { Location = new Point(20, currentY), Size = new Size(440, 24) };
            txtConfig = new TextBox { Location = new Point(20, currentY + 26), Size = new Size(390, 30), BorderStyle = BorderStyle.FixedSingle };
            btnConfigBrowse = new Button { Location = new Point(420, currentY + 25), Size = new Size(40, 26), Text = "📁", FlatStyle = FlatStyle.Flat };
            MakeButtonRounded(btnConfigBrowse, 4);
            chkConfig.CheckedChanged += (s, e) => { txtConfig.Enabled = btnConfigBrowse.Enabled = chkConfig.Checked; };
            btnConfigBrowse.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "YAML config|*.yaml;*.yml" })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        txtConfig.Text = ofd.FileName;
                    }
                }
            };
            Controls.AddRange(new Control[] { chkConfig, txtConfig, btnConfigBrowse });

            // 6. Блок опцій автоматизації (ВИРІВНЯНО СИМЕТРИЧНО ПО ЛІНІЇ ІНТЕРФЕЙСУ)
            currentY += 60 + padding;

            // НОВИЙ ЕЛЕМЕНТ: Чекбокс для перезапису файлів (стоїть першим у Блоці 6)
            chkOverwrite = new CheckBox { Location = new Point(20, currentY), Size = new Size(440, 24), FlatStyle = FlatStyle.Flat };

            // Усі інші чекбокси зміщено вниз на +26 пікселів, щоб звільнити місце
            chkDeleteMain = new CheckBox { Location = new Point(20, currentY + 26), Size = new Size(440, 24), FlatStyle = FlatStyle.Flat };

            // Підпункти зсунуті вправо (X = 45) для створення красивої дерева-ієрархії елементів
            chkDeleteSub = new CheckBox { Location = new Point(45, currentY + 52), Size = new Size(415, 24), FlatStyle = FlatStyle.Flat };
            chkMinimize = new CheckBox { Location = new Point(20, currentY + 78), Size = new Size(415, 24), FlatStyle = FlatStyle.Flat };
            chkHideProgress = new CheckBox { Location = new Point(45, currentY + 104), Size = new Size(415, 24), FlatStyle = FlatStyle.Flat };

            // Тільки чистий базовий взаємозв'язок для видалення файлів (без зайвого конфліктного сміття подій)
            chkDeleteMain.CheckedChanged += (s, e) =>
            {
                chkDeleteSub.Enabled = chkDeleteMain.Checked;
                if (!chkDeleteMain.Checked)
                {
                    chkDeleteSub.Checked = false;
                }
            };

            // Додано chkOverwrite до списку контролів форми
            Controls.AddRange(new Control[] { chkOverwrite, chkDeleteMain, chkDeleteSub, chkMinimize, chkHideProgress });

            // 7. Кнопка інтеграції/деінтеграції в провідник Windows
            // ЗАФІКСОВАНО КООРДИНАТУ Y: замість "currentY += 135 + padding;" ставимо чітко 532,
            // щоб кнопка була строго над нижньою панеллю і елементи не наповзали один на одного.
            btnIntegrate = new Button { Location = new Point(20, 532), Size = new Size(440, 35), FlatStyle = FlatStyle.Flat };
            btnIntegrate.Click += BtnIntegrate_Click;
            MakeButtonRounded(btnIntegrate, 6);
            Controls.Add(btnIntegrate);

            // Нижня панель управління (Зміна теми, збереження та скасування)
            btnThemeToggle = new Button { Location = new Point(20, 590), Size = new Size(40, 32), Text = "🌓", FlatStyle = FlatStyle.Flat };
            btnThemeToggle.Click += (s, e) =>
            {
                _settings.Theme = _settings.Theme == "Dark" ? "Light" : "Dark";
                ApplyTheme();
                if (infoTooltipForm != null && infoTooltipForm.Visible)
                {
                    infoTooltipForm.Close();                 // Закриваємо довідку при зміні теми
                }
            };
            MakeButtonRounded(btnThemeToggle, 6);

            btnOk = new Button { Location = new Point(250, 590), Size = new Size(100, 32), FlatStyle = FlatStyle.Flat };
            btnCancel = new Button { Location = new Point(360, 590), Size = new Size(100, 32), FlatStyle = FlatStyle.Flat };
            MakeButtonRounded(btnOk, 6);
            MakeButtonRounded(btnCancel, 6);

            // --- ЗАХИСТ ВІД КОНФЛІКТУ ГАЛОЧОК НА ЕТАПІ НАТИСКАННЯ ОК ---
            btnOk.Click += (s, e) =>
            {
                // Якщо користувач активував обидва несумісні режими одночасно
                if (chkMinimize.Checked && chkHideProgress.Checked)
                {
                    string lang = cbLang.SelectedItem != null ? cbLang.SelectedItem.ToString() : _settings.Language;
                    string warningText = Localization.Get(lang, "ConflictWarning\nProgress bar:\n2 checkboxes checked");
                    DialogResult dialogResult = ShowCustomMessageBox(warningText, "fb2cng GUI", buttons: MessageBoxButtons.OK);
                    return;                            // Блокуємо закриття вікна, поки користувач не виправить помилку
                }

                SaveUiToSettings();
                _settings.Save();
                Close();
            };

            btnCancel.Click += (s, e) => { Close(); };
            Controls.AddRange(new Control[] { btnThemeToggle, btnOk, btnCancel });

            // Перехоплення переміщення головної форми для динамічного оновлення позиції вікна опису
            LocationChanged += (s, e) => { UpdateHelpWindowPosition(); };
        }
        // Графічний метод створення закруглених кутів для кнопок через зміну їхнього регіону
        private void MakeButtonRounded(Button btn, int radius)
        {
            btn.Paint += (s, e) =>
            {
                Rectangle bounds = new Rectangle(0, 0, btn.Width, btn.Height);
                using (GraphicsPath path = new GraphicsPath())
                {
                    // Послідовно додаємо дуги для чотирьох кутів кнопки
                    path.AddArc(bounds.X, bounds.Y, radius * 2, radius * 2, 180, 90);
                    path.AddArc(bounds.X + bounds.Width - (radius * 2), bounds.Y, radius * 2, radius * 2, 270, 90);
                    path.AddArc(bounds.X + bounds.Width - (radius * 2), bounds.Y + bounds.Height - (radius * 2), radius * 2, radius * 2, 0, 90);
                    path.AddArc(bounds.X, bounds.Y + bounds.Height - (radius * 2), radius * 2, radius * 2, 90, 90);
                    path.CloseAllFigures();
                    btn.Region = new Region(path); // Призначаємо нову закруглену форму для кнопки
                }
            };
        }

        // Метод динамічного застосування світлої або темної теми оформлення до всіх елементів форми
        private void ApplyTheme()
        {
            bool isDark = _settings.Theme == "Dark";
            // Встановлюємо фоновий колір головної форми
            BackColor = isDark ? Color.FromArgb(24, 24, 24) : Color.FromArgb(245, 245, 245);
            Color textCol = isDark ? Color.White : Color.Black;
            Color inputBg = isDark ? Color.FromArgb(40, 40, 40) : Color.White;
            Color btnBg = isDark ? Color.FromArgb(50, 50, 50) : Color.FromArgb(230, 230, 230);
            Color accentBg = isDark ? Color.FromArgb(0, 102, 204) : Color.FromArgb(0, 120, 215);

            // Циклом обходимо всі елементи керування на формі
            foreach (Control c in Controls)
            {
                if (c is Label || c is CheckBox)
                {
                    c.ForeColor = textCol;
                }

                if (c is TextBox || c is ComboBox)
                {
                    c.BackColor = inputBg;
                    c.ForeColor = textCol;
                }
                if (c is Button b)
                {
                    // Головні кнопки робимо акцентними синіми з білим текстом
                    if (b == btnOk || b == btnIntegrate)
                    {
                        b.BackColor = accentBg;
                        b.ForeColor = Color.White;
                        b.FlatAppearance.BorderSize = 0;
                    }
                    // Кнопка "?" має зберігати стиль звичайної кнопки або виділятися
                    else if (b == btnHelp)
                    {
                        b.BackColor = isDark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(220, 220, 220);
                        b.ForeColor = textCol;
                        b.FlatAppearance.BorderColor = isDark ? Color.FromArgb(90, 90, 90) : Color.FromArgb(180, 180, 180);
                    }
                    else
                    {
                        b.BackColor = btnBg;
                        b.ForeColor = textCol;
                        b.FlatAppearance.BorderColor = isDark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(200, 200, 200);
                    }
                }
            }
        }

        // --- ЛОГІКА ДЛЯ СТВОРЕННЯ, ПРИВ'ЯЗКИ ТА ЗАКРУГЛЕННЯ ВІКНА ОПИСУ ПРОГРАМИ ---
        // Подія натискання на прямокутну кнопку зі знаком питання
        private void BtnHelp_Click(object sender, EventArgs e)
        {
            // Якщо вікно вже відкрите — закриваємо його при повторному натисканні
            if (infoTooltipForm != null && !infoTooltipForm.IsDisposed && infoTooltipForm.Visible)
            {
                infoTooltipForm.Close();
                return;
            }

            // Визначаємо поточну мову та завантажуємо локалізовані тексти
            string lang = cbLang.SelectedItem != null ? cbLang.SelectedItem.ToString() : _settings.Language;
            string helpText = Localization.Get(lang, "HelpText");
            string helpTitle = Localization.Get(lang, "HelpTitle");

            bool isDark = _settings.Theme == "Dark";

            // Створюємо нову безрамкову форму довідки
            infoTooltipForm = new Form
            {
                Text = helpTitle,
                FormBorderStyle = FormBorderStyle.None,   // Без стандартних системних рамок Windows
                ShowInTaskbar = false,                    // Не показувати окрему іконку внизу на панелі задач
                StartPosition = FormStartPosition.Manual,   // Позиція задається строго вручну через координати
                BackColor = isDark ? Color.FromArgb(32, 32, 32) : Color.White
            };

            // Гарантовано збільшений розмір вікна довідки, щоб вмістити весь текст без обрізання рядків
            int calculatedWidth = Width / 2;
            int calculatedHeight = 220;
            infoTooltipForm.Size = new Size(calculatedWidth, calculatedHeight);

            // ТЕПЕР КРАЇ ВІКНА-ДОВІДКИ ТЕЖ ЗАОКРУГЛЕНІ (в загальній стилістиці програми)
            int windowRadius = 8; // Радіус закруглення кутів вікна
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(0, 0, windowRadius * 2, windowRadius * 2, 180, 90);
                path.AddArc(infoTooltipForm.Width - (windowRadius * 2), 0, windowRadius * 2, windowRadius * 2, 270, 90);
                path.AddArc(infoTooltipForm.Width - (windowRadius * 2), infoTooltipForm.Height - (windowRadius * 2), windowRadius * 2, windowRadius * 2, 0, 90);
                path.AddArc(0, infoTooltipForm.Height - (windowRadius * 2), windowRadius * 2, windowRadius * 2, 90, 90);
                path.CloseAllFigures();
                infoTooltipForm.Region = new Region(path); // Призначаємо закруглену форму вікну
            }

            // Малюємо тонку рамку по контуру закругленого вікна, щоб воно гарно виділялося
            infoTooltipForm.Paint += (s, ev) =>
            {
                ev.Graphics.SmoothingMode = SmoothingMode.AntiAlias; // Згладжування ліній
                Color borderColor = isDark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(180, 180, 180);
                using (Pen pen = new Pen(borderColor, 1))
                {
                    using (GraphicsPath framePath = new GraphicsPath())
                    {
                        framePath.AddArc(0, 0, windowRadius * 2, windowRadius * 2, 180, 90);
                        framePath.AddArc(infoTooltipForm.Width - (windowRadius * 2), 0, windowRadius * 2, windowRadius * 2, 270, 90);
                        framePath.AddArc(infoTooltipForm.Width - (windowRadius * 2), infoTooltipForm.Height - (windowRadius * 2), windowRadius * 2, windowRadius * 2, 0, 90);
                        framePath.AddArc(0, infoTooltipForm.Height - (windowRadius * 2), windowRadius * 2, windowRadius * 2, 90, 90);
                        framePath.CloseAllFigures();
                        ev.Graphics.DrawPath(pen, framePath);
                    }
                }
            };

            // Текстовий блок RichTextBox для відображення опису програми
            RichTextBox rtbHelp = new RichTextBox
            {
                Text = helpText,
                Location = new Point(14, 14),
                Size = new Size(infoTooltipForm.Width - 28, infoTooltipForm.Height - 28),
                ForeColor = isDark ? Color.White : Color.Black,
                BackColor = infoTooltipForm.BackColor,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.None,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };

            // ВИРІВНЮВАННЯ ПО ЦЕНТРУ: Виділяємо весь текст і задаємо йому центральне вирівнювання за вашим бажанням
            rtbHelp.SelectAll();
            rtbHelp.SelectionAlignment = HorizontalAlignment.Center;
            rtbHelp.DeselectAll(); // Знімаємо виділення, щоб текст не підсвічувався синім кольором

            // Забороняємо виділення тексту мишею та появу текстового курсору
            rtbHelp.MouseDown += (s, ev) => { _ = Focus(); };
            infoTooltipForm.Controls.Add(rtbHelp);

            // Кліки по самому вікну або тексту також призведуть до його закриття
            infoTooltipForm.Click += (s, ev) => { infoTooltipForm.Close(); };
            rtbHelp.Click += (s, ev) => { infoTooltipForm.Close(); };

            // НАЙНАДІЙНІШИЙ СПОСІБ АВТОЗАКРИТТЯ: Як тільки користувач клікає БУДЬ-ДЕ поза цим вікном,
            // форма миттєво втрачає фокус (деактивується) і сама м'яко закривається в системі
            infoTooltipForm.Deactivate += (s, ev) => { infoTooltipForm.Close(); };

            // Спочатку показуємо вікно довідки як підлегле для головної форми
            infoTooltipForm.Show(this);

            // ОДРАЗУ ПІСЛЯ відображення примусово розраховуємо координати і ставимо вікно на місце
            UpdateHelpWindowPosition();
        }

        // Надійний метод динамічного оновлення координат вікна опису (прив'язка до внутрішнього лівого верхнього кута форми)
        private void UpdateHelpWindowPosition()
        {
            if (infoTooltipForm != null && !infoTooltipForm.IsDisposed && infoTooltipForm.Visible)
            {
                // RectangleToScreen(ClientRectangle) повертає точні координати внутрішнього лівого кута форми на екрані,
                // повністю ігноруючи розміри та похибки системних віконних рамок Windows (Aero/DPI)
                Point programContentTopLeft = RectangleToScreen(ClientRectangle).Location;

                // Ставимо вікно точно в лівий верхній кут вашої програми
                infoTooltipForm.Location = programContentTopLeft;
            }
        }
    }
}
