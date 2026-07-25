using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace fb2cngGUI
{
    public partial class Form1 : Form
    {
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

            // --- 1. ІНІЦІАЛІЗАЦІЯ ЕЛЕМЕНТІВ ---
            btnHelp = new Button { FlatStyle = FlatStyle.Flat };
            SetupIconRenderer(btnHelp, Properties.Resources.icon_info);
            btnHelp.Click += BtnHelp_Click;

            cbLang = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, DrawMode = DrawMode.OwnerDrawFixed };
            cbLang.Items.AddRange(["English", "Українська", "Русский"]);
            cbLang.DrawItem += ComboBox_CustomDraw;
            cbLang.SelectedIndexChanged += (s, e) => ApplyLocalization();

            cbFormat = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, DrawMode = DrawMode.OwnerDrawFixed };
            cbFormat.Items.AddRange(["EPUB2", "KEPUB", "EPUB3", "AZW8", "KFX", "PDF", "TXT", "MD"]);
            cbFormat.DrawItem += ComboBox_CustomDraw;

            txtMenu = new TextBox { BorderStyle = BorderStyle.FixedSingle, Multiline = true };
            txtFolder = new TextBox { BorderStyle = BorderStyle.FixedSingle, Multiline = true };
            txtConfig = new TextBox { BorderStyle = BorderStyle.FixedSingle, Multiline = true };

            btnFolderBrowse = new Button { FlatStyle = FlatStyle.Flat };
            SetupIconRenderer(btnFolderBrowse, Properties.Resources.folder);
            btnFolderBrowse.Click += (s, e) =>
            {
                using FolderBrowserDialog fbd = new();
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtFolder.Text = fbd.SelectedPath;
                }
            };

            btnConfigBrowse = new Button { FlatStyle = FlatStyle.Flat };
            SetupIconRenderer(btnConfigBrowse, Properties.Resources.folder);
            btnConfigBrowse.Click += (s, e) =>
            {
                using OpenFileDialog ofd = new() { Filter = "YAML config|*.yaml;*.yml" };
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtConfig.Text = ofd.FileName;
                }
            };

            btnThemeToggle = new Button { FlatStyle = FlatStyle.Flat };
            SetupIconRenderer(btnThemeToggle, Properties.Resources.day_night);
            btnThemeToggle.Click += (s, e) =>
            {
                _settings.Theme = _settings.Theme == "Dark" ? "Light" : "Dark";
                ApplyTheme();
                if (infoTooltipForm != null && infoTooltipForm.Visible)
                {
                    infoTooltipForm.Close();
                }
            };

            btnGui = new Button { FlatStyle = FlatStyle.Flat };
            string configuratorPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fb2cng_Configurator.exe");
            btnGui.Visible = File.Exists(configuratorPath);
            SetupIconRenderer(btnGui, Properties.Resources.icon_yaml);
            btnGui.Click += (s, e) =>
            {
                try { _ = Process.Start(new ProcessStartInfo { FileName = configuratorPath, UseShellExecute = true }); } catch { }
            };

            lblLang = new Label(); lblMenu = new Label(); lblFormat = new Label();
            chkFolder = new CheckBox(); chkConfig = new CheckBox(); chkOverwrite = new CheckBox();
            chkDeleteMain = new CheckBox(); chkDeleteSub = new CheckBox();
            lblDeleteSubText = new Label { Cursor = Cursors.Hand, AutoSize = false };
            chkMinimize = new CheckBox(); chkHideProgress = new CheckBox();

            chkFolder.CheckedChanged += (s, e) => { txtFolder.Enabled = btnFolderBrowse.Enabled = chkFolder.Checked; btnFolderBrowse.Invalidate(); };
            chkConfig.CheckedChanged += (s, e) => { txtConfig.Enabled = btnConfigBrowse.Enabled = chkConfig.Checked; btnConfigBrowse.Invalidate(); };
            lblDeleteSubText.Click += (s, e) => { if (chkDeleteSub.Enabled) { chkDeleteSub.Checked = !chkDeleteSub.Checked; } };
            chkDeleteMain.CheckedChanged += (s, e) =>
            {
                chkDeleteSub.Enabled = chkDeleteMain.Checked;
                if (!chkDeleteMain.Checked)
                {
                    chkDeleteSub.Checked = false;
                }

                ApplyTheme();
            };

            btnIntegrate = new Button { FlatStyle = FlatStyle.Flat };
            btnIntegrate.Click += BtnIntegrate_Click;
            btnOk = new Button { FlatStyle = FlatStyle.Flat };
            btnOk.Click += (s, e) =>
            {
                if (chkMinimize.Checked && chkHideProgress.Checked)
                {
                    string currentLang = cbLang.SelectedItem?.ToString() ?? _settings.Language;

                    _ = ShowCustomMessageBox(
                        Localization.Get(currentLang, "WarningText"),
                        Localization.Get(currentLang, "WarningTitle"),
                        MessageBoxButtons.OK
                         );
                    return;
                }
                SaveUiToSettings(); 
                _settings.Save(); 
                Close();
            };
            btnCancel = new Button { FlatStyle = FlatStyle.Flat };
            btnCancel.Click += (s, e) => Close();

            AcceptButton = btnOk; CancelButton = btnCancel;
            Controls.AddRange([lblLang, cbLang, lblMenu, txtMenu, lblFormat, cbFormat, chkFolder, txtFolder, btnFolderBrowse, chkConfig, txtConfig, btnConfigBrowse, chkOverwrite, chkDeleteMain, chkDeleteSub, lblDeleteSubText, chkMinimize, chkHideProgress, btnIntegrate, btnThemeToggle, btnGui, btnOk, btnCancel, btnHelp]);

            Load += Form1_Load;
            LocationChanged += (s, e) => UpdateHelpWindowPosition();
        }

        // --- 2. МАЛЮВАННЯ СПИСКІВ  ---
        private void ComboBox_CustomDraw(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || sender is not ComboBox cb)
            {
                return;
            }

            bool isDark = _settings.Theme == "Dark";
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            Color backColor = isSelected
                ? (isDark ? Color.FromArgb(70, 70, 70) : Color.FromArgb(245, 245, 245))
                : cb.BackColor;

            using (Brush b = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(b, e.Bounds);
            }

            TextRenderer.DrawText(e.Graphics, cb.Items[e.Index]?.ToString() ?? "", cb.Font ?? Font, e.Bounds, cb.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            if (isSelected)
            {
                e.DrawFocusRectangle();
            }
        }

        // --- 3. МАЛЮВАННЯ ІКОНОК (10% ВІДСТУП, БЕЗ ОБРІЗКИ) ---
        private static void SetupIconRenderer(Button btn, Image icon)
        {
            btn.FlatAppearance.BorderSize = 0;
            bool hovered = false;
            btn.MouseEnter += (s, e) => { hovered = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { hovered = false; btn.Invalidate(); };

            btn.Paint += (s, e) =>
            {
                Color bgColor = btn.BackColor;
                if (hovered && btn.Enabled)
                {
                    bool dark = bgColor.R < 128;
                    bgColor = dark ? Color.FromArgb(bgColor.R + 25, bgColor.G + 25, bgColor.B + 25) : Color.FromArgb(bgColor.R - 20, bgColor.G - 20, bgColor.B - 20);
                }
                using (Brush b = new SolidBrush(bgColor))
                {
                    e.Graphics.FillRectangle(b, 0, 0, btn.Width, btn.Height);
                }

                if (icon != null)
                {
                    Graphics g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    using ImageAttributes ia = new();
                    if (!btn.Enabled)
                    {
                        float[][] m = [[1, 0, 0, 0, 0], [0, 1, 0, 0, 0], [0, 0, 1, 0, 0], [0, 0, 0, 0.4f, 0], [0, 0, 0, 0, 1]];
                        ia.SetColorMatrix(new ColorMatrix(m));
                    }

                    // 10% відступ (0.1f)
                    float pX = btn.Width * 0.1f;
                    float pY = btn.Height * 0.1f;
                    RectangleF dest = new(pX, pY, btn.Width - (pX * 2), btn.Height - (pY * 2));

                    if (!btn.Enabled)
                    {
                        g.DrawImage(icon, Rectangle.Round(dest), 0, 0, icon.Width, icon.Height, GraphicsUnit.Pixel, ia);
                    }
                    else
                    {
                        g.DrawImage(icon, dest);
                    }
                }
            };
        }

        // --- 4. МАКСИМАЛЬНО КОМПАКТНИЙ ТА СИНХРОНІЗОВАНИЙ ВАРІАНТ ПОДІЇ LOAD ---
        private void Form1_Load(object? sender, EventArgs e)
        {
            try
            {
                Process current = Process.GetCurrentProcess();
                // Отримуємо список усіх процесів з такою ж назвою
                Process[] processes = Process.GetProcessesByName(current.ProcessName);

                if (processes.Length > 1)
                {
                    bool foundGui = false;

                    foreach (Process process in processes)
                    {
                        // УМОВА: Це не поточний процес ТА у процесу є реальне вікно (MainWindowHandle)
                        // Це дозволяє відрізнити головне вікно налаштувань від фонових конвертерів
                        if (process.Id != current.Id && process.MainWindowHandle != IntPtr.Zero)
                        {
                            IntPtr hWnd = process.MainWindowHandle;

                            // 1. Якщо вікно було згорнуте в панель задач — розгортаємо його (9 = SW_RESTORE)
                            if (Win32Api.IsIconic(hWnd))
                            {
                                _ = Win32Api.ShowWindow(hWnd, 9);
                            }

                            // 2. Магія фокусування: отримуємо ID потоків для "крадіжки" фокусу у Windows
                            uint foregroundThreadId = Win32Api.GetWindowThreadProcessId(Win32Api.GetForegroundWindow(), IntPtr.Zero);
                            uint currentThreadId = Win32Api.GetCurrentThreadId();

                            // 3. Якщо фокус зараз у іншої програми, тимчасово склеюємо потоки введення
                            if (foregroundThreadId != currentThreadId && foregroundThreadId != 0)
                            {
                                _ = Win32Api.AttachThreadInput(currentThreadId, foregroundThreadId, true);
                                _ = Win32Api.SetForegroundWindow(hWnd);
                                _ = Win32Api.SetActiveWindow(hWnd);
                                _ = Win32Api.AttachThreadInput(currentThreadId, foregroundThreadId, false);
                            }
                            else
                            {
                                _ = Win32Api.SetForegroundWindow(hWnd);
                                _ = Win32Api.SetActiveWindow(hWnd);
                            }

                            foundGui = true;
                            break; // Ми знайшли та активували головне вікно, далі шукати немає сенсу
                        }
                    }

                    // Якщо ми знайшли працююче вікно GUI — закриваємо цю нову копію
                    // Якщо ж запущені лише фонові процеси конвертації (без вікон) — GUI відкриється як зазвичай
                    if (foundGui)
                    {
                        Close();
                        return;
                    }
                }
            }
            catch { }

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

            // Кнопка довідки (i) - Ставимо ПЕРШОЮ та ПОВЕРХУ
            int helpSize = (int)(30 * currentScale);
            btnHelp.SetBounds(xRightField - helpSize, (int)(8 * currentScale), helpSize, helpSize);
            btnHelp.BringToFront();

            // БЛОК 1: Мова інтерфейсу
            // Обмежуємо ширину напису, щоб він не залазив під кнопку (fieldWidth - helpSize - відступ)
            lblLang.SetBounds(xLeft, startY, fieldWidth - helpSize, labelHeight);
            // КОРЕКЦІЯ: задаємо висоту елемента списку з вирахуванням 6 пікселів на системні рамки
            cbLang.ItemHeight = fieldHeight - 5;
            cbLang.SetBounds(xLeft, lblLang.Bottom + labelToFieldSpace, fieldWidth, fieldHeight);

            // БЛОК 2: Назва пункту контекстного меню
            lblMenu.SetBounds(xLeft, cbLang.Bottom + blockMargin, fieldWidth, labelHeight);
            txtMenu.SetBounds(xLeft, lblMenu.Bottom + labelToFieldSpace, fieldWidth, fieldHeight);

            // БЛОК 3: Формат вихідного документа
            lblFormat.SetBounds(xLeft, txtMenu.Bottom + blockMargin, fieldWidth, labelHeight);
            // КОРЕКЦІЯ: аналогічно задаємо ItemHeight для формату
            cbFormat.ItemHeight = fieldHeight - 5;
            cbFormat.SetBounds(xLeft, lblFormat.Bottom + labelToFieldSpace, fieldWidth, fieldHeight);

            // БЛОК 4: Папка для збереження результату
            chkFolder.SetBounds(xLeft, cbFormat.Bottom + blockMargin, fieldWidth, checkBoxHeight);
            int browseBtnWidth = (int)(38 * currentScale);
            int folderTxtWidth = fieldWidth - browseBtnWidth - (int)(8 * currentScale);
            txtFolder.SetBounds(xLeft, chkFolder.Bottom + labelToFieldSpace, folderTxtWidth, fieldHeight);
            btnFolderBrowse.SetBounds(xRightField - browseBtnWidth, txtFolder.Top, browseBtnWidth, fieldHeight);

            // БЛОК 5: Конфігураційний файл (.yaml)
            chkConfig.SetBounds(xLeft, txtFolder.Bottom + blockMargin, fieldWidth, checkBoxHeight);
            txtConfig.SetBounds(xLeft, chkConfig.Bottom + labelToFieldSpace, folderTxtWidth, fieldHeight);
            btnConfigBrowse.SetBounds(xRightField - browseBtnWidth, txtConfig.Top, browseBtnWidth, fieldHeight);

            // БЛОК 6: Опції автоматизації (Чекбокси)
            chkOverwrite.SetBounds(xLeft, txtConfig.Bottom + blockMargin, fieldWidth, checkBoxHeight);
            chkDeleteMain.SetBounds(xLeft, chkOverwrite.Bottom + spaceBetweenCheckboxes, fieldWidth, checkBoxHeight);

            int xSubLeft = (int)(38 * currentScale); // Зменшений зсув дерева ієрархії для компактності
            int checkSquareWidth = (int)(22 * currentScale);
            chkDeleteSub.SetBounds(xSubLeft, chkDeleteMain.Bottom + spaceBetweenCheckboxes, checkSquareWidth, checkBoxHeight);
            lblDeleteSubText.SetBounds(chkDeleteSub.Right, chkDeleteSub.Top, xRightField - chkDeleteSub.Right, checkBoxHeight);
            lblDeleteSubText.TextAlign = ContentAlignment.MiddleLeft;

            chkMinimize.SetBounds(xLeft, chkDeleteSub.Bottom + spaceBetweenCheckboxes, fieldWidth, checkBoxHeight);
            chkHideProgress.SetBounds(xSubLeft, chkMinimize.Bottom + spaceBetweenCheckboxes, xRightField - xSubLeft, checkBoxHeight);

            // БЛОК 7: КНОПКА ІНТЕГРАЦІЇ
            int integrateY = chkHideProgress.Bottom + blockMargin;
            btnIntegrate.SetBounds(xLeft, integrateY, fieldWidth, (int)(34 * currentScale));

            // НИЖНЯ ПАНЕЛЬ УПРАВЛІННЯ (Тема, Конфігуратор, ОК, Скасувати)
            // 1. Опускаємо кнопки нижче (збільшуємо відступ ЗВЕРХУ до кнопок з 6)
            int finalButtonsY = btnIntegrate.Bottom + blockMargin + (int)(8 * currentScale);
            // 1. Кнопка зміни теми (залишається зліва)
            btnThemeToggle.SetBounds(xLeft, finalButtonsY, (int)(40 * currentScale), (int)(30 * currentScale));
            // 2. Нова кнопка "btnGui" для запуску ЯМЛ-конфігуратора (йде відразу після теми)
            btnGui.SetBounds(btnThemeToggle.Right + (int)(6 * currentScale), finalButtonsY, (int)(40 * currentScale), (int)(30 * currentScale));

            int btnW = (int)(95 * currentScale);// Кнопки стали трішки компактнішими
            btnCancel.SetBounds(xRightField - btnW, finalButtonsY, btnW, (int)(30 * currentScale));
            btnOk.SetBounds(btnCancel.Left - btnW - (int)(8 * currentScale), finalButtonsY, btnW, (int)(30 * currentScale));

            // ГАРАНТОВАНИЙ ПЕРЕЗАПУСК ЗАОКРУГЛЕННЯ (Строго після того, як ВСІ розміри змінено!)
            MakeButtonRounded(btnFolderBrowse, 4);
            MakeButtonRounded(btnConfigBrowse, 4);
            MakeButtonRounded(btnHelp, 6);
            MakeButtonRounded(btnThemeToggle, 6);
            MakeButtonRounded(btnGui, 6); // <--- ДОДАНО: Закруглюємо нову кнопку конфігуратора на 6 пікселів
            MakeButtonRounded(btnIntegrate, 6); // Закруглюємо велику кнопку строго ТУТ, коли її розмір вже ідеальний
            MakeButtonRounded(btnOk, 6);
            MakeButtonRounded(btnCancel, 6);

            // ФІНАЛЬНИЙ РОЗРАХУНОК ВЕРТИКАЛЬНОГО РОЗМІРУ ВІКНА

            paddingBottom = (int)(13 * currentScale); // Зменшили нижній пустий відступ з 15
            finalHeight = btnOk.Bottom + paddingBottom;

            // Призначаємо фінальний, ультра-компактний розмір всієї форми
            ClientSize = new Size(calculatedWidth, finalHeight);

            // Повертаємо вікно ідеально по центру екрана монітора
            CenterToScreen();
        }
    }
}