using System.Runtime.InteropServices;

namespace fb2cngGUI
{
    internal static partial class Win32Api
    {

        // Реєстрація гарячих клавіш через LibraryImport
        [LibraryImport("user32.dll")]
        internal static partial int RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

        [LibraryImport("user32.dll")]
        internal static partial int UnregisterHotKey(nint hWnd, int id);

        // Модифікатори клавіш
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint VK_ESCAPE = 0x1B;


        [LibraryImport("user32.dll")]
        public static partial nint GetForegroundWindow();

        [LibraryImport("user32.dll")]
        public static partial uint GetWindowThreadProcessId(nint hWnd, nint lpdwProcessId);

        [LibraryImport("kernel32.dll")]
        public static partial uint GetCurrentThreadId();

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

        [LibraryImport("user32.dll")]
        public static partial nint SetActiveWindow(nint hWnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetForegroundWindow(nint hWnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ShowWindow(nint hWnd, int nCmdShow);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool IsIconic(nint hWnd);

        [LibraryImport("user32.dll", EntryPoint = "SetProcessDpiAwarenessContext")]
        public static partial int SetProcessDpiAwarenessContext(int dpiFlag);

        [LibraryImport("shell32.dll", EntryPoint = "SHFileOperationW")]
        public static partial int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        // Робота з Кошиком Windows
        // Структура повністю сумісна із генератором коду (Blittable)
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        public struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;             // Дескриптор вікна-власника діалогу операції
            public uint wFunc;              // Тип операції (наприклад, FO_DELETE — видалення)
            public IntPtr pFrom;            // Шлях до вихідного файлу (має завершуватися двома символами \0)
            public IntPtr pTo;              // Шлях до цільового файлу (при копіюванні чи переміщенні)
            public ushort fFlags;           // Прапорці керування операцією (скасування підтвердження, підтримка Undo тощо)

            // 1. Змінюємо внутрішнє поле на int (4 байти), як це вимагає Windows API BOOL.
            // Це прибирає помилку SYSLIB1051 раз і назавжди!
            private int _fAnyOperationsAborted;
            // Має повертати true, якщо користувач перервав операцію до її завершення
            // 2. Додаємо властивість для сумісності. Зовні структура працюватиме з bool, як і раніше
            public bool FAnyOperationsAborted
            {
                readonly get => _fAnyOperationsAborted != 0;
                set => _fAnyOperationsAborted = value ? 1 : 0;
            }

            public IntPtr hNameMappings;    // Об'єкт зіставлення імен файлів (використовується рідко)
            public IntPtr lpszProgressTitle;// Текст заголовка вікна прогресу видалення
        }
    }
}
