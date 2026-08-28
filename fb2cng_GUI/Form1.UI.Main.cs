using System.Diagnostics;

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
            btnHelp = new Button { FlatStyle = FlatStyle.Flat, TabStop = false };
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

            btnFolderBrowse = new Button { FlatStyle = FlatStyle.Flat, TabStop = false };
            SetupIconRenderer(btnFolderBrowse, Properties.Resources.folder);
            btnFolderBrowse.Click += (s, e) =>
            {
                using FolderBrowserDialog fbd = new();
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtFolder.Text = fbd.SelectedPath;
                }
                _ = btnOk.Focus();
            };

            btnConfigBrowse = new Button { FlatStyle = FlatStyle.Flat, TabStop = false };
            SetupIconRenderer(btnConfigBrowse, Properties.Resources.folder);
            btnConfigBrowse.Click += (s, e) =>
            {
                using OpenFileDialog ofd = new() { Filter = "YAML config|*.yaml;*.yml" };
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtConfig.Text = ofd.FileName;
                }
                _ = btnOk.Focus();
            };

            lblLang = new Label(); lblMenu = new Label(); lblFormat = new Label();
            chkFolder = new CheckBox();
            chkFolder.CheckedChanged += (s, e) => { txtFolder.Enabled = btnFolderBrowse.Enabled = chkFolder.Checked; btnFolderBrowse.Invalidate(); };
            chkConfig = new CheckBox();

            chkConfig.CheckedChanged += (s, e) => { txtConfig.Enabled = btnConfigBrowse.Enabled = chkConfig.Checked; btnConfigBrowse.Invalidate(); };
            // --- ПЕРЕЗАПИС ---
            chkOverwrite = new CheckBox { Text = string.Empty };
            lblOverwriteText = new Label { Cursor = Cursors.Hand, AutoSize = false };
            lblOverwriteText.Click += (s, e) => { if (chkOverwrite.Enabled) { chkOverwrite.Checked = !chkOverwrite.Checked; } };

            // --- ПРОПУСК ---
            chkSkipExisting = new CheckBox { Text = string.Empty };
            lblSkipExistingText = new Label { Cursor = Cursors.Hand, AutoSize = false };
            lblSkipExistingText.Click += (s, e) => { if (chkSkipExisting.Enabled) { chkSkipExisting.Checked = !chkSkipExisting.Checked; } };

            // --- ЛОГІКА ВЗАЄМОБЛОКУВАННЯ (Захист від дурня) ---
            chkOverwrite.CheckedChanged += (s, e) =>
            {
                if (chkOverwrite.Checked)
                {
                    chkSkipExisting.Checked = false;
                    chkSkipExisting.Enabled = false;
                }
                else
                {
                    chkSkipExisting.Enabled = true;
                }
                ApplyTheme();
            };

            chkSkipExisting.CheckedChanged += (s, e) =>
            {
                if (chkSkipExisting.Checked)
                {
                    chkOverwrite.Checked = false;
                    chkOverwrite.Enabled = false;
                }
                else
                {
                    chkOverwrite.Enabled = true;
                }
                ApplyTheme();
            };
            chkSkipErrors = new CheckBox();
            chkDeleteMain = new CheckBox();
            chkDeleteSub = new CheckBox { Text = string.Empty };
            lblDeleteSubText = new Label { Cursor = Cursors.Hand, AutoSize = false };
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
            chkMinimize = new CheckBox(); chkHideProgress = new CheckBox();

            btnIntegrate = new Button { FlatStyle = FlatStyle.Flat, TabStop = false };
            btnIntegrate.Click += (s, e) =>
            {
                BtnIntegrate_Click(s, e);
                btnOk.Focus();
            };

            btnThemeToggle = new Button { FlatStyle = FlatStyle.Flat, TabStop = false };
            SetupIconRenderer(btnThemeToggle, Properties.Resources.day_night);
            btnThemeToggle.Click += (s, e) =>
            {
                _settings.Theme = _settings.Theme == "Dark" ? "Light" : "Dark";
                ApplyTheme();
                if (infoTooltipForm != null && infoTooltipForm.Visible)
                {
                    infoTooltipForm.Close();
                }
                _ = btnOk.Focus();
            };

            btnGui = new Button { FlatStyle = FlatStyle.Flat, TabStop = false };
            string configuratorPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fb2cng_Configurator.exe");
            btnGui.Visible = File.Exists(configuratorPath);
            SetupIconRenderer(btnGui, Properties.Resources.icon_yaml);
            btnGui.Click += (s, e) =>
            {
                try { _ = Process.Start(new ProcessStartInfo { FileName = configuratorPath, UseShellExecute = true }); } 
                catch { }
            };

            btnOk = new Button { FlatStyle = FlatStyle.Flat };
            btnOk.Click += (s, e) =>
            {
                if (chkMinimize.Checked && chkHideProgress.Checked)
                {
                    string currentLang = cbLang.SelectedItem?.ToString() ?? _settings.Language;

                    _ = MessageService.ShowCustomMessageBox(
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

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Controls.AddRange([lblLang, cbLang, lblMenu, txtMenu, lblFormat, cbFormat, chkFolder, txtFolder, btnFolderBrowse, chkConfig,
                txtConfig, btnConfigBrowse, chkOverwrite, lblOverwriteText, chkSkipExisting, chkSkipErrors,
                lblSkipExistingText, chkDeleteMain, chkDeleteSub, lblDeleteSubText, chkMinimize, chkHideProgress,
                btnIntegrate, btnThemeToggle, btnGui, btnOk, btnCancel, btnHelp]);

            Load += Form1_Load;
            LocationChanged += (s, e) => UpdateHelpWindowPosition();
        }

        // --- ВАРІАНТ ПОДІЇ LOAD ---
        private void Form1_Load(object? sender, EventArgs e)
        {
            // 1. Обчислюємо точний масштаб DPI монітора
            float scale = DeviceDpi / 96f;

            //  ШИРИНА ПРОГРАМИ
            // Базова внутрішня ширина тепер 390 замість 480.
            int calculatedWidth = (int)(390 * scale);
            ClientSize = new Size(calculatedWidth, ClientSize.Height);

            // ВІДСТУПИ 
            int blockMargin = (int)(8 * scale);       // Простір між великими блоками
            int labelToFieldSpace = (int)(2 * scale);  // Відступ від тексту до його поля
            int spaceBetweenCheckboxes = (int)(3 * scale);
            int xLeft = (int)(15 * scale); // лівий відступ
            int xSubLeft = (int)(38 * scale);// збільшений бічний відступ для чекбоксів
            int xRightField = ClientSize.Width - xLeft;
            int fieldWidth = xRightField - xLeft;

            int labelHeight = (int)(20 * scale); // висота напису
            int fieldHeight = (int)(24 * scale); // висота полів
            int lineFrame = (int)(5 * scale);// допуск на системні рамки елементів списку
            int checkBoxHeight = (int)(22 * scale);
            int buttonHeight = (int)(30 * scale);
            int buttonWidth = (int)(95 * scale);
            int buttonInfWidth = (int)(40 * scale);
            int browseBtnWidth = (int)(38 * scale);
            int helpSize = (int)(30 * scale);

            int RadiusSmall = (int)(3 * scale);
            int RadiusMain = (int)(5 * scale);

            // Верхня стартова точка (25 щоб не налазити на кнопку "інформація")
            int startY = (int)(25 * scale);

            // Кнопка довідки (i) - Ставимо ПЕРШОЮ та ПОВЕРХУ
            btnHelp.SetBounds(xRightField - helpSize, (int)(8 * scale), helpSize, helpSize);
            btnHelp.BringToFront();

            // БЛОК 1: Мова інтерфейсу
            // Обмежуємо ширину напису, щоб він не залазив під кнопку (fieldWidth - helpSize - відступ)
            lblLang.SetBounds(xLeft, startY, fieldWidth - helpSize, labelHeight);
            // КОРЕКЦІЯ: задаємо висоту елемента списку з вирахуванням на системні рамки
            cbLang.ItemHeight = fieldHeight - lineFrame;
            cbLang.SetBounds(xLeft, lblLang.Bottom + labelToFieldSpace, fieldWidth, fieldHeight);

            // БЛОК 2: Назва пункту контекстного меню
            lblMenu.SetBounds(xLeft, cbLang.Bottom + blockMargin, fieldWidth, labelHeight);
            txtMenu.SetBounds(xLeft, lblMenu.Bottom + labelToFieldSpace, fieldWidth, fieldHeight);

            // БЛОК 3: Формат вихідного документа
            lblFormat.SetBounds(xLeft, txtMenu.Bottom + blockMargin, fieldWidth, labelHeight);
            // КОРЕКЦІЯ: аналогічно задаємо ItemHeight для формату
            cbFormat.ItemHeight = fieldHeight - lineFrame;
            cbFormat.SetBounds(xLeft, lblFormat.Bottom + labelToFieldSpace, fieldWidth, fieldHeight);

            // БЛОК 4: Папка для збереження результату
            chkFolder.SetBounds(xLeft, cbFormat.Bottom + blockMargin, fieldWidth, checkBoxHeight);
            int folderTxtWidth = fieldWidth - browseBtnWidth - blockMargin;
            txtFolder.SetBounds(xLeft, chkFolder.Bottom + labelToFieldSpace, folderTxtWidth, fieldHeight);
            btnFolderBrowse.SetBounds(xRightField - browseBtnWidth, txtFolder.Top, browseBtnWidth, fieldHeight);

            // БЛОК 5: Конфігураційний файл (.yaml)
            chkConfig.SetBounds(xLeft, txtFolder.Bottom + blockMargin, fieldWidth, checkBoxHeight);
            txtConfig.SetBounds(xLeft, chkConfig.Bottom + labelToFieldSpace, folderTxtWidth, fieldHeight);
            btnConfigBrowse.SetBounds(xRightField - browseBtnWidth, txtConfig.Top, browseBtnWidth, fieldHeight);

            // БЛОК 6: Опції автоматизації (Чекбокси)
            int checkSquareWidth = (int)(15 * scale);// Ширина квадратика галочки, для вирівнювання написів замість 22 - 15
            // Розміщуємо саму галочку перезапису (тільки квадрат)
            chkOverwrite.SetBounds(xLeft, txtConfig.Bottom + blockMargin, checkSquareWidth, checkBoxHeight);

            // Розміщуємо ТЕКСТ перезапису поруч
            lblOverwriteText.SetBounds(chkOverwrite.Right, chkOverwrite.Top, xRightField - chkOverwrite.Right, checkBoxHeight);
            lblOverwriteText.TextAlign = ContentAlignment.MiddleLeft;

            chkSkipExisting.SetBounds(xSubLeft, chkOverwrite.Bottom + spaceBetweenCheckboxes, checkSquareWidth, checkBoxHeight);
            // Розміщуємо ТЕКСТ пропуску поруч
            lblSkipExistingText.SetBounds(chkSkipExisting.Right, chkSkipExisting.Top, xRightField - chkSkipExisting.Right, checkBoxHeight);
            lblSkipExistingText.TextAlign = ContentAlignment.MiddleLeft;

            chkSkipErrors.SetBounds(xSubLeft, chkSkipExisting.Bottom + spaceBetweenCheckboxes, fieldWidth, checkBoxHeight);
            chkDeleteMain.SetBounds(xLeft, chkSkipErrors.Bottom + spaceBetweenCheckboxes, fieldWidth, checkBoxHeight);

            chkDeleteSub.SetBounds(xSubLeft, chkDeleteMain.Bottom + spaceBetweenCheckboxes, checkSquareWidth, checkBoxHeight);
            lblDeleteSubText.SetBounds(chkDeleteSub.Right, chkDeleteSub.Top, xRightField - chkDeleteSub.Right, checkBoxHeight);
            lblDeleteSubText.TextAlign = ContentAlignment.MiddleLeft;

            chkMinimize.SetBounds(xLeft, chkDeleteSub.Bottom + spaceBetweenCheckboxes, fieldWidth, checkBoxHeight);
            chkHideProgress.SetBounds(xSubLeft, chkMinimize.Bottom + spaceBetweenCheckboxes, xRightField - xSubLeft, checkBoxHeight);

            // БЛОК 7: КНОПКА ІНТЕГРАЦІЇ
            int integrateY = chkHideProgress.Bottom + blockMargin;
            btnIntegrate.SetBounds(xLeft, integrateY, fieldWidth, (int)(34 * scale));

            // НИЖНЯ ПАНЕЛЬ УПРАВЛІННЯ (Тема, Конфігуратор, ОК, Скасувати)
            // 1. Опускаємо кнопки нижче (збільшуємо відступ ЗВЕРХУ до кнопок на 6)
            int finalButtonsY = btnIntegrate.Bottom + blockMargin + (int)(6 * scale);
            // 1. Кнопка зміни теми (залишається зліва)
            btnThemeToggle.SetBounds(xLeft, finalButtonsY, buttonInfWidth, buttonHeight);
            // Розраховуємо позицію кнопок ОК/Скасувати залежно від того, чи є кнопка конфігуратора
            int nextControlX = btnThemeToggle.Right + blockMargin;
            if (btnGui.Visible)
            {
                btnGui.SetBounds(nextControlX, finalButtonsY, buttonInfWidth, buttonHeight);
            }

            btnCancel.SetBounds(xRightField - buttonWidth, finalButtonsY, buttonWidth, buttonHeight);
            btnOk.SetBounds(btnCancel.Left - buttonWidth - blockMargin, finalButtonsY, buttonWidth, buttonHeight);

            // ФІНАЛЬНИЙ РОЗРАХУНОК: Встановлюємо розмір ОДИН раз
            paddingBottom = (int)(13 * scale);
            int totalHeight = btnOk.Bottom + paddingBottom;

            // Задаємо повний розмір вікна одним махом
            Size = new Size(calculatedWidth + (Width - ClientSize.Width),
                                 totalHeight + (Height - ClientSize.Height));

            // Тільки після фінального розміру робимо заокруглення (щоб Region не поплив)
            UIButton.MakeButtonRounded(btnFolderBrowse, RadiusSmall);
            UIButton.MakeButtonRounded(btnConfigBrowse, RadiusSmall);
            UIButton.MakeButtonRounded(btnHelp, RadiusMain);
            UIButton.MakeButtonRounded(btnThemeToggle, RadiusMain);
            UIButton.MakeButtonRounded(btnGui, RadiusMain);
            UIButton.MakeButtonRounded(btnIntegrate, RadiusMain);
            UIButton.MakeButtonRounded(btnOk, RadiusMain);
            UIButton.MakeButtonRounded(btnCancel, RadiusMain);
            // Повертаємо вікно по центру екрана монітора
            CenterToScreen();
        }
    }
}