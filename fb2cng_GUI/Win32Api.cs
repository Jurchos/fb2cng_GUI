using System.Runtime.InteropServices;

namespace fb2cngGUI
{
    internal static partial class Win32Api
    {
        // Робота з Кошиком Windows
        // Структура повністю сумісна із генератором коду (Blittable)
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
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

        // Тепер LibraryImport генерує код бездоганно і без помилок
        [LibraryImport("shell32.dll", EntryPoint = "SHFileOperationW")]
        public static partial int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        [LibraryImport("user32.dll")]
        public static partial IntPtr GetForegroundWindow();

        [LibraryImport("user32.dll")]
        public static partial uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [LibraryImport("kernel32.dll")]
        public static partial uint GetCurrentThreadId();

        // Для параметрів методів [MarshalAs] працює чудово, тому тут залишаємо bool безпечно!
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetForegroundWindow(IntPtr hWnd);

        [LibraryImport("user32.dll")]
        public static partial IntPtr SetActiveWindow(IntPtr hWnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool IsIconic(IntPtr hWnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool HideCaret(IntPtr hWnd);

        [LibraryImport("user32.dll", EntryPoint = "SetProcessDpiAwarenessContext", SetLastError = true)]
        public static partial int SetProcessDpiAwarenessContext(int dpiFlag);
    }
}
