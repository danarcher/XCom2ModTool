using System;
using System.IO;
using System.Security;
using System.Xml.Linq;

namespace XCom2ModTool
{
    internal static class Report
    {
        public static Verbosity Verbosity { get; set; } = Verbosity.Concise;

        public static void Error(string message)
        {
            WriteXmlLine(Console.Error, $"<error>error: {message}</error>");
        }

        public static void Warning(string message)
        {
            WriteXmlLine(Console.Error, $"<warning>warning: {message}</warning>");
        }

        public static void WriteLine() => Console.WriteLine();

        public static void WriteLine(string message)
        {
            WriteXmlLine(Console.Out, $"<info>{message}</info>");
        }

        public static void Write(string message)
        {
            WriteXml(Console.Out, $"<info>{message}</info>");
        }

        public static void Verbose(string message, Verbosity verbosity = Verbosity.Verbose)
        {
            if (Verbosity >= verbosity)
            {
                WriteXmlLine(Console.Out, $"<verbose>{message}</verbose>");
            }
        }

        public static void Exception(Exception ex, string message = null)
        {
            Error(message ?? ex.Message);
            var detailed = ex as DetailedException;
            if (detailed != null)
            {
                foreach (var detail in detailed.Details)
                {
                    WriteXmlLine(Console.Error, $"       <error>{detail}</error>"); // No "error: " prefix, but indented to it
                }
            }
            if (Verbosity >= Verbosity.Verbose)
            {
                Error(SecurityElement.Escape(ex.ToString()));
            }
        }

        public static void WriteXml(TextWriter writer, string message)
        {
            message = message.Replace("\\<", "&lt;");

            var doc = XDocument.Parse($"<?xml version=\"1.0\"?><root>{message}</root>", LoadOptions.PreserveWhitespace);

            void Expand(XElement element)
            {
                var previousColor = Console.ForegroundColor;

                if (Settings.Default.UseColoredOutput)
                {
                    Console.ForegroundColor = element.Name.LocalName switch
                    {
                        "error" => Settings.Default.ErrorColor,
                        "warning" => Settings.Default.WarningColor,
                        "verbose" => Settings.Default.VerboseColor,
                        "info" => Settings.Default.InfoColor,
                        _ => Enum.TryParse(element.Name.LocalName, ignoreCase: true, out ConsoleColor color) ? color : previousColor
                    };
                }

                foreach (var node in element.Nodes())
                {
                    switch (node)
                    {
                        case XElement child:
                            Expand(child);
                            break;
                        case XText text:
                            writer.Write(text.Value);
                            break;
                    }
                }

                Console.ForegroundColor = previousColor;
            }

            Expand(doc.Root);
        }

        public static void WriteXmlLine(TextWriter writer, string message)
        {
            WriteXml(writer, message);
            writer.WriteLine();
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

    internal enum Verbosity
    {
        Concise = 0,
        Verbose = 1,
        Loquacious = 2,
        Periphrastic = 3,
    }
}
