
namespace fb2cngGUI
{
    // Головна форма налаштувань програми
    public partial class Form1 : Form
    {
        // Посилання на об'єкт конфігурації (налаштування)
        private readonly AppSettings _settings;

        // Елементи інтерфейсу: текстові підписи
        private Label lblLang = null!;
        private Label lblFormat = null!;
        private Label lblMenu = null!;

        // Елементи інтерфейсу: випадаючі списки
        private ComboBox cbLang = null!; 
        private ComboBox cbFormat = null!;

        // Елементи інтерфейсу: прапорці (чекбокси) - папка призначення, конфігурація, перезапис,
        // видалити з підтвердженням, видалити в корзину, мінімізувати прогрес бар, приховати прогрес бар
        private CheckBox chkFolder = null!;
        private CheckBox chkConfig = null!;
        private CheckBox chkOverwrite = null!;
        private CheckBox chkDeleteMain = null!;
        private CheckBox chkDeleteSub = null!;
        private Label lblDeleteSubText = null!;
        private CheckBox chkMinimize = null!;
        private CheckBox chkHideProgress = null!;

        // Елементи інтерфейсу: текстові поля
        private TextBox txtFolder = null!;
        private TextBox txtConfig = null!;
        private TextBox txtMenu = null!;

        // Елементи інтерфейсу: кнопки дій та вибору файлів/папок (довідка, папка призначення,
        // конфігураційний файл, інтеграція, ОК, Відміна, перемикання теми)
        private Button btnHelp = null!;
        private Button btnFolderBrowse = null!;
        private Button btnConfigBrowse = null!;
        private Button btnIntegrate = null!;
        private Button btnThemeToggle = null!;
        private Button btnOk = null!;
        private Button btnCancel = null!;
        private Button btnGui = null!;

        // Форма для відображення спливаючого вікна з описом програми
        private Form infoTooltipForm = null!;
        private int paddingBottom;
        private int finalHeight;

        // Конструктор форми: завантажує дані та налаштовує зовнішній вигляд
        public Form1()
        {
            // Завантажуємо збережені налаштування з файлу
            _settings = AppSettings.Load();

            // Створюємо та розміщуємо всі компоненти на формі вручну
            InitializeComponentsManual();

            // Заповнюємо елементи UI значеннями з налаштувань
            ApplySettingsToUI();

            // Застосовуємо поточну мову локалізації
            ApplyLocalization();

            // Встановлюємо тему оформлення (світлу або темну)
            ApplyTheme();
        }

        // Надійний метод динамічного оновлення координат вікна опису (прив'язка до внутрішнього лівого верхнього кута форми)
        private void UpdateHelpWindowPosition()
        {
            if (infoTooltipForm != null && !infoTooltipForm.IsDisposed && infoTooltipForm.Visible)
            {
                // RectangleToScreen(ClientRectangle) повертає точні координати внутрішнього лівого кута форми на екрані,
                // повністю ігноруючи розміри та похибки системних віконних рамок Windows (Aero/DPI)
                Point programContentTopLeft = RectangleToScreen(ClientRectangle).Location;

                // Ставимо вікно точно в лівий верхній кут вашої програми
                infoTooltipForm.Location = programContentTopLeft;
            }
        }
    }
}
