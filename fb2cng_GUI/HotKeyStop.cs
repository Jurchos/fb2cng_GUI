using System.Runtime.InteropServices;

namespace fb2cngGUI
{
    internal class GlobalHotkeyListener : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 1024;
        private readonly Thread? _loopThread;
        private HotkeyNativeWindow? _window;
        private volatile bool _disposed;

        public GlobalHotkeyListener()
        {
            // Запускаємо окремий потік для прослуховування клавіш
            _loopThread = new Thread(RunMessageLoop)
            {
                IsBackground = true,
                Name = "HotkeyListenerThread",
                Priority = ThreadPriority.AboveNormal // Пріоритет вище, щоб швидше реагувати
            };
            _loopThread.SetApartmentState(ApartmentState.STA);
            _loopThread.Start();
        }

        private void RunMessageLoop()
        {
            // Створюємо вікно і зберігаємо посилання
            _window = new HotkeyNativeWindow();
            _window.CreateHandle(new CreateParams());

            // Клавіша: Тільда (VK_OEM_3 має код 0xC0)
            uint keyTilde = 0xC0;

            // ЦИКЛ АГРЕСИВНОЇ РЕЄСТРАЦІЇ
            while (!_disposed)
            {
                // Якщо сигнал вже активовано іншим процесом — нам вже не треба слухати
                if (MarkerService.IsActive())
                {
                    break;
                }

                // Спробувати зареєструвати
                if (Win32Api.RegisterHotKey(_window.Handle, HOTKEY_ID, Win32Api.MOD_ALT, keyTilde) != 0)
                {
                    // Успішно! Запускаємо цикл повідомлень Windows
                    Application.Run();
                    // Коли Application.ExitThread() викликано, ми вийдемо сюди
                    break;
                }
                else
                {
                    // Якщо помилка 1409 (зайнято іншим процесом) — чекаємо 200мс і пробуємо знову
                    int err = Marshal.GetLastWin32Error();
                    if (err == 1409)
                    {
                        Thread.Sleep(200);
                        continue;
                    }
                    break; // Інша фатальна помилка
                }
            }
        }

        // Внутрішній клас вікна для перехоплення повідомлень
        // Використовуємо NativeWindow замість Form — це швидше та надійніше для фонових задач
        private class HotkeyNativeWindow : NativeWindow
        {
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
                {
                    MarkerService.Signal();
                    Application.ExitThread(); // Зупиняємо цей цикл, сигнал вже в системі
                }
                base.WndProc(ref m);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            if (_window != null)
            {
                try
                {
                    _ = Win32Api.UnregisterHotKey(_window.Handle, HOTKEY_ID);
                    _window.DestroyHandle();
                }
                catch { }
            }
            Application.ExitThread();
        }
    }
}