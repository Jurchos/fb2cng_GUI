using System.Text.Json;

namespace fb2cngGUI
{
    public class AppSettings
    {
        public string Language { get; set; } = "English";
        public string MenuTitle { get; set; } = "Convert with fbc";
        public string Format { get; set; } = "EPUB2";
        public bool UseCustomFolder { get; set; }
        public string CustomFolder { get; set; } = "";
        public bool UseCustomConfig { get; set; }
        public string CustomConfig { get; set; } = "";
        public bool OverwriteExisting { get; set; }
        public bool SkipExistingFiles { get; set; }
        public bool SkipCorruptFiles { get; set; }
        public bool DeleteAfterConvert { get; set; }
        public bool AutoDeleteToRecycle { get; set; }
        public bool StartMinimized { get; set; }
        public bool HideProgress { get; set; }
        public bool IsIntegrated { get; set; }
        public string Theme { get; set; } = "Light";

        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "GUI_config.json");

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private static readonly Lazy<AppSettings> _instance = new(Load);
        public static AppSettings Current => _instance.Value;

        private static AppSettings Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Core.WriteToLog($"CONFIG ERROR: Settings corrupted. Using defaults. Details: {ex.Message}");
            }
            return new AppSettings();
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
                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, JsonOptions));
            }
            catch (Exception ex)
            {
                // Записуємо помилку хоча б у лог, щоб не гадати чому не працює
                Core.WriteToLog("CRITICAL: Cannot save config. " + ex.Message);
            }
        }
    }

    public static class Localization
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Translations = [];

        static Localization()
        {
            Translations["English"] = new Dictionary<string, string>
            {
                ["HelpTitle"] = "About Program",
                ["HelpText"] = "A GUI wrapper for the fb2cng (fbc) converter to configure fb2 file conversion " +
                "and add a converting option to the Windows context menu.\n" +
                "======######======\nHotkeys:\n" +
                "• Enter — \"OK\",   Esc — \"Cancel\" (main window and messages)\n" +
                "• Esc — stop conversion (active progress bar, same as [x])\n" +
                "• Ctrl + Alt + Esc — abort conversion (globally, even if the progress bar is hidden/minimized)\n" +
                "======######======\n{0}\nVersion: {1}",
                ["Lang"] = "Interface Language",
                ["Format"] = "Output Document Format",
                ["Folder"] = "Output Folder",
                ["Config"] = "Configuration (.yaml)",
                ["Menu"] = "Context Menu Item Name",
                ["OverwriteFiles"] = "Overwrite already existing files",
                ["SkipExisting"] = "Skip already existing files",
                ["SkipErrors"] = "Skip corrupted/invalid files",
                ["DeleteMain"] = "Delete fb2 files selected for conversion",
                ["DeleteSub"] = "Automatically delete files to the Recycle Bin",
                ["Minimize"] = "Minimize progress bar window",
                ["HideProg"] = "Hide progress bar window",
                ["Integrate"] = "Integrate",
                ["Deintegrate"] = "Deintegrate",
                ["Success"] = "\nSuccess!\n\u2705",
                ["Ok"] = "OK",
                ["Cancel"] = "Cancel",
                ["ProgressTitle"] = "Converting...",
                ["ConfirmStopTitle"] = "Stop Conversion",
                ["ConfirmStopText"] = "Stop conversion and close the program?",
                ["ConfirmTitle"] = "File Deletion",
                ["ConfirmText"] = "File \"{0}\" will be permanently deleted.",
                ["WarningTitle"] = "Configuration Error",
                ["WarningText"] = "Settings conflict:\n" +
                                  "Multiple progress window options selected simultaneously.",
                ["FbcMissingTitle"] = "Component Missing",
                ["FbcMissingText"] = "Converter program not found:\n" +
                                     "please verify that fbc.exe is present in the application folder!",
                ["YamlErrorTitle"] = "Configuration Error",
                ["YamlErrorText"] = "Invalid configuration file (.yaml).\n" +
                                    "Conversion will be stopped.\n(check logs)",
                ["FileExistsTitle"] = "Conversion Error",
                ["FileExistsText"] = "Failed to convert file \"{0}\".\n" +
                                     "The file already exists.\n(overwrite is disabled)\n" +
                                     "*********\nDo you want to continue?",
                ["CorruptFileTitle"] = "Validity Error",
                ["CorruptFileText"] = "Failed to convert file \"{0}\".\n" +
                                      "Source file is corrupted or invalid (check logs).\n" +
                                      "*********\nDo you want to continue?",
                ["UnknownErrorTitle"] = "Unknown Error",
                ["UnknownErrorText"] = "An unidentified error has occurred.\n" +
                                       "To identify the possible cause, please check gui_errors.log and fbc.log"
            };
            Translations["Українська"] = new Dictionary<string, string>
            {
                ["HelpTitle"] = "Про програму",
                ["HelpText"] = "Програма-оболонка конвертера fb2cng (fbc) для налаштування конвертації fb2-файлів " +
                "з додаванням опції конвертування до контекстного меню Windows.\n" +
                "======######======\nГарячі клавіші:\n" +
                "• Enter — «ОК»,   Esc — «Скасувати» (головне вікно та повідомлення)\n" +
                "• Esc — зупинити конвертацію (активне вікно прогресу, аналогічно [x])\n" +
                "• Ctrl + Alt + Esc — припинити конвертацію (глобально, та якщо вікно прогресу відключено/мінімізовано)\n" +
                "======######======\n{0}\nВерсія: {1}",
                ["Lang"] = "Мова інтерфейсу",
                ["Format"] = "Формат вихідного документа",
                ["Folder"] = "Папка для збереження",
                ["Config"] = "Конфігурація (.yaml)",
                ["Menu"] = "Назва пункту меню",
                ["OverwriteFiles"] = "Перезаписувати вже існуючі файли",
                ["SkipExisting"] = "Пропускати вже існуючі файли",
                ["SkipErrors"] = "Пропускати пошкоджені файли",
                ["DeleteMain"] = "Видаляти файли fb2, обрані для конвертації",
                ["DeleteSub"] = "Автоматично видаляти файли в корзину",
                ["Minimize"] = "Мінімізувати вікно прогресу",
                ["HideProg"] = "Не показувати вікно прогресу",
                ["Integrate"] = "Інтегрувати",
                ["Deintegrate"] = "Деінтегрувати",
                ["Success"] = "\nУспішно!\n\u2705",
                ["Ok"] = "ОК",
                ["Cancel"] = "Скасувати",
                ["ProgressTitle"] = "Конвертація...",
                ["ConfirmStopTitle"] = "Зупинка конвертації",
                ["ConfirmStopText"] = "Зупинити конвертацію та закрити програму?",
                ["ConfirmTitle"] = "Видалення файлів",
                ["ConfirmText"] = "Файл \"{0}\" буде остаточно видалений.",
                ["WarningTitle"] = "Помилка конфігурації",
                ["WarningText"] = "Конфлікт налаштувань:\n" +
                                  "одночасно встановлено 2 галочки для вікна прогресу",
                ["FbcMissingTitle"] = "Відсутній компонент",
                ["FbcMissingText"] = "Відсутня програма-конвертор:\n" +
                                     "перевірте наявність файлу fbc.exe в папці з програмою!",
                ["YamlErrorTitle"] = "Помилка конфігурації",
                ["YamlErrorText"] = "Некоректний файл налаштувань (.yaml).\n" +
                                    "Конвертацію буде припинено.\n(див. логи)",
                ["FileExistsTitle"] = "Помилка конвертації",
                ["FileExistsText"] = "Конвертацію файлу \"{0}\" скасовано.\n" +
                                     "Такий файл вже існує.\n(перезапис вимкнено)\n" +
                                     "*********\nПродовжити роботу?",
                ["CorruptFileTitle"] = "Помилка валідності",
                ["CorruptFileText"] = "Конвертацію файлу \"{0}\" скасовано.\n" +
                                      "Вихідний файл пошкоджений або має невірний формат (див. логи).\n" +
                                      "*********\nПродовжити роботу?",
                ["UnknownErrorTitle"] = "Невідома помилка",
                ["UnknownErrorText"] = "Виникла невстановлена помилка.\n" +
                                       "Для виявлення можливої причини перегляньте gui_errors.log та fbc.log"
            };
            Translations["Русский"] = new Dictionary<string, string>
            {
                ["HelpTitle"] = "О программе",
                ["HelpText"] = "Программа-оболочка конвертера fb2cng (fbc) для настройки конвертации fb2-файлов " +
                "с добавлением опции конвертирования в контекстное меню Windows.\n" +
                "======######======\nГорячие клавиши:\n" +
                "• Enter — «ОК»,   Esc — «Отмена» (главное окно и сообщения)\n" +
                "• Esc — остановить конвертацию (активное окно прогресса, тоже что [x])\n" +
                "• Ctrl + Alt + Esc — прервать конвертацию (глобально, а также если окно прогресса отключено/минимизировано)\n" +
                "======######======\n{0}\nВерсия: {1}",
                ["Lang"] = "Язык интерфейса",
                ["Format"] = "Формат выходного документа",
                ["Folder"] = "Папка для сохранения",
                ["Config"] = "Конфигурация (.yaml)",
                ["Menu"] = "Название пункта меню",
                ["OverwriteFiles"] = "Перезаписывать уже существующие файлы",
                ["SkipExisting"] = "Пропускать уже существующие файлы",
                ["SkipErrors"] = "Пропускать поврежденные файлы",
                ["DeleteMain"] = "Удалять файлы fb2, выбранные для конвертации",
                ["DeleteSub"] = "Автоматически удалять файлы в корзину",
                ["Minimize"] = "Сворачивать окно прогресса",
                ["HideProg"] = "Не показывать окно прогресса",
                ["Integrate"] = "Интегрировать",
                ["Deintegrate"] = "Деинтегировать",
                ["Success"] = "\nУспех!\n\u2705",
                ["Ok"] = "ОК",
                ["Cancel"] = "Отмена",
                ["ProgressTitle"] = "Конвертация...",
                ["ConfirmStopTitle"] = "Остановка конвертации",
                ["ConfirmStopText"] = "Остановить конвертацию и закрыть программу?",
                ["ConfirmTitle"] = "Удаление файлов",
                ["ConfirmText"] = "Файл \"{0}\" будет удален навсегда.",
                ["WarningTitle"] = "Ошибка конфигурации",
                ["WarningText"] = "Конфликт настроек:\n" +
                                  "одновременно выбраны два варианта окна прогресса",
                ["FbcMissingTitle"] = "Отсутствует компонент",
                ["FbcMissingText"] = "Программа-конвертер не найдена:\n" +
                                     "проверьте наличие файла fbc.exe в папке с программой!",
                ["YamlErrorTitle"] = "Ошибка конфигурации",
                ["YamlErrorText"] = "Некорректный файл настроек (.yaml).\n" +
                                    "Конвертация будет прекращена.\n(см. логи)",
                ["FileExistsTitle"] = "Ошибка конвертации",
                ["FileExistsText"] = "Конвертация файла \"{0}\" отменена.\n" +
                                     "Конечный файл уже существует.\n(перезапись отключена)\n" +
                                     "*********\nПродолжить работу?",
                ["CorruptFileTitle"] = "Ошибка валидности",
                ["CorruptFileText"] = "Конвертация файла \"{0}\" отменена.\n" +
                                      "Исходный файл поврежден или имеет неверный формат (см. логи).\n" +
                                      "*********\nПродолжить работу?",
                ["UnknownErrorTitle"] = "Неизвестная ошибка",
                ["UnknownErrorText"] = "Произошла неустановленная ошибка.\nДля выяснения возможной причины просмотрите gui_errors.log и fbc.log"
            };
        }

        public static string Get(string lang, string key)
        {
            // Використовуємо спосіб: якщо мова є і слово в ній є — повертаємо переклад, інакше — сам ключ
            return Translations.TryGetValue(lang, out Dictionary<string, string>? langDict) && langDict.TryGetValue(key, out string? translation)
                ? translation
                : key;
        }
    }
}
