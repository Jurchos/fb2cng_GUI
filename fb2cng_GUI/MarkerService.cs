
namespace fb2cngGUI
{
    public static class MarkerService
    {
        private const string SignalName = @"Local\fb2cng_Stop_Signal";
        private const string ExcludeMutexName = @"Local\fb2cng_FbcMissing_Mutex";

        // Використовуємо поле для миттєвої реакції всередині одного процесу
        private static volatile bool _internalStopSignal;
        // Зберігаємо handle відкритого сигналу, щоб ОС не видалила його завчасно
        private static EventWaitHandle? _persistentHandle;
        private static readonly Lock _lock = new();


        // 1. ПЕРЕВІРКА СИГНАЛУ ЗУПИНКИ
        public static bool IsActive()
        {
            if (_internalStopSignal)
            {
                return true;
            }

            try
            {
                // Намагаємося відкрити існуючий сигнал
                if (EventWaitHandle.TryOpenExisting(SignalName, out EventWaitHandle? stopEvent))
                {
                    using (stopEvent)
                    {
                        return stopEvent.WaitOne(0);
                    }
                }
            }
            catch { }
            return false;
        }

        // 2. СТВОРЕННЯ СИГНАЛУ ЗУПИНКИ
        public static void Signal()
        {
            _internalStopSignal = true; // Миттєвий локальний сигнал
            lock (_lock)
            {
                try
                {
                    // Створюємо або відкриваємо сигнал і залишаємо його в статичній змінній
                    _persistentHandle ??= new EventWaitHandle(false, EventResetMode.ManualReset, SignalName);
                    _ = _persistentHandle.Set();
                }
                catch { }
            }
        }

        // 3. ОЧИЩЕННЯ СИГНАЛУ
        public static void Clear()
        {
            _internalStopSignal = false;
            lock (_lock)
            {
                try
                {
                    _ = (_persistentHandle?.Reset());
                    if (EventWaitHandle.TryOpenExisting(SignalName, out EventWaitHandle? stopEvent))
                    {
                        using (stopEvent)
                        {
                            _ = stopEvent.Reset();
                        }
                    }
                }
                catch { }
            }
        }

        public static bool TryAcquireExclusivity(out Mutex mutex)
        {
            mutex = new Mutex(true, ExcludeMutexName, out bool createdNew);
            return createdNew;
        }
    }
}