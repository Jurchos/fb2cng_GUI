namespace fb2cngGUI
{
    internal class GlobalHotkeyListener : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 999;

        private Thread? _loopThread;
        // Зберігаємо посилання на вікно, щоб мати до нього доступ із Dispose
        private HotkeyWindow? _window;

        public GlobalHotkeyListener()
        {
            // Запускаємо окремий потік для прослуховування клавіш
            _loopThread = new Thread(RunMessageLoop)
            {
                IsBackground = true,
                Name = "HotkeyListenerThread"
            };
            _loopThread.SetApartmentState(ApartmentState.STA);
            _loopThread.Start();
        }

        private void RunMessageLoop()
        {
            // Створюємо вікно і зберігаємо посилання
            _window = new HotkeyWindow();

            // Реєструємо Ctrl(0x0002) + Alt(0x0001) + Esc(0x1B)
            if (Win32Api.RegisterHotKey(_window.Handle, HOTKEY_ID, Win32Api.MOD_CONTROL | Win32Api.MOD_ALT, Win32Api.VK_ESCAPE) == 0)
            {
                // Якщо не вдалося зареєструвати (наприклад, комбінація зайнята іншою програмою)
                return;
            }

            try
            {
                // Запускаємо цикл повідомлень. Потік "зависне" тут до зупинки.
                Application.Run();
            }
            finally
            {
                // Коли цикл зупиниться (через Close або ExitThread), прибираємо реєстрацію
                if (_window != null && _window.IsHandleCreated)
                {
                    _ = Win32Api.UnregisterHotKey(_window.Handle, HOTKEY_ID);
                }
            }
        }

        // Внутрішній клас вікна для перехоплення повідомлень
        private class HotkeyWindow : Form
        {
            public HotkeyWindow()
            {
                // Робимо вікно повністю невидимим
                WindowState = FormWindowState.Minimized;
                ShowInTaskbar = false;
                FormBorderStyle = FormBorderStyle.None;
                Load += (s, e) => Size = new Size(0, 0);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
                {
                    // Сигнал про зупинку
                    MarkerService.Signal();
                }
                base.WndProc(ref m);
            }
        }

        public void Dispose()
        {
            // ПРАВИЛЬНЕ ЗАКРИТТЯ:
            if (_window != null && _window.IsHandleCreated)
            {
                try
                {
                    // Просимо вікно закритися у своєму потоці
                    _ = _window.BeginInvoke(new Action(() =>
                    {
                        _window.Close();          // Закриваємо вікно
                        Application.ExitThread(); // Зупиняємо цикл повідомлень потоку
                    }));
                }
                catch { }
            }

            _loopThread = null;
            GC.SuppressFinalize(this);
        }
    }
}