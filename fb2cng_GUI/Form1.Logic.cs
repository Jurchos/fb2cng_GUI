using Microsoft.Win32;

namespace fb2cngGUI
{
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

            // Встановлюємо галочку перезапису з файлу конфігурації
            chkOverwrite.Checked = _settings.OverwriteExisting;
            chkSkipExisting.Checked = _settings.SkipExistingFiles;
            chkSkipExisting.Enabled = !chkOverwrite.Checked; // Блокуємо, якщо перезапис увімкнено
            chkOverwrite.Enabled = !chkSkipExisting.Checked;

            chkSkipErrors.Checked = _settings.SkipCorruptFiles;
            chkDeleteMain.Checked = _settings.DeleteAfterConvert;
            chkDeleteSub.Checked = _settings.AutoDeleteToRecycle;
            chkDeleteSub.Enabled = chkDeleteMain.Checked;

            chkMinimize.Checked = _settings.StartMinimized;
            chkHideProgress.Checked = _settings.HideProgress;

            txtFolder.Enabled = btnFolderBrowse.Enabled = chkFolder.Checked;
            txtConfig.Enabled = btnConfigBrowse.Enabled = chkConfig.Checked;
            UpdateIntegrateButtonText();
            ApplyTheme();
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
            _settings.OverwriteExisting = chkOverwrite.Checked;
            _settings.SkipExistingFiles = chkSkipExisting.Checked;
            _settings.SkipCorruptFiles = chkSkipErrors.Checked;
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
            chkOverwrite.Text = string.Empty;
            lblOverwriteText.Text = Localization.Get(lang, "OverwriteFiles");
            chkSkipExisting.Text = string.Empty;
            lblSkipExistingText.Text = Localization.Get(lang, "SkipExisting");
            chkSkipErrors.Text = Localization.Get(lang, "SkipErrors");
            chkDeleteMain.Text = Localization.Get(lang, "DeleteMain");
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
        // Список ключів реєстру
        private static string[] GetRegistryPaths(string title)
        {
            return
            [
        $@"Software\Classes\.fb2\shell\{title}",
        $@"Software\Classes\fb2file\shell\{title}",
        $@"Software\Classes\SystemFileAssociations\.fb2\shell\{title}",
        $@"Software\Classes\Directory\shell\{title}",
        $@"Software\Classes\.fb2.zip\shell\{title}",
        $@"Software\Classes\fb2zipfile\shell\{title}",
        $@"Software\Classes\SystemFileAssociations\.fb2.zip\shell\{title}",
        $@"Software\Classes\SystemFileAssociations\.zip\shell\{title}"
    ];
        }
        private void PerformRegistryOperation(bool isSilent)
        {
            string menuNameFromUi = txtMenu.Text.Trim();
            if (string.IsNullOrWhiteSpace(menuNameFromUi))
            {
                return;
            }

            // Формуємо шлях до нашої програми з аргументом для файлу
            string exeCommand = $"\"{Application.ExecutablePath}\" \"%1\"";
            string lang = cbLang.SelectedItem?.ToString() ?? _settings.Language;

            try
            {
                if (!_settings.IsIntegrated)
                {
                    // --- ПРОЦЕС ІНТЕГРАЦІЇ ---

                    // Створюємо зв'язки розширень з типами файлів (Association)
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.fb2"))
                    {
                        key.SetValue("", "fb2file");
                    }

                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.fb2.zip"))
                    {
                        key.SetValue("", "fb2zipfile");
                    }

                    // Використовуємо назву з UI для створення нових ключів
                    foreach (string path in GetRegistryPaths(menuNameFromUi))
                    {
                        using RegistryKey shellKey = Registry.CurrentUser.CreateSubKey(path);
                        shellKey.SetValue("", menuNameFromUi);
                        using RegistryKey cmdKey = shellKey.CreateSubKey("command");
                        cmdKey.SetValue("", exeCommand);
                    }

                    // Оновлюємо налаштування назвою, яку фактично записали в реєстр
                    _settings.MenuTitle = menuNameFromUi;
                    _settings.IsIntegrated = true;
                }
                else
                {
                    // --- ПРОЦЕС ДЕІНТЕГРАЦІЇ ---

                    // Видаляємо всі ключі меню, які ми створювали раніше (використовуючи стару назву)
                    foreach (string path in GetRegistryPaths(_settings.MenuTitle))
                    {
                        Registry.CurrentUser.DeleteSubKeyTree(path, false);
                    }

                    // Чистимо створений нами тип файлу для zip
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\fb2zipfile", false);

                    _settings.IsIntegrated = false;
                }

                // Оновлюємо та зберігаємо налаштування
                _settings.Save();
                UpdateIntegrateButtonText();

                if (!isSilent)
                {
                    _ = MessageService.ShowCustomMessageBox(
                        Localization.Get(lang, "Success"),
                        "Registry",
                        MessageBoxButtons.OK);
                }
            }
            catch (Exception ex)
            {
                Core.WriteToLog($"REGISTRY ERROR: {ex.Message}");
                if (!isSilent)
                {
                    _ = MessageService.ShowCustomMessageBox($"Registry Error: {ex.Message}", "Error", MessageBoxButtons.OK);
                }
            }
        }

        // Оновлений обробник кнопки
        private void BtnIntegrate_Click(object? sender, EventArgs e)
        {
            PerformRegistryOperation(isSilent: false);
        }

        // Оновлений метод синхронізації
        private void SyncRegistryPathIfNeeded()
        {
            if (!_settings.IsIntegrated)
            {
                return;
            }

            try
            {
                string regPath = $@"Software\Classes\.fb2\shell\{_settings.MenuTitle}\command";
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(regPath);
                if (key != null)
                {
                    string? currentRegValue = key.GetValue("")?.ToString();
                    if (!string.IsNullOrEmpty(currentRegValue) && !currentRegValue.Contains(Application.ExecutablePath))
                    {
                        // Вимикаємо прапорець, щоб PerformRegistryOperation зробив "інсталяцію"
                        _settings.IsIntegrated = false;
                        PerformRegistryOperation(isSilent: true); // ТИХИЙ РЕЖИМ
                        Core.WriteToLog("Registry path synchronized silently.");
                    }
                }
            }
            catch (Exception ex) { Core.WriteToLog("Failed to sync registry path: " + ex.Message); }
        }
    }
}
