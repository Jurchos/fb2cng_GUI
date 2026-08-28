
namespace fb2cngGUI
{
    public partial class Form1 : Form
    {
        // Метод динамічного застосування світлої або темної теми оформлення до всіх елементів форми
        private void ApplyTheme()
        {
            bool isDark = _settings.Theme == "Dark";
            // Встановлюємо фоновий колір головної форми
            BackColor = isDark ? Color.FromArgb(24, 24, 24) : Color.FromArgb(245, 245, 245);
            Color textCol = isDark ? Color.White : Color.Black;
            // Колір для заблокованого тексту в темній темі (світло-сірий)
            Color disabledTextCol = isDark ? Color.FromArgb(120, 120, 120) : SystemColors.GrayText;
            Color inputBg = isDark ? Color.FromArgb(40, 40, 40) : Color.White;
            Color btnBg = isDark ? Color.FromArgb(50, 50, 50) : Color.FromArgb(230, 230, 230);
            Color accentBg = isDark ? Color.FromArgb(0, 102, 204) : Color.FromArgb(0, 120, 215);

            // Циклом обходимо всі елементи керування на формі
            foreach (Control c in Controls)
            {
                if (c is Label or CheckBox)
                {
                    c.ForeColor = textCol;
                }

                if (c is TextBox or ComboBox)
                {
                    c.BackColor = inputBg;
                    c.ForeColor = textCol;
                }
                if (c is Button b)
                {
                    // Головні кнопки робимо акцентними синіми з білим текстом
                    if (b == btnOk || b == btnIntegrate)
                    {
                        b.BackColor = accentBg;
                        b.ForeColor = Color.White;
                        b.FlatAppearance.BorderSize = 0;
                    }
                    // Кнопка "і/?" має зберігати стиль звичайної кнопки або виділятися
                    else if (b == btnHelp)
                    {
                        b.BackColor = isDark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(220, 220, 220);
                        b.ForeColor = textCol;
                        b.FlatAppearance.BorderColor = isDark ? Color.FromArgb(90, 90, 90) : Color.FromArgb(180, 180, 180);
                    }
                    else
                    {
                        b.BackColor = btnBg;
                        b.ForeColor = textCol;
                        b.FlatAppearance.BorderColor = isDark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(200, 200, 200);
                    }
                    // --- Примусово перемальовуємо кнопку для оновлення заокругленої рамки ---
                    b.Invalidate();
                }
            }
            if (lblOverwriteText is var labelOW and not null)
            {
                labelOW.ForeColor = chkOverwrite.Enabled ? textCol : disabledTextCol;
            }
            // lblSkipExistingText для пропуску
            if (lblSkipExistingText is var labelExp and not null)
            {
                labelExp.ForeColor = chkSkipExisting.Enabled ? textCol : disabledTextCol;
            }
            // Для видалення файлів (DeleteSub)
            if (lblDeleteSubText is var labelDel and not null)
            {
                labelDel.ForeColor = chkDeleteSub.Enabled ? textCol : disabledTextCol;
            }
        }
    }
}

