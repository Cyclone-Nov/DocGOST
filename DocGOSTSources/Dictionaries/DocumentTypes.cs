using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Models
{
    class Document
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }

    class DocumentTypes
    {
        public IDictionary<string, IDictionary<string, Document>> Documents { get; private set; } = new Dictionary<string, IDictionary<string, Document>>();

        public DocumentTypes()
        {
        }

        public void Load()
        {
            string current = string.Empty;
            foreach (var line in Utils.ReadCfgFileLines("Documents"))
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                string[] split = line.Split(new char[] { '/' });

                if (split.Length == 1)
                {
                    current = line.TrimEnd(new char[] { ':' });
                    if (!Documents.ContainsKey(current))
                        Documents.Add(current, new Dictionary<string, Document>());
                    else
                    {

                    }

                    continue;
                }

                if (string.IsNullOrEmpty(current))
                    continue;

                if (split.Length == 2)
                {
                    string name = split[1];
                    string code = split[0];

                    if (Documents.ContainsKey(current))
                    {
                       if (!Documents[current].ContainsKey(name))                       
                            Documents[current].Add(name, new Document() { Code = code, Name = name });
                       //else
                       // err.log(repeat)
                    } else
                    {

                    }
                }
            }
        }

        public Document GetDocument(string aType, string aName)
        {
            if (string.IsNullOrEmpty(aType) || string.IsNullOrEmpty(aName))
            {
                return null;
            }

            IDictionary<string, Document> docs;
            if (Documents.TryGetValue(aType, out docs))
            {
                Document doc;
                if (docs.TryGetValue(aName, out doc))
                {
                    return doc;
                }
            }
            return null;
        }
    }
}
