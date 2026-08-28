
namespace fb2cngGUI
{
    public static class MessageService
    {
        // Вікно кастомних MessageBox з вирівнюванням тексту по центру
        public static DialogResult ShowCustomMessageBox(string text, string caption, MessageBoxButtons buttons)
        {
            AppSettings settings = AppSettings.Current; // Беремо глобальні налаштування
            using Form msgForm = new();
            bool isDark = settings.Theme == "Dark";
            msgForm.Text = caption;
            msgForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            msgForm.MaximizeBox = false;
            msgForm.MinimizeBox = false;
            msgForm.StartPosition = FormStartPosition.CenterScreen;
            msgForm.Font = new Font("Segoe UI", 10F);
            msgForm.BackColor = isDark ? Color.FromArgb(24, 24, 24) : Color.FromArgb(245, 245, 245);

            // Вираховуємо коефіцієнт масштабування на основі висоти шрифту форми
            float currentScale = msgForm.DeviceDpi / 96f;

            // Основні відступи та розміри
            int paddingTop = (int)(14 * currentScale);    // Відступ від верхнього краю до тексту
            int paddingMiddle = (int)(14 * currentScale); // Відступ між текстом та кнопкою
            int paddingBottom = (int)(10 * currentScale); // Відступ від кнопки до низу вікна
            int buttonHeight = (int)(28 * currentScale);  // Адаптивна висота кнопки ОК
            int buttonWidth = (int)(85 * currentScale);  // Адаптивна ширина кнопки ОК

            // Розмір вікна повідомлення
            int calculatedWidth = (int)(320 * currentScale);
            msgForm.ClientSize = new Size(calculatedWidth, msgForm.ClientSize.Height);

            // Налаштування стилів кнопок
            Color btnBg = isDark ? Color.FromArgb(50, 50, 50) : Color.FromArgb(230, 230, 230);
            Color btnTextCol = isDark ? Color.White : Color.Black;
            Color accentBg = isDark ? Color.FromArgb(0, 102, 204) : Color.FromArgb(0, 120, 215);

            // Кнопка ОК є завжди, має бути зверху, щоб на неї переводити фокус (курсор) 
            Button btnOkCustom = new()
            {
                Text = Localization.Get(settings.Language, "Ok"),
                DialogResult = DialogResult.OK,
                Size = new Size(buttonWidth, buttonHeight),
                FlatStyle = FlatStyle.Flat,
                BackColor = accentBg,
                ForeColor = Color.White,
                TabIndex = 0
            };
            btnOkCustom.FlatAppearance.BorderSize = 0;
            UIButton.MakeButtonRounded(btnOkCustom, 5);

            // НАЛАШТУВАННЯ ТЕКСТОВОГО ПОЛЯ
            RichTextBox rtbText = new()
            {
                Text = text.Trim(),
                Width = msgForm.ClientSize.Width - (int)(20 * currentScale), // Відступи від країв
                ForeColor = isDark ? Color.White : Color.Black,
                BackColor = msgForm.BackColor,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.None,
                Cursor = Cursors.Arrow,       // Стрілка замість текстового курсору
                Font = msgForm.Font,
                TabStop = false,              // Важливо: захист від Tab
                ReadOnly = true,              // Важливо: захист від редагування
                ShortcutsEnabled = false,     // Важливо: Вимикаємо контекстне меню
            };

            // ПЕРЕХОПЛЕННЯ ФОКУСУ
            rtbText.MouseDown += (s, e) => btnOkCustom.Focus();
            rtbText.GotFocus += (s, e) => btnOkCustom.Focus();
            rtbText.Enter += (s, e) => btnOkCustom.Focus();

            // Вирівнювання тексту по центру
            rtbText.SelectAll();
            rtbText.SelectionAlignment = HorizontalAlignment.Center;
            rtbText.DeselectAll();

            msgForm.Controls.Add(rtbText);

            // Розрахунок висоти (через GetPositionFromCharIndex)
            int lastCharIndex = rtbText.Text.Length - 1;
            if (lastCharIndex < 0) lastCharIndex = 0;
            Point lastCharPos = rtbText.GetPositionFromCharIndex(lastCharIndex);
            int actualContentHeight = lastCharPos.Y + rtbText.Font.Height;

            // Встановлюємо висоту, але не менше ніж 50px (для компактних повідомлень)
            rtbText.Height = Math.Max(actualContentHeight, (int)(50 * currentScale));

            // 5. Позиціонування
            rtbText.Location = new Point((msgForm.ClientSize.Width - rtbText.Width) / 2, paddingTop);

            // КНОПКИ
            // Розраховуємо Y-координату для кнопок (строго під текстом)
            int buttonsY = rtbText.Bottom + paddingMiddle;

            if (buttons == MessageBoxButtons.OK)
            {
                // Центруємо одну кнопку OK по горизонталі
                btnOkCustom.Location = new Point((msgForm.ClientSize.Width - btnOkCustom.Width) / 2, buttonsY);

                msgForm.Controls.Add(btnOkCustom);
                msgForm.AcceptButton = btnOkCustom;
            }

            else if (buttons == MessageBoxButtons.OKCancel)
            {
                Button btnCancelCustom = new()
                {
                    Text = Localization.Get(settings.Language, "Cancel"),
                    DialogResult = DialogResult.Cancel,
                    Size = new Size(buttonWidth, buttonHeight),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = btnBg,
                    ForeColor = btnTextCol,
                    TabIndex = 1
                };
                btnCancelCustom.FlatAppearance.BorderColor = isDark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(200, 200, 200);
                UIButton.MakeButtonRounded(btnCancelCustom, 5);

                // Розподіляємо дві кнопки симетрично відносно центру форми
                int spacing = (int)(15 * currentScale);
                int totalButtonsWidth = btnOkCustom.Width + spacing + btnCancelCustom.Width;
                int startX = (msgForm.ClientSize.Width - totalButtonsWidth) / 2;

                btnOkCustom.Location = new Point(startX, buttonsY);
                btnCancelCustom.Location = new Point(startX + btnOkCustom.Width + spacing, buttonsY);

                msgForm.Controls.AddRange([btnOkCustom, btnCancelCustom]);
                msgForm.AcceptButton = btnOkCustom;
                msgForm.CancelButton = btnCancelCustom;
            }

            // ФІНАЛЬНИЙ РОЗРАХУНОК ВЕРТИКАЛЬНОГО РОЗМІРУ ВІКНА
            int finalHeight = buttonsY + buttonHeight + paddingBottom;
            msgForm.ClientSize = new Size(calculatedWidth, finalHeight);

            // Центрування динамічної форми msgForm на екрані монітора
            Screen? screen = Screen.PrimaryScreen;
            if (screen != null)
            {
                msgForm.StartPosition = FormStartPosition.Manual;
                msgForm.Location = new Point(
                    screen.Bounds.Left + (screen.Bounds.Width - msgForm.Width) / 2,
                    screen.Bounds.Top + (screen.Bounds.Height - msgForm.Height) / 2
                );
            }

            msgForm.TopMost = true;

            msgForm.Shown += (s, e) =>
            {
                try
                {
                    nint msgFormHandle = msgForm.Handle;

                    // 1. Отримуємо стан вікна.Якщо вікно згорнуте — відновлюємо
                    if (Win32Api.IsIconic(msgFormHandle))
                    {
                        Win32Api.ShowWindow(msgFormHandle, 9); // SW_RESTORE
                    }

                    // 2. Стандартний блок примусової активації
                    nint foregroundWindowHandle = Win32Api.GetForegroundWindow();
                    uint foregroundThreadId = Win32Api.GetWindowThreadProcessId(foregroundWindowHandle, nint.Zero);
                    uint currentThreadId = Win32Api.GetCurrentThreadId();

                    // Якщо наше вікно не в фокусі — «прив'язуємося» до чужого потоку, щоб отримати право на фокус
                    if (foregroundThreadId != currentThreadId && foregroundThreadId != 0)
                    {
                        Win32Api.AttachThreadInput(currentThreadId, foregroundThreadId, true);
                        Win32Api.SetForegroundWindow(msgFormHandle);
                        Win32Api.SetActiveWindow(msgFormHandle);
                        msgForm.Activate();
                        Win32Api.AttachThreadInput(currentThreadId, foregroundThreadId, false);
                    }
                    else
                    {
                        Win32Api.SetForegroundWindow(msgFormHandle);
                        Win32Api.SetActiveWindow(msgFormHandle);
                        msgForm.Activate();
                    }

                    // 3. Примусово виводимо на передній план через WinForms
                    msgForm.BringToFront();
                }
                catch { }

                // 4. Фокус на кнопку
                _ = btnOkCustom.Focus();
            };

            // ФІКС: Передаємо активну форму як власника. 
            // Це прив'язує MessageBox до головного вікна і не дає йому "провалитися" назад.
            Form? activeOwner = Form.ActiveForm;
            if (activeOwner != null && activeOwner != msgForm)
            {
                return msgForm.ShowDialog(activeOwner);
            }
            else
            {
                return msgForm.ShowDialog();
            }
        }
    }
}