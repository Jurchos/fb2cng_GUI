using System.Text.Json;

namespace fb2cngGUI
{
    // Клас для збереження та завантаження конфігурації додатка
    public class AppSettings
    {
        public string Language { get; set; } = "English";
        public string Format { get; set; } = "EPUB2";
        public bool UseCustomFolder { get; set; }
        public string CustomFolder { get; set; } = "";
        public bool UseCustomConfig { get; set; }
        public string CustomConfig { get; set; } = "";
        public string MenuTitle { get; set; } = "Convert with fbc";
        public bool IsIntegrated { get; set; }
        public string Theme { get; set; } = "Dark";
        public bool DeleteAfterConvert { get; set; }
        public bool AutoDeleteToRecycle { get; set; }
        public bool StartMinimized { get; set; }
        public bool HideProgress { get; set; }
        public bool OverwriteExisting { get; set; }

        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "GUI_config.json");

        // Створюємо один статичний об'єкт налаштувань.
        // Тепер він ініціалізується лише раз при запуску програми і перевикористовується в пам'яті.
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static AppSettings Load()
        {
            if (!File.Exists(ConfigPath))
            {
                return new AppSettings();
            }

            // Передаємо JsonOptions також сюди, щоб серіалізатор читав файл з тими ж налаштуваннями
            try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(ConfigPath), JsonOptions) ?? new AppSettings(); }
            catch { return new AppSettings(); }
        }

        public void Save()
        {
            try
            {
                string? dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    _ = Directory.CreateDirectory(dir);
                }

                // ВИПРАВЛЕННЯ: Замість створення "new" використовуємо готовий статичний об'єкт JsonOptions
                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, JsonOptions));
            }
            catch { }
        }
    }

    public static class Localization
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Translations = [];

        static Localization()
        {
            Translations["English"] = new Dictionary<string, string>
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
                ["ProgressTitle"] = "Converting...",
                ["Success"] = "\nSuccess!\n\u2705",
                ["WarningTitle"] = "Configuration Error",
                ["WarningText"] = "Conflict: Multiple progress window options selected simultaneously.",
                ["FbcMissingTitle"] = "Component Missing",
                ["FbcMissingText"] = "Converter program not found: please verify that fbc.exe is present in the application folder!",
                ["YamlBrokenTitle"] = "Conversion failed",
                ["YamlBrokenText"] = "Possible causes of the problem:\n1. Invalid configuration file (.yaml)" +
                                                                    "\n2. Target file already exists (overwrite is disabled)" +
                                                                    "\n3. Source .fb2 file is corrupted.",
                ["OverwriteFiles"] = "Overwrite already existing files",
                ["DeleteMain"] = "Delete fb2 files selected for conversion",
                ["DeleteSub"] = "Automatically delete files to the Recycle Bin",
                ["ConfirmTitle"] = "File Deletion",
                ["ConfirmText"] = "File \"{0}\" will be permanently deleted.",
                ["Minimize"] = "Minimize progress bar window",
                ["HideProg"] = "Hide progress bar window",
                ["HelpTitle"] = "About Program",
                ["HelpText"] = "A GUI wrapper for the fb2cng (fbc) converter to configure fb2 file conversion " +
                "and add a converting option to the Windows context menu.\n\nCreated by Jurchos & Gemini\nVersion: 1.2"
            };
            Translations["Українська"] = new Dictionary<string, string>
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
                ["ProgressTitle"] = "Конвертація...",
                ["Success"] = "\nУспішно!\n\u2705",
                ["WarningTitle"] = "Помилка конфігурації",
                ["WarningText"] = "Конфлікт налаштувань: одночасно встановлено 2 галочки для вікна прогресу",
                ["FbcMissingTitle"] = "Відсутній компонент",
                ["FbcMissingText"] = "Відсутня програма-конвертор: перевірте наявність файлу fbc.exe в папці з програмою!",
                ["YamlBrokenTitle"] = "Збій конвертації",
                ["YamlBrokenText"] = "Можливі причини проблеми:\n1. Некоректний файл налаштувань (.yaml)" +
                                                              "\n2. Цільовий файл вже існує (вимкнено перезапис)" +
                                                              "\n3. Вихідний файл .fb2 пошкоджений.",
                ["OverwriteFiles"] = "Перезаписувати уже існуючі файли",
                ["DeleteMain"] = "Видаляти файли fb2, обрані для конвертації",
                ["DeleteSub"] = "Автоматично видаляти файли в корзину",
                ["ConfirmTitle"] = "Видалення файлів",
                ["ConfirmText"] = "Файл \"{0}\" буде остаточно видалений.",
                ["Minimize"] = "Мінімізувати вікно прогресу",
                ["HideProg"] = "Не показувати вікно прогресу",
                ["HelpTitle"] = "Про програму",
                ["HelpText"] = "Програма-оболонка конвертера fb2cng (fbc) для налаштування конвертації fb2-файлів " +
                "з додаванням опції конвертування до контекстного меню Windows.\n\nСтворено: Jurchos & Gemini\nВерсія: 1.2"
            };
            Translations["Русский"] = new Dictionary<string, string>
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
                ["ProgressTitle"] = "Конвертация...",
                ["Success"] = "\nУспех!\n\u2705",
                ["WarningTitle"] = "Ошибка конфигурации",
                ["WarningText"] = "Конфликт настроек: одновременно выбраны два варианта окна прогресса",
                ["FbcMissingTitle"] = "Отсутствует компонент",
                ["FbcMissingText"] = "Программа-конвертер не найдена: проверьте наличие файла fbc.exe в папке с программой!",
                ["YamlBrokenTitle"] = "Сбой конвертации",
                ["YamlBrokenText"] = "Возможные причины проблемы:\n1. Некорректный файл настроек (.yaml)" +
                                                                         "\n2. Целевой файл уже существует (перезапись отключена)" +
                                                                         "\n3. Исходный файл .fb2 поврежден.",
                ["OverwriteFiles"] = "Перезаписывать уже существующие файлы",
                ["DeleteMain"] = "Удалять файлы fb2, выбранные для конвертации",
                ["DeleteSub"] = "Автоматически удалять файлы в корзину",
                ["ConfirmTitle"] = "Удаление файлов",
                ["ConfirmText"] = "Файл \"{0}\" будет удален навсегда.",
                ["Minimize"] = "Сворачивать окно прогресса",
                ["HideProg"] = "Не показывать окно прогресса",
                ["HelpTitle"] = "О программе",
                ["HelpText"] = "Программа-оболочка конвертера fb2cng (fbc) для настройки конвертации fb2-файлов " +
                "с добавлением опции конвертирования в контекстное меню Windows.\n\nСоздано: Jurchos & Gemini\nВерсия: 1.2"
            };
        }

        public static string Get(string lang, string key)
        {
            // 1. Шукаємо мову за один крок
            if (Translations.TryGetValue(lang, out Dictionary<string, string>? langDict))
            {
                // 2. Шукаємо слово всередині цієї мови за один крок
                if (langDict.TryGetValue(key, out string? translation))
                {
                    return translation; // Повертаємо переклад
                }
            }

            return key; // Якщо мови або слова немає — повертаємо сам ключ
        }
    }
}
