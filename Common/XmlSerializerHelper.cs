using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace GostDOC.Common
{
    public static class XmlSerializeHelper
    {
        private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public static bool SaveXmlStructFile<T>(T aSaveClass, string aFileName)
        {
            if (string.IsNullOrWhiteSpace(aFileName))
            {
                _logger.Error("File name can't be empty!");
                return false;
            }

            bool res = true;

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (System.Xml.XmlWriter xml_writer = System.Xml.XmlWriter.Create(aFileName,
                    new System.Xml.XmlWriterSettings { Encoding = System.Text.Encoding.UTF8, Indent = true }))
                {
                    serializer.Serialize(xml_writer, aSaveClass);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                res = false;
            }

            return res;
        }

        public static bool LoadXmlStruct<T>(ref T aLoadClass, string aXmlContent)
        {
            if (string.IsNullOrWhiteSpace(aXmlContent))
            {
                _logger.Error("Xml content can't be empty!");
                return false;
            }

            bool res = false;
            try
            {
                using (var reader = new StringReader(aXmlContent))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    T tmp = (T)serializer.Deserialize(reader);
                    if (tmp != null)
                    {
                        aLoadClass = tmp;
                        res = true;
                    }
                    else
                    {
                        _logger.Error("Deserialization error!");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }
            return res;
        }

        public static bool LoadXmlStructFile<T>(ref T aLoadClass, string aFileName)
        {
            if (string.IsNullOrWhiteSpace(aFileName))
            {
                _logger.Error("File name can't be empty!");
                return false;
            }

            bool res = false;
            if (!File.Exists(aFileName))
            {
                _logger.Error("File by name {0} not exist", aFileName);
                return false;
            }

            try
            {
                using (XmlReader reader = XmlReader.Create(aFileName))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    T tmp = (T)serializer.Deserialize(reader);
                    if (tmp != null)
                    {
                        aLoadClass = tmp;
                        res = true;
                    }
                    else
                    {
                        _logger.Error("Serialized file {0} is null", aFileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }
            return res;
        }
    }
}
