using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace GostDOC.Models
{
    static class XUtils
    {
        public static XElement FindElement(this XElement root, string elementName)
        {
            try
            {
                return root.XPathSelectElement($".//{elementName}");
            }
            catch (Exception)
            {
            }
            return null;
        }

        public static IEnumerable<XElement> FindElements(this XElement root, string elementName)
        {
            try
            {
                return root.XPathSelectElements($".//{elementName}");
            }
            catch (Exception)
            {
            }
            return null;
        }

        public static string ReadElementValue(this XElement root, string attributeName, string valueName = "value")
        {
            try
            {
                var element = root.XPathSelectElement($".//attribute[@name='{attributeName}']");
                if (element != null)
                {
                    return element.Attribute(valueName)?.Value;
                }
            }
            catch (Exception)
            {
            }
            return null;
        }
    }
}
