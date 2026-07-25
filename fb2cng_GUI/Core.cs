using System.Runtime.InteropServices;

namespace fb2cngGUI
{
    public static class Core
    {
        // Метод безпечного видалення файлу — відправка його у Кошик Windows замість повного стирання
        public static void SendToRecycleBin(string path)
        {
            const uint FO_DELETE = 0x0003;                       // Код операції: Видалення
            const ushort FOF_ALLOWUNDO = 0x0040;                 // Прапорець: Дозволити скасування
            const ushort FOF_NOCONFIRMATION = 0x0010;            // Прапорець: Не показувати стандартне вікно підтвердження Windows
            // Формуємо структуру операції. Важливо: шлях pFrom має закінчуватися подвійним нульовим символом
            string doubleNullTerminatedPath = path + '\0' + '\0';
            IntPtr pFromPointer = Marshal.StringToHGlobalUni(doubleNullTerminatedPath);

            try
            {
                Win32Api.SHFILEOPSTRUCT fileOp = new()
                {
                    wFunc = FO_DELETE,
                    pFrom = pFromPointer,
                    fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION
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
            try { using FileStream fs = File.Open(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.None); return true; }
            catch { return false; }
        }

        // --- Керування DPI ---
        public static void SetDpiAware()
        {
            try { _ = Win32Api.SetProcessDpiAwarenessContext(-4); } catch { }
        }

        // Керування маркером помилки
        private static readonly string MarkerPath = Path.Combine(Path.GetTempPath(), "fbc_yaml_error.tmp");

        public static void ClearAllMarkers()
        {
            try
            {
                foreach (string file in Directory.GetFiles(Path.GetTempPath(), "*_yaml_error.tmp"))
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch { }
        }
    }
}
