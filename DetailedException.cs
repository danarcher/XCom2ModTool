using System;

namespace XCom2ModTool
{
    internal class DetailedException : Exception
    {
        public DetailedException(string message, params string[] details) : base(message)
        {
            Details = details;
        }

        public DetailedException(string message, Exception innerException, params string[] details) : base(message, innerException)
        {
            Details = details;
        }

        public string[] Details { get; }
    }
}
