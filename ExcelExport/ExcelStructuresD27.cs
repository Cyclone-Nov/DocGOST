using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GostDOC.ExcelExport
{
    class ComponentD27
    {
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public int Column { get; set; }
        public uint Count { get; set; } = 1;
    }

    class HeaderSize
    {
        public int Width { get; set; } = 1;
        public int Height { get; set; } = 1;

        public static HeaderSize operator+(HeaderSize a, HeaderSize b)
        {
            return new HeaderSize()
            {
                Width = a.Width + b.Width,
                Height = a.Height + b.Height
            };
        }
    }

    class ComplexD27
    {
        public string Name { get; set; }
        public int Column { get; set; }
        public int Row { get; set; }
        public HeaderSize Size { get; set; } = new HeaderSize();
        public List<ComplexD27> SubComplex { get; set; }
    }

    class ComponentGroupD27
    {
        public string Name { get; set; }
        public List<ComponentD27> Components { get; set; } = new List<ComponentD27>();
        public ComponentGroupD27(string aName)
        {
            Name = aName;
        }
    }
}
