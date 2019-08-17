using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XCom2ModTool
{
    internal static class XElementExtensions
    {
        public static XElement GetElementByLocalName(this XElement element, string localName)
        {
            return element.Elements().FirstOrDefault(x => string.Equals(x.Name.LocalName, localName, StringComparison.Ordinal));
        }

        public static IEnumerable<XElement> GetElementsByLocalName(this XElement element, string localName)
        {
            return element.Elements().Where(x => string.Equals(x.Name.LocalName, localName, StringComparison.Ordinal));
        }

        public static XAttribute GetAttributeByLocalName(this XElement element, string localName)
        {
            return element.Attributes().FirstOrDefault(x => string.Equals(x.Name.LocalName, localName, StringComparison.Ordinal));
        }
    }
}
