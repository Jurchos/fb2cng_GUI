using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace fb2cng_GUI
{
    internal static class Program
    {
        // Функція WinAPI для коректного налаштування масштабування інтерфейсу (DPI) на моніторах з високою роздільною здатністю
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwarenessContext(int dpiFlag);

        // --- БЛОК WINDOWS API ДЛЯ РОБОТИ З КОШИКОМ ---
        // Структура, яка описує параметри файлової операції для функції SHFileOperation
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;                  // Дескриптор вікна-власника діалогу операції
            public uint wFunc;                   // Тип операції (наприклад, FO_DELETE — видалення)
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pFrom;                 // Шлях до вихідного файлу (має завершуватися двома символами \0)
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pTo;                   // Шлях до цільового файлу (при копіюванні чи переміщенні)
            public ushort fFlags;                // Прапорці керування операцією (скасування підтвердження, підтримка Undo тощо)
            public bool fAnyOperationsAborted;   // Повертає true, якщо користувач перервав операцію до її завершення
            public IntPtr hNameMappings;          // Об'єкт зіставлення імен файлів (використовується рідко)
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszProgressTitle;     // Текст заголовка вікна прогресу видалення
        }

        // Імпорт системної функції WinAPI для безпечного виконання операцій над файловою системою
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        // Метод безпечного видалення файлу — відправка його у Кошик Windows замість повного стирання
        private static void SendToRecycleBin(string path)
        {
            const uint FO_DELETE = 0x0003;          // Код операції: Видалення
            const ushort FOF_ALLOWUNDO = 0x0040;     // Прапорець: Дозволити скасування операції (перемістити в Кошик)
            const ushort FOF_NOCONFIRMATION = 0x0010; // Прапорець: Не показувати стандартне вікно підтвердження Windows

            // Формуємо структуру операції. Важливо: шлях pFrom має закінчуватися подвійним нульовим символом
            SHFILEOPSTRUCT fileOp = new SHFILEOPSTRUCT
            {
                wFunc = FO_DELETE,
                pFrom = path + '\0' + '\0',
                fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION
            };

            try { _ = SHFileOperation(ref fileOp); } catch { }
        }

        // Метод перевірки готовності файлу: перевіряє, чи не заблокований файл іншим процесом (наприклад, конвертером)
        private static bool IsFileReady(string filename)
        {
            try
            {
                // Намагаємося ексклюзивно відкрити файл. Якщо успішно — файл вільний і готовий до видалення
                using (FileStream fileStream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    return true;
                }
            }
            catch { return false; } // Якщо виникла помилка доступу, значить файл ще зайнятий
        }

        // Головна точка входу в програму
        [STAThread]
        private static void Main(string[] args)
        {
            // Вмикаємо правильне масштабування шрифтів та інтерфейсу додатка (DPI-Aware)
            try { _ = SetProcessDpiAwarenessContext(-4); } catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Перевіряємо наявність вхідних аргументів (якщо файл чи папку перетягнули на іконку або викликали з меню)
            if (args != null && args.Length > 0)
            {
                string inputPath = args[0];

                // ПЕРЕВІРКА: Користувач клікнув на папку чи на окремий файл?
                if (Directory.Exists(inputPath))
                {
                    // Шукаємо всі файли з розширенням .fb2 в обраній папці та усіх її підпапках (пакетний режим)
                    string[] fb2Files = Directory.GetFiles(inputPath, "*.fb2", SearchOption.AllDirectories);

                    if (fb2Files.Length == 0)
                    {
                        return;
                    }

                    // Послідовно запускаємо нову копію нашої програми для кожного знайденого файлу
                    foreach (string file in fb2Files)
                    {
                        try
                        {
                            ProcessStartInfo selfPsi = new ProcessStartInfo
                            {
                                FileName = Application.ExecutablePath,
                                Arguments = "\"" + file + "\"", // Передаємо шлях до конкретного fb2 файлу
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                            using (Process selfProcess = Process.Start(selfPsi))
                            {
                                selfProcess?.WaitForExit(); // Чекаємо завершення обробки поточного файлу перед переходом до наступного
                            }
                        }
                        catch { }
                    }
                    return;
                }
                else if (File.Exists(inputPath))
                {
                    // ЛОГІКА ДЛЯ ОБРОБКИ ОДНОГО КОНКРЕТНОГО ФАЙЛУ (З ЧЕРГОЮ ЧЕРЕЗ СИСТЕМНИЙ MUTEX)
                    // Mutex дозволяє впорядкувати запуск конвертації: якщо виділено 10 файлів одночасно, вони оброблятимуться строго по черзі
                    using (Mutex mutex = new Mutex(false, "Global\\fb2cng_GUI_Queue_Mutex"))
                    {
                        bool hasHandle = false;
                        try
                        {
                            // Очікуємо своєї черги на обробку протягом максимум 5 хвилин
                            hasHandle = mutex.WaitOne(TimeSpan.FromMinutes(5));

                            // Якщо таймаут вийшов і черга не дійшла
                            if (!hasHandle)
                            {
                                // Оскільки це фонова черга файлів, можна просто вийти, 
                                // або показати повідомлення. Перериваємо обробку цього файлу:
                                return;
                            }
                        }
                        catch (AbandonedMutexException)
                        {
                            // Якщо попередній процес аварійно завершився, м'ютекс переходить до нас
                            hasHandle = true;
                        }

                        try
                        {
                            string sourceFb2 = inputPath;
                            AppSettings settings = AppSettings.Load(); // Завантажуємо поточні налаштування оболонки

                            // Визначаємо цільову папку для збереження сконвертованого документа
                            string targetDir = settings.UseCustomFolder && Directory.Exists(settings.CustomFolder)
                                ? settings.CustomFolder
                                : Path.GetDirectoryName(sourceFb2);

                            string appDir = AppDomain.CurrentDomain.BaseDirectory;
                            string fbcPath = Path.Combine(appDir, "fbc.exe"); // Шукаємо консольний конвертер поруч із нашою програмою
                            if (!File.Exists(fbcPath))
                            {
                                return;
                            }

                            string formatLower = settings.Format.ToLower();
                            string fbcArgs = "";

                            // Якщо ввімкнено використання кастомного конфігу YAML — додаємо відповідний аргумент
                            if (settings.UseCustomConfig && File.Exists(settings.CustomConfig))
                            {
                                fbcArgs += "-c \"" + settings.CustomConfig + "\" ";
                            }

                            // Формуємо повний рядок аргументів для виклику консольного fbc.exe
                            fbcArgs += "convert --to " + formatLower + " \"" + sourceFb2 + "\" \"" + targetDir + "\"";

                            // Описуємо базові параметри запуску консольного процесу конвертера
                            ProcessStartInfo psi = new ProcessStartInfo
                            {
                                FileName = fbcPath,
                                Arguments = fbcArgs,
                                CreateNoWindow = true,                  // Повністю приховуємо чорне вікно консолі fbc.exe
                                UseShellExecute = false,
                                WindowStyle = ProcessWindowStyle.Hidden,
                                WorkingDirectory = appDir
                            };

                            bool conversionSuccess = false;

                            // ЧИСТА ТА ПРАВИЛЬНА ПЕРЕВІРКА РЕЖИМІВ ЗАПУСКУ (БЕЗ ЖОДНИХ НАКЛАДЕНЬ ДУЖОК)
                            if (!settings.HideProgress)
                            {
                                // =========================================================================
                                // --- РЕЖИМ 1: ЗВИЧАЙНИЙ ЗАПУСК З ВІКНОМ ПРОГРЕСУ ---
                                // =========================================================================
                                bool isDark = settings.Theme == "Dark";
                                Form progressForm = new Form
                                {
                                    Text = settings.Language == "Русский" ? "Конвертация..." : (settings.Language == "Українська" ? "Конвертація..." : "Converting..."),
                                    Size = new Size(360, 120),
                                    FormBorderStyle = FormBorderStyle.FixedDialog,
                                    MaximizeBox = false,
                                    MinimizeBox = true,
                                    ShowInTaskbar = true,
                                    StartPosition = FormStartPosition.CenterScreen,
                                    BackColor = isDark ? Color.FromArgb(24, 24, 24) : Color.FromArgb(245, 245, 245),
                                    TopMost = !settings.StartMinimized
                                };

                                if (settings.StartMinimized)
                                {
                                    progressForm.WindowState = FormWindowState.Minimized;
                                }

                                Label lblInfo = new Label
                                {
                                    Text = Path.GetFileName(sourceFb2),
                                    Location = new Point(20, 15),
                                    Size = new Size(310, 20),
                                    ForeColor = isDark ? Color.White : Color.Black,
                                    Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                                    AutoEllipsis = true
                                };

                                ProgressBar progressBar = new ProgressBar
                                {
                                    Location = new Point(20, 42),
                                    Size = new Size(305, 18),
                                    Style = ProgressBarStyle.Marquee,
                                    MarqueeAnimationSpeed = 30
                                };

                                progressForm.Controls.AddRange(new Control[] { lblInfo, progressBar });

                                // ДЛЯ РЕЖИМУ З ВІКНОМ ОБОВ'ЯЗКОВО ВМИКАЕМ ПЕРЕНАПРАВЛЕННЯ ПОТОКІВ ТА ЧИТАЄМО ЇХ
                                psi.RedirectStandardOutput = true;
                                psi.RedirectStandardError = true;

                                Thread processThread = new Thread(() =>
                                {
                                    try
                                    {
                                        using (Process p = Process.Start(psi))
                                        {
                                            if (p != null)
                                            {
                                                p.BeginOutputReadLine();
                                                p.BeginErrorReadLine();
                                                p.WaitForExit();
                                                if (p.ExitCode == 0)
                                                {
                                                    conversionSuccess = true;
                                                }
                                            }
                                        }
                                    }
                                    catch { }

                                    if (progressForm.IsHandleCreated)
                                    {
                                        if (progressForm.InvokeRequired)
                                        {
                                            _ = progressForm.BeginInvoke(new MethodInvoker(progressForm.Close));
                                        }
                                        else
                                        {
                                            progressForm.Close();
                                        }
                                    }
                                });

                                processThread.Start();
                                _ = progressForm.ShowDialog();
                                processThread.Join();
                            }
                            else
                            {
                                // =========================================================================
                                // --- РЕЖИМ 2: ПОВНІСТЮ ТИХИЙ ФОНОВИЙ ЗАПУСК КОНВЕРТЕРА ---
                                // =========================================================================
                                try
                                {
                                    // ПРИМУСОВО ВИМИКАЄМО ПЕРЕНАПРАВЛЕННЯ ПОТОКІВ, ЩОБ БУФЕР ОС НЕ БЛОКУВАВ ПРОЦЕС
                                    psi.RedirectStandardOutput = false;
                                    psi.RedirectStandardError = false;

                                    using (Process p = Process.Start(psi))
                                    {
                                        if (p != null)
                                        {
                                            p.WaitForExit(); // Конвертер тихо виконає роботу у фоні та закриється
                                            if (p.ExitCode == 0)
                                            {
                                                conversionSuccess = true;
                                            }
                                        }
                                    }
                                }
                                catch { }

                                // РЕЗЕРВНИЙ ЗАХИСТ: Якщо файл фізично створився — фіксуємо успіх конвертації
                                if (!conversionSuccess)
                                {
                                    string expectedExt = "." + settings.Format.ToLower().Replace("kepub", "epub").Replace("azw8", "azw3");
                                    string expectedFile = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(sourceFb2) + expectedExt);

                                    if (File.Exists(expectedFile))
                                    {
                                        conversionSuccess = true;
                                    }
                                }
                            }
                            // =========================================================================
                            // ЛОГІКА ОЧИЩЕННЯ ТА ВИДАЛЕННЯ ФАЙЛІВ (Винесена за межі умов, працює завжди!)
                            // =========================================================================
                            if (conversionSuccess && settings.DeleteAfterConvert && File.Exists(sourceFb2))
                            {
                                // Невелика пауза (200 мс) для того, щоб ОС гарантовано зняла всі дескриптори з файлу
                                Thread.Sleep(200);

                                // Режим 1: Автоматичне тихе видалення в Кошик Windows
                                if (settings.AutoDeleteToRecycle)
                                {
                                    SendToRecycleBin(sourceFb2);
                                }
                                // Режим 2: Повне видалення з викликом кастомного діалогового вікна підтвердження
                                else
                                {
                                    using (Form1 helperForm = new Form1())
                                    {
                                        // Отримуємо локалізовані тексти для вікна підтвердження
                                        string title = Localization.Get(settings.Language, "ConfirmTitle");
                                        string rawText = Localization.Get(settings.Language, "ConfirmText");

                                        // Витягуємо тільки назву файлу (наприклад, "Книга.fb2") замість повного шляху
                                        string fileName = Path.GetFileName(sourceFb2);

                                        // Підставляємо назву файлу в наш локалізований шаблон тексту
                                        string text = string.Format(rawText, fileName);

                                        // Якщо користувач натиснув кнопку "OK" у кастомному MessageBox (текст буде по центру)
                                        if (helperForm.ShowCustomMessageBox(text, title, MessageBoxButtons.OKCancel) == DialogResult.OK)
                                        {
                                            // Цикл очікування звільнення файлу (до 2 секунд), якщо диск повільний або зайнятий
                                            for (int i = 0; i < 20; i++)
                                            {
                                                if (IsFileReady(sourceFb2))
                                                {
                                                    break;
                                                }

                                                Thread.Sleep(100);
                                            }

                                            // Остаточне безповоротне видалення файлу з диску
                                            try { File.Delete(sourceFb2); } catch { }
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            // Обов'язково звільняємо системний Mutex, щоб наступний файл у черзі міг почати конвертацію
                            // Звільняємо м'ютекс ТІЛЬКИ якщо ми його успішно захопили
                            if (hasHandle)
                            {
                                mutex.ReleaseMutex();
                            }
                        }
                    }
                }
            }
            else
            {
                // ЯКЩО АРГУМЕНТІВ ЗАПУСКУ НЕМАЄ: Просто запускаємо головне вікно налаштувань програми
                Application.Run(new Form1());
            }
        }
    }
}
