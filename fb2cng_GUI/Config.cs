using System;
using System.Collections.Generic;
using System.IO;

namespace fb2cng_GUI
{
    // Клас для збереження та завантаження конфігурації додатка
    public class AppSettings
    {
        public string Language { get; set; }
        public string Format { get; set; }
        public bool UseCustomFolder { get; set; }
        public string CustomFolder { get; set; }
        public bool UseCustomConfig { get; set; }
        public string CustomConfig { get; set; }
        public string MenuTitle { get; set; }
        public bool IsIntegrated { get; set; }
        public string Theme { get; set; }
        public bool DeleteAfterConvert { get; set; }
        public bool AutoDeleteToRecycle { get; set; }
        public bool StartMinimized { get; set; }
        public bool HideProgress { get; set; }

        public AppSettings()
        {
            Language = "English";
            Format = "EPUB2";
            UseCustomFolder = false;
            CustomFolder = "";
            UseCustomConfig = false;
            CustomConfig = "";
            MenuTitle = "Convert with fbc";
            IsIntegrated = false;
            Theme = "Dark";
            DeleteAfterConvert = false;
            AutoDeleteToRecycle = false;
            StartMinimized = false;
            HideProgress = false;
        }

        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gui_config.txt");

        public static AppSettings Load()
        {
            AppSettings settings = new AppSettings();
            if (!File.Exists(ConfigPath))
            {
                return settings;
            }

            try
            {
                string[] lines = File.ReadAllLines(ConfigPath);
                foreach (string line in lines)
                {
                    string[] parts = line.Split(new char[] { '=' }, 2);
                    if (parts.Length < 2)
                    {
                        continue;
                    }

                    string key = parts[0].Trim();
                    string val = parts[1].Trim();

                    if (key == "Language")
                    {
                        settings.Language = val;
                    }
                    else if (key == "Format")
                    {
                        settings.Format = val;
                    }
                    else if (key == "UseCustomFolder")
                    {
                        settings.UseCustomFolder = bool.Parse(val);
                    }
                    else if (key == "CustomFolder")
                    {
                        settings.CustomFolder = val;
                    }
                    else if (key == "UseCustomConfig")
                    {
                        settings.UseCustomConfig = bool.Parse(val);
                    }
                    else if (key == "CustomConfig")
                    {
                        settings.CustomConfig = val;
                    }
                    else if (key == "MenuTitle")
                    {
                        settings.MenuTitle = val;
                    }
                    else if (key == "IsIntegrated")
                    {
                        settings.IsIntegrated = bool.Parse(val);
                    }
                    else if (key == "Theme")
                    {
                        settings.Theme = val;
                    }
                    else if (key == "DeleteAfterConvert")
                    {
                        settings.DeleteAfterConvert = bool.Parse(val);
                    }
                    else if (key == "AutoDeleteToRecycle")
                    {
                        settings.AutoDeleteToRecycle = bool.Parse(val);
                    }
                    else if (key == "StartMinimized")
                    {
                        settings.StartMinimized = bool.Parse(val);
                    }
                    else if (key == "HideProgress")
                    {
                        settings.HideProgress = bool.Parse(val);
                    }
                }
            }
            catch { }
            return settings;
        }

        public void Save()
        {
            List<string> lines = new List<string>
            {
                "Language=" + Language,
                "Format=" + Format,
                "UseCustomFolder=" + UseCustomFolder.ToString(),
                "CustomFolder=" + CustomFolder,
                "UseCustomConfig=" + UseCustomConfig.ToString(),
                "CustomConfig=" + CustomConfig,
                "MenuTitle=" + MenuTitle,
                "IsIntegrated=" + IsIntegrated.ToString(),
                "Theme=" + Theme,
                "DeleteAfterConvert=" + DeleteAfterConvert.ToString(),
                "AutoDeleteToRecycle=" + AutoDeleteToRecycle.ToString(),
                "StartMinimized=" + StartMinimized.ToString(),
                "HideProgress=" + HideProgress.ToString()
            };
            File.WriteAllLines(ConfigPath, lines.ToArray());
        }
    }

    // Клас багатомовної локалізації інтерфейсу
    public static class Localization
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new Dictionary<string, Dictionary<string, string>>();

        static Localization()
        {
            Dictionary<string, string> en = new Dictionary<string, string>
            {
                ["Lang"] = "Interface Language",
                ["Format"] = "Output Document Format",
                ["Folder"] = "Output Folder",
                ["Config"] = "Configuration (.yaml)",
                ["Menu"] = "Context Menu Item Name",
                ["Integrate"] = "Integrate",
                ["Deintegrate"] = "Deintegrate",
                ["Ok"] = "OK",
                ["Cancel"] = "Cancel",
                ["Success"] = "\nSuccess!\n\u2705",
                ["DeleteMain"] = "Delete fb2 files selected for conversion",
                ["DeleteSub"] = "Automatically delete files to the Recycle Bin",
                ["ConfirmTitle"] = "File Deletion",
                ["ConfirmText"] = "File \"{0}\" will be permanently deleted.",
                ["Minimize"] = "Minimize progress bar window",
                ["HideProg"] = "Hide progress bar window",
                ["HelpTitle"] = "About Program",
                ["HelpText"] = "A GUI wrapper for the fb2cng (fbc) converter to configure fb2 file conversion and add a converting option to the Windows context menu.\n\nCreated by Jurchos & Gemini\nVersion: 0.2"
            };
            Translations["English"] = en;

            Dictionary<string, string> uk = new Dictionary<string, string>
            {
                ["Lang"] = "Мова інтерфейсу",
                ["Format"] = "Формат вихідного документа",
                ["Folder"] = "Папка для збереження",
                ["Config"] = "Конфігурація (.yaml)",
                ["Menu"] = "Назва пункту меню",
                ["Integrate"] = "Інтегрувати",
                ["Deintegrate"] = "Деінтегрувати",
                ["Ok"] = "ОК",
                ["Cancel"] = "Скасувати",
                ["Success"] = "\nУспішно!\n\u2705",
                ["DeleteMain"] = "Видаляти файли fb2, обрані для конвертації",
                ["DeleteSub"] = "Автоматично видаляти файли в корзину",
                ["ConfirmTitle"] = "Видалення файлів",
                ["ConfirmText"] = "Файл \"{0}\" буде остаточно видалений.",
                ["Minimize"] = "Мінімізувати вікно прогресу",
                ["HideProg"] = "Не показувати вікно прогресу",
                ["HelpTitle"] = "Про програму",
                ["HelpText"] = "Програма-оболонка конвертера fb2cng (fbc) для налаштування конвертації fb2-файлів з додаванням опції конвертування до контекстного меню Windows.\n\nСтворено: Jurchos & Gemini\nВерсія: 0.2"
            };
            Translations["Українська"] = uk;

            Dictionary<string, string> ru = new Dictionary<string, string>
            {
                ["Lang"] = "Язык интерфейса",
                ["Format"] = "Формат выходного документа",
                ["Folder"] = "Папка для сохранения",
                ["Config"] = "Конфигурация (.yaml)",
                ["Menu"] = "Название пункта меню",
                ["Integrate"] = "Интегрировать",
                ["Deintegrate"] = "Деинтегировать",
                ["Ok"] = "ОК",
                ["Cancel"] = "Отмена",
                ["Success"] = "\nУспех!\n\u2705",
                ["DeleteMain"] = "Удалять файлы fb2, выбранные для конвертации",
                ["DeleteSub"] = "Автоматически удалять файлы в корзину",
                ["ConfirmTitle"] = "Удаление файлов",
                ["ConfirmText"] = "Файл \"{0}\" будет удален навсегда.",
                ["Minimize"] = "Сворачивать окно прогресса",
                ["HideProg"] = "Не показывать окно прогресса",
                ["HelpTitle"] = "О программе",
                ["HelpText"] = "Программа-оболочка конвертера fb2cng (fbc) для настройки конвертации fb2-файлов с добавлением опции конвертирования в контекстное меню Windows.\n\nСоздано: Jurchos & Gemini\nВерсия: 0.2"
            };
            Translations["Русский"] = ru;
        }

        public static string Get(string lang, string key)
        {
            return Translations.ContainsKey(lang) && Translations[lang].ContainsKey(key) ? Translations[lang][key] : key;
        }
    }
}
