
using System.Drawing.Drawing2D;

namespace fb2cngGUI
{
    public partial class Form1 : Form
    {
        // Вікно кастомних MessageBox з вирівнюванням тексту по центру
        public DialogResult ShowCustomMessageBox(string text, string caption, MessageBoxButtons buttons)
        {
            using Form msgForm = new();
            bool isDark = _settings.Theme == "Dark";
            msgForm.Text = caption;
            msgForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            msgForm.MaximizeBox = false;
            msgForm.MinimizeBox = false;
            msgForm.StartPosition = FormStartPosition.CenterScreen;
            msgForm.Font = new Font("Segoe UI", 10F);
            msgForm.BackColor = isDark ? Color.FromArgb(24, 24, 24) : Color.FromArgb(245, 245, 245);

            // --- 1. АВТОМАТИЧНЕ ВИЗНАЧЕННЯ МАСШТАБУ DPI ДЛЯ ВІКНА ПОДІЇ ---
            // Вираховуємо коефіцієнт масштабування на основі висоти шрифту форми
            float currentScale = msgForm.Font.Height / 18f;

            // --- 2. МАСШТАБОВАНІ ВІДСТУПИ ТА РОЗМІРИ ---
            int paddingTop = (int)(15 * currentScale);    // Відступ від верхнього краю до тексту
            int paddingMiddle = (int)(10 * currentScale); // Відступ між текстом та кнопкою
            int paddingBottom = (int)(10 * currentScale); // Відступ від кнопки до низу вікна
            int buttonHeight = (int)(32 * currentScale);  // Адаптивна висота кнопки ОК
            int buttonWidth = (int)(100 * currentScale);  // Адаптивна ширина кнопки ОК

            // Масштабуємо загальну базову ширину вікна повідомлення (на 100% була 385)
            int calculatedWidth = (int)(330 * currentScale);
            msgForm.ClientSize = new Size(calculatedWidth, msgForm.ClientSize.Height);

            // Налаштування для розташування повідомлення (Ширина тексту адаптується під форму)
            RichTextBox rtbText = new()
            {
                Text = text,
                Width = msgForm.ClientSize.Width - (int)(35 * currentScale), // Симетричні відступи з боків
                ForeColor = isDark ? Color.White : Color.Black,
                BackColor = msgForm.BackColor,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.None,
                TabStop = false,   // Забороняє фокусування кнопкою Tab
                TabIndex = 99      // Зміщуємо в кінець черги фокусування
            };
            rtbText.Cursor = Cursors.Arrow; // Щоб при наведенні на текст не з'являлася "паличка" виділення
            // Вирівнювання тексту повідомлень суворо по центру
            rtbText.SelectAll();
            rtbText.SelectionAlignment = HorizontalAlignment.Center;
            rtbText.DeselectAll();

            // ПРИХОВУВАННЯ КУРСОРУ ПРИ ВЗАЄМОДІЇ З ТЕКСТОМ
            rtbText.MouseDown += (s, e) => { _ = Win32Api.HideCaret(rtbText.Handle); _ = msgForm.Focus(); };
            rtbText.GotFocus += (s, e) => { _ = Win32Api.HideCaret(rtbText.Handle); };

            msgForm.Controls.Add(rtbText); // Додаємо на форму перед розрахунками

            // --- 3. ДИНАМІЧНИЙ РОЗРАХУНОК ВИСОТИ ТЕКСТУ ПІД НОВИЙ DPI ---
            // Дізнаємося реальну висоту відрендереного тексту в пікселях з урахуванням масштабу
            int lastCharIndex = rtbText.TextLength > 0 ? rtbText.TextLength - 1 : 0;
            Point lastCharPos = rtbText.GetPositionFromCharIndex(lastCharIndex);
            int textHeight = lastCharPos.Y + rtbText.Font.Height + (int)(10 * currentScale);

            // Задаємо мінімальну висоту текстової коробки під поточний масштаб
            int minTextHeight = (int)(50 * currentScale);
            if (textHeight < minTextHeight)
            {
                textHeight = minTextHeight;
            }
            rtbText.Height = textHeight;

            // Позиціонуємо RichTextBox рівно по центру форми з відступом paddingTop
            rtbText.Location = new Point((msgForm.ClientSize.Width - rtbText.Width) / 2, paddingTop);

            // Розраховуємо точну Y-координату для кнопки (завжди під текстом на відстані paddingMiddle)
            int buttonsY = rtbText.Bottom + paddingMiddle;

            // Налаштування стилів кнопок
            Color btnBg = isDark ? Color.FromArgb(50, 50, 50) : Color.FromArgb(230, 230, 230);
            Color btnTextCol = isDark ? Color.White : Color.Black;
            Color accentBg = isDark ? Color.FromArgb(0, 102, 204) : Color.FromArgb(0, 120, 215);

            // Змінна для збереження кнопки, яка прийме на себе перший фокус
            Button? primaryButton = null;

            buttonsY = rtbText.Bottom + paddingMiddle;

            if (buttons == MessageBoxButtons.OK)
            {
                Button btnOkCustom = new()
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Size = new Size(buttonWidth, buttonHeight),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = accentBg,
                    ForeColor = Color.White,
                    TabIndex = 0
                };
                btnOkCustom.FlatAppearance.BorderSize = 0;
                MakeButtonRounded(btnOkCustom, 6);

                // Центруємо одну кнопку OK по горизонталі
                btnOkCustom.Location = new Point((msgForm.ClientSize.Width - btnOkCustom.Width) / 2, buttonsY);

                msgForm.Controls.Add(btnOkCustom);
                msgForm.AcceptButton = btnOkCustom;
                primaryButton = btnOkCustom;
            }
            else if (buttons == MessageBoxButtons.OKCancel)
            {
                Button btnOkCustom = new()
                {
                    Text = Localization.Get(_settings.Language, "Ok"),
                    DialogResult = DialogResult.OK,
                    Size = new Size(buttonWidth, buttonHeight),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = accentBg,
                    ForeColor = Color.White,
                    TabIndex = 0
                };
                btnOkCustom.FlatAppearance.BorderSize = 0;
                MakeButtonRounded(btnOkCustom, 6);

                Button btnCancelCustom = new()
                {
                    Text = Localization.Get(_settings.Language, "Cancel"),
                    DialogResult = DialogResult.Cancel,
                    Size = new Size(buttonWidth, buttonHeight),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = btnBg,
                    ForeColor = btnTextCol,
                    TabIndex = 1
                };
                btnCancelCustom.FlatAppearance.BorderColor = isDark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(200, 200, 200);
                MakeButtonRounded(btnCancelCustom, 6);

                // Розподіляємо дві кнопки симетрично відносно центру форми
                int spacing = (int)(15 * currentScale);
                int totalButtonsWidth = btnOkCustom.Width + spacing + btnCancelCustom.Width;
                int startX = (msgForm.ClientSize.Width - totalButtonsWidth) / 2;

                btnOkCustom.Location = new Point(startX, buttonsY);
                btnCancelCustom.Location = new Point(startX + btnOkCustom.Width + spacing, buttonsY);

                msgForm.Controls.AddRange([btnOkCustom, btnCancelCustom]);
                msgForm.AcceptButton = btnOkCustom;
                msgForm.CancelButton = btnCancelCustom;
                primaryButton = btnOkCustom;
            }

            msgForm.TopMost = true;

            // ФІНАЛЬНИЙ РОЗРАХУНОК ВЕРТИКАЛЬНОГО РОЗМІРУ ВІКНА
            int finalHeight = paddingTop + rtbText.Height + paddingMiddle + buttonHeight + paddingBottom;
            msgForm.ClientSize = new Size(calculatedWidth, finalHeight);

            // Надійне WinAPI центрування динамічної форми msgForm на екрані монітора
            Rectangle primaryScreen = Screen.FromControl(this).Bounds; // варіант з var краще ніж Rectangle, а може і ні
            msgForm.Location = new Point(
                primaryScreen.Left + ((primaryScreen.Width - msgForm.Width) / 2),
                primaryScreen.Top + ((primaryScreen.Height - msgForm.Height) / 2)
            );

            // Налаштування поведінки вікна перед показом
            msgForm.StartPosition = FormStartPosition.CenterScreen;
            msgForm.TopMost = true;

            // ГАРАНТОВАНЕ ЗАБИРАННЯ ФОКУСУ ЧЕРЕЗ СКЛЕЮВАННЯ ПОТОКІВ WINDOWS
            msgForm.Shown += (s, e) =>
            {
                try
                {
                    IntPtr msgFormHandle = msgForm.Handle;

                    // 1. Отримуємо ID потоку вікна, яке зараз реально активне в Windows
                    IntPtr foregroundWindowHandle = Win32Api.GetForegroundWindow();
                    uint foregroundThreadId = Win32Api.GetWindowThreadProcessId(foregroundWindowHandle, IntPtr.Zero);

                    // 2. Отримуємо ID потоку нашого поточного вікна з повідомленням
                    uint currentThreadId = Win32Api.GetCurrentThreadId();

                    // 3. Якщо фокус у якоїсь іншої програми, тимчасово склеюємо потоки введення
                    if (foregroundThreadId != currentThreadId && foregroundThreadId != 0)
                    {
                        _ = Win32Api.AttachThreadInput(currentThreadId, foregroundThreadId, true);

                        // Примусово виводимо вікно на передній план та активуємо
                        _ = Win32Api.SetForegroundWindow(msgFormHandle);
                        _ = Win32Api.SetActiveWindow(msgFormHandle);
                        msgForm.Activate();

                        // Відклеюємо потоки назад, щоб не порушувати роботу ОС
                        _ = Win32Api.AttachThreadInput(currentThreadId, foregroundThreadId, false);
                    }
                    else
                    {
                        // Якщо ми і так були активні, просто стандартно фокусуємо
                        _ = Win32Api.SetForegroundWindow(msgFormHandle);
                        _ = Win32Api.SetActiveWindow(msgFormHandle);
                        msgForm.Activate();
                    }
                }
                catch { }

                // 4. Передаємо фокус безпосередньо на головну кнопку форми
                if (primaryButton != null)
                {
                    _ = primaryButton.Focus();
                }

                _ = msgForm.BeginInvoke(new Action(() => { _ = Win32Api.HideCaret(rtbText.Handle); }));
            };

            return msgForm.ShowDialog();
        }

        // --- ЛОГІКА ДЛЯ СТВОРЕННЯ, ПРИВ'ЯЗКИ ТА ЗАКРУГЛЕННЯ ВІКНА ОПИСУ ПРОГРАМИ ---
        // Подія натискання на прямокутну кнопку зі знаком питання (i)
        private void BtnHelp_Click(object? sender, EventArgs e)
        {
            // Якщо вікно вже відкрите — закриваємо його при повторному натисканні
            if (infoTooltipForm != null && !infoTooltipForm.IsDisposed && infoTooltipForm.Visible)
            {
                infoTooltipForm.Close();
                return;
            }

            // Визначаємо поточну мову та завантажуємо локалізовані тексти
            string lang = cbLang.SelectedItem?.ToString() ?? _settings.Language;
            string helpText = Localization.Get(lang, "HelpText");
            string helpTitle = Localization.Get(lang, "HelpTitle");

            bool isDark = _settings.Theme == "Dark";

            // Створюємо нову безрамкову форму довідки
            infoTooltipForm = new Form
            {
                Text = helpTitle,
                FormBorderStyle = FormBorderStyle.None,   // Без стандартних системних рамок Windows
                ShowInTaskbar = false,                    // Не показувати окрему іконку внизу на панелі задач
                StartPosition = FormStartPosition.Manual,   // Позиція задається строго вручну через координати
                BackColor = isDark ? Color.FromArgb(32, 32, 32) : Color.White
            };

            // Ширина становитиме 65% від ширини головного вікна
            int calculatedWidth = (int)(Width * 0.65);

            // --- 1. СТВОРЮЄМО ЗАГОЛОВОК ЯК ЗВИЧАЙНИЙ ЕЛЕМЕНТ (БЕЗ DOCK) ---
            int titleHeight = 35; // Висота нашого кастомного заголовка
            Label titleLabel = new()
            {
                Text = helpTitle,
                Location = new Point(0, 0),
                Width = calculatedWidth,
                Height = titleHeight,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = isDark ? Color.White : Color.Black,

                // КОЛІР ФОНУ ТЕПЕР ЗБІГАЄТЬСЯ З КОЛЬОРОМ РАМКИ (ТЕМНИЙ АБО СВІТЛИЙ СІРИЙ)
                BackColor = isDark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(180, 180, 180)
            };

            // --- 2. СТВОРЮЄМО ТЕКСТОВИЙ БЛОК (ЗСУВАЄМО НА ВИСОТУ ЗАГОЛОВКА) ---
            RichTextBox rtbHelp = new()
            {
                Text = helpText,
                Location = new Point(14, titleHeight + 7), // 7 пікселів відступу зверху
                Width = calculatedWidth - 28, // Ширина тексту залежить від форми
                ForeColor = isDark ? Color.White : Color.Black,
                BackColor = infoTooltipForm.BackColor,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.None,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                TabStop = false
            };

            // ВИРІВНЮВАННЯ ПО ЦЕНТРУ: Виділяємо весь текст і задаємо йому центральне вирівнювання
            rtbHelp.SelectAll();
            rtbHelp.SelectionAlignment = HorizontalAlignment.Center;
            rtbHelp.DeselectAll(); // Знімаємо виділення, щоб текст не підсвічувався синім кольором

            // Забороняємо виділення тексту мишею та появу текстового курсору
            rtbHelp.MouseDown += (s, ev) => { _ = Focus(); };

            // Додаємо елементи на форму
            infoTooltipForm.Controls.Add(titleLabel);
            infoTooltipForm.Controls.Add(rtbHelp);

            // --- ДИНАМІЧНИЙ РОЗРАХУНОК ВИСОТИ ВІКНА ДОВІДКИ ---
            int lastCharIndex = rtbHelp.TextLength > 0 ? rtbHelp.TextLength - 1 : 0;
            Point lastCharPos = rtbHelp.GetPositionFromCharIndex(lastCharIndex);
            // Чиста висота тексту (+5px запас для нижніх хвостиків літер у, ц, щ, д)
            int textHeight = lastCharPos.Y + rtbHelp.Font.Height + 5;

            // Встановлюємо висоту елемента тексту
            rtbHelp.Height = textHeight;

            // Розраховуємо фінальну висоту форми (висота тексту + висота заголовка +  мінімальні відступи зверху та знизу)
            int calculatedHeight = titleHeight + rtbHelp.Height + 12;

            // Задаємо меншу мінімальну висоту, щоб форма могла бути компактнішою
            if (calculatedHeight < 60)
            {
                calculatedHeight = 60;
            }

            // Призначаємо динамічні розміри формі
            infoTooltipForm.Size = new Size(calculatedWidth, calculatedHeight);
            // --------------------------------------------------

            // ТЕПЕР КРАЇ ВІКНА-ДОВІДКИ ТЕЖ ЗАОКРУГЛЕНІ (Виконується після визначення точних розмірів)
            int windowRadius = 8; // Радіус закруглення кутів вікна
            using (GraphicsPath path = new())
            {
                path.AddArc(0, 0, windowRadius * 2, windowRadius * 2, 180, 90);
                path.AddArc(infoTooltipForm.Width - (windowRadius * 2), 0, windowRadius * 2, windowRadius * 2, 270, 90);
                path.AddArc(infoTooltipForm.Width - (windowRadius * 2), infoTooltipForm.Height - (windowRadius * 2), windowRadius * 2, windowRadius * 2, 0, 90);
                path.AddArc(0, infoTooltipForm.Height - (windowRadius * 2), windowRadius * 2, windowRadius * 2, 90, 90);
                path.CloseAllFigures();
                infoTooltipForm.Region = new Region(path); // Призначаємо закруглену форму вікну
            }


            // Малюємо рамку знизу та справа (з заходом на половину радіуса на незадіяних кутах)
            infoTooltipForm.Paint += (s, ev) =>
            {
                ev.Graphics.SmoothingMode = SmoothingMode.AntiAlias; // Згладжування ліній
                Color borderColor = isDark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(180, 180, 180);
                using Pen pen = new(borderColor, 1);
                using GraphicsPath framePath = new();
                // 1. Починаємо на ПРАВОМУ ВЕРХНЬОМУ куті (заходимо на половину радіуса)
                // Починаємо вести лінію від середини верхнього закруглення вправо
                framePath.AddArc(
                    infoTooltipForm.Width - (windowRadius * 2) - 1,
                    0,
                    windowRadius * 2,
                    windowRadius * 2,
                    270, 45 // Малюємо лише половину дуги (45 градусів замість 90)
                );

                // 2. Ведемо лінію вниз по ВСЬОМУ ПРАВОМУ КРАЮ форми
                framePath.AddLine(
                    infoTooltipForm.Width - 1,
                    windowRadius,
                    infoTooltipForm.Width - 1,
                    infoTooltipForm.Height - windowRadius
                );

                // 3. Огинаємо ПРАВИЙ НИЖНІЙ кут повністю (на всі 90 градусів)
                framePath.AddArc(
                    infoTooltipForm.Width - (windowRadius * 2) - 1,
                    infoTooltipForm.Height - (windowRadius * 2) - 1,
                    windowRadius * 2,
                    windowRadius * 2,
                    0, 90
                );

                // 4. Ведемо лінію вліво по ВСЬОМУ НИЖНЬОМУ КРАЮ форми
                framePath.AddLine(
                    infoTooltipForm.Width - windowRadius,
                    infoTooltipForm.Height - 1,
                    windowRadius,
                    infoTooltipForm.Height - 1
                );

                // 5. Огинаємо ЛІВИЙ НИЖНІЙ кут повністю (на всі 90 градусів)
                framePath.AddArc(
                    0,
                    infoTooltipForm.Height - (windowRadius * 2) - 1,
                    windowRadius * 2,
                    windowRadius * 2,
                    90, 90
                );

                // 6. Завершуємо шлях на ЛІВОМУ боці, піднявшись лише до середини кута (на висоту одного радіуса)
                framePath.AddLine(
                    0,
                    infoTooltipForm.Height - windowRadius,
                    0,
                    infoTooltipForm.Height - windowRadius
                );

                // Малюємо отриману Г-подібну рамку
                ev.Graphics.DrawPath(pen, framePath);
            };

            // НАЙНАДІЙНІШИЙ СПОСІБ АВТОЗАКРИТТЯ: Як тільки користувач клікає БУДЬ-ДЕ поза цим вікном,
            // форма миттєво втрачає фокус (деактивується) і сама м'яко закривається в системі
            infoTooltipForm.Deactivate += (s, ev) => { infoTooltipForm.Close(); };

            // Спочатку показуємо вікно довідки як підлегле для головної форми
            infoTooltipForm.Show(this);

            // ОДРАЗУ ПІСЛЯ відображення примусово розраховуємо координати і ставимо вікно на місце
            UpdateHelpWindowPosition();
        }
    }

}