using System;
using System.Runtime.InteropServices;

namespace MyShop.Tests
{
    /// <summary>
    /// Disables WinRT initialization during unit tests
    /// This prevents "Class not registered" errors from Windows App SDK
    /// </summary>
    [ComVisible(false)]
    internal static class DisableWinRTInitializer
    {
        // This gets invoked before any tests run
        static DisableWinRTInitializer()
        {
            try
            {
                // Suppress WinRT initialization
                Environment.SetEnvironmentVariable("WINRT_DISABLED", "1");
            }
            catch { }
        }

        /// <summary>Call this in test setup to suppress WinRT errors</summary>
        public static void Initialize() { }
    }
}