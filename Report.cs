using System;

namespace XCom2ModTool
{
    internal static class Report
    {
        public static bool IsVerbose = false;

        public static void Error(string message)
        {
            using (new ColorChange(Settings.Default.ErrorColor))
            {
                Console.Error.WriteLine($"error: {message}");
            }
        }

        public static void Warning(string message)
        {
            using (new ColorChange(Settings.Default.WarningColor))
            {
                Console.Error.WriteLine($"warning: {message}");
            }
        }

        public static void Verbose(string message)
        {
            using (new ColorChange(Settings.Default.VerboseColor))
            {
                Console.Error.WriteLine(message);
            }
        }

        public static void Exception(Exception ex, string message = null)
        {
            Error(message ?? ex.Message);
            if (IsVerbose)
            {
                using (new ColorChange(Settings.Default.ErrorColor))
                {
                    Console.Error.WriteLine(ex.ToString());
                }
            }
        }

        public class ColorChange : IDisposable
        {
            private readonly ConsoleColor previousColor;

            public ColorChange(ConsoleColor color)
            {
                if (Settings.Default.UseColoredOutput)
                {
                    previousColor = Console.ForegroundColor;
                    Console.ForegroundColor = color;
                }
            }

            public void Dispose()
            {
                Console.ForegroundColor = previousColor;
            }
        }
    }
}
