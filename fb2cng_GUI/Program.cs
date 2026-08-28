using System.Diagnostics;

namespace fb2cngGUI
{

    internal static class Program
    {
        [STAThread]
        internal static void Main(string[] args)
        {
            // Налаштування DPI
            _ = Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Режим GUI
            if (args == null || args.Length == 0)
            {
                // Перевіряємо, чи вже відкрите вікно налаштувань
                if (CheckAndActivateExistingGui())
                {
                    return;
                }

                MarkerService.Clear(); // Очищаємо маркери при старті інтерфейсу
                Application.Run(new Form1());
                return;
            }

            // Режим Конвертації
            // Перевірка fbc.exe

            if (!CheckFbcComponent())
            {
                return;
            }

            string inputPath = args[0];
            ConversService conversService = new(AppSettings.Current);

            // Вмикаємо "слухач" гарячих клавіш
            using GlobalHotkeyListener hotkeyListener = new();

            if (Directory.Exists(inputPath))
            {
                conversService.ProcessDirectory(inputPath);
            }
            else if (File.Exists(inputPath))
            {
                _ = conversService.ProcessSingleFile(inputPath);
            }
        }

        private static bool CheckFbcComponent()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string fbcPath = Path.Combine(appDir, Core.ConverterExe);

            // 1. Якщо файл є — все добре
            if (File.Exists(fbcPath))
            {
                return true;
            }

            // Використовуємо Mutex для перевірки, чи ми перші, хто покаже помилку
            if (MarkerService.TryAcquireExclusivity(out Mutex mutex))
            {
                using (mutex) // Mutex буде звільнено після закриття блоку
                {
                    AppSettings settings = AppSettings.Current;
                    string lang = settings.Language;

                    string errorText = Localization.Get(lang, "FbcMissingText");
                    string errorTitle = Localization.Get(lang, "FbcMissingTitle");

                    _ = MessageService.ShowCustomMessageBox(errorText, errorTitle, buttons: MessageBoxButtons.OK);

                    // Вмикаємо сигнал зупинки в пам'яті для решти процесів
                    MarkerService.Signal();
                }
            }
            return false;
        }

        private static bool CheckAndActivateExistingGui()
        {
            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);
            if (processes.Length > 1)
            {
                foreach (Process process in processes)
                {
                    // Шукаємо інший процес з таким самим ім'ям, у якого є вікно
                    if (process.Id != current.Id && process.MainWindowHandle != IntPtr.Zero)
                    {
                        IntPtr hWnd = process.MainWindowHandle;
                        if (Win32Api.IsIconic(hWnd))
                        {
                            _ = Win32Api.ShowWindow(hWnd, 9); // SW_RESTORE
                        }

                        _ = Win32Api.SetForegroundWindow(hWnd);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}