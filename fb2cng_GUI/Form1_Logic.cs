using Microsoft.Win32;

namespace fb2cngGUI
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
            _settings.Language = cbLang.SelectedItem?.ToString() ?? "English";
            _settings.Format = cbFormat.SelectedItem?.ToString() ?? "EPUB2";
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
            string lang = cbLang.SelectedItem?.ToString() ?? _settings.Language;
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
            chkDeleteSub.Text = ""; // Залишаємо порожнім
            lblDeleteSubText.Text = Localization.Get(lang, "DeleteSub");
            chkMinimize.Text = Localization.Get(lang, "Minimize");
            chkHideProgress.Text = Localization.Get(lang, "HideProg");
            UpdateIntegrateButtonText();
        }

        // Оновлення напису на кнопці роботи з реєстром Windows (Інтегрувати/Деінтегрувати)
        private void UpdateIntegrateButtonText()
        {
            string lang = cbLang.SelectedItem?.ToString() ?? _settings.Language;
            btnIntegrate.Text = _settings.IsIntegrated ? Localization.Get(lang, "Deintegrate") : Localization.Get(lang, "Integrate");
        }
        // Логіка реєстрації програми в контекстному меню Windows
        private void BtnIntegrate_Click(object? sender, EventArgs e)
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
                    string[] pathsToRegister = [
                @"Software\Classes\.fb2\shell\" + txtMenu.Text,
                @"Software\Classes\fb2file\shell\" + txtMenu.Text,
                @"Software\Classes\SystemFileAssociations\.fb2\shell\" + txtMenu.Text,
                @"Software\Classes\Directory\shell\" + txtMenu.Text,
                // реєстрація для файлів .fb2.zip та стандартних .zip
                @"Software\Classes\.fb2.zip\shell\" + txtMenu.Text,
                @"Software\Classes\SystemFileAssociations\.fb2.zip\shell\" + txtMenu.Text,
                @"Software\Classes\SystemFileAssociations\.zip\shell\" + txtMenu.Text
            ];

                    using (RegistryKey rootKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.fb2")) { rootKey.SetValue("", "fb2file"); }

                    // створення типу файлу для .fb2.zip (щоб система точно знала, як його обробляти)
                    using (RegistryKey rootZipKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.fb2.zip")) { rootZipKey.SetValue("", "fb2zipfile"); }
                    using (RegistryKey zipFileKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\fb2zipfile\shell\" + txtMenu.Text))
                    {
                        zipFileKey.SetValue("", txtMenu.Text);
                        using RegistryKey cmdKey = zipFileKey.CreateSubKey("command");
                        cmdKey.SetValue("", "\"" + exePath + "\" \"%1\"");
                    }

                    foreach (string path in pathsToRegister)
                    {
                        using RegistryKey menuKey = Registry.CurrentUser.CreateSubKey(path);
                        menuKey.SetValue("", txtMenu.Text);
                        using RegistryKey cmdKey = menuKey.CreateSubKey("command");
                        cmdKey.SetValue("", "\"" + exePath + "\" \"%1\"");
                    }
                    _settings.IsIntegrated = true;
                }
                else
                {
                    string[] pathsToDelete = [
                @"Software\Classes\.fb2\shell\" + _settings.MenuTitle,
                @"Software\Classes\fb2file\shell\" + _settings.MenuTitle,
                @"Software\Classes\SystemFileAssociations\.fb2\shell\" + _settings.MenuTitle,
                @"Software\Classes\Directory\shell\" + _settings.MenuTitle,
                // видалення ключів для .fb2.zip та .zip при деінтеграції
                @"Software\Classes\.fb2.zip\shell\" + _settings.MenuTitle,
                @"Software\Classes\fb2zipfile\shell\" + _settings.MenuTitle,
                @"Software\Classes\SystemFileAssociations\.fb2.zip\shell\" + _settings.MenuTitle,
                @"Software\Classes\SystemFileAssociations\.zip\shell\" + _settings.MenuTitle
            ];

                    foreach (string path in pathsToDelete) { Registry.CurrentUser.DeleteSubKeyTree(path, false); }

                    // чистимо створений нами тип файлу fb2zipfile
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\fb2zipfile", false);

                    _settings.IsIntegrated = false;
                }
                _settings.MenuTitle = txtMenu.Text;
                _settings.Save();
                UpdateIntegrateButtonText();

                string currentLang = cbLang.SelectedItem?.ToString() ?? _settings.Language;
                string successText = Localization.Get(currentLang, "Success");

                DialogResult dialogResult = ShowCustomMessageBox(successText, "Reg.Changed", MessageBoxButtons.OK);
            }
            catch (Exception ex) { _ = ShowCustomMessageBox("Registry Error: " + ex.Message, "Error", MessageBoxButtons.OK); }
        }
    }
}
