using System.Runtime.InteropServices;

namespace fb2cngGUI
{
    public static class Core
    {
        // Одне місце для назви файлу конвертера
        public const string ConverterExe = "fbc.exe";
        private static readonly Lock _logLock = new();

        public static void WriteToLog(string message, string? targetFile = null)
        {
            // Якщо повідомлення порожнє — нічого не робимо
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            string logPath = Path.Combine(logDir, "gui_errors.log");

            // Використовуємо механізм Lock з .NET 10 для внутріпроц. безпеки
            lock (_logLock)
            {
                try
                {
                    if (!Directory.Exists(logDir))
                    {
                        _ = Directory.CreateDirectory(logDir);
                    }

                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message.Trim()}" +
                                      (string.IsNullOrEmpty(targetFile) ? "" : $" | Target: {Path.GetFileName(targetFile)}") +
                                      Environment.NewLine;

                    // Використовуємо StreamWriter з налаштуванням FileShare.ReadWrite для меншої кількості конфліктів
                    using FileStream stream = new(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    using StreamWriter writer = new(stream);
                    writer.Write(logEntry);
                }
                catch
                {
                    // КРИТИЧНО: Тут порожній блок catch. 
                    // Якщо навіть лог не зміг записатися (наприклад, немає прав доступу), 
                    // ми просто мовчимо, щоб не зупинити всю програму.
                }
            }
        }

        // Метод безпечного видалення файлу — відправка його у Кошик Windows замість повного стирання
        public static void SendToRecycleBin(string path)
        {
            const uint FO_DELETE = 0x0003;                       // Код операції: Видалення
            const ushort FOF_ALLOWUNDO = 0x0040;                 // Прапорець: Дозволити скасування
            const ushort FOF_NOCONFIRMATION = 0x0010;            // Прапорець: Не показувати стандартне вікно підтвердження Windows
            const ushort FOF_SILENT = 0x0004;                    // ДОДАНО: приховує вікно прогресу видалення Windows
            // Формуємо структуру операції. Важливо: шлях pFrom має закінчуватися подвійним нульовим символом
            string doubleNullTerminatedPath = path + '\0' + '\0';
            IntPtr pFromPointer = Marshal.StringToHGlobalUni(doubleNullTerminatedPath);

            try
            {
                Win32Api.SHFILEOPSTRUCT fileOp = new()
                {
                    wFunc = FO_DELETE,
                    pFrom = pFromPointer,
                    fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT
                };

                _ = Win32Api.SHFileOperation(ref fileOp);
            }
            catch { }

            finally
            {
                if (pFromPointer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pFromPointer);
                }
            }
        }

        // Метод перевірки готовності файлу: перевіряє, чи не заблокований файл іншим процесом (наприклад, конвертером)
        public static bool IsFileReady(string filename)
        {
            if (!File.Exists(filename))
            {
                return false;
            }

            try
            {
                // Намагаємося відкрити файл з ексклюзивним доступом (FileShare.None).
                // Якщо конвертер fbc.exe ще пише в нього, виникне IOException.
                using FileStream fs = File.Open(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

                // Додатково перевіряємо, що файл не порожній
                return fs.Length > 0;
            }
            catch (IOException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

        // --- Керування DPI ---
        public static void SetDpiAware()
        {
            try
            {
                _ = Win32Api.SetProcessDpiAwarenessContext(-4);
            }
            catch { }
        }
    }
}
