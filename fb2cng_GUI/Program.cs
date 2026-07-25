using System.Diagnostics;
using System.IO.Compression;

namespace fb2cngGUI
{
    internal static class Program
    {
        [STAThread]
        internal static void Main(string[] args)
        {
            // 1. Налаштування DPI
            _ = Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 2. Визначення актуального маркера та розумне очищення сесії
            string markerPath = GetMarkerPathAndSmartCleanup();

            // 3. Якщо запуск без аргументів — режим GUI
            if (args == null || args.Length == 0)
            {
                // ОЧИЩЕННЯ МАРКЕРА: видаляємо всі залишки в Temp
                Core.ClearAllMarkers();

                Application.Run(new Form1());
                return;
            }

            // 4. Перевірка компонента FBC.EXE
            if (!CheckFbcComponent(markerPath))
            {
                return;
            }

            // 5. Вибір режиму обробки: Папка чи Файл
            string inputPath = args[0];

            if (Directory.Exists(inputPath))
            {
                ProcessDirectory(inputPath);
            }
            else if (File.Exists(inputPath))
            {
                ProcessSingleFile(inputPath, markerPath);
            }
        }

        private static string GetMarkerPathAndSmartCleanup()
        {
            string tempDir = Path.GetTempPath();
            // Шлях за замовчуванням (якщо маркерів ще не існує)
            string markerPath = Path.Combine(tempDir, "fbc_yaml_error.tmp");

            try
            {
                // 1. ШУКАЄМО ВСІ МАРКЕРИ (підтримуємо fbc_..., fb2cng_... та будь-які інші версії)
                string[] errorFiles = Directory.GetFiles(tempDir, "*_yaml_error.tmp");

                if (errorFiles.Length > 0)
                {
                    string latestFilePath = errorFiles[0];
                    DateTime latestTime = File.GetLastWriteTime(latestFilePath);

                    // Шукаємо серед знайдених найновіший файл за часом останньої зміни
                    foreach (string file in errorFiles)
                    {
                        DateTime currentTime = File.GetLastWriteTime(file);
                        if (currentTime > latestTime)
                        {
                            latestTime = currentTime;
                            latestFilePath = file;
                        }
                    }
                    markerPath = latestFilePath; // Вибираємо найактуальніший маркер
                }

                // 2. БЕЗПЕЧНЕ ЧИТАННЯ (захист від блокування файлу іншим процесом)
                if (File.Exists(markerPath))
                {
                    string content = "";

                    // Використовуємо спеціальний режим FileShare.ReadWrite, 
                    // який дозволяє читати файл, навіть якщо в цей момент інший конвертер у нього пише
                    using (FileStream fs = new(markerPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (StreamReader sr = new(fs))
                    {
                        content = sr.ReadToEnd();
                    }

                    if (!string.IsNullOrEmpty(content))
                    {
                        // Якщо всередині записано час (Ticks)
                        if (long.TryParse(content, out long lastErrorTicks))
                        {
                            DateTime lastErrorTime = new(lastErrorTicks, DateTimeKind.Utc);

                            // Якщо з моменту закриття вікна помилки пройшло більше 2 секунд - очищаємо сесію
                            if ((DateTime.UtcNow - lastErrorTime).TotalSeconds > 2)
                            {
                                File.Delete(markerPath);
                            }
                        }
                        // Якщо файл порожній (сміття) - теж видаляємо
                        else if (string.IsNullOrWhiteSpace(content))
                        {
                            File.Delete(markerPath);
                        }
                    }
                }
            }
            catch { } // Будь-які помилки доступу просто ігноруємо, щоб не переривати роботу

            return markerPath;
        }

        // 3. ПЕРЕВІРКА НАЯВНОСТІ КОНВЕРТЕРА FBC.EXE
        private static bool CheckFbcComponent(string markerPath)
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string fbcPath = Path.Combine(appDir, "fbc.exe");

            if (File.Exists(fbcPath)) return true;

            bool isFirstMissingWindow = false;
            try
            {
                using (FileStream fs = new(markerPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    using StreamWriter writer = new(fs);
                    // Записуємо ЧАС (Ticks) замість тексту, щоб маркер міг автоматично очиститися!
                    writer.Write(DateTime.UtcNow.Ticks.ToString());
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

                using (Form1 tempForm = new())
                {
                    _ = tempForm.ShowCustomMessageBox(errorText, errorTitle, buttons: MessageBoxButtons.OK);
                }

                // Оновлюємо час у маркері ПІСЛЯ того, як користувач закрив вікно мишкою
                try { File.WriteAllText(markerPath, DateTime.UtcNow.Ticks.ToString()); } catch { }
            }
            return false;
        }

        private static void ProcessDirectory(string inputPath)
        {
            // Шукаємо всі файли з розширенням .fb2 в обраній папці та усіх її підпапках (пакетний режим)
            // 1.Шукаємо абсолютно всі файли в папці та підпапках
            string[] allFiles = Directory.GetFiles(inputPath, "*.*", SearchOption.AllDirectories);

            // 2. Створюємо список для відбору книг та архівів
            List<string> filteredFiles = [];

            foreach (string file in allFiles)
            {
                string lowerFile = file.ToLowerInvariant();
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
            string[] fb2Files = [.. filteredFiles];

            // Послідовно запускаємо нову копію нашої програми для кожного знайденого файлу
            foreach (string file in fb2Files)
            {
                try
                {
                    ProcessStartInfo selfPsi = new()
                    {
                        FileName = Application.ExecutablePath,
                        Arguments = "\"" + file + "\"", // Передаємо шлях до конкретного fb2 файлу
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using Process? selfProcess = Process.Start(selfPsi);
                    selfProcess?.WaitForExit(); // Чекаємо завершення обробки поточного файлу перед переходом до наступного
                }
                catch { }
            }
        }

        private static void ProcessSingleFile(string inputPath, string markerPath)
        {
            // Оголошуємо одну змінну на самому початку блоку обробки файлу
            string? extractedFb2Path = null;
            // Тепер програма офіційно вважатиме архівом і .zip, і .fb2.zip на самому старті!
            bool isZipFile = inputPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                     inputPath.Contains(".fb2.zip", StringComparison.OrdinalIgnoreCase);
            // ЛОГІКА ДЛЯ ОБРОБКИ ОДНОГО КОНКРЕТНОГО ФАЙЛУ (З ЧЕРГОЮ ЧЕРЕЗ СИСТЕМНИЙ MUTEX)
            // Mutex дозволяє впорядкувати запуск конвертації: якщо виділено 10 файлів одночасно, вони оброблятимуться строго по черзі
            using Mutex mutex = new(false, "Global\\fb2cng_GUI_Queue_Mutex");
            bool hasHandle = false;
            try
            {
                // Очікуємо своєї черги на обробку протягом максимум 5 хвилин
                hasHandle = mutex.WaitOne(TimeSpan.FromMinutes(5));

                // Якщо таймаут вийшов і черга не дійшла
                if (!hasHandle || File.Exists(markerPath))
                {
                    // Якщо раптом дескриптор отримано (але файл-маркер існує), звільняємо його перед виходом
                    if (hasHandle)
                    {
                        try { mutex.ReleaseMutex(); } catch { }
                    }

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
                if (hasHandle)
                {
                    try { mutex.ReleaseMutex(); } catch { }
                }

                return;
            }
            try
            {
                string sourceFb2 = inputPath;
                AppSettings settings = AppSettings.Load(); // Завантажуємо поточні налаштування оболонки

                if (isZipFile)
                {
                    string? extracted = RunZipExtractionLogic(inputPath, settings, out extractedFb2Path, ref hasHandle, mutex);

                    // Якщо повернувся null та це архів — значить файли оброблені пакетом всередині
                    if (extracted == null || (extractedFb2Path == null && !hasHandle))
                    {
                        return;
                    }
                    sourceFb2 = extracted;
                }
                // 1. Виклик конвертації 
                // Другу зміннуголошуємо і присвоюємо, безпосередньо перед використанням, де вона гарантовано читається. Visual Studio задоволена.
                bool conversionSuccess = RunFbcProcess(sourceFb2, inputPath, settings, markerPath, ref hasHandle, mutex);
                // 2. Виклик видалення
                RunDeletionLogic(sourceFb2, inputPath, conversionSuccess, settings);
            }

            finally
            {
                if (extractedFb2Path != null && File.Exists(extractedFb2Path))
                {
                    try { File.Delete(extractedFb2Path); } catch { }
                }

                if (hasHandle)
                {
                    try { mutex.ReleaseMutex(); } catch { }
                }
            }
        }

        private static string? RunZipExtractionLogic(string inputPath, AppSettings settings, out string? extractedFb2Path, ref bool hasHandle, Mutex mutex)
        {
            extractedFb2Path = null;
            try
            {
                // 1. Створюємо тимчасову папку для повного розпакування
                string tempZipDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_zipdir");
                ZipFile.ExtractToDirectory(inputPath, tempZipDir);

                // 2. Шукаємо абсолютно всі файли .fb2 всередині цього архіву
                string[] extractedFiles = Directory.GetFiles(tempZipDir, "*.fb2", SearchOption.AllDirectories);

                // --- ЯКЩО В АРХІВІ БІЛЬШЕ ОДНОГО ФАЙЛУ ---
                if (extractedFiles.Length > 1)
                {
                    string originalDirForZip = Path.GetDirectoryName(inputPath) ?? "";
                    string targetDirForZip = settings.UseCustomFolder && Directory.Exists(settings.CustomFolder)
                        ? settings.CustomFolder
                        : (string.IsNullOrEmpty(originalDirForZip) ? "." : originalDirForZip);

                    if (hasHandle)
                    {
                        try { mutex.ReleaseMutex(); } catch { }
                        hasHandle = false;
                    }

                    // Прапорець, який покаже, чи всі підпроцеси завершилися успішно
                    bool allSubProcessesFinished = true;

                    // Запускаємо копію програми для кожного файлу з архіву СЕПАРАТНО
                    foreach (string extractedFile in extractedFiles)
                    {
                        string safeTmpFb2 = Path.Combine(targetDirForZip, "fbc_tmp_" + Guid.NewGuid().ToString() + "_" + Path.GetFileName(extractedFile));
                        File.Copy(extractedFile, safeTmpFb2, true);

                        try
                        {
                            ProcessStartInfo selfPsi = new()
                            {
                                FileName = Application.ExecutablePath,
                                Arguments = "\"" + safeTmpFb2 + "\"",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                            using Process? selfProcess = Process.Start(selfPsi);
                            selfProcess?.WaitForExit();
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
                            Core.SendToRecycleBin(inputPath); // Відправляємо в Кошик оригінальний .zip або .fb2.zip
                        }
                        else
                        {
                            // Якщо увімкнено видалення з підтвердженням — показуємо ОДНЕ вікно на весь архів
                            using Form1 helperForm = new();
                            string title = Localization.Get(settings.Language, "ConfirmTitle");
                            string rawText = Localization.Get(settings.Language, "ConfirmText");
                            string fileName = Path.GetFileName(inputPath);
                            string text = string.Format(rawText, fileName);

                            if (helperForm.ShowCustomMessageBox(text, title, MessageBoxButtons.OKCancel) == DialogResult.OK)
                            {
                                for (int i = 0; i < 20; i++)
                                {
                                    if (Core.IsFileReady(inputPath))
                                    {
                                        break;
                                    }

                                    Thread.Sleep(100);
                                }
                                try { File.Delete(inputPath); } catch { }
                            }
                        }
                    }
                    if (Directory.Exists(tempZipDir))
                    {
                        try { Directory.Delete(tempZipDir, true); } catch { }
                    }

                    return null;
                }
                else if (extractedFiles.Length == 1) // ЯКЩО В АРХІВІ ВСЬОГО ОДИН ФАЙЛ
                {
                    extractedFb2Path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_" + Path.GetFileName(extractedFiles[0]));
                    File.Copy(extractedFiles[0], extractedFb2Path, true);
                    if (Directory.Exists(tempZipDir))
                    {
                        try { Directory.Delete(tempZipDir, true); } catch { }
                    }

                    return extractedFb2Path;
                }
                if (Directory.Exists(tempZipDir))
                {
                    try { Directory.Delete(tempZipDir, true); } catch { }
                }
            }
            catch { }
            return null;
        }

        private static bool RunFbcProcess(string sourceFb2, string inputPath, AppSettings settings, string markerPath, ref bool hasHandle, Mutex mutex)
        {
            // Визначаємо цільову папку для збереження сконвертованого документа
            // Спочатку визначаємо оригінальну папку, де лежить вихідний файл (або архів)
            // 1. Якщо GetDirectoryName поверне null, підставляємо "." (поточна папка)
            string originalDir = Path.GetDirectoryName(inputPath) ?? ".";  // Фіксуємо рідну папку файлу/архіву до будь-яких розпакувань

            // 2. Тепер originalDir — це гарантовано рядок, і targetDir прийме його без попереджень
            string targetDir = (settings.UseCustomFolder && Directory.Exists(settings.CustomFolder))
                ? settings.CustomFolder
                : originalDir; // Завжди зберігаємо результат туди, де лежала книга, а не в Temp!


            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string fbcPath = Path.Combine(appDir, "fbc.exe"); // Шукаємо консольний конвертер поруч із нашою програмою
            if (!File.Exists(fbcPath))
            {
                return false;
            }

            string formatLower = settings.Format.ToLowerInvariant();
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
            ProcessStartInfo psi = new()
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
                // Перевіряємо маркер перед створенням форми, щоб уникнути миготіння прогрес-барів
                if (File.Exists(markerPath))
                {
                    return false;
                }

                bool isDark = settings.Theme == "Dark";
                Form progressForm = new()
                {

                    Text = Localization.Get(settings.Language, "ProgressTitle") ?? "Converting...",
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

                Label lblInfo = new()
                {
                    Text = Path.GetFileName(sourceFb2),
                    Location = new Point(paddingX, (int)(15 * progressScale)),
                    Size = new Size(fieldWidth, (int)(20 * progressScale)),
                    ForeColor = isDark ? Color.White : Color.Black,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                    AutoEllipsis = true
                };

                ProgressBar progressBar = new()
                {
                    // Позиціонуємо індикатор строго под текстом на основі його відмасштабованого низу
                    Location = new Point(paddingX, lblInfo.Bottom + (int)(7 * progressScale)),
                    Size = new Size(fieldWidth, (int)(18 * progressScale)),
                    Style = ProgressBarStyle.Marquee,
                    MarqueeAnimationSpeed = 30
                };

                progressForm.Controls.AddRange([lblInfo, progressBar]);

                // --- 3. ФІНАЛЬНА КОРЕКЦІЯ ВИСОТИ ВІКНА ПІД РЕАЛЬНИЙ DPI ---
                // Збираємо висоту форми як конструктор, щоб рамки Windows нічого не обрізали знизу
                int requiredHeight = lblInfo.Top + lblInfo.Height + (int)(7 * progressScale) + progressBar.Height + (int)(25 * progressScale);
                progressForm.ClientSize = new Size(progressForm.ClientSize.Width, requiredHeight);

                // --- 4. ДОДАТКОВЕ ПЕРЕЦЕНТРУВАННЯ ПІСЛЯ ЗМІНИ КОРДОНІВ ---
                // Гарантує появу вікна процесу чітко посередині монітора на будь-якому масштабі
                progressForm.Load += (s, e) =>
                {
                    Rectangle screen = Screen.FromControl(progressForm).Bounds;
                    progressForm.Location = new Point(
                        screen.Left + ((screen.Width - progressForm.Width) / 2),
                        screen.Top + ((screen.Height - progressForm.Height) / 2)
                    );
                };

                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                Thread processThread = new(() =>
                {
                    bool hasError = false;
                    System.Text.StringBuilder errorTextCollector = new(); // Для логу

                    try
                    {
                        using Process? p = Process.Start(psi);
                        if (p != null)
                        {
                            // Пасивно збираємо текст для логу
                            p.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) { _ = errorTextCollector.AppendLine(e.Data); } };
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
                                // ПИШЕМО В ЛОГ ТІЛЬКИ ТУТ
                                // Беремо лише перший рядок помилки, щоб уникнути дублювання від fbc.exe
                                string fullError = errorTextCollector.ToString().Trim();
                                string firstLine = fullError.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? fullError;

                                WriteToLog($"Conversion failed (Code {p.ExitCode}). Error: {firstLine}", sourceFb2);
                            }
                        }
                        else
                        {
                            // Якщо процес не зміг запуститися (p == null)
                            hasError = true;
                        }
                    }
                    catch (Exception ex) // Додайте (Exception ex) сюди
                    {
                        hasError = true;
                        WriteToLog($"Execution error: {ex.Message}", sourceFb2); // Тепер "ex" працює
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
                            using (FileStream fs = new(markerPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                            {
                                using StreamWriter writer = new(fs);
                                writer.Write("yaml_error");
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
                            using (Form1 tempForm = new())
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
                    return false;
                }

                try
                {
                    // ПРИМУСОВО ВИМИКАЄМО ПЕРЕНАПРАВЛЕННЯ ПОТОКІВ, ЩОБ БУФЕР ОС НЕ БЛОКУВАВ ПРОЦЕС
                    psi.RedirectStandardOutput = false;
                    psi.RedirectStandardError = true; // Дозволяємо зчитування для логу
                    System.Text.StringBuilder quietErrorLog = new();

                    using Process? p = Process.Start(psi);
                    if (p != null)
                    {
                        p.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) { _ = quietErrorLog.AppendLine(e.Data); } };
                        p.BeginErrorReadLine();
                        p.WaitForExit(); // Конвертер тихо виконає роботу у фоні та закриється
                        if (p.ExitCode == 0)
                        {
                            conversionSuccess = true;
                        }
                        else
                        {
                            // Беремо лише перший рядок помилки
                            string fullError = quietErrorLog.ToString().Trim();
                            string firstLine = fullError.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? fullError;

                            WriteToLog($"Quiet conversion failed (Code {p.ExitCode}). Error: {firstLine}", sourceFb2);
                            // conversionSuccess залишиться false, і далі спрацює блок створення маркера
                        }
                    }
                }
                catch { }

                // РЕЗЕРВНИЙ ЗАХИСТ: Якщо файл фізично створився — фіксуємо успех конвертації
                if (!conversionSuccess)
                {
                    string expectedExt = "." + settings.Format.ToLowerInvariant().Replace("kepub", "epub").Replace("azw8", "azw3");
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
                        using (FileStream fs = new(markerPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        {
                            using StreamWriter writer = new(fs);
                            writer.Write(DateTime.UtcNow.Ticks.ToString());
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

                        using (Form1 tempForm = new())
                        {
                            _ = tempForm.ShowCustomMessageBox(errorText, errorTitle, MessageBoxButtons.OK);
                        }

                        // Записуємо час закриття вікна користувачем, щоб скинути 2-секундний таймер сесії
                        try { File.WriteAllText(markerPath, DateTime.UtcNow.Ticks.ToString()); } catch { }
                    }

                    if (hasHandle) { try { mutex.ReleaseMutex(); } catch { } hasHandle = false; }
                    return false; // Виходимо, оскільки конвертація провалилася
                }
            }
            return conversionSuccess;
        }

        private static void RunDeletionLogic(string sourceFb2, string inputPath, bool conversionSuccess, AppSettings settings)
        {
            // ==================================================
            // УНІВЕРСАЛЬНА ЛОГІКА ОЧИЩЕННЯ ТА ВИДАЛЕННЯ ФАЙЛІВ
            // ==================================================
            if (conversionSuccess && settings.DeleteAfterConvert && File.Exists(sourceFb2))
            {
                // --- НАЙНАДІЙНІШИЙ ЗАХИСТ ВІД ТИМЧАСОВИХ ФАЙЛІВ ---
                // ОПТИМІЗАЦІЯ: Перевіряємо шляхи без виділення зайвої пам'яті за допомогою StringComparison
                bool isTemporaryFile = sourceFb2.Contains(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase)
                                    || Path.GetFileName(sourceFb2).StartsWith("fbc_tmp_", StringComparison.OrdinalIgnoreCase);

                // ВИПРАВЛЕННЯ: Інвертуємо умову (!isTemporaryFile), щоб прибрати порожній блок if
                if (!isTemporaryFile)
                {
                    // ТУТ ПРАЦЮЄ СТАНДАРТНЕ ВИДАЛЕННЯ ТІЛЬКИ ДЛЯ ОРИГІНАЛЬНИХ ФАЙЛІВ КОРИСТУВАЧА:
                    Thread.Sleep(200);

                    // Режим 1: Автоматичне тихе видалення в Кошик Windows
                    if (settings.AutoDeleteToRecycle)
                    {
                        Core.SendToRecycleBin(sourceFb2);
                    }
                    // Режим 2: Повне видалення з викликом кастомного діалогового вікна підтвердження
                    else
                    {
                        using Form1 helperForm = new();
                        string title = Localization.Get(settings.Language, "ConfirmTitle");
                        string rawText = Localization.Get(settings.Language, "ConfirmText");
                        string displayedName = Path.GetFileName(sourceFb2);
                        string text = string.Format(rawText, displayedName);

                        if (helperForm.ShowCustomMessageBox(text, title, MessageBoxButtons.OKCancel) == DialogResult.OK)
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                if (Core.IsFileReady(sourceFb2))
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

            // === АВТОМАТИЧНЕ ВИДАЛЕННЯ ОРИГІНАЛЬНОГО АРХІВУ .FB2.ZIP ===
            // Якщо конвертація пройшла успішно, а вхідний файл був архівом .fb2.zip
            if (conversionSuccess && File.Exists(inputPath) && inputPath.Contains(".fb2.zip", StringComparison.OrdinalIgnoreCase))
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
                            Core.SendToRecycleBin(inputPath); // Тихо відправляємо ОРИГІНАЛЬНИЙ архів у Кошик
                        }
                        else
                        {
                            // Або показуємо одне красиве вікно з РЕАЛЬНОЮ назвою архіву замість Guid
                            using Form1 helperForm = new();
                            string title = Localization.Get(finallySettings.Language, "ConfirmTitle");
                            string rawText = Localization.Get(finallySettings.Language, "ConfirmText");
                            string fileName = Path.GetFileName(inputPath); // Отримуємо "книга.fb2.zip"
                            string text = string.Format(rawText, fileName);

                            if (helperForm.ShowCustomMessageBox(text, title, MessageBoxButtons.OKCancel) == DialogResult.OK)
                            {
                                for (int i = 0; i < 20; i++)
                                {
                                    if (Core.IsFileReady(inputPath))
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
                catch (Exception ex)
                {
                    WriteToLog($"File deletion failed: {ex.Message}", sourceFb2);
                }
            }
        }
        private static void WriteToLog(string message, string? targetFile = null)
        {
            try
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "gui_errors.log");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}" +
                                  (string.IsNullOrEmpty(targetFile) ? "" : $" | Target: {Path.GetFileName(targetFile)}") +
                                  Environment.NewLine;
                lock (logDir) { File.AppendAllText(logPath, logEntry); }
            }
            catch { }
        }
    }
}