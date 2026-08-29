
namespace fb2cngGUI
{
    public static class MarkerService
    {
        private const string SignalName = @"Local\fb2cng_Stop_Signal";
        private const string ExcludeMutexName = @"Local\fb2cng_FbcMissing_Mutex";

        // Зберігаємо handle відкритого сигналу, щоб ОС не видалила його завчасно
        private static EventWaitHandle? _persistentHandle;
        private static readonly Lock _lock = new();
        // Використовуємо поле для миттєвої реакції всередині одного процесу
        private static volatile bool _internalStopSignal;

        public static void EnsureInitialized()
        {
            if (_persistentHandle == null)
            {
                lock (_lock)
                {
                    try
                    {
                        // Створюємо іменований івент, який бачать всі процеси
                        _persistentHandle ??= new EventWaitHandle(false, EventResetMode.ManualReset, SignalName);
                    }
                    catch { }
                }
            }
        }

        public static bool IsActive()
        {
            if (_internalStopSignal)
            {
                return true;
            }

            EnsureInitialized();
            try { return _persistentHandle?.WaitOne(0) ?? false; }
            catch { return false; }
        }

        public static void Signal()
        {
            _internalStopSignal = true;
            EnsureInitialized();
            try { _ = (_persistentHandle?.Set()); }
            catch { }
        }

        public static void Clear()
        {
            _internalStopSignal = false;
            EnsureInitialized();
            try { _ = (_persistentHandle?.Reset()); }
            catch { }
        }

        public static bool TryAcquireExclusivity(out Mutex mutex)
        {
            mutex = new Mutex(true, ExcludeMutexName, out bool createdNew);
            return createdNew;
        }
    }
}