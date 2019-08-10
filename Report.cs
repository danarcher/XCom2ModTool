using System;

namespace XCom2ModTool
{
    internal static class Report
    {
        public static bool IsVerbose = false;

        public static void Error(string message)
        {
            Console.Error.WriteLine($"error: {message}");
        }

        public static void Warning(string message)
        {
            Console.Error.WriteLine($"warning: {message}");
        }

        public static void Verbose(string message)
        {
            if (IsVerbose)
            {
                Console.Error.WriteLine(message);
            }
        }

        public static void Exception(Exception ex)
        {
            Error(ex.Message);
            if (IsVerbose)
            {
                Console.Error.WriteLine(ex.ToString());
            }
        }
    }
}
