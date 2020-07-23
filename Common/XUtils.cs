using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GostDOC.Common
{
    static class XUtils
    {
        public static XElement FindElement(this XDocument doc, string elementName)
        {
            foreach (XNode node in doc.DescendantNodes())
            {
                if (node is XElement)
                {
                    XElement element = (XElement)node;
                    if (element.Name.LocalName.Equals(elementName))
                        return element;
                }
            }
            return null;
        }

        public static IList<XElement> FindElements(this XDocument doc, string elementName)
        {
            List<XElement> elements = new List<XElement>();
            foreach (XNode node in doc.DescendantNodes())
            {
                if (node is XElement)
                {
                    XElement element = (XElement)node;
                    if (element.Name.LocalName.Equals(elementName))
                        elements.Add(element);
                }
            }
            return elements;
        }
        public static string ReadElementValue(this XDocument doc, string elementName)
        {
            foreach (XNode node in doc.DescendantNodes())
            {
                if (node is XElement)
                {
                    XElement element = (XElement)node;
                    if (element.Attribute("name")?.Value == elementName)
                        return element.Attribute("value")?.Value;
                }
            }
            return null;
        }
        public static IList<string> ReadElementValues(this XDocument doc, string elementName)
        {
            List<string> elements = new List<string>();
            foreach (XNode node in doc.DescendantNodes())
            {
                if (node is XElement)
                {
                    XElement element = (XElement)node;
                    if (element.Attribute("name")?.Value == elementName)
                        elements.Add(element.Attribute("value")?.Value);
                }
            }
            return elements;
        }
    }
}
