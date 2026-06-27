using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace fb2cng_GUI
{
    // Головна форма налаштувань програми
    public partial class Form1 : Form
    {
        // Посилання на об'єкт конфігурації (налаштування)
        private readonly AppSettings _settings;

        // Елементи інтерфейсу: текстові підписи
        private Label lblLang, lblFormat, lblMenu;

        // Елементи інтерфейсу: випадаючі списки
        private ComboBox cbLang, cbFormat;

        // Елементи інтерфейсу: прапорці (чекбокси) - папка призначення, конфігурація, перезапис,
        // видалити з підтвердженням, видалити в корзину, мінімізувати прогрес бар, приховати прогрес бар
        private CheckBox chkFolder, chkConfig, chkOverwrite, chkDeleteMain, chkDeleteSub, chkMinimize, chkHideProgress;

        // Елементи інтерфейсу: текстові поля
        private TextBox txtFolder, txtConfig, txtMenu;

        // Елементи інтерфейсу: кнопки дій та вибору файлів/папок (довідка, папка призначення,
        // конфігураційний файл, інтеграція, ОК, Відміна, перемикання теми)
        private Button btnHelp, btnFolderBrowse, btnConfigBrowse, btnIntegrate, btnOk, btnCancel, btnThemeToggle;

        // Форма для відображення спливаючого вікна з описом програми
        private Form infoTooltipForm;
        private int paddingBottom;
        private int finalHeight;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

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

            FormBorderStyle = FormBorderStyle.FixedSingle;  // Заборона зміни розміру вікна користувачем
            MaximizeBox = false;                            // Вимкнення кнопки розгортання на весь екран
            StartPosition = FormStartPosition.CenterScreen; // Поява по центру екрана
            Font = new Font("Segoe UI", 10F, FontStyle.Regular); // Стандартний шрифт

            // ---КНОПКА ДОВІДКИ-- -
            btnHelp = new Button
            {
                Text = "", // Порожній текст, малюємо вручну
                FlatStyle = FlatStyle.Flat,
            };
            btnHelp.FlatAppearance.BorderSize = 0; // Ховаємо стандартну рамку

            // Змінна для відстеження наведення миші на кнопку допомоги
            bool isHelpHovered = false;

            // Події для ефекту підсвічування при наведенні
            btnHelp.MouseEnter += (s, e) => { isHelpHovered = true; btnHelp.Invalidate(); };
            btnHelp.MouseLeave += (s, e) => { isHelpHovered = false; btnHelp.Invalidate(); };

            // БЕРЕМО ІКОНКУ НАПРЯМУ З РЕСУРСІВ ПРОЕКТУ
            // Примітка: Visual Studio автоматично замінить дефіс на підкреслення: icon_info
            Image infoIcon = Properties.Resources.icon_info;

            // Динамічне малювання картинки
            btnHelp.Paint += (s, e) =>
            {
                // 1. Визначаємо колір фону кнопки (з урахуванням наведення курсору)
                Color baseBgColor = btnHelp.BackColor;
                Color drawBgColor = baseBgColor;

                if (isHelpHovered)
                {
                    // Автоматично адаптуємо підсвічування під світлу або темну тему
                    bool isDark = baseBgColor.R < 128;
                    drawBgColor = isDark
                        ? Color.FromArgb(baseBgColor.R + 25, baseBgColor.G + 25, baseBgColor.B + 25)
                        : Color.FromArgb(baseBgColor.R - 20, baseBgColor.G - 20, baseBgColor.B - 20);
                }

                // Заливка фону кнопки
                using (Brush backBrush = new SolidBrush(drawBgColor))
                {
                    e.Graphics.FillRectangle(backBrush, 0, 0, btnHelp.Width, btnHelp.Height);
                }

                // 2. Малювання іконки з автоматичним масштабуванням
                if (infoIcon != null)
                {
                    // Налаштування високої якості рендерингу зображення
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    // Розраховуємо відступ (10% від розміру кнопки), щоб іконка не липла до країв
                    int paddingX = (int)(btnHelp.Width * 0.10);
                    int paddingY = (int)(btnHelp.Height * 0.10);

                    // Визначаємо область для малювання (автоматично масштабується під DPI програми)
                    Rectangle destRect = new Rectangle(
                        paddingX,
                        paddingY,
                        btnHelp.Width - (paddingX * 2),
                        btnHelp.Height - (paddingY * 2)
                    );

                    e.Graphics.DrawImage(infoIcon, destRect);
                }
                else
                {
                    // Резервний варіант на випадок відсутності ресурсу
                    using (Font fallbackFont = new Font("Segoe UI", btnHelp.Height * 0.4f, FontStyle.Bold))
                    using (Brush textBrush = new SolidBrush(btnHelp.ForeColor))
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        e.Graphics.DrawString("?", fallbackFont, textBrush, new RectangleF(0, 0, btnHelp.Width, btnHelp.Height), sf);
                    }
                }
            };

            btnHelp.Click += BtnHelp_Click;
            Controls.Add(btnHelp);

            // 1. Блок: Мова інтерфейсу (Додаємо OwnerDrawFixed)
            lblLang = new Label();
            cbLang = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                DrawMode = DrawMode.OwnerDrawFixed // Звільняє висоту комбобоксу від обмежень ОС
            };
            // Додаємо подію, щоб текст у комбобоксі малювався по центру (обов'язково для OwnerDrawFixed)
            cbLang.DrawItem += (s, e) =>
            {
                if (e.Index < 0)
                {
                    return;
                }
                e.DrawBackground();
                TextRenderer.DrawText(e.Graphics, cbLang.Items[e.Index].ToString(), cbLang.Font, e.Bounds, cbLang.ForeColor, TextFormatFlags.VerticalCenter);
                e.DrawFocusRectangle();
            };
            cbLang.Items.AddRange(new object[] { "English", "Українська", "Русский" });
            cbLang.SelectedIndexChanged += (s, e) => { ApplyLocalization(); };
            Controls.AddRange(new Control[] { lblLang, cbLang });

            // 2. Блок: Назва пункту контекстного меню (Вмикаємо Multiline)
            lblMenu = new Label();
            txtMenu = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true // Звільняє висоту текстового поля
            };
            Controls.AddRange(new Control[] { lblMenu, txtMenu });

            // 3. Блок: Формат вихідного документа (Додаємо OwnerDrawFixed)
            lblFormat = new Label();
            cbFormat = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            cbFormat.DrawItem += (s, e) =>
            {
                if (e.Index < 0)
                {
                    return;
                }
                e.DrawBackground();
                TextRenderer.DrawText(e.Graphics, cbFormat.Items[e.Index].ToString(), cbFormat.Font, e.Bounds, cbFormat.ForeColor, TextFormatFlags.VerticalCenter);
                e.DrawFocusRectangle();
            };
            cbFormat.Items.AddRange(new object[] { "EPUB2", "KEPUB", "EPUB3", "AZW8", "KFX", "PDF", "TXT", "MD" });
            Controls.AddRange(new Control[] { lblFormat, cbFormat });

            // 4. Блок: Папка для збереження результату
            chkFolder = new CheckBox();
            txtFolder = new TextBox { BorderStyle = BorderStyle.FixedSingle };

            btnFolderBrowse = new Button
            {
                Text = "", // Порожній текст, малюємо вручну
                FlatStyle = FlatStyle.Flat
            };
            btnFolderBrowse.FlatAppearance.BorderSize = 0; // Ховаємо стандартну рамку

            // Відстеження наведення миші для ефекту підсвічування
            bool isOutFolderHovered = false;
            btnFolderBrowse.MouseEnter += (s, e) => { isOutFolderHovered = true; btnFolderBrowse.Invalidate(); };
            btnFolderBrowse.MouseLeave += (s, e) => { isOutFolderHovered = false; btnFolderBrowse.Invalidate(); };

            // Використовуємо ту саму іконку папки з ресурсів
            Image outFolderIcon = Properties.Resources.folder;

            // Динамічне малювання іконки
            btnFolderBrowse.Paint += (s, e) =>
            {
                // 1. Визначаємо колір фону з урахуванням наведення миші
                Color baseBgColor = btnFolderBrowse.BackColor;
                Color drawBgColor = baseBgColor;

                if (isOutFolderHovered && btnFolderBrowse.Enabled) // Підсвічуємо лише якщо кнопка активна
                {
                    bool isDark = baseBgColor.R < 128;
                    drawBgColor = isDark
                        ? Color.FromArgb(baseBgColor.R + 25, baseBgColor.G + 25, baseBgColor.B + 25)
                        : Color.FromArgb(baseBgColor.R - 20, baseBgColor.G - 20, baseBgColor.B - 20);
                }

                // Заливка фону кнопки
                using (Brush backBrush = new SolidBrush(drawBgColor))
                {
                    e.Graphics.FillRectangle(backBrush, 0, 0, btnFolderBrowse.Width, btnFolderBrowse.Height);
                }

                // 2. Малювання іконки з автоматичним масштабуванням 100-200%
                if (outFolderIcon != null)
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    // Якщо кнопка вимкнена (Enabled = false), робимо іконку напівпрозорою
                    if (!btnFolderBrowse.Enabled)
                    {
                        float[][] ptsArray = {
                                  new float[] {1, 0, 0, 0, 0},
                                  new float[] {0, 1, 0, 0, 0},
                                  new float[] {0, 0, 1, 0, 0},
                                  new float[] {0, 0, 0, 0.4f, 0}, // 40% непрозорості
                                  new float[] {0, 0, 0, 0, 1}
                                             };
                        using (ImageAttributes imageAttributes = new ImageAttributes())
                        {
                            imageAttributes.SetColorMatrix(new ColorMatrix(ptsArray));

                            int paddingX = (int)(btnFolderBrowse.Width * 0.10);
                            int paddingY = (int)(btnFolderBrowse.Height * 0.10);
                            Rectangle destRect = new Rectangle(paddingX, paddingY, btnFolderBrowse.Width - (paddingX * 2), btnFolderBrowse.Height - (paddingY * 2));

                            e.Graphics.DrawImage(outFolderIcon, destRect, 0, 0, outFolderIcon.Width, outFolderIcon.Height, GraphicsUnit.Pixel, imageAttributes);
                            return;
                        }
                    }

                    // Малювання активної іконки з відступом 10%
                    int pX = (int)(btnFolderBrowse.Width * 0.10);
                    int pY = (int)(btnFolderBrowse.Height * 0.10);
                    Rectangle dRect = new Rectangle(pX, pY, btnFolderBrowse.Width - (pX * 2), btnFolderBrowse.Height - (pY * 2));

                    e.Graphics.DrawImage(outFolderIcon, dRect);
                }
                else
                {
                    // Резервний варіант
                    using (Font fallbackFont = new Font("Segoe UI", btnFolderBrowse.Height * 0.4f, FontStyle.Bold))
                    using (Brush textBrush = new SolidBrush(btnFolderBrowse.ForeColor))
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        e.Graphics.DrawString("...", fallbackFont, textBrush, new RectangleF(0, 0, btnFolderBrowse.Width, btnFolderBrowse.Height), sf);
                    }
                }
            };

            // Обов'язково викликаємо Invalidate(), щоб кнопка миттєво перемальовувалась (ставала напівпрозорою чи активною) при зміні чекбокса
            chkFolder.CheckedChanged += (s, e) => { txtFolder.Enabled = btnFolderBrowse.Enabled = chkFolder.Checked; btnFolderBrowse.Invalidate(); };

            btnFolderBrowse.Click += (s, e) =>
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    if (fbd.ShowDialog() == DialogResult.OK) { txtFolder.Text = fbd.SelectedPath; }
                }
            };
            Controls.AddRange(new Control[] { chkFolder, txtFolder, btnFolderBrowse });


            // 5. Блок: Конфігураційний файл (.yaml)
            chkConfig = new CheckBox();
            txtConfig = new TextBox { BorderStyle = BorderStyle.FixedSingle };

            btnConfigBrowse = new Button
            {
                Text = "", // Порожній текст, малюємо картинку вручну
                FlatStyle = FlatStyle.Flat
            };
            btnConfigBrowse.FlatAppearance.BorderSize = 0; // Ховаємо стандартну рамку для краси

            // Відстеження наведення миші для ефекту підсвічування
            bool isFolderHovered = false;
            btnConfigBrowse.MouseEnter += (s, e) => { isFolderHovered = true; btnConfigBrowse.Invalidate(); };
            btnConfigBrowse.MouseLeave += (s, e) => { isFolderHovered = false; btnConfigBrowse.Invalidate(); };

            // Завантажуємо іконку папки з ресурсів
            Image folderIcon = Properties.Resources.folder;

            // Динамічне малювання іконки папки
            btnConfigBrowse.Paint += (s, e) =>
            {
                // 1. Визначаємо колір фону з урахуванням наведення миші
                Color baseBgColor = btnConfigBrowse.BackColor;
                Color drawBgColor = baseBgColor;

                if (isFolderHovered && btnConfigBrowse.Enabled) // Підсвічуємо лише якщо кнопка активна
                {
                    bool isDark = baseBgColor.R < 128;
                    drawBgColor = isDark
                        ? Color.FromArgb(baseBgColor.R + 25, baseBgColor.G + 25, baseBgColor.B + 25)
                        : Color.FromArgb(baseBgColor.R - 20, baseBgColor.G - 20, baseBgColor.B - 20);
                }

                // Заливка фону кнопки
                using (Brush backBrush = new SolidBrush(drawBgColor))
                {
                    e.Graphics.FillRectangle(backBrush, 0, 0, btnConfigBrowse.Width, btnConfigBrowse.Height);
                }

                // 2. Малювання іконки папки з автоматичним масштабуванням 100-200%
                if (folderIcon != null)
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    // Потужний захист: якщо кнопка вимкнена (Enabled = false), малюємо іконку напівпрозорою
                    if (!btnConfigBrowse.Enabled)
                    {
                        // Створюємо матрицю для знебарвлення/напівпрозорості
                        float[][] ptsArray = {
                                  new float[] {1, 0, 0, 0, 0},
                                  new float[] {0, 1, 0, 0, 0},
                                  new float[] {0, 0, 1, 0, 0},
                                  new float[] {0, 0, 0, 0.4f, 0}, // 40% непрозорості для ефекту Disabled
                                  new float[] {0, 0, 0, 0, 1}
                                  };
                        using (ImageAttributes imageAttributes = new ImageAttributes())
                        {
                            imageAttributes.SetColorMatrix(new ColorMatrix(ptsArray));

                            int paddingX = (int)(btnConfigBrowse.Width * 0.10);
                            int paddingY = (int)(btnConfigBrowse.Height * 0.10);
                            Rectangle destRect = new Rectangle(paddingX, paddingY, btnConfigBrowse.Width - (paddingX * 2), btnConfigBrowse.Height - (paddingY * 2));

                            e.Graphics.DrawImage(folderIcon, destRect, 0, 0, folderIcon.Width, folderIcon.Height, GraphicsUnit.Pixel, imageAttributes);
                            return; // Виходимо, щоб не малювати звичайну іконку поверх
                        }
                    }

                    // Малювання звичайної активної іконки з відступом 10%
                    int pX = (int)(btnConfigBrowse.Width * 0.10);
                    int pY = (int)(btnConfigBrowse.Height * 0.10);
                    Rectangle dRect = new Rectangle(pX, pY, btnConfigBrowse.Width - (pX * 2), btnConfigBrowse.Height - (pY * 2));

                    e.Graphics.DrawImage(folderIcon, dRect);
                }
                else
                {
                    // Резервний текстовий варіант
                    using (Font fallbackFont = new Font("Segoe UI", btnConfigBrowse.Height * 0.4f, FontStyle.Bold))
                    using (Brush textBrush = new SolidBrush(btnConfigBrowse.ForeColor))
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        e.Graphics.DrawString("...", fallbackFont, textBrush, new RectangleF(0, 0, btnConfigBrowse.Width, btnConfigBrowse.Height), sf);
                    }
                }
            };

            chkConfig.CheckedChanged += (s, e) => { txtConfig.Enabled = btnConfigBrowse.Enabled = chkConfig.Checked; btnConfigBrowse.Invalidate(); };
            btnConfigBrowse.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "YAML config|*.yaml;*.yml" })
                {
                    if (ofd.ShowDialog() == DialogResult.OK) { txtConfig.Text = ofd.FileName; }
                }
            };
            Controls.AddRange(new Control[] { chkConfig, txtConfig, btnConfigBrowse });

            // 6. Блок опцій автоматизації
            chkOverwrite = new CheckBox { FlatStyle = FlatStyle.Flat };
            chkDeleteMain = new CheckBox { FlatStyle = FlatStyle.Flat };
            chkDeleteSub = new CheckBox { FlatStyle = FlatStyle.Flat };
            chkMinimize = new CheckBox { FlatStyle = FlatStyle.Flat };
            chkHideProgress = new CheckBox { FlatStyle = FlatStyle.Flat };

            chkDeleteMain.CheckedChanged += (s, e) =>
            {
                chkDeleteSub.Enabled = chkDeleteMain.Checked;
                if (!chkDeleteMain.Checked)
                {
                    chkDeleteSub.Checked = false;
                }
            };
            Controls.AddRange(new Control[] { chkOverwrite, chkDeleteMain, chkDeleteSub, chkMinimize, chkHideProgress });

            // 7. Кнопка інтеграції/деінтеграції в провідник Windows
            btnIntegrate = new Button { FlatStyle = FlatStyle.Flat };
            btnIntegrate.Click += BtnIntegrate_Click;
            Controls.Add(btnIntegrate);

            // Нижня панель управління (Зміна теми, збереження та скасування)
            btnThemeToggle = new Button
            {
                Text = "", // Порожній текст
                FlatStyle = FlatStyle.Flat
            };
            btnThemeToggle.FlatAppearance.BorderSize = 0; // Ховаємо стандартну рамку

            // Перемінна для відстеження, чи наведено курсор на кнопку
            bool isHovered = false;

            // Події для ефекту підсвічування при наведенні
            btnThemeToggle.MouseEnter += (s, e) => { isHovered = true; btnThemeToggle.Invalidate(); };
            btnThemeToggle.MouseLeave += (s, e) => { isHovered = false; btnThemeToggle.Invalidate(); };

            // БЕРЕМО ІКОНКУ НАПРЯМУ З РЕСУРСІВ ПРОЕКТУ
            Image themeIcon = Properties.Resources.day_night;

            // Динамічне малювання
            btnThemeToggle.Paint += (s, e) =>
            {
                // 1. Визначаємо колір фону (якщо миша наведена — робимо колір трохи світлішим/темнішим)
                Color baseBgColor = btnThemeToggle.BackColor;
                Color drawBgColor = baseBgColor;

                if (isHovered)
                {
                    bool isDark = baseBgColor.R < 128;
                    drawBgColor = isDark
                        ? Color.FromArgb(baseBgColor.R + 25, baseBgColor.G + 25, baseBgColor.B + 25)
                        : Color.FromArgb(baseBgColor.R - 20, baseBgColor.G - 20, baseBgColor.B - 20);
                }

                // Заливка фону кнопки
                using (Brush backBrush = new SolidBrush(drawBgColor))
                {
                    e.Graphics.FillRectangle(backBrush, 0, 0, btnThemeToggle.Width, btnThemeToggle.Height);
                }

                // 2. Малювання іконки з автоматичним масштабуванням 100-200%
                if (themeIcon != null)
                {
                    // Налаштування високої якості рендерингу зображення
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    // Розраховуємо відступ 10%, як і в першій кнопці, для ідеального вигляду
                    int paddingX = (int)(btnThemeToggle.Width * 0.10);
                    int paddingY = (int)(btnThemeToggle.Height * 0.10);

                    // Визначаємо область для малювання (автоматично масштабується під розміри кнопки)
                    Rectangle destRect = new Rectangle(
                        paddingX,
                        paddingY,
                        btnThemeToggle.Width - (paddingX * 2),
                        btnThemeToggle.Height - (paddingY * 2)
                    );

                    e.Graphics.DrawImage(themeIcon, destRect);
                }
                else
                {
                    // Резервний варіант: якщо ресурс не знайдено, малюємо старий символ ◐
                    using (Font fallbackFont = new Font("Segoe UI", btnThemeToggle.Height * 0.5f, FontStyle.Bold))
                    using (Brush textBrush = new SolidBrush(btnThemeToggle.ForeColor))
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        e.Graphics.DrawString("\u25D0", fallbackFont, textBrush, new RectangleF(0, 0, btnThemeToggle.Width, btnThemeToggle.Height), sf);
                    }
                }
            };

            btnThemeToggle.Click += (s, e) =>
            {
                _settings.Theme = _settings.Theme == "Dark" ? "Light" : "Dark";
                ApplyTheme();
                if (infoTooltipForm != null && infoTooltipForm.Visible)
                {
                    infoTooltipForm.Close();
                }
            };
            Controls.Add(btnThemeToggle);


            btnOk = new Button { FlatStyle = FlatStyle.Flat };
            btnCancel = new Button { FlatStyle = FlatStyle.Flat };

            // --- ДОДАВАННЯ ГАРЯЧИХ КЛАВІШ ДЛЯ ГОЛОВНОГО ВІКНА ---
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            // --- ЗАХИСТ ВІД КОНФЛІКТУ ГАЛОЧОК НА ЕТАПІ НАТИСКАННЯ ОК ---
            btnOk.Click += (s, e) =>
            {
                if (chkMinimize.Checked && chkHideProgress.Checked)
                {
                    string lang = cbLang.SelectedItem != null ? cbLang.SelectedItem.ToString() : _settings.Language;
                    string warningText = Localization.Get(lang, "WarningText");
                    string warningTitle = Localization.Get(lang, "WarningTitle");
                    _ = ShowCustomMessageBox(warningText, warningTitle, buttons: MessageBoxButtons.OK);
                    return; // Блокуємо закриття вікна
                }

                SaveUiToSettings();
                _settings.Save();
                Close();
            };

            btnCancel.Click += (s, e) => { Close(); };
            Controls.AddRange(new Control[] { btnOk, btnCancel });

            LocationChanged += (s, e) => { UpdateHelpWindowPosition(); };

            // --- МАКСИМАЛЬНО КОМПАКТНИЙ ТА СИНХРОНІЗОВАНИЙ ВАРІАНТ ПОДІЇ LOAD ---
            Load += (s, e) =>
            {
                // 1. Обчислюємо точний масштаб DPI монітора
                float currentScale = Font.Height / 18f;

                // 2. МАКСИМАЛЬНО ЩІЛЬНІ ВІДСТУПИ (Зменшено для повної компактності)
                int blockMargin = (int)(8 * currentScale);       // Мінімізований простір МІЖ великими блоками
                int labelToFieldSpace = (int)(2 * currentScale);  // Відступ від тексту до його поля

                int labelHeight = (int)(20 * currentScale); // Трохи зменшили висоту напису
                int fieldHeight = (int)(24 * currentScale); // Нова витончена висота полів (-4 пікселі) 
                int checkBoxHeight = (int)(22 * currentScale);
                int spaceBetweenCheckboxes = (int)(3 * currentScale);

                //  ШИРИНА ПРОГРАМИ
                // Базова внутрішня ширина тепер 380 замість 480. Форма стане витонченішою!
                int calculatedWidth = (int)(380 * currentScale);
                ClientSize = new Size(calculatedWidth, ClientSize.Height);

                // Розраховуємо нові ідеально симетричні відступи від країв програми
                int xLeft = (int)(15 * currentScale); // Тонкі акуратні бічні поля по 15 пікселів
                int xRightField = ClientSize.Width - xLeft;
                int fieldWidth = xRightField - xLeft;

                // Верхня стартова точка (25 щоб не налазити на кнопку "інформація")
                int startY = (int)(25 * currentScale);

                // ==========================================
                // БЛОК 1: Мова інтерфейсу
                // ==========================================
                lblLang.SetBounds(xLeft, startY, fieldWidth, labelHeight);

                // КОРЕКЦІЯ: задаємо висоту елемента списку з вирахуванням 6 пікселів на системні рамки
                cbLang.ItemHeight = fieldHeight - 6;
                cbLang.SetBounds(xLeft, lblLang.Bottom + labelToFieldSpace, fieldWidth, fieldHeight);

                // ==========================================
                // БЛОК 2: Назва пункту контекстного меню (працює ідеально)
                // ==========================================
                lblMenu.SetBounds(xLeft, cbLang.Bottom + blockMargin, fieldWidth, labelHeight);
                txtMenu.SetBounds(xLeft, lblMenu.Bottom + labelToFieldSpace, fieldWidth, fieldHeight);

                // ==========================================
                // БЛОК 3: Формат вихідного документа
                // ==========================================
                lblFormat.SetBounds(xLeft, txtMenu.Bottom + blockMargin, fieldWidth, labelHeight);

                // КОРЕКЦІЯ: аналогічно задаємо ItemHeight для формату
                cbFormat.ItemHeight = fieldHeight - 6;
                cbFormat.SetBounds(xLeft, lblFormat.Bottom + labelToFieldSpace, fieldWidth, fieldHeight);

                // ==========================================
                // БЛОК 4: Папка для збереження результату
                // ==========================================
                chkFolder.SetBounds(xLeft, cbFormat.Bottom + blockMargin, fieldWidth, checkBoxHeight);

                int browseBtnWidth = (int)(38 * currentScale);
                int folderTxtWidth = fieldWidth - browseBtnWidth - (int)(8 * currentScale);

                // КРИТИЧНЕ ВИПРАВЛЕННЯ: Створюємо TextBox з увімкненим Multiline прямо під час налаштування геометрії,
                // це змусить його СУВОРО підкорятися висоті fieldHeight і не стискатися системою!
                txtFolder.Multiline = true;
                txtFolder.SetBounds(xLeft, chkFolder.Bottom + labelToFieldSpace, folderTxtWidth, fieldHeight);

                // Тепер кнопка і поле мають абсолютно ОДНАКОВУ висоту fieldHeight і позицію Top,
                // вони стануть ідеальними близнюками без жодних поправок та зміщень!
                btnFolderBrowse.SetBounds(xRightField - browseBtnWidth, txtFolder.Top, browseBtnWidth, fieldHeight);

                // ==========================================
                // БЛОК 5: Конфігураційний файл (.yaml)
                // ==========================================
                chkConfig.SetBounds(xLeft, txtFolder.Bottom + blockMargin, fieldWidth, checkBoxHeight);

                // Аналогічно вмикаємо Multiline для поля конфігу
                txtConfig.Multiline = true;
                txtConfig.SetBounds(xLeft, chkConfig.Bottom + labelToFieldSpace, folderTxtWidth, fieldHeight);

                // Виставляємо кнопці конфігу ідентичні з полем координати та висоту
                btnConfigBrowse.SetBounds(xRightField - browseBtnWidth, txtConfig.Top, browseBtnWidth, fieldHeight);

                // ==========================================
                // БЛОК 6: Опції автоматизації (Чекбокси)
                // ==========================================
                chkOverwrite.SetBounds(xLeft, txtConfig.Bottom + blockMargin, fieldWidth, checkBoxHeight);

                chkDeleteMain.SetBounds(xLeft, chkOverwrite.Bottom + spaceBetweenCheckboxes, fieldWidth, checkBoxHeight);

                int xSubLeft = (int)(38 * currentScale); // Зменшений зсув дерева ієрархії для компактності
                chkDeleteSub.SetBounds(xSubLeft, chkDeleteMain.Bottom + spaceBetweenCheckboxes, xRightField - xSubLeft, checkBoxHeight);

                chkMinimize.SetBounds(xLeft, chkDeleteSub.Bottom + spaceBetweenCheckboxes, fieldWidth, checkBoxHeight);

                chkHideProgress.SetBounds(xSubLeft, chkMinimize.Bottom + spaceBetweenCheckboxes, xRightField - xSubLeft, checkBoxHeight);

                // ==========================================
                // БЛОК 7: КНОПКА ІНТЕГРАЦІЇ (КРИТИЧНЕ ВИПРАВЛЕННЯ ЧЕРЕЗ SETBOUNDS)
                // ==========================================
                // Використовуємо SetBounds БЕЗ початкового закруглення — це змусить кнопку 100% відмасштабуватися і стати на місце!
                int integrateY = chkHideProgress.Bottom + blockMargin;
                int integrateHeight = (int)(34 * currentScale);
                btnIntegrate.SetBounds(xLeft, integrateY, fieldWidth, integrateHeight);

                // ==========================================
                // НИЖНЯ ПАНЕЛЬ УПРАВЛІННЯ (Тема, ОК, Скасувати)
                // ==========================================
                int finalButtonsY = btnIntegrate.Bottom + blockMargin + (int)(6 * currentScale);

                btnThemeToggle.SetBounds(xLeft, finalButtonsY, (int)(40 * currentScale), (int)(30 * currentScale));

                int btnWidth = (int)(95 * currentScale);  // Кнопки стали трішки компактнішими
                int btnHeight = (int)(30 * currentScale);

                btnCancel.SetBounds(xRightField - btnWidth, finalButtonsY, btnWidth, btnHeight);
                btnOk.SetBounds(btnCancel.Left - btnWidth - (int)(8 * currentScale), finalButtonsY, btnWidth, btnHeight);

                // КНОПКА ДОВІДКИ (i) — чітко у верхньому правому кутку
                btnHelp.SetBounds(xRightField - (int)(30 * currentScale), (int)(8 * currentScale), (int)(30 * currentScale), (int)(30 * currentScale));

                // =========================================================================
                // ГАРАНТОВАНИЙ ПЕРЕЗАПУСК ЗАОКРУГЛЕННЯ (Строго після того, як ВСІ розміри змінено!)
                // =========================================================================
                MakeButtonRounded(btnFolderBrowse, 4);
                MakeButtonRounded(btnConfigBrowse, 4);
                MakeButtonRounded(btnHelp, 6);
                MakeButtonRounded(btnThemeToggle, 6);
                MakeButtonRounded(btnIntegrate, 6); // Закруглюємо велику кнопку строго ТУТ, коли її розмір вже ідеальний
                MakeButtonRounded(btnOk, 6);
                MakeButtonRounded(btnCancel, 6);

                // ==========================================
                // ФІНАЛЬНИЙ РОЗРАХУНОК ВЕРТИКАЛЬНОГО РОЗМІРУ ВІКНА
                // ==========================================
                paddingBottom = (int)(15 * currentScale); // Зменшили нижній пустий відступ
                finalHeight = btnOk.Bottom + paddingBottom;

                // Призначаємо фінальний, ультра-компактний розмір всієї форми
                ClientSize = new Size(calculatedWidth, finalHeight);

                // Повертаємо вікно ідеально по центру екрана монітора
                CenterToScreen();
            };
        }
        // Графічний метод створення закруглених кутів для кнопок через зміну їхнього регіону
        private void MakeButtonRounded(Button btn, int radius)
        {
            // Крок 1. Надійний Region (без змін)
            using (GraphicsPath path = new GraphicsPath())
            {
                float r = radius;
                path.AddArc(0, 0, r * 2, r * 2, 180, 90);
                path.AddArc(btn.Width - (r * 2), 0, r * 2, r * 2, 270, 90);
                path.AddArc(btn.Width - (r * 2), btn.Height - (r * 2), r * 2, r * 2, 0, 90);
                path.AddArc(0, btn.Height - (r * 2), r * 2, r * 2, 90, 90);
                path.CloseAllFigures();

                btn.Region = new Region(path);
            }

            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;

            // Крок 2. Малювання рамки з розширенням назовні для світлої теми
            btn.Paint += (s, ev) =>
            {
                ev.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                bool isDarkTheme = _settings.Theme == "Dark";

                if (isDarkTheme)
                {
                    // ДЛЯ ТЕМНОЇ ТЕМИ: ваш перевірений ідеальний варіант (залишаємо без змін)
                    using (GraphicsPath buttonFramePath = new GraphicsPath())
                    {
                        float r = radius;
                        float startXY = 0.5f;
                        float sizeAdjustment = 1.0f;

                        buttonFramePath.AddArc(startXY, startXY, r * 2, r * 2, 180, 90);
                        buttonFramePath.AddArc(btn.Width - (r * 2) - sizeAdjustment, startXY, r * 2, r * 2, 270, 90);
                        buttonFramePath.AddArc(btn.Width - (r * 2) - sizeAdjustment, btn.Height - (r * 2) - sizeAdjustment, r * 2, r * 2, 0, 90);
                        buttonFramePath.AddArc(startXY, btn.Height - (r * 2) - sizeAdjustment, r * 2, r * 2, 90, 90);
                        buttonFramePath.CloseAllFigures();

                        Color btnBorderColor = btn.FlatAppearance.BorderColor != Color.Empty && btn.FlatAppearance.BorderColor != Color.Transparent
                            ? btn.FlatAppearance.BorderColor : btn.ForeColor;

                        using (Pen pen = new Pen(btnBorderColor, 1.2F))
                        {
                            ev.Graphics.DrawPath(pen, buttonFramePath);
                        }
                    }
                }
                else
                {
                    // ДЛЯ СВІТЛОЇ ТЕМИ: Розширюємо геометрію рамки назовні (startXY = 0)
                    using (GraphicsPath buttonFramePath = new GraphicsPath())
                    {
                        float r = radius;
                        float startXY = 0.0f; // Зміщуємо на самий край для розширення назовні
                        float sizeAdjustment = 0.0f; // Прибираємо стискання рамки

                        buttonFramePath.AddArc(startXY, startXY, r * 2, r * 2, 180, 90);
                        buttonFramePath.AddArc(btn.Width - (r * 2) - sizeAdjustment, startXY, r * 2, r * 2, 270, 90);
                        buttonFramePath.AddArc(btn.Width - (r * 2) - sizeAdjustment, btn.Height - (r * 2) - sizeAdjustment, r * 2, r * 2, 0, 90);
                        buttonFramePath.AddArc(startXY, btn.Height - (r * 2) - sizeAdjustment, r * 2, r * 2, 90, 90);
                        buttonFramePath.CloseAllFigures();

                        if (btn.ForeColor == Color.White)
                        {
                            // СИНІ АКЦЕНТНІ КНОПКИ ("Інтегрувати" та "ОК"):
                            // Шар 1: Товста підкладка кольору фону для зачистки бруду
                            using (Pen bgPen = new Pen(btn.BackColor, 2.5F))
                            {
                                ev.Graphics.DrawPath(bgPen, buttonFramePath);
                            }

                            // Шар 2: Збільшуємо товщину світлої рамки до 2.2F, щоб вона м'яко вийшла назовні
                            // Також трохи збільшили непрозорість (зі 140 до 160), щоб краще перекрити сині точки
                            using (Pen overlayPen = new Pen(Color.FromArgb(160, Color.White), 2.2F))
                            {
                                ev.Graphics.DrawPath(overlayPen, buttonFramePath);
                            }
                        }
                        else
                        {
                            // ЗВИЧАЙНІ КНОПКИ ("Тема", "Відмінити", "Довідка"):
                            Color btnBorderColor = btn.FlatAppearance.BorderColor != Color.Empty && btn.FlatAppearance.BorderColor != Color.Transparent
                                ? btn.FlatAppearance.BorderColor : Color.FromArgb(100, btn.ForeColor);

                            using (Pen pen = new Pen(btnBorderColor, 2.0F)) // Збільшили до 2.0F для ідеального згладжування
                            {
                                ev.Graphics.DrawPath(pen, buttonFramePath);
                            }
                        }
                    }
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
                    // Кнопка "і/?" має зберігати стиль звичайної кнопки або виділятися
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
        // Подія натискання на прямокутну кнопку зі знаком питання (i)
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

            // Ширина становитиме 65% від ширини головного вікна
            int calculatedWidth = (int)(Width * 0.65);

            // --- 1. СТВОРЮЄМО ЗАГОЛОВОК ЯК ЗВИЧАЙНИЙ ЕЛЕМЕНТ (БЕЗ DOCK) ---
            int titleHeight = 35; // Висота нашого кастомного заголовка
            Label titleLabel = new Label
            {
                Text = helpTitle,
                Location = new Point(0, 0),
                Width = calculatedWidth,
                Height = titleHeight,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = isDark ? Color.White : Color.Black,

                // КОЛІР ФОНУ ТЕПЕР ЗБІГАЄТЬСЯ З КОЛЬОРОМ РАМКИ (ТЕМНИЙ АБО СВІТЛИЙ СІРИЙ)
                BackColor = isDark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(180, 180, 180)
            };

            // --- 2. СТВОРЮЄМО ТЕКСТОВИЙ БЛОК (ЗСУВАЄМО НА ВИСОТУ ЗАГОЛОВКА) ---
            RichTextBox rtbHelp = new RichTextBox
            {
                Text = helpText,
                Location = new Point(14, titleHeight + 7), // 7 пікселів відступу зверху
                Width = calculatedWidth - 28, // Ширина тексту залежить від форми
                ForeColor = isDark ? Color.White : Color.Black,
                BackColor = infoTooltipForm.BackColor,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.None,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                TabStop = false
            };

            // ВИРІВНЮВАННЯ ПО ЦЕНТРУ: Виділяємо весь текст і задаємо йому центральне вирівнювання
            rtbHelp.SelectAll();
            rtbHelp.SelectionAlignment = HorizontalAlignment.Center;
            rtbHelp.DeselectAll(); // Знімаємо виділення, щоб текст не підсвічувався синім кольором

            // Забороняємо виділення тексту мишею та появу текстового курсору
            rtbHelp.MouseDown += (s, ev) => { _ = Focus(); };

            // Додаємо елементи на форму
            infoTooltipForm.Controls.Add(titleLabel);
            infoTooltipForm.Controls.Add(rtbHelp);

            // --- ДИНАМІЧНИЙ РОЗРАХУНОК ВИСОТИ ВІКНА ДОВІДКИ ---
            int lastCharIndex = rtbHelp.TextLength > 0 ? rtbHelp.TextLength - 1 : 0;
            Point lastCharPos = rtbHelp.GetPositionFromCharIndex(lastCharIndex);
            // Чиста висота тексту (+5px запас для нижніх хвостиків літер у, ц, щ, д)
            int textHeight = lastCharPos.Y + rtbHelp.Font.Height + 5;

            // Встановлюємо висоту елемента тексту
            rtbHelp.Height = textHeight;

            // Розраховуємо фінальну висоту форми (висота тексту + висота заголовка +  мінімальні відступи зверху та знизу)
            int calculatedHeight = titleHeight + rtbHelp.Height + 12;

            // Задаємо меншу мінімальну висоту, щоб форма могла бути компактнішою
            if (calculatedHeight < 60)
            {
                calculatedHeight = 60;
            }

            // Призначаємо динамічні розміри формі
            infoTooltipForm.Size = new Size(calculatedWidth, calculatedHeight);
            // --------------------------------------------------

            // ТЕПЕР КРАЇ ВІКНА-ДОВІДКИ ТЕЖ ЗАОКРУГЛЕНІ (Виконується після визначення точних розмірів)
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


            // Малюємо рамку знизу та справа (з заходом на половину радіуса на незадіяних кутах)
            infoTooltipForm.Paint += (s, ev) =>
            {
                ev.Graphics.SmoothingMode = SmoothingMode.AntiAlias; // Згладжування ліній
                Color borderColor = isDark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(180, 180, 180);
                using (Pen pen = new Pen(borderColor, 1))
                {
                    using (GraphicsPath framePath = new GraphicsPath())
                    {
                        // 1. Починаємо на ПРАВОМУ ВЕРХНЬОМУ куті (заходимо на половину радіуса)
                        // Починаємо вести лінію від середини верхнього закруглення вправо
                        framePath.AddArc(
                            infoTooltipForm.Width - (windowRadius * 2) - 1,
                            0,
                            windowRadius * 2,
                            windowRadius * 2,
                            270, 45 // Малюємо лише половину дуги (45 градусів замість 90)
                        );

                        // 2. Ведемо лінію вниз по ВСЬОМУ ПРАВОМУ КРАЮ форми
                        framePath.AddLine(
                            infoTooltipForm.Width - 1,
                            windowRadius,
                            infoTooltipForm.Width - 1,
                            infoTooltipForm.Height - windowRadius
                        );

                        // 3. Огинаємо ПРАВИЙ НИЖНІЙ кут повністю (на всі 90 градусів)
                        framePath.AddArc(
                            infoTooltipForm.Width - (windowRadius * 2) - 1,
                            infoTooltipForm.Height - (windowRadius * 2) - 1,
                            windowRadius * 2,
                            windowRadius * 2,
                            0, 90
                        );

                        // 4. Ведемо лінію вліво по ВСЬОМУ НИЖНЬОМУ КРАЮ форми
                        framePath.AddLine(
                            infoTooltipForm.Width - windowRadius,
                            infoTooltipForm.Height - 1,
                            windowRadius,
                            infoTooltipForm.Height - 1
                        );

                        // 5. Огинаємо ЛІВИЙ НИЖНІЙ кут повністю (на всі 90 градусів)
                        framePath.AddArc(
                            0,
                            infoTooltipForm.Height - (windowRadius * 2) - 1,
                            windowRadius * 2,
                            windowRadius * 2,
                            90, 90
                        );

                        // 6. Завершуємо шлях на ЛІВОМУ боці, піднявшись лише до середини кута (на висоту одного радіуса)
                        framePath.AddLine(
                            0,
                            infoTooltipForm.Height - windowRadius,
                            0,
                            infoTooltipForm.Height - windowRadius
                        );

                        // Малюємо отриману Г-подібну рамку
                        ev.Graphics.DrawPath(pen, framePath);
                    }
                }
            };

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
