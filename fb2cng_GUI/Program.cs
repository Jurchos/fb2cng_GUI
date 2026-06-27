using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression; // Потрібна для розпакування ZIP
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

        // Імпорт системних функцій WinAPI для безпечного виконання операцій над файловою системою

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

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
            try { _ = SetProcessDpiAwarenessContext(-4); } catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // --- ДИНАМІЧНЕ ВИЗНАЧЕННЯ АКТУАЛЬНОГО МАРКЕРА ПОМИЛКИ ---
            string tempDir = Path.GetTempPath();
            string markerPath = Path.Combine(tempDir, "fbc_yaml_error.tmp"); // За замовчуванням для v1.5.5+

            try
            {
                // Шукаємо всі можливі файли помилок у Temp
                string[] errorFiles = Directory.GetFiles(tempDir, "*_yaml_error.tmp");

                if (errorFiles.Length > 0)
                {
                    string latestFilePath = errorFiles[0];
                    DateTime latestTime = File.GetLastWriteTime(latestFilePath);

                    // Шукаємо найновіший файл через звичайний цикл
                    foreach (string file in errorFiles)
                    {
                        DateTime currentTime = File.GetLastWriteTime(file);
                        if (currentTime > latestTime)
                        {
                            latestTime = currentTime;
                            latestFilePath = file;
                        }
                    }

                    markerPath = latestFilePath; // Повертаємо знайдений шлях у вашого глобального markerPath
                }
            }
            catch { }
            // --------------------------------------------------------

            // --- РОЗУМНЕ АВТОМАТИЧНЕ ОЧИЩЕННЯ СЕСІЇ ПОМИЛОК ---
            if (File.Exists(markerPath))
            {
                try
                {
                    if (File.Exists(markerPath)) // Додаткова перевірка, щоб не тригерети зайвий раз catch
                    {
                        string content = File.ReadAllText(markerPath);

                        // Якщо всередині маркера записано час закриття вікна
                        if (long.TryParse(content, out long lastErrorTicks))
                        {
                            DateTime lastErrorTime = new DateTime(lastErrorTicks, DateTimeKind.Utc);

                            // Якщо з моменту закриття минулого вікна помилки пройшло БІЛЬШЕ 2 секунд
                            if ((DateTime.UtcNow - lastErrorTime).TotalSeconds > 2)
                            {
                                File.Delete(markerPath); // Це точно нова конвертація користувача, праска очищена!
                            }
                        }
                        else
                        {
                            // Якщо всередині файлу якийсь "мусор" замість цифр і файл порожній — видаляємо його.
                            // Якщо там текст помилки від fbc, не чіпаємо, щоб GUI вивів його користувачу.
                            if (string.IsNullOrWhiteSpace(content))
                            {
                                File.Delete(markerPath);
                            }
                        }
                    }
                }
                catch { }
            }
            // --------------------------------------------------

            // 1. ОЧИЩЕННЯ МАРКЕРА: Очищаємо історію тільки якщо користувач запустив саму програму (GUI) без аргументів
            if (args == null || args.Length == 0)
            {
                // Очищаємо як основний визначений маркер, так і взагалі всі залишки в Temp
                try
                {
                    string[] allMarkers = Directory.GetFiles(tempDir, "*_yaml_error.tmp");
                    foreach (string currentFile in allMarkers)
                    {
                        if (File.Exists(currentFile)) { File.Delete(currentFile); }
                    }
                }
                catch { }
            }

            // 3. ПЕРЕВІРКА НАЯВНОСТІ КОНВЕРТЕРА FBC.EXE
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string fbcPath = Path.Combine(appDir, "fbc.exe");

            if (!File.Exists(fbcPath))
            {
                bool isFirstMissingWindow = false;
                try
                {
                    using (FileStream fs = new FileStream(markerPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        using (StreamWriter writer = new StreamWriter(fs))
                        {
                            // Записуємо ЧАС (Ticks) замість тексту, щоб маркер міг автоматично очиститися!
                            writer.Write(DateTime.UtcNow.Ticks.ToString());
                        }
                    }
                    isFirstMissingWindow = true;
                }
                catch (IOException) { isFirstMissingWindow = false; }
                catch { }

                if (isFirstMissingWindow)
                {
                    AppSettings settings = AppSettings.Load();
                    string lang = settings.Language;

                    // ЧИСТА ПОТРІЙНА ЛОКАЛІЗАЦІЯ: беремо тексти строго зі словника Config.cs
                    string errorText = Localization.Get(lang, "FbcMissingText");
                    string errorTitle = Localization.Get(lang, "FbcMissingTitle");

                    using (Form1 tempForm = new Form1())
                    {
                        _ = tempForm.ShowCustomMessageBox(errorText, errorTitle, buttons: MessageBoxButtons.OK);
                    }

                    // Оновлюємо час у маркері ПІСЛЯ того, як користувач закрив вікно мишкою
                    try { File.WriteAllText(markerPath, DateTime.UtcNow.Ticks.ToString()); } catch { }
                }
                return;
            }

            // Перевіряємо наявність вхідних аргументів (якщо файл чи папку перетягнули на іконку або викликали з меню)
            if (args != null && args.Length > 0)
            {
                string inputPath = args[0];

                // ПЕРЕВІРКА: Користувач клікнув на папку чи на окремий файл?
                if (Directory.Exists(inputPath))
                {
                    // Шукаємо всі файли з розширенням .fb2 в обраній папці та усіх її підпапках (пакетний режим)
                    // 1.Шукаємо абсолютно всі файли в папці та підпапках
                    string[] allFiles = Directory.GetFiles(inputPath, "*.*", SearchOption.AllDirectories);

                    // 2. Створюємо список для відбору книг та архівів
                    System.Collections.Generic.List<string> filteredFiles = new System.Collections.Generic.List<string>();

                    foreach (string file in allFiles)
                    {
                        string lowerFile = file.ToLower();
                        if (lowerFile.EndsWith(".fb2") || lowerFile.EndsWith(".zip"))
                        {
                            filteredFiles.Add(file);
                        }
                    }

                    // 3. ПЕРЕВІРКА: Якщо нічого не знайшли — виходимо
                    if (filteredFiles.Count == 0)
                    {
                        return;
                    }

                    // 4. ОГОЛОШУЄМО МАСИВ (Повертаємо тип string[] назад, щоб зникли всі помилки)
                    string[] fb2Files = filteredFiles.ToArray();

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
                    // Оголошуємо змінні на самому початку блоку обробки файлу
                    bool conversionSuccess = false;
                    string extractedFb2Path = null;
                    // Тепер програма офіційно вважатиме архівом і .zip, і .fb2.zip на самому старті!
                    bool isZipFile = inputPath.ToLower().EndsWith(".zip") || inputPath.ToLower().Contains(".fb2.zip");
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
                        // 2. МИТТЄВИЙ ЗАХИСТ: Якщо маркер помилки вже встановлено попереднім файлом — тихо виходимо
                        if (File.Exists(markerPath))
                        {
                            return;
                        }
                        try
                        {
                            string sourceFb2 = inputPath;
                            AppSettings settings = AppSettings.Load(); // Завантажуємо поточні налаштування оболонки

                            if (isZipFile)
                            {
                                try
                                {
                                    // 1. Створюємо тимчасову папку для повного розпакування
                                    string tempZipDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_zipdir");
                                    ZipFile.ExtractToDirectory(sourceFb2, tempZipDir);

                                    // 2. Шукаємо абсолютно всі файли .fb2 всередині цього архіву
                                    string[] extractedFiles = Directory.GetFiles(tempZipDir, "*.fb2", SearchOption.AllDirectories);
                                    if (extractedFiles.Length > 0)
                                    {
                                        // --- ЯКЩО В АРХІВІ БІЛЬШЕ ОДНОГО ФАЙЛУ ---
                                        if (extractedFiles.Length > 1)
                                        {
                                            string originalDirForZip = Path.GetDirectoryName(inputPath);
                                            string targetDirForZip = settings.UseCustomFolder && Directory.Exists(settings.CustomFolder)
                                                ? settings.CustomFolder
                                                : originalDirForZip;

                                            if (hasHandle)
                                            {
                                                try { mutex.ReleaseMutex(); } catch { }
                                                hasHandle = false;
                                            }
                                            try { mutex.Dispose(); } catch { }

                                            // Прапорець, який покаже, чи всі підпроцеси завершилися успішно
                                            bool allSubProcessesFinished = true;

                                            // Запускаємо копію програми для кожного файлу з архіву СЕПАРАТНО
                                            foreach (string extractedFile in extractedFiles)
                                            {
                                                string safeTmpFb2 = Path.Combine(targetDirForZip, "fbc_tmp_" + Guid.NewGuid().ToString() + "_" + Path.GetFileName(extractedFile));
                                                File.Copy(extractedFile, safeTmpFb2, true);

                                                try
                                                {
                                                    ProcessStartInfo selfPsi = new ProcessStartInfo
                                                    {
                                                        FileName = Application.ExecutablePath,
                                                        Arguments = "\"" + safeTmpFb2 + "\"",
                                                        UseShellExecute = false,
                                                        CreateNoWindow = true
                                                    };
                                                    using (Process selfProcess = Process.Start(selfPsi))
                                                    {
                                                        selfProcess?.WaitForExit();
                                                    }
                                                }
                                                catch { allSubProcessesFinished = false; }
                                                finally
                                                {
                                                    if (File.Exists(safeTmpFb2)) { try { File.Delete(safeTmpFb2); } catch { } }
                                                }
                                            }

                                            // === ОСТАТОЧНИЙ ФІКС ВИДАЛЕННЯ ДЛЯ .ZIP ТА .FB2.ZIP ===
                                            // Якщо користувач увімкнув видалення, то САМЕ ТУТ (батьківський процес) видаляємо оригінальний архів,
                                            // бо підпроцеси коду fbc_tmp_ його не чіпатимуть!
                                            if (allSubProcessesFinished && settings.DeleteAfterConvert && File.Exists(inputPath))
                                            {
                                                Thread.Sleep(200);
                                                if (settings.AutoDeleteToRecycle)
                                                {
                                                    SendToRecycleBin(inputPath); // Відправляємо в Кошик оригінальний .zip або .fb2.zip
                                                }
                                                else
                                                {
                                                    // Якщо увімкнено видалення з підтвердженням — показуємо ОДНЕ вікно на весь архів
                                                    using (Form1 helperForm = new Form1())
                                                    {
                                                        string title = Localization.Get(settings.Language, "ConfirmTitle");
                                                        string rawText = Localization.Get(settings.Language, "ConfirmText");
                                                        string fileName = Path.GetFileName(inputPath);
                                                        string text = string.Format(rawText, fileName);

                                                        if (helperForm.ShowCustomMessageBox(text, title, MessageBoxButtons.OKCancel) == DialogResult.OK)
                                                        {
                                                            for (int i = 0; i < 20; i++)
                                                            {
                                                                if (IsFileReady(inputPath))
                                                                {
                                                                    break;
                                                                }

                                                                Thread.Sleep(100);
                                                            }
                                                            try { File.Delete(inputPath); } catch { }
                                                        }
                                                    }
                                                }
                                            }
                                            // ====================================================

                                            if (Directory.Exists(tempZipDir)) { try { Directory.Delete(tempZipDir, true); } catch { } }
                                            return;
                                        }
                                        else
                                        {
                                            // ЯКЩО В АРХІВІ ВСЬОГО ОДИН ФАЙЛ:
                                            extractedFb2Path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_" + Path.GetFileName(extractedFiles[0]));
                                            File.Copy(extractedFiles[0], extractedFb2Path, true);

                                            // Важливо: залишаємо sourceFb2 як є (тимчасовим для конвертера), 
                                            // але для логіки видалення в кінці програми ми перевикористаємо inputPath!
                                            sourceFb2 = extractedFb2Path;
                                        }
                                    }

                                    if (Directory.Exists(tempZipDir)) { try { Directory.Delete(tempZipDir, true); } catch { } }
                                }
                                catch
                                {
                                    conversionSuccess = false;
                                }
                            }


                            // Визначаємо цільову папку для збереження сконвертованого документа
                            // Спочатку визначаємо оригінальну папку, де лежить вихідний файл (або архів)
                            string originalDir = Path.GetDirectoryName(inputPath); // Фіксуємо рідну папку файлу/архіву до будь-яких розпакувань

                            string targetDir = settings.UseCustomFolder && Directory.Exists(settings.CustomFolder)
                                ? settings.CustomFolder
                                : originalDir; // Завжди зберігаємо результат туди, де лежала книга, а не в Temp!


                            appDir = AppDomain.CurrentDomain.BaseDirectory;
                            fbcPath = Path.Combine(appDir, "fbc.exe"); // Шукаємо консольний конвертер поруч із нашою програмою
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

                            // МОДИФІКАЦІЯ АРГУМЕНТІВ: Якщо увімкнено чекбокс, додаємо команду перезапису файлу --ow
                            if (settings.OverwriteExisting)
                            {
                                fbcArgs += "convert --to " + formatLower + " --ow \"" + sourceFb2 + "\" \"" + targetDir + "\"";
                            }
                            else
                            {
                                // Стандартна поведінка (без перезапису), яка була раніше
                                fbcArgs += "convert --to " + formatLower + " \"" + sourceFb2 + "\" \"" + targetDir + "\"";
                            }

                            // Описуємо базові параметри запуска консольного процесу конвертера
                            ProcessStartInfo psi = new ProcessStartInfo
                            {
                                FileName = fbcPath,
                                Arguments = fbcArgs,
                                CreateNoWindow = true,                  // Повністю приховуємо чорне вікно консолі fbc.exe
                                UseShellExecute = false,
                                WindowStyle = ProcessWindowStyle.Hidden,
                                WorkingDirectory = appDir
                            };

                            conversionSuccess = false;

                            // ЧИСТА ТА ПРАВИЛЬНА ПЕРЕВІРКА РЕЖИМІВ ЗАПУСКУ (БЕЗ ЖОДНИХ НАКЛАДЕНЬ ДУЖОК)
                            if (!settings.HideProgress)
                            {
                                // =========================================================================
                                // --- РЕЖИМ 1: ЗВИЧАЙНИЙ ЗАПУСК З ВІКНОМ ПРОГРЕСУ ---
                                // =========================================================================
                                // Перевіряємо маркер перед створенням форми, щоб уникнути миготіння прогрес-барів
                                if (File.Exists(markerPath))
                                {
                                    return;
                                }

                                bool isDark = settings.Theme == "Dark";
                                Form progressForm = new Form
                                {
                                    Text = settings.Language == "Русский" ? "Конвертация..." : (settings.Language == "Українська" ? "Конвертація..." : "Converting..."),
                                    FormBorderStyle = FormBorderStyle.FixedDialog,
                                    MaximizeBox = false,
                                    MinimizeBox = true,
                                    ShowInTaskbar = true,
                                    StartPosition = FormStartPosition.CenterScreen,
                                    BackColor = isDark ? Color.FromArgb(24, 24, 24) : Color.FromArgb(245, 245, 245),
                                    TopMost = !settings.StartMinimized
                                };

                                // --- 1. АВТОМАТИЧНЕ ВИЗНАЧЕННЯ МАСШТАБУ DPI ДЛЯ ПРОГРЕС-ФОРМИ ---
                                // Вираховуємо точний коефіцієнт масштабу монітора (1.0, 1.25, 1.5, 2.0)
                                float progressScale = progressForm.CreateGraphics().DpiY / 96f;

                                // Масштабуємо розміри самого вікна форми (на 100% було 360x120)
                                int pWidth = (int)(330 * progressScale);
                                int pHeight = (int)(115 * progressScale);
                                progressForm.Size = new Size(pWidth, pHeight);

                                if (settings.StartMinimized)
                                {
                                    progressForm.WindowState = FormWindowState.Minimized;
                                }

                                // --- 2. МАСШТАБУВАННЯ ВНУТРІШНІХ ЕЛЕМЕНТІВ ---
                                int paddingX = (int)(20 * progressScale); // Лівий відступ
                                int fieldWidth = progressForm.ClientSize.Width - (paddingX * 2); // Симетрична корисна ширина

                                Label lblInfo = new Label
                                {
                                    Text = Path.GetFileName(sourceFb2),
                                    Location = new Point(paddingX, (int)(15 * progressScale)),
                                    Size = new Size(fieldWidth, (int)(20 * progressScale)),
                                    ForeColor = isDark ? Color.White : Color.Black,
                                    Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                                    AutoEllipsis = true
                                };

                                ProgressBar progressBar = new ProgressBar
                                {
                                    // Позиціонуємо індикатор строго під текстом на основі його відмасштабованого низу
                                    Location = new Point(paddingX, lblInfo.Bottom + (int)(7 * progressScale)),
                                    Size = new Size(fieldWidth, (int)(18 * progressScale)),
                                    Style = ProgressBarStyle.Marquee,
                                    MarqueeAnimationSpeed = 30
                                };

                                progressForm.Controls.AddRange(new Control[] { lblInfo, progressBar });

                                // --- 3. ФІНАЛЬНА КОРЕКЦІЯ ВИСОТИ ВІКНА ПІД РЕАЛЬНИЙ DPI ---
                                // Збираємо висоту форми як конструктор, щоб рамки Windows нічого не обрізали знизу
                                int requiredHeight = lblInfo.Top + lblInfo.Height + (int)(7 * progressScale) + progressBar.Height + (int)(25 * progressScale);
                                progressForm.ClientSize = new Size(progressForm.ClientSize.Width, requiredHeight);

                                // --- 4. ДОДАТКОВЕ ПЕРЕЦЕНТРУВАННЯ ПІСЛЯ ЗМІНИ КОРДОНІВ ---
                                // Гарантує появу вікна процесу чітко посередині монітора на будь-якому масштабі
                                progressForm.Load += (s, e) =>
                                {
                                    var screen = Screen.FromControl(progressForm).Bounds; // var залишаємо, не змінюємо тип на Rectangle
                                    progressForm.Location = new Point(
                                        screen.Left + ((screen.Width - progressForm.Width) / 2),
                                        screen.Top + ((screen.Height - progressForm.Height) / 2)
                                    );
                                };

                                psi.RedirectStandardOutput = true;
                                psi.RedirectStandardError = true;

                                Thread processThread = new Thread(() =>
                                {
                                    bool hasError = false;

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
                                                else
                                                {
                                                    hasError = true;
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        hasError = true;
                                    }

                                    // Автоматично закриваємо вікно прогресу поточної книги
                                    if (progressForm.IsHandleCreated)
                                    {
                                        _ = progressForm.BeginInvoke(new MethodInvoker(progressForm.Close));
                                    }

                                    // ОБРОБКА КРИТИЧНОГО ЗБОЮ КОНВЕРТАЦІЇ (.yaml АБО ЧЕРЕЗ ПЕРЕЗАПИС)
                                    if (hasError)
                                    {
                                        bool isFirstErrorProcess = false;

                                        try
                                        {
                                            using (FileStream fs = new FileStream(markerPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                                            {
                                                using (StreamWriter writer = new StreamWriter(fs)) { writer.Write("yaml_error"); }
                                            }
                                            isFirstErrorProcess = true;
                                        }
                                        catch (IOException) { isFirstErrorProcess = false; }
                                        catch { }

                                        if (isFirstErrorProcess)
                                        {
                                            string lang = settings.Language;

                                            // Викликаємо нові унікальні ключі для зламаного .yaml чи перезапису
                                            string errorText = Localization.Get(lang, "YamlBrokenText");
                                            string errorTitle = Localization.Get(lang, "YamlBrokenTitle");

                                            DialogResult dialogResult;
                                            using (Form1 tempForm = new Form1())
                                            {
                                                dialogResult = tempForm.ShowCustomMessageBox(errorText, errorTitle, MessageBoxButtons.OK);
                                            }
                                            try
                                            {
                                                // Записуємо поточний час у мілісекундах
                                                File.WriteAllText(markerPath, DateTime.UtcNow.Ticks.ToString());
                                            }
                                            catch { }
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

                                // МИТТЄВИЙ ЗАХИСТ ДЛЯ ТИХОГО РЕЖИМУ: якщо маркер вже є — тихо виходимо без запуску
                                if (File.Exists(markerPath))
                                {
                                    if (hasHandle) { try { mutex.ReleaseMutex(); } catch { } hasHandle = false; }
                                    return;
                                }

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

                                // РЕЗЕРВНИЙ ЗАХИСТ: Якщо файл фізично створився — фіксуємо успех конвертації
                                if (!conversionSuccess)
                                {
                                    string expectedExt = "." + settings.Format.ToLower().Replace("kepub", "epub").Replace("azw8", "azw3");
                                    string expectedFile = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(sourceFb2) + expectedExt);

                                    if (File.Exists(expectedFile))
                                    {
                                        conversionSuccess = true;
                                    }
                                }

                                // ОБРОБКА КРИТИЧНОГО ЗБОЮ КОНВЕРТАЦІЇ ДЛЯ ТИХОГО РЕЖИМУ
                                if (!conversionSuccess)
                                {
                                    bool isFirstErrorProcess = false;
                                    try
                                    {
                                        // Атомарне створення маркера на рівні ОС Windows
                                        using (FileStream fs = new FileStream(markerPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                                        {
                                            using (StreamWriter writer = new StreamWriter(fs))
                                            {
                                                writer.Write(DateTime.UtcNow.Ticks.ToString());
                                            }
                                        }
                                        isFirstErrorProcess = true;
                                    }
                                    catch (IOException) { isFirstErrorProcess = false; }
                                    catch { }

                                    if (isFirstErrorProcess)
                                    {
                                        string lang = settings.Language;

                                        // ЧИСТА ПОТРІЙНА ЛОКАЛІЗАЦІЯ: Беремо тексти строго зі словника Config.cs
                                        string errorText = Localization.Get(lang, "YamlBrokenText");
                                        string errorTitle = Localization.Get(lang, "YamlBrokenTitle");

                                        _ = MessageBox.Show(errorText, errorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);

                                        // Записуємо час закриття вікна користувачем, щоб скинути 2-секундний таймер сесії
                                        try { File.WriteAllText(markerPath, DateTime.UtcNow.Ticks.ToString()); } catch { }
                                    }

                                    if (hasHandle) { try { mutex.ReleaseMutex(); } catch { } hasHandle = false; }
                                    return; // Виходимо, оскільки конвертація провалилася
                                }
                            }
                            // ==================================================
                            // УНІВЕРСАЛЬНА ЛОГІКА ОЧИЩЕННЯ ТА ВИДАЛЕННЯ ФАЙЛІВ
                            // ==================================================
                            if (conversionSuccess && settings.DeleteAfterConvert && File.Exists(sourceFb2))
                            {
                                // --- НАЙНАДІЙНІШИЙ ЗАХИСТ ВІД ТИМЧАСОВИХ ФАЙЛІВ ---
                                // Перевіряємо, чи містить шлях до файлу згадку про тимчасову папку Temp.
                                // Якщо файл лежить у Temp (неважливо, з Guid він чи з fbc_tmp_),
                                // ми ПОВНІСТЮ ІГНОРУЄМО цей блок. Вікна з Guid більше ніколи не з'являться!
                                bool isTemporaryFile = sourceFb2.ToLower().Contains(Path.GetTempPath().ToLower())
                                                    || Path.GetFileName(sourceFb2).ToLower().StartsWith("fbc_tmp_");

                                if (isTemporaryFile)
                                {
                                    // Тихо пропускаємо, цей файл самостійно і чисто видалиться нижче у блоці finally
                                }
                                else
                                {
                                    // ТУТ ПРАЦЮЄ СТАНДАРТНЕ ВИДАЛЕННЯ ТІЛЬКИ ДЛЯ ОРИГІНАЛЬНИХ ФАЙЛІВ КОРИСТУВАЧА:
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
                                            string title = Localization.Get(settings.Language, "ConfirmTitle");
                                            string rawText = Localization.Get(settings.Language, "ConfirmText");
                                            string displayedName = Path.GetFileName(sourceFb2);
                                            string text = string.Format(rawText, displayedName);

                                            if (helperForm.ShowCustomMessageBox(text, title, MessageBoxButtons.OKCancel) == DialogResult.OK)
                                            {
                                                for (int i = 0; i < 20; i++)
                                                {
                                                    if (IsFileReady(sourceFb2))
                                                    {
                                                        break;
                                                    }
                                                    Thread.Sleep(100);
                                                }

                                                try { File.Delete(sourceFb2); }
                                                catch { }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            // === АВТОМАТИЧНЕ ВИДАЛЕННЯ ОРИГІНАЛЬНОГО АРХІВУ .FB2.ZIP ===
                            // Якщо конвертація пройшла успішно, а вхідний файл був архівом .fb2.zip
                            if (conversionSuccess && File.Exists(inputPath) && inputPath.ToLower().Contains(".fb2.zip"))
                            {
                                try
                                {
                                    // АВТОНОМНЕ ЗАВАНТАЖЕННЯ: Усуває помилку CS0103 про відсутність "settings"
                                    AppSettings finallySettings = AppSettings.Load();

                                    if (finallySettings.DeleteAfterConvert)
                                    {
                                        Thread.Sleep(200); // Коротка пауза для зняття дескрипторів

                                        if (finallySettings.AutoDeleteToRecycle)
                                        {
                                            SendToRecycleBin(inputPath); // Тихо відправляємо ОРИГІНАЛЬНИЙ архів у Кошик
                                        }
                                        else
                                        {
                                            // Або показуємо одне красиве вікно з РЕАЛЬНОЮ назвою архіву замість Guid
                                            using (Form1 helperForm = new Form1())
                                            {
                                                string title = Localization.Get(finallySettings.Language, "ConfirmTitle");
                                                string rawText = Localization.Get(finallySettings.Language, "ConfirmText");
                                                string fileName = Path.GetFileName(inputPath); // Отримуємо "книга.fb2.zip"
                                                string text = string.Format(rawText, fileName);

                                                if (helperForm.ShowCustomMessageBox(text, title, MessageBoxButtons.OKCancel) == DialogResult.OK)
                                                {
                                                    for (int i = 0; i < 20; i++)
                                                    {
                                                        if (IsFileReady(inputPath))
                                                        {
                                                            break;
                                                        }

                                                        Thread.Sleep(100);
                                                    }
                                                    try { File.Delete(inputPath); } catch { }
                                                }
                                            }
                                        }
                                    }
                                }
                                catch { }
                            }
                            // ==========================================================

                            // --- НАДІЙНЕ ВИДАЛЕННЯ ТИМЧАСОВОГО РОЗПАКОВАНОГО ФАЙЛУ (Ваш стандартний код) ---
                            if (extractedFb2Path != null && File.Exists(extractedFb2Path))
                            {
                                try { File.Delete(extractedFb2Path); } catch { }
                            }
                            // ---------------------------------------------------------

                            if (hasHandle)
                            {
                                try { mutex.ReleaseMutex(); } catch { }
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