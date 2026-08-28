using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace fb2cngGUI
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        // Очищення всіх ресурсів, що використовуються.
        /// <param name="disposing">true, якщо керовані ресурси слід звільнити; в іншому випадку — false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        // --- МАЛЮВАННЯ СПИСКІВ  ---
        private void ComboBox_CustomDraw(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || sender is not ComboBox cb)
            {
                return;
            }

            bool isDark = _settings.Theme == "Dark";
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            // Малюємо фон
            Color backColor = isSelected
                ? (isDark ? Color.FromArgb(70, 70, 70) : Color.FromArgb(240, 240, 240))
                : cb.BackColor;

            using (Brush b = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(b, e.Bounds);
            }

            // Малюємо текст
            string text = cb.Items[e.Index]?.ToString() ?? string.Empty;
            TextRenderer.DrawText(e.Graphics, text, cb.Font ?? Font, e.Bounds, cb.ForeColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);

            if (isSelected) e.DrawFocusRectangle();
        }

        // --- МАЛЮВАННЯ ІКОНОК (10% ВІДСТУП, БЕЗ ОБРІЗКИ) ---
        private static void SetupIconRenderer(Button btn, Image icon)
        {
            btn.FlatAppearance.BorderSize = 0;
            bool hovered = false;

            if (btn.Tag != null) return;
            btn.Tag = "Rendered";

            btn.MouseEnter += (s, e) => { hovered = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { hovered = false; btn.Invalidate(); };

            btn.Paint += (s, e) =>
            {
                Color bgColor = btn.BackColor;
                if (hovered && btn.Enabled)
                {
                    bool dark = bgColor.R < 128;
                    bgColor = dark ? Color.FromArgb(bgColor.R + 25, bgColor.G + 25, bgColor.B + 25) : Color.FromArgb(bgColor.R - 20, bgColor.G - 20, bgColor.B - 20);
                }
                using (Brush b = new SolidBrush(bgColor))
                {
                    e.Graphics.FillRectangle(b, 0, 0, btn.Width, btn.Height);
                }

                if (icon != null)
                {
                    Graphics g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    using ImageAttributes ia = new();
                    if (!btn.Enabled)
                    {
                        float[][] m = [[1, 0, 0, 0, 0], [0, 1, 0, 0, 0], [0, 0, 1, 0, 0], [0, 0, 0, 0.4f, 0], [0, 0, 0, 0, 1]];
                        ia.SetColorMatrix(new ColorMatrix(m));
                    }

                    // 10% відступ (0.1f)
                    float pX = btn.Width * 0.1f;
                    float pY = btn.Height * 0.1f;
                    RectangleF dest = new(pX, pY, btn.Width - (pX * 2), btn.Height - (pY * 2));

                    if (!btn.Enabled)
                    {
                        g.DrawImage(icon, Rectangle.Round(dest), 0, 0, icon.Width, icon.Height, GraphicsUnit.Pixel, ia);
                    }
                    else
                    {
                        g.DrawImage(icon, dest);
                    }
                }
            };
        }
    }
}
