using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Interfaces;

namespace GostDOC.ViewModels
{
    class GraphValueVM : IMemento<object>
    {
        private class GraphValueMemento
        {
            public string Name { get; set; }
            public string Text { get; set; }
        }
        public object Memento
        {
            get
            {
                return new GraphValueMemento()
                {
                    Name = Name.Value,
                    Text = Text.Value
                };
            }

            set
            {
                GraphValueMemento memento = value as GraphValueMemento;
                Name.Value = memento.Name;
                Text.Value = memento.Text;
            }
        }

        public ObservableProperty<string> Name { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Text { get; } = new ObservableProperty<string>();

        public GraphValueVM()
        {
        }

        public GraphValueVM(string aName, string aText)
        {
            Name.Value = aName;
            Text.Value = aText;
        }
    }
}
