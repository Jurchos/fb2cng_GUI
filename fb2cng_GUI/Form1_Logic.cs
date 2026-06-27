using Microsoft.Win32;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace fb2cng_GUI
{
    // Опис логічної частини головної форми додатка
    public partial class Form1 : Form
    {
        // Метод копіювання значень із об'єкта конфігурації у відповідні елементи форми (UI)
        private void ApplySettingsToUI()
        {
            cbLang.SelectedItem = _settings.Language;
            cbFormat.SelectedItem = _settings.Format;
            chkFolder.Checked = _settings.UseCustomFolder;
            txtFolder.Text = _settings.CustomFolder;
            chkConfig.Checked = _settings.UseCustomConfig;
            txtConfig.Text = _settings.CustomConfig;
            txtMenu.Text = _settings.MenuTitle;

            // ЗАВАНТАЖЕННЯ НОВОЇ ОПЦІЇ: Встановлюємо галочку перезапису з файлу конфігурації
            chkOverwrite.Checked = _settings.OverwriteExisting;

            chkDeleteMain.Checked = _settings.DeleteAfterConvert;
            chkDeleteSub.Checked = _settings.AutoDeleteToRecycle;
            chkDeleteSub.Enabled = chkDeleteMain.Checked;

            // Чисте виведення прапорців з файлу конфігурації без динамічних блокувань
            chkMinimize.Checked = _settings.StartMinimized;
            chkHideProgress.Checked = _settings.HideProgress;

            txtFolder.Enabled = btnFolderBrowse.Enabled = chkFolder.Checked;
            txtConfig.Enabled = btnConfigBrowse.Enabled = chkConfig.Checked;
            UpdateIntegrateButtonText();
        }

        // Зчитування значень з елементів форми для їхнього подальшого збереження у файл
        private void SaveUiToSettings()
        {
            _settings.Language = cbLang.SelectedItem != null ? cbLang.SelectedItem.ToString() : "English";
            _settings.Format = cbFormat.SelectedItem != null ? cbFormat.SelectedItem.ToString() : "EPUB2";
            _settings.UseCustomFolder = chkFolder.Checked;
            _settings.CustomFolder = txtFolder.Text;
            _settings.UseCustomConfig = chkConfig.Checked;
            _settings.CustomConfig = txtConfig.Text;
            _settings.MenuTitle = txtMenu.Text;

            // ЗБЕРЕЖЕННЯ НОВОЇ ОПЦІЇ: Зчитуємо стан галочки перезапису перед збереженням
            _settings.OverwriteExisting = chkOverwrite.Checked;

            _settings.DeleteAfterConvert = chkDeleteMain.Checked;
            _settings.AutoDeleteToRecycle = chkDeleteSub.Checked;
            _settings.StartMinimized = chkMinimize.Checked;
            _settings.HideProgress = chkHideProgress.Checked;
        }

        // Динамічний переклад написів інтерфейсу програми на вибрану мову
        private void ApplyLocalization()
        {
            string lang = cbLang.SelectedItem != null ? cbLang.SelectedItem.ToString() : _settings.Language;
            lblLang.Text = Localization.Get(lang, "Lang");
            lblFormat.Text = Localization.Get(lang, "Format");
            chkFolder.Text = Localization.Get(lang, "Folder");
            chkConfig.Text = Localization.Get(lang, "Config");
            lblMenu.Text = Localization.Get(lang, "Menu");
            btnOk.Text = Localization.Get(lang, "Ok");
            btnCancel.Text = Localization.Get(lang, "Cancel");

            // ПЕРЕКЛАД НОВОЇ ОПЦІЇ: Динамічно змінюємо текст на украинську/англійську/російську
            chkOverwrite.Text = Localization.Get(lang, "OverwriteFiles");
            chkDeleteMain.Text = Localization.Get(lang, "DeleteMain");
            chkDeleteSub.Text = Localization.Get(lang, "DeleteSub");
            chkMinimize.Text = Localization.Get(lang, "Minimize");
            chkHideProgress.Text = Localization.Get(lang, "HideProg");
            UpdateIntegrateButtonText();
        }

        // Оновлення напису на кнопці роботи з реєстром Windows (Інтегрувати/Деінтегрувати)
        private void UpdateIntegrateButtonText()
        {
            string lang = cbLang.SelectedItem != null ? cbLang.SelectedItem.ToString() : _settings.Language;
            btnIntegrate.Text = _settings.IsIntegrated ? Localization.Get(lang, "Deintegrate") : Localization.Get(lang, "Integrate");
        }
        // Логіка реєстрації програми в контекстному меню Windows
        private void BtnIntegrate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMenu.Text))
            {
                return;
            }

            try
            {
                if (!_settings.IsIntegrated)
                {
                    string exePath = Application.ExecutablePath;
                    string[] pathsToRegister = new string[] {
                @"Software\Classes\.fb2\shell\" + txtMenu.Text,
                @"Software\Classes\fb2file\shell\" + txtMenu.Text,
                @"Software\Classes\SystemFileAssociations\.fb2\shell\" + txtMenu.Text,
                @"Software\Classes\Directory\shell\" + txtMenu.Text,
                // ДОДАНО: реєстрація для файлів .fb2.zip та стандартних .zip
                @"Software\Classes\.fb2.zip\shell\" + txtMenu.Text,
                @"Software\Classes\SystemFileAssociations\.fb2.zip\shell\" + txtMenu.Text,
                @"Software\Classes\SystemFileAssociations\.zip\shell\" + txtMenu.Text
            };

                    using (RegistryKey rootKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.fb2")) { rootKey.SetValue("", "fb2file"); }

                    // ДОДАНО: Створення типу файлу для .fb2.zip (щоб система точно знала, як його обробляти)
                    using (RegistryKey rootZipKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.fb2.zip")) { rootZipKey.SetValue("", "fb2zipfile"); }
                    using (RegistryKey zipFileKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\fb2zipfile\shell\" + txtMenu.Text))
                    {
                        zipFileKey.SetValue("", txtMenu.Text);
                        using (RegistryKey cmdKey = zipFileKey.CreateSubKey("command")) { cmdKey.SetValue("", "\"" + exePath + "\" \"%1\""); }
                    }

                    foreach (string path in pathsToRegister)
                    {
                        using (RegistryKey menuKey = Registry.CurrentUser.CreateSubKey(path))
                        {
                            menuKey.SetValue("", txtMenu.Text);
                            using (RegistryKey cmdKey = menuKey.CreateSubKey("command")) { cmdKey.SetValue("", "\"" + exePath + "\" \"%1\""); }
                        }
                    }
                    _settings.IsIntegrated = true;
                }
                else
                {
                    string[] pathsToDelete = new string[] {
                @"Software\Classes\.fb2\shell\" + _settings.MenuTitle,
                @"Software\Classes\fb2file\shell\" + _settings.MenuTitle,
                @"Software\Classes\SystemFileAssociations\.fb2\shell\" + _settings.MenuTitle,
                @"Software\Classes\Directory\shell\" + _settings.MenuTitle,
                // ДОДАНО: видалення ключів для .fb2.zip та .zip при деінтеграції
                @"Software\Classes\.fb2.zip\shell\" + _settings.MenuTitle,
                @"Software\Classes\fb2zipfile\shell\" + _settings.MenuTitle,
                @"Software\Classes\SystemFileAssociations\.fb2.zip\shell\" + _settings.MenuTitle,
                @"Software\Classes\SystemFileAssociations\.zip\shell\" + _settings.MenuTitle
            };

                    foreach (string path in pathsToDelete) { Registry.CurrentUser.DeleteSubKeyTree(path, false); }

                    // ДОДАНО: чистимо створений нами тип файлу fb2zipfile
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\fb2zipfile", false);

                    _settings.IsIntegrated = false;
                }
                _settings.MenuTitle = txtMenu.Text;
                _settings.Save();
                UpdateIntegrateButtonText();

                string currentLang = cbLang.SelectedItem != null ? cbLang.SelectedItem.ToString() : _settings.Language;
                string successText = Localization.Get(currentLang, "Success");

                DialogResult dialogResult = ShowCustomMessageBox(successText, "Reg.Changed", MessageBoxButtons.OK);
            }
            catch (Exception ex) { _ = ShowCustomMessageBox("Registry Error: " + ex.Message, "Error", MessageBoxButtons.OK); }
        }

        // Системна функція Windows для миттєвого приховування курсору
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool HideCaret(IntPtr hWnd);

        // Вікно кастомних MessageBox з вирівнюванням тексту по центру
        public DialogResult ShowCustomMessageBox(string text, string caption, MessageBoxButtons buttons)
        {
            using (Form msgForm = new Form())
            {
                bool isDark = _settings.Theme == "Dark";
                msgForm.Text = caption;
                msgForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                msgForm.MaximizeBox = false;
                msgForm.MinimizeBox = false;
                msgForm.StartPosition = FormStartPosition.CenterScreen;
                msgForm.Font = new Font("Segoe UI", 10F);
                msgForm.BackColor = isDark ? Color.FromArgb(24, 24, 24) : Color.FromArgb(245, 245, 245);

                // --- 1. АВТОМАТИЧНЕ ВИЗНАЧЕННЯ МАСШТАБУ DPI ДЛЯ ВІКНА ПОДІЇ ---
                // Вираховуємо коефіцієнт масштабування на основі висоти шрифту форми
                float currentScale = msgForm.Font.Height / 18f;

                // --- 2. МАСШТАБОВАНІ ВІДСТУПИ ТА РОЗМІРИ ---
                int paddingTop = (int)(15 * currentScale);    // Відступ від верхнього краю до тексту
                int paddingMiddle = (int)(10 * currentScale); // Відступ між текстом та кнопкою
                int paddingBottom = (int)(10 * currentScale); // Відступ від кнопки до низу вікна
                int buttonHeight = (int)(32 * currentScale);  // Адаптивна висота кнопки ОК
                int buttonWidth = (int)(100 * currentScale);  // Адаптивна ширина кнопки ОК

                // Масштабуємо загальну базову ширину вікна повідомлення (на 100% була 385)
                int calculatedWidth = (int)(330 * currentScale);
                msgForm.ClientSize = new Size(calculatedWidth, msgForm.ClientSize.Height);

                // Налаштування для розташування повідомлення (Ширина тексту адаптується під форму)
                RichTextBox rtbText = new RichTextBox
                {
                    Text = text,
                    Width = msgForm.ClientSize.Width - (int)(35 * currentScale), // Симетричні відступи з боків
                    ForeColor = isDark ? Color.White : Color.Black,
                    BackColor = msgForm.BackColor,
                    BorderStyle = BorderStyle.None,
                    ReadOnly = true,
                    ScrollBars = RichTextBoxScrollBars.None,
                    TabStop = false,   // Забороняє фокусування кнопкою Tab
                    TabIndex = 99      // Зміщуємо в кінець черги фокусування
                };

                // Вирівнювання тексту повідомлень суворо по центру
                rtbText.SelectAll();
                rtbText.SelectionAlignment = HorizontalAlignment.Center;
                rtbText.DeselectAll();

                // ПРИХОВУВАННЯ КУРСОРУ ПРИ ВЗАЄМОДІЇ З ТЕКСТОМ
                rtbText.MouseDown += (s, e) => { _ = HideCaret(rtbText.Handle); _ = msgForm.Focus(); };
                rtbText.GotFocus += (s, e) => { _ = HideCaret(rtbText.Handle); };

                msgForm.Controls.Add(rtbText); // Додаємо на форму перед розрахунками

                // --- 3. ДИНАМІЧНИЙ РОЗРАХУНОК ВИСОТИ ТЕКСТУ ПІД НОВИЙ DPI ---
                // Дізнаємося реальну висоту відрендереного тексту в пікселях з урахуванням масштабу
                int lastCharIndex = rtbText.TextLength > 0 ? rtbText.TextLength - 1 : 0;
                Point lastCharPos = rtbText.GetPositionFromCharIndex(lastCharIndex);
                int textHeight = lastCharPos.Y + rtbText.Font.Height + (int)(10 * currentScale);

                // Задаємо мінімальну висоту текстової коробки під поточний масштаб
                int minTextHeight = (int)(50 * currentScale);
                if (textHeight < minTextHeight)
                {
                    textHeight = minTextHeight;
                }
                rtbText.Height = textHeight;

                // Позиціонуємо RichTextBox рівно по центру форми з відступом paddingTop
                rtbText.Location = new Point((msgForm.ClientSize.Width - rtbText.Width) / 2, paddingTop);

                // Розраховуємо точну Y-координату для кнопки (завжди під текстом на відстані paddingMiddle)
                int buttonsY = rtbText.Bottom + paddingMiddle;

                // Налаштування стилів кнопок
                Color btnBg = isDark ? Color.FromArgb(50, 50, 50) : Color.FromArgb(230, 230, 230);
                Color btnTextCol = isDark ? Color.White : Color.Black;
                Color accentBg = isDark ? Color.FromArgb(0, 102, 204) : Color.FromArgb(0, 120, 215);

                // Змінна для збереження кнопки, яка прийме на себе перший фокус
                Button primaryButton = null;

                buttonsY = rtbText.Bottom + paddingMiddle;

                if (buttons == MessageBoxButtons.OK)
                {
                    Button btnOkCustom = new Button
                    {
                        Text = "OK",
                        DialogResult = DialogResult.OK,
                        Size = new Size(buttonWidth, buttonHeight),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = accentBg,
                        ForeColor = Color.White,
                        TabIndex = 0
                    };
                    btnOkCustom.FlatAppearance.BorderSize = 0;
                    MakeButtonRounded(btnOkCustom, 6);

                    // Центруємо одну кнопку OK по горизонталі
                    btnOkCustom.Location = new Point((msgForm.ClientSize.Width - btnOkCustom.Width) / 2, buttonsY);

                    msgForm.Controls.Add(btnOkCustom);
                    msgForm.AcceptButton = btnOkCustom;
                    primaryButton = btnOkCustom;
                }
                else if (buttons == MessageBoxButtons.OKCancel)
                {
                    Button btnOkCustom = new Button
                    {
                        Text = Localization.Get(_settings.Language, "Ok"),
                        DialogResult = DialogResult.OK,
                        Size = new Size(buttonWidth, buttonHeight),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = accentBg,
                        ForeColor = Color.White,
                        TabIndex = 0
                    };
                    btnOkCustom.FlatAppearance.BorderSize = 0;
                    MakeButtonRounded(btnOkCustom, 6);

                    Button btnCancelCustom = new Button
                    {
                        Text = Localization.Get(_settings.Language, "Cancel"),
                        DialogResult = DialogResult.Cancel,
                        Size = new Size(buttonWidth, buttonHeight),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = btnBg,
                        ForeColor = btnTextCol,
                        TabIndex = 1
                    };
                    btnCancelCustom.FlatAppearance.BorderColor = isDark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(200, 200, 200);
                    MakeButtonRounded(btnCancelCustom, 6);

                    // Розподіляємо дві кнопки симетрично відносно центру форми
                    int spacing = (int)(15 * currentScale);
                    int totalButtonsWidth = btnOkCustom.Width + spacing + btnCancelCustom.Width;
                    int startX = (msgForm.ClientSize.Width - totalButtonsWidth) / 2;

                    btnOkCustom.Location = new Point(startX, buttonsY);
                    btnCancelCustom.Location = new Point(startX + btnOkCustom.Width + spacing, buttonsY);

                    msgForm.Controls.AddRange(new Control[] { btnOkCustom, btnCancelCustom });
                    msgForm.AcceptButton = btnOkCustom;
                    msgForm.CancelButton = btnCancelCustom;
                    primaryButton = btnOkCustom;
                }

                msgForm.TopMost = true;

                // ФІНАЛЬНИЙ РОЗРАХУНОК ВЕРТИКАЛЬНОГО РОЗМІРУ ВІКНА
                int finalHeight = paddingTop + rtbText.Height + paddingMiddle + buttonHeight + paddingBottom;
                msgForm.ClientSize = new Size(calculatedWidth, finalHeight);

                // Надійне WinAPI центрування динамічної форми msgForm на екрані монітора
                var primaryScreen = Screen.FromControl(this).Bounds; // варіант з var краще ніж Rectangle
                msgForm.Location = new Point(
                    primaryScreen.Left + ((primaryScreen.Width - msgForm.Width) / 2),
                    primaryScreen.Top + ((primaryScreen.Height - msgForm.Height) / 2)
                );

                // Налаштування поведінки вікна перед показом
                msgForm.StartPosition = FormStartPosition.CenterScreen;
                msgForm.TopMost = true;

                // ГАРАНТОВАНЕ ЗАБИРАННЯ ФОКУСУ ЧЕРЕЗ СКЛЕЮВАННЯ ПОТОКІВ WINDOWS
                msgForm.Shown += (s, e) =>
                {
                    try
                    {
                        IntPtr msgFormHandle = msgForm.Handle;

                        // 1. Отримуємо ID потоку вікна, яке зараз реально активне в Windows
                        IntPtr foregroundWindowHandle = Program.GetForegroundWindow();
                        uint foregroundThreadId = Program.GetWindowThreadProcessId(foregroundWindowHandle, IntPtr.Zero);

                        // 2. Отримуємо ID потоку нашого поточного вікна з повідомленням
                        uint currentThreadId = Program.GetCurrentThreadId();

                        // 3. Якщо фокус у якоїсь іншої програми, тимчасово склеюємо потоки введення
                        if (foregroundThreadId != currentThreadId && foregroundThreadId != 0)
                        {
                            _ = Program.AttachThreadInput(currentThreadId, foregroundThreadId, true);

                            // Примусово виводимо вікно на передній план та активуємо
                            _ = Program.SetForegroundWindow(msgFormHandle);
                            _ = Program.SetActiveWindow(msgFormHandle);
                            msgForm.Activate();

                            // Відклеюємо потоки назад, щоб не порушувати роботу ОС
                            _ = Program.AttachThreadInput(currentThreadId, foregroundThreadId, false);
                        }
                        else
                        {
                            // Якщо ми і так були активні, просто стандартно фокусуємо
                            _ = Program.SetForegroundWindow(msgFormHandle);
                            _ = Program.SetActiveWindow(msgFormHandle);
                            msgForm.Activate();
                        }
                    }
                    catch { }

                    // 4. Передаємо фокус безпосередньо на головну кнопку форми
                    if (primaryButton != null)
                    {
                        _ = primaryButton.Focus();
                    }

                    _ = msgForm.BeginInvoke(new Action(() => { _ = HideCaret(rtbText.Handle); }));
                };

                return msgForm.ShowDialog();
            }
        }
    }
}
