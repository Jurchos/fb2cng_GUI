using System.Drawing.Drawing2D;

namespace fb2cngGUI
{
    public static class UIButton
    {
        public static void MakeButtonRounded(Button btn, int radius)
        {
            if (btn == null)
            {
                return;
            }

            // Створюємо шлях для REGION (клікабельна зона)
            using (GraphicsPath path = new())
            {
                float r = radius;
                float w = btn.Width;
                float h = btn.Height;

                path.AddArc(0, 0, r * 2, r * 2, 180, 90);
                path.AddArc(w - (r * 2), 0, r * 2, r * 2, 270, 90);
                path.AddArc(w - (r * 2), h - (r * 2), r * 2, r * 2, 0, 90);
                path.AddArc(0, h - (r * 2), r * 2, r * 2, 90, 90);
                path.CloseAllFigures();

                btn.Region?.Dispose();

                btn.Region = new Region(path);
            }

            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Tag = radius;

            // Перепідписуємося на подію малювання
            btn.Paint -= OnButtonPaintDrawBorder;
            btn.Paint += OnButtonPaintDrawBorder;
        }

        private static void OnButtonPaintDrawBorder(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn || btn.Tag == null)
            {
                return;
            }

            int r = (int)btn.Tag;
            Graphics g = e.Graphics;

            // НАЛАШТУВАННЯ ДЛЯ МАКСИМАЛЬНОЇ ЯКОСТІ
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            bool isDark = AppSettings.Current.Theme == "Dark";

            // Створюємо шлях для рамки, який іде строго по межі кнопки
            // Використовуємо -1 піксель від ширини/висоти для ідеального вписування
            using GraphicsPath borderPath = new();
            float d = r * 2.0f;
            float w = btn.Width - 1.0f;
            float h = btn.Height - 1.0f;
            float off = 0.5f; // Невеликий зсув для центрування лінії Pen

            borderPath.AddArc(off, off, d, d, 180, 90);
            borderPath.AddArc(w - d + off, off, d, d, 270, 90);
            borderPath.AddArc(w - d + off, h - d + off, d, d, 0, 90);
            borderPath.AddArc(off, h - d + off, d, d, 90, 90);
            borderPath.CloseAllFigures();

            // МАЛЮВАННЯ ФОНУ (це важливо для згладжування краю)
            // Ми малюємо тоненьку лінію кольором фону форми по самому краю, 
            // щоб "обдурити" око і прибрати драбинку
            Color parentColor = btn.Parent?.BackColor ?? btn.BackColor;
            using (Pen bgSoftener = new(parentColor, 1.0f))
            {
                g.DrawPath(bgSoftener, borderPath);
            }

            // МАЛЮВАННЯ ОСНОВНОЇ РАМКИ
            if (isDark)
            {
                Color borderColor = btn.FlatAppearance.BorderColor != Color.Empty ? btn.FlatAppearance.BorderColor : Color.FromArgb(100, 100, 100);
                using Pen pen = new(borderColor, 1.2f);
                g.DrawPath(pen, borderPath);
            }
            else
            {
                if (btn.ForeColor == Color.White) // Сині кнопки
                {
                    // Використовуємо напівпрозорий білий, щоб він м'яко лягав на синій фон
                    using Pen pen = new(Color.FromArgb(150, Color.White), 1.5f);
                    g.DrawPath(pen, borderPath);
                }
                else // Звичайні сірі кнопки
                {
                    using Pen pen = new(Color.FromArgb(120, btn.ForeColor), 1.0f);
                    g.DrawPath(pen, borderPath);
                }
            }
        }
    }
}