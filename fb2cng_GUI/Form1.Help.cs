using System.Drawing.Drawing2D;
using System.Reflection;

namespace fb2cngGUI
{
    public partial class Form1 : Form
    {
        // Константи для стилізації
        private const int BaseHelpWindowRadius = 8;
        private const int BaseTitleHeight = 30; // Висота кастомної довідки
        private const float WidthPercentage = 0.65f; // Ширина 65% від ширини головного вікна

        // Динамічні властивості, що враховують DPI
        private float ScaleFactor => DeviceDpi / 96f;
        private int TitleHeight => (int)(BaseTitleHeight * ScaleFactor);
        private int HelpWindowRadius => (int)(BaseHelpWindowRadius * ScaleFactor);

        // --- ЛОГІКА ДЛЯ СТВОРЕННЯ, ПРИВ'ЯЗКИ ТА ЗАКРУГЛЕННЯ ВІКНА ОПИСУ ПРОГРАМИ ---
        // Подія натискання на прямокутну кнопку зі знаком питання (i)
        private void BtnHelp_Click(object? sender, EventArgs e)
        {
            // 1. Перевірка стану (чи не відкрите вже вікно)
            if (IsHelpWindowOpen())
            {
                infoTooltipForm.Close();
                return;
            }
            // НОВЕ: Якщо вікно закрилося щойно (кліком по кнопці), не відкриваємо його знову
            if ((DateTime.Now - _lastHelpCloseTime).TotalMilliseconds < 200)
            {
                return;
            }

            // 2. Підготовка даних та форми
            bool isDark = _settings.Theme == "Dark";
            string helpText = PrepareHelpContent(out string helpTitle);

            infoTooltipForm = CreateEmptyHelpForm(helpTitle, isDark);
            int calculatedWidth = (int)(Width * WidthPercentage);

            // 3. Створення та налаштування елементів керування
            Label titleLabel = CreateTitleLabel(helpTitle, calculatedWidth, isDark);
            RichTextBox rtbHelp = CreateContentTextBox(helpText, calculatedWidth, isDark);

            infoTooltipForm.Controls.AddRange([titleLabel, rtbHelp]);

            // 4. Динамічний розрахунок розмірів
            AdjustFormSize(rtbHelp, calculatedWidth);

            // 5. Візуальні ефекти та події
            ApplyRounding(infoTooltipForm, HelpWindowRadius);
            SetupHelpEvents(isDark);

            // 6. Відображення
            infoTooltipForm.Show(this);
            UpdateHelpWindowPosition();
        }

        private bool IsHelpWindowOpen()
        {
            return infoTooltipForm != null && !infoTooltipForm.IsDisposed && infoTooltipForm.Visible;
        }

        private string PrepareHelpContent(out string title)
        {

            // Визначаємо поточну мову та завантажуємо локалізовані тексти
            string lang = cbLang.SelectedItem?.ToString() ?? _settings.Language;
            title = Localization.Get(lang, "HelpTitle");
            string rawHelpText = Localization.Get(lang, "HelpText");
            string version = GetSimpleVersion();
            string copyright = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";

            return string.Format(rawHelpText, copyright, version);
        }

        private static string GetSimpleVersion()
        {
            // Дістаємо версію (вона буде як у властивостях проекту)
            string version = Application.ProductVersion;
            // У .NET часто додається ревізія (напр. 1.3.0), якщо хочемо тільки 1.3:
            return Version.TryParse(version, out Version? v) ? $"{v.Major}.{v.Minor}" : version;
        }

        private static Form CreateEmptyHelpForm(string helpTitle, bool isDark)
        {
            return new Form
            {
                Text = helpTitle,
                FormBorderStyle = FormBorderStyle.None,   // Без стандартних системних рамок Windows
                ShowInTaskbar = false,                    // Не показувати окрему іконку внизу на панелі задач
                StartPosition = FormStartPosition.Manual,   // Позиція задається строго вручну через координати
                BackColor = isDark ? Color.FromArgb(32, 32, 32) : Color.White
            };
        }

        private Label CreateTitleLabel(string helpTitle, int width, bool isDark)
        {
            return new Label
            {
                Text = helpTitle,
                Location = new Point(0, 0),
                Width = width,
                Height = TitleHeight,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(24, 0, 0, 0),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = isDark ? Color.White : Color.Black,
                // КОЛІР ФОНУ ТЕПЕР ЗБІГАЄТЬСЯ З КОЛЬОРОМ РАМКИ (ТЕМНИЙ АБО СВІТЛИЙ СІРИЙ)
                BackColor = isDark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(180, 180, 180)
            };
        }

        private RichTextBox CreateContentTextBox(string helpText, int width, bool isDark)
        {
            // --- СТВОРЮЄМО ТЕКСТОВИЙ БЛОК (ЗСУВАЄМО НА ВИСОТУ ЗАГОЛОВКА) ---
            int sidePadding = (int)(10 * ScaleFactor);// Боковий відступ
            int topSpacing = (int)(7 * ScaleFactor);// Відступ від заголовка

            RichTextBox rtbHelp = new()
            {
                Text = helpText,
                Location = new Point(sidePadding, TitleHeight + topSpacing),
                Width = width - (sidePadding * 2), // Ширина тексту залежить від форми
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
            // Заборона фокусу при кліку
            rtbHelp.MouseDown += (s, ev) => { _ = Focus(); };

            return rtbHelp;
        }
 
        private void AdjustFormSize(RichTextBox rtbHelp, int width)
        {
            // --- ДИНАМІЧНИЙ РОЗРАХУНОК ВИСОТИ ВІКНА ДОВІДКИ ---
            int lastCharIndex = rtbHelp.TextLength > 0 ? rtbHelp.TextLength - 1 : 0;
            Point lastCharPos = rtbHelp.GetPositionFromCharIndex(lastCharIndex);
            // Чиста висота тексту (+5px запас для нижніх хвостиків літер у, ц, щ, д)
            int textHeight = lastCharPos.Y + rtbHelp.Font.Height + (int)(5 * ScaleFactor);

            // Встановлюємо висоту елемента тексту
            rtbHelp.Height = textHeight;

            // Розраховуємо фінальну висоту форми (висота тексту + висота заголовка +  мінімальні відступи зверху та знизу)
            int bottomPadding = (int)(10 * ScaleFactor);
            int calculatedHeight = TitleHeight + rtbHelp.Height + bottomPadding;

            // Задаємо меншу мінімальну висоту, щоб форма могла бути компактнішою
            if (calculatedHeight < (int)(60 * ScaleFactor))
            {
                calculatedHeight = (int)(60 * ScaleFactor);
            }

            // Призначаємо динамічні розміри формі
            infoTooltipForm.Size = new Size(width, calculatedHeight);
        }

        private static void ApplyRounding(Form form, int radius)
        {
            using GraphicsPath path = new();
            int twoR = radius * 2;
            path.AddArc(0, 0, twoR, twoR, 180, 90);
            path.AddArc(form.Width - twoR, 0, twoR, twoR, 270, 90);
            path.AddArc(form.Width - (radius * 2), form.Height - twoR, twoR, twoR, 0, 90);
            path.AddArc(0, form.Height - twoR, twoR, twoR, 90, 90);
            path.CloseAllFigures();
            form.Region?.Dispose(); // Якщо Region є — видалити, якщо null — нічого не робити
            form.Region = new Region(path); // Призначаємо закруглену форму вікну
        }

        private void SetupHelpEvents(bool isDark)
        {
            // СПОСІБ АВТОЗАКРИТТЯ: Як тільки користувач клікає БУДЬ-ДЕ поза цим вікном,
            // форма миттєво втрачає фокус (деактивується) і сама м'яко закривається в системі
            infoTooltipForm.Deactivate += (s, ev) =>
            {
                _lastHelpCloseTime = DateTime.Now; // Запам'ятовуємо час закриття
                infoTooltipForm.Close();
            };
            infoTooltipForm.Paint += (s, ev) => DrawHelpBorder(ev.Graphics, isDark);
        }

        private void DrawHelpBorder(Graphics g, bool isDark)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias; // Згладжування ліній
            Color borderColor = isDark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(180, 180, 180);
            using Pen pen = new(borderColor, 1);
            using GraphicsPath framePath = new();

            int wRadius = HelpWindowRadius;
            int twoR = wRadius * 2;
            int wForm = infoTooltipForm.Width;
            int hForm = infoTooltipForm.Height;
            // 1. Малюємо Г-подібну рамку (права та нижня сторони)
            framePath.AddArc(wForm - twoR - 1, 0, twoR, twoR, 270, 45);// Малюємо лише половину дуги (45 градусів замість 90)

            // 2. Ведемо лінію вниз по ВСЬОМУ ПРАВОМУ КРАЮ форми
            framePath.AddLine(wForm - 1, wRadius, wForm - 1, hForm - wRadius);

            // 3. Огинаємо ПРАВИЙ НИЖНІЙ кут повністю (на всі 90 градусів)
            framePath.AddArc(wForm - twoR - 1, hForm - twoR - 1, twoR, twoR, 0, 90);

            // 4. Ведемо лінію вліво по ВСЬОМУ НИЖНЬОМУ КРАЮ форми
            framePath.AddLine(wForm - wRadius, hForm - 1, wRadius, hForm - 1);

            // 5. Огинаємо ЛІВИЙ НИЖНІЙ кут повністю (на всі 90 градусів)
            framePath.AddArc(0, hForm - twoR - 1, twoR, twoR, 90, 90);

            // 6. Завершуємо шлях на ЛІВОМУ боці, піднявшись лише до середини кута (на висоту одного радіуса)
            framePath.AddLine(0, hForm - wRadius, 0, hForm - wRadius);

            // Малюємо отриману Г-подібну рамку
            g.DrawPath(pen, framePath);
        }

        // Метод динамічного оновлення координат вікна опису (прив'язка до внутрішнього лівого верхнього кута форми)
        private void UpdateHelpWindowPosition()
        {
            if (IsHelpWindowOpen())
            {
                infoTooltipForm.Location = RectangleToScreen(ClientRectangle).Location;
            }
        }
    }
}