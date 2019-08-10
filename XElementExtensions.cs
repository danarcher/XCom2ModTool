using System;
using System.Linq;
using System.Xml.Linq;

namespace XCom2ModTool
{
    internal static class XElementExtensions
    {
        public static XElement Local(this XElement element, string localName)
        {
            return element.Elements().FirstOrDefault(x => string.Equals(x.Name.LocalName, localName, StringComparison.Ordinal));
        }
    }
}
