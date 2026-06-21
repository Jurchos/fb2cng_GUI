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
                        @"Software\Classes\Directory\shell\" + txtMenu.Text
                    };

                    using (RegistryKey rootKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.fb2")) { rootKey.SetValue("", "fb2file"); }

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
                        @"Software\Classes\Directory\shell\" + _settings.MenuTitle
                    };

                    foreach (string path in pathsToDelete) { Registry.CurrentUser.DeleteSubKeyTree(path, false); }
                    _settings.IsIntegrated = false;
                }
                _settings.MenuTitle = txtMenu.Text;
                _settings.Save();
                UpdateIntegrateButtonText();

                string currentLang = cbLang.SelectedItem != null ? cbLang.SelectedItem.ToString() : _settings.Language;
                string successText = Localization.Get(currentLang, "Success");

                DialogResult dialogResult = ShowCustomMessageBox(successText, "fb2cng GUI", MessageBoxButtons.OK);
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
                msgForm.Size = new Size(360, 190);
                msgForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                msgForm.MaximizeBox = false;
                msgForm.MinimizeBox = false;
                msgForm.StartPosition = FormStartPosition.CenterScreen;
                msgForm.Font = new Font("Segoe UI", 10F);
                msgForm.BackColor = isDark ? Color.FromArgb(24, 24, 24) : Color.FromArgb(245, 245, 245);

                // Налаштування для розташування повідомлення
                RichTextBox rtbText = new RichTextBox
                {
                    Text = text,
                    Location = new Point(20, 20),  // Підняли напис вище (було 26)
                    Size = new Size(305, 80),      // Трохи збільшили висоту (з 70 до 80), щоб текст точно не обрізався знизу
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
                rtbText.MouseDown += (s, e) => { HideCaret(rtbText.Handle); msgForm.Focus(); };
                rtbText.GotFocus += (s, e) => { HideCaret(rtbText.Handle); };

                msgForm.Controls.Add(rtbText);

                Color btnBg = isDark ? Color.FromArgb(50, 50, 50) : Color.FromArgb(230, 230, 230);
                Color btnTextCol = isDark ? Color.White : Color.Black;
                Color accentBg = isDark ? Color.FromArgb(0, 102, 204) : Color.FromArgb(0, 120, 215);

                // Змінна для збереження кнопки, яка прийме на себе перший фокус
                Button primaryButton = null;

                if (buttons == MessageBoxButtons.OK)
                {
                    Button btnOkCustom = new Button
                    {
                        Text = "OK",
                        DialogResult = DialogResult.OK,
                        Location = new Point(225, 105),
                        Size = new Size(100, 32),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = accentBg,
                        ForeColor = Color.White,
                        TabIndex = 0 // Робимо цю кнопку першою у черзі фокусування
                    };
                    btnOkCustom.FlatAppearance.BorderSize = 0;
                    MakeButtonRounded(btnOkCustom, 6);
                    msgForm.Controls.Add(btnOkCustom);
                    primaryButton = btnOkCustom;
                }
                else if (buttons == MessageBoxButtons.OKCancel)
                {
                    Button btnOkCustom = new Button
                    {
                        Text = Localization.Get(_settings.Language, "Ok"),
                        DialogResult = DialogResult.OK,
                        Location = new Point(115, 105),
                        Size = new Size(100, 32),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = accentBg,
                        ForeColor = Color.White,
                        TabIndex = 0 // Робимо кнопку ОК першою у черзі фокусування
                    };
                    btnOkCustom.FlatAppearance.BorderSize = 0;
                    MakeButtonRounded(btnOkCustom, 6);

                    Button btnCancelCustom = new Button
                    {
                        Text = Localization.Get(_settings.Language, "Cancel"),
                        DialogResult = DialogResult.Cancel,
                        Location = new Point(225, 105),
                        Size = new Size(100, 32),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = btnBg,
                        ForeColor = btnTextCol,
                        TabIndex = 1
                    };
                    btnCancelCustom.FlatAppearance.BorderColor = isDark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(200, 200, 200);
                    MakeButtonRounded(btnCancelCustom, 6);

                    msgForm.Controls.AddRange(new Control[] { btnOkCustom, btnCancelCustom });
                    primaryButton = btnOkCustom;
                }

                msgForm.TopMost = true;

                // ГАРАНТОВАНЕ ЗАБИРАННЯ ФОКУСУ ТА ПРИХОВУВАННЯ КУРСОРУ ПРИ ВІДКРИТТІ
                msgForm.Shown += (s, e) =>
                {
                    // Примусово переводимо активність вікна на кнопку, а не на текст
                    if (primaryButton != null)
                    {
                        _ = primaryButton.Focus();
                    }

                    // Асинхронний виклик (BeginInvoke) наздоганяє та знищує каретку 
                    // відразу після того, як ОС Windows завершить усі свої внутрішні рендери
                    _ = msgForm.BeginInvoke(new Action(() => { _ = HideCaret(rtbText.Handle); }));
                };

                return msgForm.ShowDialog();
            }
        }
    }
}
