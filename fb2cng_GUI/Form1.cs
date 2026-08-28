
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

        // Елементи інтерфейсу: прапорці (чекбокси), мітки
        private CheckBox chkFolder = null!;
        private CheckBox chkConfig = null!;
        private CheckBox chkOverwrite = null!;
        private CheckBox chkSkipExisting = null!;
        private Label lblOverwriteText = null!;
        private Label lblSkipExistingText = null!;
        private CheckBox chkSkipErrors = null!;
        private CheckBox chkDeleteMain = null!;
        private CheckBox chkDeleteSub = null!;
        private Label lblDeleteSubText = null!;
        private CheckBox chkMinimize = null!;
        private CheckBox chkHideProgress = null!;

        // Елементи інтерфейсу: текстові поля
        private TextBox txtFolder = null!;
        private TextBox txtConfig = null!;
        private TextBox txtMenu = null!;

        // Елементи інтерфейсу: кнопки дій та вибору
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
        private DateTime _lastHelpCloseTime = DateTime.MinValue;
        private int paddingBottom;

        // Конструктор форми: завантажує дані та налаштовує зовнішній вигляд
        public Form1()
        {
            AutoScaleMode = AutoScaleMode.None;
            // Завантажуємо збережені налаштування з файлу
            _settings = AppSettings.Current;

            // Створюємо та розміщуємо всі компоненти на формі вручну
            InitializeComponentsManual();

            // Заповнюємо елементи UI значеннями з налаштувань
            ApplySettingsToUI();

            // Застосовуємо поточну мову локалізації
            ApplyLocalization();

            // Встановлюємо тему оформлення (світлу або темну)
            ApplyTheme();

            SyncRegistryPathIfNeeded();
        }
    }
}
