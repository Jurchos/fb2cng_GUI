using System.Diagnostics;

namespace fb2cngGUI
{
    // Результат обробки одного файлу
    public enum ProcessResult { Success, Continue, Stop }
    // Додамо внутрішній статус спеціально для UI
    internal enum UiResult { Success, Failed, UserCancelled }

    public class ConversService(AppSettings settings)
    {
        // Головний метод для обробки папки
        public void ProcessDirectory(string inputPath)
        {
            IEnumerable<string> filteredFiles = Directory.EnumerateFiles(inputPath, "*.*", SearchOption.AllDirectories)
                .Where(static file => file.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase) ||
                               file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

            foreach (string file in filteredFiles)
            {
                if (MarkerService.IsActive())
                {
                    break;
                }

                if (!ProcessSingleFile(file))
                {
                    break;
                }
            }
        }

        // Головний метод для обробки одного файлу (через Mutex)
        public bool ProcessSingleFile(string inputPath)
        {
            using Mutex mutex = new(false, "Local\\fb2cng_GUI_Queue_Mutex");
            bool hasHandle = false;
            try
            {
                hasHandle = mutex.WaitOne(TimeSpan.FromMinutes(5));

                // ПЕРЕВІРКА: Якщо поки ми чекали в черзі, користувач натиснув "Стоп"
                if (!hasHandle || MarkerService.IsActive())
                {
                    return false; // Повертаємо false, що зупинить цикл foreach у ProcessDirectory
                }

                ProcessResult result = RunFbcProcess(inputPath);

                if (result == ProcessResult.Success)
                {
                    RunDeletionLogic(inputPath, true);
                }

                // Якщо результат Stop, повертаємо false для виходу з циклу
                return result != ProcessResult.Stop && !MarkerService.IsActive();
            }
            finally
            {
                if (hasHandle)
                {
                    try { mutex.ReleaseMutex(); } catch { }
                }
            }
        }

        private ProcessResult RunFbcProcess(string sourceFb2)
        {
            ProcessStartInfo psi = PrepareStartInfo(sourceFb2, out string targetDir, out string format);
            if (string.IsNullOrEmpty(psi.FileName))
            {
                return ProcessResult.Stop;
            }

            DateTime startTime = DateTime.Now;
            string errorOutput;
            bool conversionSuccess;

            if (!settings.HideProgress)
            {
                // Перевірка перед запуском
                if (MarkerService.IsActive())
                {
                    return ProcessResult.Stop;
                }

                UiResult uiRes = RunWithProgressUI(psi, sourceFb2, out errorOutput);

                // ЯКЩО КОРИСТУВАЧ СКАСУВАВ — ЖОДНИХ ВІКОН ПОМИЛОК
                if (uiRes == UiResult.UserCancelled || MarkerService.IsActive())
                {
                    CleanupFailedConversion(targetDir, sourceFb2, format, startTime);
                    return ProcessResult.Stop;
                }
                conversionSuccess = uiRes == UiResult.Success;
            }
            else
            {
                conversionSuccess = RunSilent(psi, out errorOutput);
                if (MarkerService.IsActive())
                {
                    return ProcessResult.Stop;
                }
            }

            if (conversionSuccess)
            {
                return ProcessResult.Success;
            }

            // Сюди ми дійдемо ТІЛЬКИ якщо uiRes == Failed і це НЕ було скасуванням
            CleanupFailedConversion(targetDir, sourceFb2, format, startTime);
            return HandleConversionError(errorOutput, sourceFb2);
        }

        private ProcessStartInfo PrepareStartInfo(string sourceFb2, out string targetDir, out string formatLower)
        {
            // 1. Визначаємо цільову папку
            string originalDir = Path.GetDirectoryName(sourceFb2) ?? ".";
            targetDir = (settings.UseCustomFolder && Directory.Exists(settings.CustomFolder))
                ? settings.CustomFolder
                : originalDir;

            formatLower = settings.Format.ToLowerInvariant();

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string fbcPath = Path.Combine(appDir, Core.ConverterExe);

            // 2. Перевірка наявності fbc.exe
            if (!File.Exists(fbcPath))
            {
                targetDir = "";
                return new ProcessStartInfo("");
            }

            ProcessStartInfo psi = new()
            {
                FileName = fbcPath,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                WorkingDirectory = appDir
            };

            // 3. Додавання конфігурації
            // Прапорець -c треба додавати ТІЛЬКИ разом із шляхом
            if (settings.UseCustomConfig && File.Exists(settings.CustomConfig))
            {
                psi.ArgumentList.Add("-c");
                psi.ArgumentList.Add(settings.CustomConfig);
            }

            // 4. Команда та параметри
            psi.ArgumentList.Add("convert");
            psi.ArgumentList.Add("--to");
            psi.ArgumentList.Add(formatLower);

            if (settings.OverwriteExisting)
            {
                psi.ArgumentList.Add("--ow");
            }

            // 5. Шляхи (ArgumentList сам додасть лапки, якщо в шляху є пробіли)
            psi.ArgumentList.Add(sourceFb2);
            psi.ArgumentList.Add(targetDir);

            return psi;
        }

        private UiResult RunWithProgressUI(ProcessStartInfo psi, string sourceFile, out string errorOutput)
        {
            string localError = "";
            UiResult finalStatus = UiResult.Failed; // Початковий статус — помилка
            using Form progressForm = CreateProgressForm(sourceFile);
            Process? volatileProcess = null;

            Thread processThread = new(() =>
            {
                try
                {
                    using Process p = new();
                    p.StartInfo = psi;
                    volatileProcess = p;

                    _ = p.Start();
                    localError = p.StandardError.ReadToEnd();
                    p.WaitForExit();

                    // ПЕРЕВІРКА ПІСЛЯ ВИХОДУ: 
                    // Якщо активовано сигнал зупинки, статус ТІЛЬКИ UserCancelled
                    finalStatus = MarkerService.IsActive() ? UiResult.UserCancelled : (p.ExitCode == 0) ? UiResult.Success : UiResult.Failed;
                }
                catch
                {
                    finalStatus = MarkerService.IsActive() ? UiResult.UserCancelled : UiResult.Failed;
                }
                finally
                {
                    volatileProcess = null;
                    if (progressForm.IsHandleCreated)
                    {
                        _ = progressForm.BeginInvoke(new Action(progressForm.Close));
                    }
                }
            });

            progressForm.FormClosing += (s, ev) =>
            {
                Process? p = volatileProcess;
                if (p != null && !p.HasExited)
                {
                    if (ConfirmStopConversion())
                    {
                        // КРОК 1: Встановлюємо глобальний сигнал зупинки
                        MarkerService.Signal();
                        // КРОК 2: Міняємо статус на "Скасовано користувачем"
                        finalStatus = UiResult.UserCancelled;
                        // КРОК 3: Вбиваємо процес fbc.exe
                        try { p.Kill(true); }
                        catch { }
                    }
                    else
                    {
                        ev.Cancel = true; // Користувач передумав закривати
                    }
                }
            };

            processThread.Start();
            _ = progressForm.ShowDialog();
            processThread.Join();

            errorOutput = localError;
            return finalStatus;
        }

        private Form CreateProgressForm(string sourceFile)
        {
            bool isDark = settings.Theme == "Dark";

            Form progressForm = new()
            {
                Text = Localization.Get(settings.Language, "ProgressTitle") ?? "Converting...",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = true,
                ShowInTaskbar = true,
                StartPosition = FormStartPosition.CenterScreen,
                TopMost = true,
                BackColor = isDark ? Color.FromArgb(24, 24, 24) : Color.FromArgb(245, 245, 245),

                KeyPreview = true // Дозволяє формі бачити натискання клавіш раніше за кнопки
            };

            // Обробка натискання клавіш
            progressForm.KeyDown += (s, e) =>
            {
                // Перевіряємо клавішу Escape АБО комбінацію Ctrl + C
                if (e.KeyCode == Keys.Escape || (e.Control && e.KeyCode == Keys.C))
                {
                    // Просто закриваємо форму, FormClosing (є в коді) перехопить це. 
                    progressForm.Close();
                }
            };

            float scale = progressForm.DeviceDpi / 96f;
            progressForm.Size = new Size((int)(330 * scale), (int)(115 * scale));

            if (settings.StartMinimized)
            {
                progressForm.WindowState = FormWindowState.Minimized;
            }

            Label lblInfo = new()
                {
                Text = Path.GetFileName(sourceFile),
                Location = new Point((int)(20 * scale), (int)(15 * scale)),
                Size = new Size(progressForm.ClientSize.Width - (int)(40 * scale), (int)(20 * scale)),
                ForeColor = isDark ? Color.White : Color.Black,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                AutoEllipsis = true
                };

            ProgressBar progressBar = new()
            {
                Location = new Point(lblInfo.Left, lblInfo.Bottom + (int)(7 * scale)),
                Size = new Size(lblInfo.Width, (int)(18 * scale)),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30
            };
            progressForm.Controls.AddRange([lblInfo, progressBar]);
            return progressForm;
        }

        private bool ConfirmStopConversion()
        {
            string lang = settings.Language;
            // Отримуємо перекладені тексти
            return MessageService.ShowCustomMessageBox(
                   Localization.Get(lang, "ConfirmStopText"),
                   Localization.Get(lang, "ConfirmStopTitle"),
            MessageBoxButtons.OKCancel) == DialogResult.OK;
        }

        private static bool RunSilent(ProcessStartInfo psi, out string errorOutput)
        {
            errorOutput = "";
            try
            {
                using Process? p = Process.Start(psi);
                if (p != null)
                {
                    // Читаємо відразу. Програма зачекає тут, поки fbc.exe не закриє потік помилок.
                    // Використовуємо ReadToEnd до WaitForExit, щоб уникнути переповнення буфера
                    string error = p.StandardError.ReadToEnd();
                    p.WaitForExit();

                    errorOutput = error;
                    return p.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                errorOutput = ex.Message;
            }

            return false;
        }

        private static string FormatFbcError(string rawError)
        {
            if (string.IsNullOrWhiteSpace(rawError))
            {
                return "Unknown error";
            }

            // 1. Розбиваємо вивід на окремі рядки, бо fbc може видати кілька JSON-об'єктів
            string[] lines = rawError.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            List<string> cleanMessages = [];

            foreach (string line in lines)
            {
                string currentLine = line.Trim();

                // 2. Якщо рядок містить JSON з помилкою {"error": "..."}
                if (currentLine.Contains("\"error\":"))
                {
                    try
                    {
                        // Шукаємо текст після "error":"
                        int startIdx = currentLine.IndexOf("\"error\":") + 8;
                        // Знаходимо наступну лапку
                        int firstQuote = currentLine.IndexOf('"', startIdx);
                        if (firstQuote != -1)
                        {
                            // Знаходимо закриваючу лапку (з кінця цього JSON об'єкта)
                            int lastQuote = currentLine.LastIndexOf('"');
                            if (lastQuote > firstQuote)
                            {
                                string msg = currentLine.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
                                // Очищаємо від екранованих лапок і додаємо
                                cleanMessages.Add(msg.Replace("\\\"", "\""));
                                continue;
                            }
                        }
                    }
                    catch { }
                }

                // 3. Якщо рядок НЕ JSON, але містить ERROR, видаляємо технічну частину (таймстампи fbc)
                // Приклад: 2026-08-23T19:13:47.649+0300 ERROR fbc ...
                if (currentLine.Contains("ERROR"))
                {
                    int errorPos = currentLine.IndexOf("ERROR");
                    string afterError = currentLine[errorPos..].Trim();
                    if (!string.IsNullOrWhiteSpace(afterError))
                    {
                        cleanMessages.Add(afterError);
                    }
                }
            }

            // 4. Збираємо докупи тільки унікальні повідомлення, щоб не дублювати "Conversion failed"
            if (cleanMessages.Count > 0)
            {
                List<string> distinctMessages = [.. cleanMessages.Distinct()];

                // --- Спеціальна обробка для перезапису (already exists) ---
                // Якщо в логу є повідомлення про перезапис, ми не з'єднуємо все в "кашу",
                // а беремо тільки саме повідомлення про конфлікт файлу (воно зазвичай найкоротше).
                if (distinctMessages.Any(static m => m.Contains("already exists")))
                {
                    return distinctMessages.Where(static m => m.Contains("already exists"))
                                           .OrderBy(static m => m.Length)
                                           .First();
                }

                // Для всіх інших випадків (YAML, Validity) — залишаємо повний технічний ланцюжок через |
                return string.Join(" | ", distinctMessages);
            }

            return rawError.Trim();
        }

        private ProcessResult HandleConversionError(string error, string sourceFile)
        {
            // КРИТИЧНО: Якщо активовано сигнал зупинки — МОВЧИМО
            if (MarkerService.IsActive())
            {
                return ProcessResult.Stop;
            }

            // Якщо помилка сталася через те, що ми самі вбили процес (Access is denied або подібне)
            // або якщо помилка порожня — просто виходимо.
            if (string.IsNullOrWhiteSpace(error) || error.Contains("terminated") || error.Contains("killed"))
            {
                return ProcessResult.Stop;
            }

            string lang = settings.Language;
            string fileName = Path.GetFileName(sourceFile);
            string errLow = error.ToLowerInvariant();

            // Отримуємо "красиву" версію помилки для логу
            string detailedError = FormatFbcError(error);

            // 1. КРИТИЧНА ПОМИЛКА YAML (Зупиняємо чергу повністю)
            if (errLow.Contains("configuration") || errLow.Contains("yaml"))
            {
                // Записуємо в лог повний технічний опис (з номером рядка тощо)
                Core.WriteToLog($"YAML ERROR: {detailedError}", sourceFile);
                MarkerService.Signal(); // Зупиняємо всю чергу
                _ = MessageService.ShowCustomMessageBox(Localization.Get(lang, "YamlErrorText"), Localization.Get(lang, "YamlErrorTitle"), MessageBoxButtons.OK);
                return ProcessResult.Stop;
            }

            // 2. ФАЙЛ ВЖЕ ІСНУЄ (Запитуємо: пропустити цей файл чи зупинити все)
            if (errLow.Contains("already exists"))
            {
                // ОБОВ'ЯЗКОВО записуємо в лог, навіть якщо користувач пропустить це вікно
                Core.WriteToLog($"FILE EXISTS ERROR: {detailedError}", sourceFile);
                // Якщо увімкнено "Пропускати" — просто йдемо далі мовчки
                if (settings.SkipExistingFiles)
                {
                    return ProcessResult.Continue;
                }
                // Перевірка перед показом вікна
                if (MarkerService.IsActive())
                {
                    return ProcessResult.Stop;
                }
                // Інакше показуємо вікно запиту 
                string msg = string.Format(Localization.Get(lang, "FileExistsText"), fileName);

                // Якщо користувач натиснув Cancel (продовжити роботу? - НІ)
                if (MessageService.ShowCustomMessageBox(msg, Localization.Get(lang, "FileExistsTitle"), MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    return ProcessResult.Continue; // Продовжити чергу
                }

                MarkerService.Signal();
                return ProcessResult.Stop; // Зупинити все
            }

            // 3. ПОШКОДЖЕНИЙ ФАЙЛ (Якщо не встановлено чек-ігнор, то меседжбокс і після ОК йдемо до наступного файлу)
            if (errLow.Contains("not recognized") || errLow.Contains("valid") || errLow.Contains("load book") || errLow.Contains("failed to open"))
            {
                Core.WriteToLog($"VALIDITY ERROR: {detailedError}", sourceFile);

                // Якщо увімкнено "Пропускати", просто йдемо далі
                if (settings.SkipCorruptFiles)
                {
                    return ProcessResult.Continue;
                }
                if (MarkerService.IsActive())
                {
                    return ProcessResult.Stop;
                }

                string msg = string.Format(Localization.Get(lang, "CorruptFileText"), fileName);

                // Якщо користувач натиснув Cancel
                if (MessageService.ShowCustomMessageBox(msg, Localization.Get(lang, "CorruptFileTitle"), MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    return ProcessResult.Continue;
                }

                MarkerService.Signal();
                return ProcessResult.Stop;
            }

            // 4. ІНШІ НЕПЕРЕДБАЧЕНІ ПОМИЛКИ
            if (!string.IsNullOrWhiteSpace(error) && !MarkerService.IsActive())
            {
                Core.WriteToLog($"UNKNOWN ERROR: {error.Trim()}", sourceFile);
                _ = MessageService.ShowCustomMessageBox(Localization.Get(lang, "UnknownErrorText"), Localization.Get(lang, "UnknownErrorTitle"), MessageBoxButtons.OK);
            }
            return ProcessResult.Stop;
        }

        private void RunDeletionLogic(string sourceFb2, bool conversionSuccess)
        {
            // 1. Перевіряємо, чи взагалі потрібно щось видаляти
            if (!conversionSuccess || !settings.DeleteAfterConvert || !File.Exists(sourceFb2))
            {
                return;
            }

            // Додаємо мікро-паузу, щоб система встигла закрити всі дескриптори після fbc.exe
            Thread.Sleep(150);
            // 2. Якщо файл дійсно лежить у системній папці Temp — видаляємо без запитань
            if (sourceFb2.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    File.Delete(sourceFb2);
                }
                catch { }
                return;
            }

            // 3. ЛОГІКА ВИДАЛЕННЯ
            if (settings.AutoDeleteToRecycle)
            {
                // Для кошика не потрібна сувора перевірка IsFileReady, 
                // Windows Shell API (SHFileOperation) сам впорається з чергою доступу.
                Core.SendToRecycleBin(sourceFb2);
            }
            else
            {
                // Для повного видалення File.Delete перевірка потрібна, 
                // але ми зробимо її менш суворою (тільки на читання)
                string title = Localization.Get(settings.Language, "ConfirmTitle");
                string rawText = Localization.Get(settings.Language, "ConfirmText");
                string text = string.Format(rawText, Path.GetFileName(sourceFb2));

                if (MessageService.ShowCustomMessageBox(text, title, MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    // Робимо 10 спроб видалення протягом 2 секунд
                    bool deleted = false;
                    for (int i = 0; i < 10; i++)
                    {
                        try
                        {
                            if (!File.Exists(sourceFb2)) { deleted = true; break; }
                            File.Delete(sourceFb2);
                            deleted = true;
                            break;
                        }
                        catch (IOException) // Файл зайнятий (антивірус, система)
                        {
                            Thread.Sleep(200);
                        }
                        catch { break; } // Інші критичні помилки (права доступу тощо)
                    }

                    if (!deleted)
                    {
                        Core.WriteToLog("FAILED TO DELETE: File is locked by another process.", sourceFb2);
                    }
                }
            }
        }
        private static void CleanupFailedConversion(string targetDir, string sourceFb2, string format, DateTime processStartTime)
        {
            // Якщо процес було вбито або він впав, спробуємо видалити битий файл
            // щоб не залишати сміття в папці призначення
            try
            {
                string fileNameOnly = Path.GetFileNameWithoutExtension(sourceFb2);
                string expectedFile = Path.Combine(targetDir, fileNameOnly + "." + format);

                if (File.Exists(expectedFile))
                {
                    // Перевіряємо час створення або зміни файлу.
                    // Якщо файл з'явився (або змінився) ПІСЛЯ того, як ми запустили конвертацію — це сміття.
                    // Додаємо невеликий запас (-1 сек), бо час у файловій системі може округлюватися.
                    DateTime fileTime = File.GetLastWriteTime(expectedFile);

                    if (fileTime >= processStartTime.AddSeconds(-1))
                    {
                        File.Delete(expectedFile);
                    }
                }
            }
            catch { /* Ігноруємо помилки видалення сміття */ }
        }
    }
}